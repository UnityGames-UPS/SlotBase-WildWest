using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace SlotGame
{
    /// <summary>
    /// UI Manager - controls all UI elements
    /// MODIFICATIONS:
    /// - Auto play button shows selected rounds "Start Autoplay(x)"
    /// - Turbo/Quick spin toggles work universally (not just autoplay)
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("Bet Controls")]
        [SerializeField] private TMP_Text betAmountText;
        [SerializeField] private Button betPlusButton;
        [SerializeField] private Button betMinusButton;

        [Header("Balance & Win")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text winAmountText;

        [Header("Spin Button")]
        [SerializeField] private Button spinButton;
        [SerializeField] private GameObject spinNormalImage;
        [SerializeField] private GameObject spinStopImage;

        [Header("Auto Play Panel")]
        [SerializeField] private GameObject autoPlayPanel;
        [SerializeField] private Button autoPlayOpenButton;
        [SerializeField] private Button autoPlayCloseButton;
        [SerializeField] private Button autoPlayStartButton;
        [SerializeField] private TMP_Text autoPlayStartButtonText; // NEW: Text component for button
        
        [Header("Auto Play Settings")]
        [SerializeField] private Toggle turboToggle;
        [SerializeField] private Toggle quickSpinToggle;
        [SerializeField] private RoundButton[] roundButtons;
        
        [Header("Auto Play Display")]
        [SerializeField] private GameObject autoPlayCountDisplay;
        [SerializeField] private TMP_Text autoPlayCountText;

        [Header("Free Spins")]
        [SerializeField] private GameObject freeSpinPanel;
        [SerializeField] private TMP_Text freeSpinCountText;

        [Header("Popups")]
        [SerializeField] private GameObject lowBalancePopup;
        [SerializeField] private Button lowBalanceCloseButton;
        [SerializeField] private GameObject disconnectionPopup;
        [SerializeField] private Button disconnectionCloseButton;

        private int selectedRounds = 10;
        private Tween balanceTween;
        private Tween winTween;

        #region Initialization

        private void Start()
        {
            SetupButtons();
            SetupAutoPlayPanel();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Hide stop sign at start, show spin button
            if (spinNormalImage) spinNormalImage.SetActive(true);
            if (spinStopImage) spinStopImage.SetActive(false);
            
            // Hide panels
            if (autoPlayPanel) autoPlayPanel.SetActive(false);
            if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(false);
            if (freeSpinPanel) freeSpinPanel.SetActive(false);
            if (lowBalancePopup) lowBalancePopup.SetActive(false);
            if (disconnectionPopup) disconnectionPopup.SetActive(false);

            // NEW: Update auto play button text with default rounds
            UpdateAutoPlayButtonText();

            Debug.Log("[UIManager] ✅ UI Initialized - Spin button visible, Stop hidden");
        }

        private void SetupButtons()
        {
            if (betPlusButton) betPlusButton.onClick.AddListener(() => gameManager.IncreaseBet());
            if (betMinusButton) betMinusButton.onClick.AddListener(() => gameManager.DecreaseBet());
            if (spinButton) spinButton.onClick.AddListener(OnSpinButtonClicked);
            if (autoPlayOpenButton) autoPlayOpenButton.onClick.AddListener(OpenAutoPlayPanel);
            if (autoPlayCloseButton) autoPlayCloseButton.onClick.AddListener(CloseAutoPlayPanel);
            if (autoPlayStartButton) autoPlayStartButton.onClick.AddListener(StartAutoPlay);
            
            // MODIFIED: Turbo/Quick toggles now work universally
            if (turboToggle) turboToggle.onValueChanged.AddListener(OnTurboToggle);
            if (quickSpinToggle) quickSpinToggle.onValueChanged.AddListener(OnQuickSpinToggle);
            
            if (lowBalanceCloseButton) lowBalanceCloseButton.onClick.AddListener(() => lowBalancePopup.SetActive(false));
            if (disconnectionCloseButton) disconnectionCloseButton.onClick.AddListener(() => gameManager.ExitGame());
        }

        private void SetupAutoPlayPanel()
        {
            int[] roundValues = { 10, 20, 30, 50, 100, 500, 1000 };
            
            for (int i = 0; i < roundButtons.Length && i < roundValues.Length; i++)
            {
                int rounds = roundValues[i];
                roundButtons[i].rounds = rounds;
                roundButtons[i].button.onClick.AddListener(() => SelectRounds(rounds));
            }

            SelectRounds(10);
        }

        #endregion

        #region Game Events

        internal void OnGameInitialized()
        {
            UpdateBetDisplay();
            UpdateBalanceDisplay();
            UpdateWinDisplay(0);
            
            Debug.Log("[UIManager] 🎮 Game initialized");
        }

        internal void OnSpinStarted()
        {
            // Show stop button, hide spin button
            if (spinNormalImage) spinNormalImage.SetActive(false);
            if (spinStopImage) spinStopImage.SetActive(true);

            SetBetControlsEnabled(false);
            UpdateWinDisplay(0);
        }

        internal void OnSpinStopping(SpinResult result)
        {
            Debug.Log($"[UIManager] 🎯 Spin stopping - Win: {result.winAmount:F2}");
        }

        internal void OnSpinCompleted(SpinResult result)
        {
            // Show spin button, hide stop button
            if (spinNormalImage) spinNormalImage.SetActive(true);
            if (spinStopImage) spinStopImage.SetActive(false);

            // Only enable bet controls if not auto playing and not in free spins
            if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
            {
                SetBetControlsEnabled(true);
            }

            AnimateBalanceUpdate(result.playerData.balance);
            AnimateWinUpdate(result.winAmount);
            
            Debug.Log($"[UIManager] ✅ Spin completed - Balance: {result.playerData.balance:F2}, Win: {result.winAmount:F2}");
        }

        #endregion

        #region Button Handlers

        private void OnSpinButtonClicked()
        {
            if (gameManager.IsSpinning())
            {
                gameManager.RequestStop();
            }
            else
            {
                gameManager.RequestSpin();
            }
        }

        private void OpenAutoPlayPanel()
        {
            if (autoPlayPanel) autoPlayPanel.SetActive(true);
        }

        private void CloseAutoPlayPanel()
        {
            if (autoPlayPanel) autoPlayPanel.SetActive(false);
        }

        private void StartAutoPlay()
        {
            // MODIFIED: Don't pass speed to StartAutoPlay, it uses current speed setting
            gameManager.StartAutoPlay(selectedRounds);
            CloseAutoPlayPanel();
        }

        private void SelectRounds(int rounds)
        {
            selectedRounds = rounds;

            foreach (var btn in roundButtons)
            {
                bool isSelected = btn.rounds == rounds;
                if (btn.selectedIndicator)
                    btn.selectedIndicator.SetActive(isSelected);
            }

            // NEW: Update button text when rounds change
            UpdateAutoPlayButtonText();
        }

        /// <summary>
        /// NEW: Update auto play button text to show selected rounds
        /// </summary>
        private void UpdateAutoPlayButtonText()
        {
            if (autoPlayStartButtonText)
            {
                autoPlayStartButtonText.text = $"Start Autoplay({selectedRounds})";
            }
        }

        /// <summary>
        /// MODIFIED: Turbo toggle now works universally, not just for autoplay
        /// </summary>
        private void OnTurboToggle(bool isOn)
        {
            if (isOn)
            {
                // Disable quick spin if turbo is enabled
                if (quickSpinToggle && quickSpinToggle.isOn)
                {
                    quickSpinToggle.isOn = false;
                }
                gameManager.SetSpinSpeed(SpinSpeed.Turbo);
            }
            else
            {
                // If turning off turbo and quick is also off, set to normal
                if (!quickSpinToggle || !quickSpinToggle.isOn)
                {
                    gameManager.SetSpinSpeed(SpinSpeed.Normal);
                }
            }
        }

        /// <summary>
        /// MODIFIED: Quick spin toggle now works universally, not just for autoplay
        /// </summary>
        private void OnQuickSpinToggle(bool isOn)
        {
            if (isOn)
            {
                // Disable turbo if quick spin is enabled
                if (turboToggle && turboToggle.isOn)
                {
                    turboToggle.isOn = false;
                }
                gameManager.SetSpinSpeed(SpinSpeed.QuickSpin);
            }
            else
            {
                // If turning off quick and turbo is also off, set to normal
                if (!turboToggle || !turboToggle.isOn)
                {
                    gameManager.SetSpinSpeed(SpinSpeed.Normal);
                }
            }
        }

        #endregion

        #region Auto Play Events

        internal void OnAutoPlayStarted()
        {
            Debug.Log("[UIManager] 🔁 Auto play started");
            
            if (autoPlayCountDisplay) 
            {
                autoPlayCountDisplay.SetActive(true);
            }
            
            UpdateAutoPlayCount();
            SetBetControlsEnabled(false);
            if (autoPlayOpenButton) autoPlayOpenButton.interactable = false;
        }

        internal void OnAutoPlayStopped()
        {
            Debug.Log("[UIManager] ⏹️ Auto play stopped");
            
            if (autoPlayCountDisplay) 
            {
                autoPlayCountDisplay.SetActive(false);
            }

            SetBetControlsEnabled(true);
            if (autoPlayOpenButton) autoPlayOpenButton.interactable = true;
        }

        internal void UpdateAutoPlayCount()
        {
            if (gameManager.isAutoPlaying && autoPlayCountText)
            {
                int remaining = gameManager.autoPlayRemainingRounds;
                autoPlayCountText.text = remaining.ToString();
                
                if (autoPlayCountDisplay && !autoPlayCountDisplay.activeSelf)
                {
                    autoPlayCountDisplay.SetActive(true);
                }
            }
        }

        #endregion

        #region Free Spins Events

        internal void OnFreeSpinsStarted(int spins)
        {
            Debug.Log($"[UIManager] 🎁 Free spins started: {spins}");
            
            if (freeSpinPanel) freeSpinPanel.SetActive(true);
            UpdateFreeSpinCount();
            SetBetControlsEnabled(false);
        }

        internal void OnFreeSpinsEnded()
        {
            Debug.Log("[UIManager] 🎁 Free spins ended");
            
            if (freeSpinPanel) freeSpinPanel.SetActive(false);
            SetBetControlsEnabled(true);
        }

        internal void UpdateFreeSpinCount()
        {
            if (freeSpinCountText)
            {
                freeSpinCountText.text = $"{gameManager.freeSpinsRemaining} Free Spins";
            }
        }

        #endregion

        #region Display Updates

        internal void UpdateBetDisplay()
        {
            if (betAmountText)
                betAmountText.text = gameManager.currentBetAmount.ToString("F2");
        }

        private void UpdateBalanceDisplay()
        {
            if (balanceText)
                balanceText.text = gameManager.playerData.balance.ToString("F2");
        }

        private void UpdateWinDisplay(double amount)
        {
            if (winAmountText)
                winAmountText.text = amount.ToString("F2");
        }

        private void AnimateBalanceUpdate(double newBalance)
        {
            if (balanceTween != null) balanceTween.Kill();

            double oldBalance = gameManager.playerData.balance;
            
            balanceTween = DOTween.To(
                () => oldBalance,
                x => {
                    if (balanceText != null)
                        balanceText.text = x.ToString("F2");
                },
                newBalance,
                1f
            );
        }

        private void AnimateWinUpdate(double winAmount)
        {
            if (winTween != null) winTween.Kill();

            if (winAmount > 0)
            {
                winTween = DOTween.To(
                    () => 0.0,
                    x => UpdateWinDisplay(x),
                    winAmount,
                    1.5f
                );
            }
            else
            {
                UpdateWinDisplay(0);
            }
        }

        #endregion

        #region Helper Methods

        private void SetBetControlsEnabled(bool enabled)
        {
            if (betPlusButton) betPlusButton.interactable = enabled;
            if (betMinusButton) betMinusButton.interactable = enabled;
        }

        internal void ShowLowBalancePopup()
        {
            Debug.Log("[UIManager] ⚠️ Low balance");
            if (lowBalancePopup) lowBalancePopup.SetActive(true);
        }

        internal void ShowDisconnectionPopup()
        {
            Debug.Log("[UIManager] ⚠️ Disconnected");
            if (disconnectionPopup) disconnectionPopup.SetActive(true);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (balanceTween != null) balanceTween.Kill();
            if (winTween != null) winTween.Kill();
        }

        #endregion
    }

    [System.Serializable]
    public class RoundButton
    {
        public Button button;
        public int rounds;
        public GameObject selectedIndicator;
    }
}