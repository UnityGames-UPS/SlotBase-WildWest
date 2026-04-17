using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Loading & Intro")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Image loadingBarFill;
    [SerializeField] private RectTransform loadingBarEndMarker;
    [SerializeField] private GameObject introAnimationObject;
    [SerializeField] private GameObject gameScreen;
    
    [Header("Loading Settings")]
    [SerializeField] private float firstPauseAt = 0.35f; // 30-40%
    [SerializeField] private float firstPauseDuration = 0.3f;
    [SerializeField] private float secondPauseAt = 0.75f; // 70-80%
    [SerializeField] private float secondPauseDuration = 0.3f;
    [SerializeField] private float loadingSpeed = 0.5f; // Fill amount per second
    [SerializeField] private float introAnimDuration = 2f;

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
    [SerializeField] private TMP_Text autoPlayStartButtonText;
    
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

    [Header("Free Spin Start Popup")]
    [SerializeField] private GameObject freeSpinStartPopup;
    [SerializeField] private RectTransform freeSpinStartPopupRect;
    [SerializeField] private Image freeSpinStartPopImage;
    [SerializeField] private Button freeSpinStartCloseButton;
    [SerializeField] private Image freeSpinStartCountTens;
    [SerializeField] private Image freeSpinStartCountOnes;
    [SerializeField] private Sprite[] numberSprites; // 0-9 sprites

    [Header("Free Spin End Popup")]
    [SerializeField] private GameObject freeSpinEndPopup;
    [SerializeField] private RectTransform freeSpinEndPopupRect;
    [SerializeField] private Image freeSpinEndPopImage;
    [SerializeField] private Button freeSpinEndCloseButton;
    
    [Header("Free Spin End - Win Amount Display")]
    [SerializeField] private Image[] winAmountDigits; // 6 images for win amount (e.g., 333.33)
    [SerializeField] private GameObject decimalPoint;
    
    [Header("Free Spin End - Total Spin Count")]
    [SerializeField] private Image freeSpinEndCountTens;
    [SerializeField] private Image freeSpinEndCountOnes;

    [Header("Popups")]
    [SerializeField] private GameObject lowBalancePopup;
    [SerializeField] private Button lowBalanceCloseButton;
    [SerializeField] private GameObject disconnectionPopup;
    [SerializeField] private Button disconnectionCloseButton;

    [Header("Animation Settings")]
    [SerializeField] private float winCountDuration = 0.25f;
    [SerializeField] private float balanceCountDuration = 1.0f;
    [SerializeField] private float popupAppearY = 555f;
    [SerializeField] private float popupFinalY = 165f;
    [SerializeField] private float popupDropDuration = 0.8f;
    [SerializeField] private int popupBounceCount = 2;

    [Header("Test Controls")]
    [SerializeField] private Button testFreeSpinButton;

    private int selectedRounds = 10;
    private Tween balanceTween;
    private Tween winTween;
    private double totalFreeSpinWin = 0;
    private int totalFreeSpinsAwarded = 0;

    #region Initialization

    private void Start()
    {
        SetupButtons();
        SetupAutoPlayPanel();
        
        // Start with loading sequence
        StartCoroutine(LoadingSequence());
    }

    private void InitializeUI()
    {
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);
        
        if (autoPlayPanel) autoPlayPanel.SetActive(false);
        if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(false);
        if (freeSpinPanel) freeSpinPanel.SetActive(false);
        if (lowBalancePopup) lowBalancePopup.SetActive(false);
        if (disconnectionPopup) disconnectionPopup.SetActive(false);
        
        // Initialize free spin popups
        if (freeSpinStartPopup) freeSpinStartPopup.SetActive(false);
        if (freeSpinEndPopup) freeSpinEndPopup.SetActive(false);

        UpdateAutoPlayButtonText();

        Debug.Log("[UIManager] UI Initialized - Spin button visible, Stop hidden");
    }

    #endregion

    #region Loading & Intro Sequence

    private System.Collections.IEnumerator LoadingSequence()
    {
        // Initial setup - show loading screen, hide game screen
        if (loadingScreen) loadingScreen.SetActive(true);
        if (gameScreen) gameScreen.SetActive(false);
        if (introAnimationObject) introAnimationObject.SetActive(false);
        
        // Initialize loading bar
        if (loadingBarFill)
        {
            loadingBarFill.fillAmount = 0f;
            UpdateLoadingBarEndMarker(0f);
        }

        Debug.Log("[UIManager] Loading sequence started");

        // Phase 1: Load to 30-40%
        yield return StartCoroutine(FillLoadingBar(0f, firstPauseAt));
        yield return new WaitForSeconds(firstPauseDuration);

        // Phase 2: Load to 70-80%
        yield return StartCoroutine(FillLoadingBar(firstPauseAt, secondPauseAt));
        yield return new WaitForSeconds(secondPauseDuration);

        // Phase 3: Load to 100%
        yield return StartCoroutine(FillLoadingBar(secondPauseAt, 1f));
        
        Debug.Log("[UIManager] Loading complete");

        // Show intro animation
        if (introAnimationObject)
        {
            introAnimationObject.SetActive(true);
            Debug.Log("[UIManager] Intro animation started");
        }

        yield return new WaitForSeconds(introAnimDuration);

        // Hide loading screen and intro animation
        if (loadingScreen) loadingScreen.SetActive(false);
        if (introAnimationObject) introAnimationObject.SetActive(false);

        // Enable game screen
        if (gameScreen) gameScreen.SetActive(true);

        // Initialize UI
        InitializeUI();

        Debug.Log("[UIManager] Game screen enabled, loading sequence complete");
    }

    private System.Collections.IEnumerator FillLoadingBar(float fromAmount, float toAmount)
    {
        if (!loadingBarFill) yield break;

        float currentAmount = fromAmount;
        
        while (currentAmount < toAmount)
        {
            currentAmount += loadingSpeed * Time.deltaTime;
            currentAmount = Mathf.Min(currentAmount, toAmount);
            
            loadingBarFill.fillAmount = currentAmount;
            UpdateLoadingBarEndMarker(currentAmount);
            
            yield return null;
        }
    }

    private void UpdateLoadingBarEndMarker(float fillAmount)
    {
        if (!loadingBarEndMarker || !loadingBarFill) return;

        // Calculate the position of the end marker based on fill amount
        // Assuming the loading bar fills horizontally from left to right
        RectTransform barRect = loadingBarFill.rectTransform;
        float barWidth = barRect.rect.width;
        
        // Position the marker at the end of the filled portion
        float xPosition = (fillAmount - 0.5f) * barWidth;
        
        loadingBarEndMarker.anchoredPosition = new Vector2(xPosition, loadingBarEndMarker.anchoredPosition.y);
    }

    private void SetupButtons()
    {
        if (betPlusButton) betPlusButton.onClick.AddListener(() => gameManager.IncreaseBet());
        if (betMinusButton) betMinusButton.onClick.AddListener(() => gameManager.DecreaseBet());
        if (spinButton) spinButton.onClick.AddListener(OnSpinButtonClicked);
        if (autoPlayOpenButton) autoPlayOpenButton.onClick.AddListener(OpenAutoPlayPanel);
        if (autoPlayCloseButton) autoPlayCloseButton.onClick.AddListener(CloseAutoPlayPanel);
        if (autoPlayStartButton) autoPlayStartButton.onClick.AddListener(StartAutoPlay);
        
        if (turboToggle) turboToggle.onValueChanged.AddListener(OnTurboToggle);
        if (quickSpinToggle) quickSpinToggle.onValueChanged.AddListener(OnQuickSpinToggle);
        
        if (lowBalanceCloseButton) lowBalanceCloseButton.onClick.AddListener(() => lowBalancePopup.SetActive(false));
        if (disconnectionCloseButton) disconnectionCloseButton.onClick.AddListener(() => gameManager.ExitGame());
        
        // Free spin popup buttons
        if (freeSpinStartCloseButton) freeSpinStartCloseButton.onClick.AddListener(CloseFreeSpinStartPopup);
        if (freeSpinEndCloseButton) freeSpinEndCloseButton.onClick.AddListener(CloseFreeSpinEndPopup);
        
        // Test button
        if (testFreeSpinButton) testFreeSpinButton.onClick.AddListener(TestFreeSpinPopups);
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
        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(true);

        SetBetControlsEnabled(false);
        
        if (autoPlayOpenButton) autoPlayOpenButton.interactable = false;
        
        UpdateWinDisplay(0);
    }

    internal void OnSpinStopping(SpinResult result)
    {
        Debug.Log($"[UIManager] Spin stopping - Win: {result.winAmount:F2}");
        
        if (result.winAmount > 0)
        {
            AnimateWinUpdate(result.winAmount);
            
            // Track free spin wins
            if (gameManager.isInFreeSpins)
            {
                totalFreeSpinWin += result.winAmount;
            }
        }
    }

    internal void OnSpinCompleted(SpinResult result)
    {
        if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
        {
            if (spinNormalImage) spinNormalImage.SetActive(true);
            if (spinStopImage) spinStopImage.SetActive(false);
        }
        if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
        {
            SetBetControlsEnabled(true);
            if (autoPlayOpenButton) autoPlayOpenButton.interactable = true;
        }

        AnimateBalanceUpdate(result.playerData.balance);
        
        Debug.Log($"[UIManager] Spin completed - Balance: {result.playerData.balance:F2}, Win: {result.winAmount:F2}");
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
        if (autoPlayPanel)
        {
            autoPlayPanel.SetActive(true);
            AnimatePopupOpen(autoPlayPanel.GetComponent<RectTransform>());
        }
    }

    private void CloseAutoPlayPanel()
    {
        if (autoPlayPanel)
        {
            AnimatePopupClose(autoPlayPanel.GetComponent<RectTransform>(), () => {
                autoPlayPanel.SetActive(false);
            });
        }
    }

    private void StartAutoPlay()
    {
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

        UpdateAutoPlayButtonText();
    }

    private void UpdateAutoPlayButtonText()
    {
        if (autoPlayStartButtonText)
        {
            autoPlayStartButtonText.text = $"Start Autoplay({selectedRounds})";
        }
    }

    private void OnTurboToggle(bool isOn)
    {
        if (isOn)
        {
            if (quickSpinToggle && quickSpinToggle.isOn)
            {
                quickSpinToggle.isOn = false;
            }
            gameManager.SetSpinSpeed(SpinSpeed.Turbo);
        }
        else
        {
            if (!quickSpinToggle || !quickSpinToggle.isOn)
            {
                gameManager.SetSpinSpeed(SpinSpeed.Normal);
            }
        }
    }

    private void OnQuickSpinToggle(bool isOn)
    {
        if (isOn)
        {
            if (turboToggle && turboToggle.isOn)
            {
                turboToggle.isOn = false;
            }
            gameManager.SetSpinSpeed(SpinSpeed.QuickSpin);
        }
        else
        {
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
        Debug.Log("[UIManager] Auto play started");
        
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
        Debug.Log("[UIManager] Auto play stopped");
        
        if (autoPlayCountDisplay) 
        {
            autoPlayCountDisplay.SetActive(false);
        }

        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);

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
        Debug.Log($"[UIManager] Free spins started: {spins}");
        
        totalFreeSpinWin = 0;
        totalFreeSpinsAwarded = spins;
        
        // Show free spin start popup with animation
        ShowFreeSpinStartPopup(spins);
        
        if (freeSpinPanel) freeSpinPanel.SetActive(true);
        UpdateFreeSpinCount();
        SetBetControlsEnabled(false);
    }

    internal void OnFreeSpinsEnded()
    {
        Debug.Log($"[UIManager] Free spins ended - Total Win: {totalFreeSpinWin:F2}");
        
        // Show free spin end popup with total win
        ShowFreeSpinEndPopup(totalFreeSpinWin, totalFreeSpinsAwarded);
        
        if (freeSpinPanel) freeSpinPanel.SetActive(false);
        
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);
        
        SetBetControlsEnabled(true);
        if (autoPlayOpenButton) autoPlayOpenButton.interactable = true;
    }

    internal void UpdateFreeSpinCount()
    {
        if (freeSpinCountText)
        {
            freeSpinCountText.text = $"{gameManager.freeSpinsRemaining} Free Spins";
        }
    }

    #endregion

    #region Free Spin Popup Animations

    private void ShowFreeSpinStartPopup(int spinCount)
    {
        if (!freeSpinStartPopup || !freeSpinStartPopupRect) return;

        // Set count display
        SetTwoDigitNumberDisplay(freeSpinStartCountTens, freeSpinStartCountOnes, spinCount);

        // Enable popup
        freeSpinStartPopup.SetActive(true);

       // Set initial position (high Y position)
        freeSpinStartPopupRect.anchoredPosition = new Vector2(
            freeSpinStartPopupRect.anchoredPosition.x,
            popupAppearY
        );

        // Reset scale
        freeSpinStartPopupRect.localScale = Vector3.one;

        // Animate drop with bounce
        freeSpinStartPopupRect.DOAnchorPosY(popupFinalY, popupDropDuration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => {
                Debug.Log("[UIManager] Free spin start popup animation complete");
            });
    }

    private void CloseFreeSpinStartPopup()
    {
        if (!freeSpinStartPopup || !freeSpinStartPopupRect) return;

        // Classic pop-out animation
        Sequence closeSeq = DOTween.Sequence();
        
        // Scale up slightly then shrink to zero
        closeSeq.Append(freeSpinStartPopupRect.DOScale(1.1f, 0.1f));
        closeSeq.Append(freeSpinStartPopupRect.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        closeSeq.OnComplete(() => {
            freeSpinStartPopup.SetActive(false);
            freeSpinStartPopupRect.localScale = Vector3.one;
            
            // Notify GameManager to start first free spin
            if (gameManager && gameManager.isInFreeSpins)
            {
                Debug.Log("[UIManager] Notifying GameManager to start first free spin");
                gameManager.StartFirstFreeSpin();
            }
        });
    }

    private void ShowFreeSpinEndPopup(double totalWin, int totalSpins)
    {
        if (!freeSpinEndPopup || !freeSpinEndPopupRect) return;

        // Set total win amount display
        SetWinAmountDisplay(totalWin);
        
        // Set total spins display
        SetTwoDigitNumberDisplay(freeSpinEndCountTens, freeSpinEndCountOnes, totalSpins);

        // Enable popup
        freeSpinEndPopup.SetActive(true);

        // Set initial position (high Y position)
        freeSpinEndPopupRect.anchoredPosition = new Vector2(
            freeSpinEndPopupRect.anchoredPosition.x,
            popupAppearY
        );

        // Reset scale
        freeSpinEndPopupRect.localScale = Vector3.one;

        // Animate drop with bounce
        freeSpinEndPopupRect.DOAnchorPosY(popupFinalY, popupDropDuration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => {
                Debug.Log("[UIManager] Free spin end popup animation complete");
            });
    }

    private void CloseFreeSpinEndPopup()
    {
        if (!freeSpinEndPopup || !freeSpinEndPopupRect) return;

        // Classic pop-out animation
        Sequence closeSeq = DOTween.Sequence();
        
        closeSeq.Append(freeSpinEndPopupRect.DOScale(1.1f, 0.1f));
        closeSeq.Append(freeSpinEndPopupRect.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        closeSeq.OnComplete(() => {
            freeSpinEndPopup.SetActive(false);
            freeSpinEndPopupRect.localScale = Vector3.one;
        });
    }

    #endregion

    #region Number Display Helpers

    private void SetTwoDigitNumberDisplay(Image tensImage, Image onesImage, int number)
    {
        if (numberSprites == null || numberSprites.Length < 10)
        {
            Debug.LogError("[UIManager] Number sprites not assigned or incomplete!");
            return;
        }

        int tens = number / 10;
        int ones = number % 10;

        // If only single digit, hide tens and show only ones
        if (number < 10)
        {
            if (tensImage) tensImage.gameObject.SetActive(false);
            if (onesImage)
            {
                onesImage.gameObject.SetActive(true);
                onesImage.sprite = numberSprites[ones];
            }
        }
        else
        {
            if (tensImage)
            {
                tensImage.gameObject.SetActive(true);
                tensImage.sprite = numberSprites[tens];
            }
            if (onesImage)
            {
                onesImage.gameObject.SetActive(true);
                onesImage.sprite = numberSprites[ones];
            }
        }
    }

    private void SetWinAmountDisplay(double amount)
    {
        if (winAmountDigits == null || winAmountDigits.Length == 0)
        {
            Debug.LogError("[UIManager] Win amount digit images not assigned!");
            return;
        }

        if (numberSprites == null || numberSprites.Length < 10)
        {
            Debug.LogError("[UIManager] Number sprites not assigned!");
            return;
        }

        // Format the amount to string
        string amountStr = amount.ToString("F2"); // e.g., "333.33"
        
        // Remove decimal point from string for digit extraction
        string digitsOnly = amountStr.Replace(".", "");
        
        // Check if we need to show decimal
        bool hasDecimal = amountStr.Contains(".");
        int decimalPosition = hasDecimal ? amountStr.IndexOf('.') : -1;

        // Deactivate all digits first
        foreach (var digit in winAmountDigits)
        {
            if (digit) digit.gameObject.SetActive(false);
        }

        // Hide decimal point initially
        if (decimalPoint) decimalPoint.SetActive(false);

        // Display digits from right to left
        int digitIndex = 0;
        int charIndex = amountStr.Length - 1;

        // Process from right to left
        while (charIndex >= 0 && digitIndex < winAmountDigits.Length)
        {
            char c = amountStr[charIndex];

            if (c == '.')
            {
                // Show decimal point
                if (decimalPoint)
                {
                    decimalPoint.SetActive(true);
                    // Position it correctly between digits if needed
                }
            }
            else if (char.IsDigit(c))
            {
                int num = int.Parse(c.ToString());
                if (winAmountDigits[digitIndex])
                {
                    winAmountDigits[digitIndex].gameObject.SetActive(true);
                    winAmountDigits[digitIndex].sprite = numberSprites[num];
                }
                digitIndex++;
            }

            charIndex--;
        }

        Debug.Log($"[UIManager] Displaying win amount: {amount:F2}");
    }

    #endregion

    #region Popup Animations (Generic)

    private void AnimatePopupOpen(RectTransform popupRect)
    {
        if (!popupRect) return;

        // Start from scale 0
        popupRect.localScale = Vector3.zero;

        // Pop in animation
        popupRect.DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack);
    }

    private void AnimatePopupClose(RectTransform popupRect, System.Action onComplete)
    {
        if (!popupRect) return;

        Sequence closeSeq = DOTween.Sequence();
        
        closeSeq.Append(popupRect.DOScale(1.1f, 0.1f));
        closeSeq.Append(popupRect.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        closeSeq.OnComplete(() => {
            popupRect.localScale = Vector3.one;
            onComplete?.Invoke();
        });
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
            balanceCountDuration
        ).SetEase(Ease.OutCubic);
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
                winCountDuration
            )
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                UpdateWinDisplay(winAmount);
                Debug.Log($"[UIManager] Win count animation completed: {winAmount:F2}");
            });
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
        Debug.Log("[UIManager] Low balance");
        if (lowBalancePopup) lowBalancePopup.SetActive(true);
    }

    internal void ShowDisconnectionPopup()
    {
        Debug.Log("[UIManager] Disconnected");
        if (disconnectionPopup) disconnectionPopup.SetActive(true);
    }

    #endregion

    #region Test Functions

    private void TestFreeSpinPopups()
    {
        Debug.Log("[UIManager] Testing free spin popups");
        
        // Test start popup
        StartCoroutine(TestFreeSpinSequence());
    }

    private System.Collections.IEnumerator TestFreeSpinSequence()
    {
        // Show start popup with 15 spins
        ShowFreeSpinStartPopup(15);
        
        // Wait 3 seconds
        yield return new WaitForSeconds(3f);
        
        // Close start popup (would normally be done by user)
        // CloseFreeSpinStartPopup();
        
        // Wait 2 seconds
        // yield return new WaitForSeconds(2f);
        
        // Show end popup with win amount
        // ShowFreeSpinEndPopup(1234.56, 15);
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (balanceTween != null) balanceTween.Kill();
        if (winTween != null) winTween.Kill();
        DOTween.KillAll();
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