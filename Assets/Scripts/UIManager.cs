using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

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
    
    [Header("Loading Settings - Random Stops")]
    [SerializeField] private int minStops = 2;
    [SerializeField] private int maxStops = 3;
    [SerializeField] private float minStopPosition = 0.2f; // Don't stop before 20%
    [SerializeField] private float maxStopPosition = 0.9f; // Don't stop after 90%
    [SerializeField] private float minStopDuration = 0.2f;
    [SerializeField] private float maxStopDuration = 0.5f;
    [SerializeField] private float loadingSpeed = 0.5f; // Fill amount per second
    [SerializeField] private float introAnimDuration = 2f;

    [Header("Bet Controls")]
    [SerializeField] private TMP_Text betAmountText;
    [SerializeField] private Button betPlusButton;
    [SerializeField] private Button betMinusButton;

    [Header("Max Bet Indicator")]
    [SerializeField] private GameObject maxBetObject; // Object to show when max bet is selected
    [SerializeField] private float maxBetDisplayDuration = 1f; // How long to show the indicator

    [Header("Balance & Win")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text winAmountText;

    [Header("Display Panel - Game Rules & Win")]
    [SerializeField] private GameObject displayPanel; // Parent panel
    [SerializeField] private GameObject gameRuleObject; // Shows by default
    [SerializeField] private GameObject winDisplayObject; // Shows when player wins
    [SerializeField] private TMP_Text winDisplayText; // Text showing "WIN X"
    [SerializeField] private float winDisplayDuration = 1f; // How long to show win display

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
    [SerializeField] private Transform winAmountContainer; // Horizontal layout group container
    [SerializeField] private HorizontalLayoutGroup winAmountLayoutGroup; // Reference to the layout group
    [SerializeField] private Image[] winAmountDigits; // 6 images for digits (123.33)
    [SerializeField] private GameObject decimalPointObject; // Decimal point object
    
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
    private Coroutine maxBetCoroutine;
    private Coroutine winDisplayCoroutine;

    #region Initialization

    private void Start()
    {
        SetupButtons();
        SetupAutoPlayPanel();
        
        // Initialize display panel
        InitializeDisplayPanel();
        
        // Hide max bet indicator initially
        if (maxBetObject) maxBetObject.SetActive(false);
        
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

    private void InitializeDisplayPanel()
    {
        // Show game rules by default, hide win display
        if (gameRuleObject) gameRuleObject.SetActive(true);
        if (winDisplayObject) winDisplayObject.SetActive(false);
        
        Debug.Log("[UIManager] Display panel initialized - Game rules visible");
    }

    #endregion

    #region Loading & Intro Sequence

    private IEnumerator LoadingSequence()
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

        Debug.Log("[UIManager] Loading sequence started with random stops");

        // Generate random stop points
        int numberOfStops = Random.Range(minStops, maxStops + 1);
        float[] stopPoints = GenerateRandomStopPoints(numberOfStops);
        
        Debug.Log($"[UIManager] Generated {numberOfStops} random stops at: {string.Join(", ", System.Array.ConvertAll(stopPoints, x => x.ToString("F2")))}");

        // Fill to each stop point
        float currentFill = 0f;
        for (int i = 0; i < stopPoints.Length; i++)
        {
            yield return StartCoroutine(FillLoadingBar(currentFill, stopPoints[i]));
            
            // Pause at stop point with random duration
            float pauseDuration = Random.Range(minStopDuration, maxStopDuration);
            Debug.Log($"[UIManager] Pausing at {stopPoints[i]:F2} for {pauseDuration:F2}s");
            yield return new WaitForSeconds(pauseDuration);
            
            currentFill = stopPoints[i];
        }

        // Final fill to 100%
        yield return StartCoroutine(FillLoadingBar(currentFill, 1f));
        
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

    private float[] GenerateRandomStopPoints(int count)
    {
        float[] stops = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            stops[i] = Random.Range(minStopPosition, maxStopPosition);
        }
        
        // Sort the stops so they occur in order
        System.Array.Sort(stops);
        
        return stops;
    }

    private IEnumerator FillLoadingBar(float fromAmount, float toAmount)
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

    #endregion

    #region Bet Management

    internal void UpdateBetDisplay()
    {
        if (betAmountText)
            betAmountText.text = gameManager.currentBetAmount.ToString("F2");
        
        // Update bet button states based on current index
        UpdateBetButtonStates();
        
        // Check if max bet is selected
        CheckAndShowMaxBet();
    }

    /// <summary>
    /// Update bet button interactability based on min/max bet index
    /// </summary>
    private void UpdateBetButtonStates()
    {
        if (gameManager.gameConfig == null) return;
        
        int maxBetIndex = gameManager.gameConfig.availableBets.Count - 1;
        
        // Disable minus button if at index 0
        if (betMinusButton)
        {
            betMinusButton.interactable = gameManager.currentBetIndex > 0;
        }
        
        // Disable plus button if at max index
        if (betPlusButton)
        {
            betPlusButton.interactable = gameManager.currentBetIndex < maxBetIndex;
        }
    }

    private void CheckAndShowMaxBet()
    {
        if (gameManager.gameConfig == null) return;
        
        int maxBetIndex = gameManager.gameConfig.availableBets.Count - 1;
        
        if (gameManager.currentBetIndex == maxBetIndex)
        {
            ShowMaxBetIndicator();
        }
    }

    private void ShowMaxBetIndicator()
    {
        if (maxBetObject == null) return;
        
        // Stop any existing coroutine
        if (maxBetCoroutine != null)
        {
            StopCoroutine(maxBetCoroutine);
        }
        
        maxBetCoroutine = StartCoroutine(MaxBetIndicatorRoutine());
    }

    private IEnumerator MaxBetIndicatorRoutine()
    {
        if (maxBetObject)
        {
            maxBetObject.SetActive(true);
            Debug.Log($"[UIManager] Max bet indicator shown for {maxBetDisplayDuration}s");
        }
        
        yield return new WaitForSeconds(maxBetDisplayDuration);
        
        if (maxBetObject)
        {
            maxBetObject.SetActive(false);
        }
    }

    #endregion

    #region Display Panel - Game Rules & Win Text

    /// <summary>
    /// Show win text in display panel when player wins
    /// </summary>
    private void ShowWinInDisplay(double winAmount)
    {
        // Stop any existing win display coroutine
        if (winDisplayCoroutine != null)
        {
            StopCoroutine(winDisplayCoroutine);
        }
        
        if (gameRuleObject) gameRuleObject.SetActive(false);
        if (winDisplayObject) winDisplayObject.SetActive(true);
        
        if (winDisplayText)
        {
            winDisplayText.text = $"WIN {winAmount:F2}";
        }
        
        Debug.Log($"[UIManager] Display panel showing win: {winAmount:F2}");
        
        // Start coroutine to hide win display after duration (only if not in auto play or free spins)
        if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
        {
            winDisplayCoroutine = StartCoroutine(HideWinDisplayAfterDelay());
        }
    }

    /// <summary>
    /// Hide win display after a delay and show game rules
    /// </summary>
    private IEnumerator HideWinDisplayAfterDelay()
    {
        yield return new WaitForSeconds(winDisplayDuration);
        ShowGameRulesInDisplay();
    }

    /// <summary>
    /// Reset display panel to show game rules
    /// Called at start of each spin, or when continuing in auto play/free spins
    /// </summary>
    private void ShowGameRulesInDisplay()
    {
        if (winDisplayObject) winDisplayObject.SetActive(false);
        if (gameRuleObject) gameRuleObject.SetActive(true);
        
        Debug.Log("[UIManager] Display panel showing game rules");
    }

    #endregion

    #region Spin Button Control

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

    internal void OnGameInitialized()
    {
        UpdateBetDisplay();
        UpdateBalanceDisplay();
        UpdateWinDisplay(0);
        
        SetBetControlsEnabled(true);
        SetSpinButtonState(true); // Enable spin button
        SetAutoPlayButtonEnabled(true);
    }

    internal void OnSpinStarted()
    {
        // Reset display panel to game rules at start of new spin
        // This applies to both normal spins and continuing auto play / free spins
        ShowGameRulesInDisplay();
        
        SetBetControlsEnabled(false);
        SetAutoPlayButtonEnabled(false);
        
        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(true);
        
        // DON'T update win display to 0 here - only update on result
    }

    internal void OnSpinStopping(SpinResult result)
    {
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);
        
        // Show win in display panel if there's a win
        if (result.winAmount > 0)
        {
            ShowWinInDisplay(result.winAmount);
        }
    }

    internal void OnSpinCompleted(SpinResult result)
    {
        UpdateBalanceDisplay();
        
        // Only update win amount text if there's a win or if it needs to be reset to 0
        if (result.winAmount > 0)
        {
            AnimateWinUpdate(result.winAmount);
            
            if (gameManager.isInFreeSpins)
            {
                totalFreeSpinWin += result.winAmount;
            }
        }
        else
        {
            // Only update to 0 if there was no win
            UpdateWinDisplay(0);
        }
        
        if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
        {
            SetBetControlsEnabled(true);
            SetAutoPlayButtonEnabled(true);
            SetSpinButtonState(true);
        }
    }

    /// <summary>
    /// Set spin button interactable state - used during win animations
    /// </summary>
    private void SetSpinButtonState(bool interactable)
    {
        if (spinButton)
        {
            spinButton.interactable = interactable;
        }
    }

    /// <summary>
    /// Set auto play button interactable state
    /// </summary>
    private void SetAutoPlayButtonEnabled(bool enabled)
    {
        if (autoPlayOpenButton)
        {
            autoPlayOpenButton.interactable = enabled;
        }
    }

    /// <summary>
    /// Disable controls during win animation
    /// </summary>
    internal void DisableControlsDuringWinAnimation()
    {
        SetSpinButtonState(false);
        SetBetControlsEnabled(false);
        SetAutoPlayButtonEnabled(false);
        
        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(false);
        
        Debug.Log("[UIManager] All controls disabled for win animation");
    }

    /// <summary>
    /// Re-enable controls after win animation
    /// </summary>
    internal void EnableControlsAfterWinAnimation()
    {
        SetSpinButtonState(true);
        
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);
        
        // Only enable bet controls and auto play if not in auto play or free spins
        if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
        {
            SetBetControlsEnabled(true);
            SetAutoPlayButtonEnabled(true);
        }
        
        Debug.Log("[UIManager] Controls re-enabled after win animation");
    }

    #endregion

    #region Auto Play

    private void SetupAutoPlayPanel()
    {
        if (roundButtons == null || roundButtons.Length == 0) return;

        for (int i = 0; i < roundButtons.Length; i++)
        {
            int rounds = roundButtons[i].rounds;
            roundButtons[i].button?.onClick.AddListener(() => SelectRounds(rounds));
        }

        SelectRounds(roundButtons[0].rounds);
    }

    private void SelectRounds(int rounds)
    {
        selectedRounds = rounds;

        foreach (var rb in roundButtons)
        {
            bool isSelected = rb.rounds == rounds;
            if (rb.selectedIndicator != null)
                rb.selectedIndicator.SetActive(isSelected);
        }

        UpdateAutoPlayButtonText();
        Debug.Log($"[UIManager] Selected {rounds} rounds");
    }

    private void UpdateAutoPlayButtonText()
    {
        if (autoPlayStartButtonText)
            autoPlayStartButtonText.text = $"START AUTOPLAY({selectedRounds})";
    }

    private void OpenAutoPlayPanel()
    {
        if (autoPlayPanel) autoPlayPanel.SetActive(true);
        Debug.Log("[UIManager] Auto play panel opened");
    }

    private void CloseAutoPlayPanel()
    {
        if (autoPlayPanel) autoPlayPanel.SetActive(false);
        Debug.Log("[UIManager] Auto play panel closed");
    }

    private void StartAutoPlay()
    {
        CloseAutoPlayPanel();
        gameManager.StartAutoPlay(selectedRounds);
    }

    private void OnTurboToggle(bool isOn)
    {
        if (isOn)
        {
            gameManager.SetSpinSpeed(SpinSpeed.Turbo);
            if (quickSpinToggle) quickSpinToggle.isOn = false;
        }
        else
        {
            if (quickSpinToggle && !quickSpinToggle.isOn)
            {
                gameManager.SetSpinSpeed(SpinSpeed.Normal);
            }
        }
    }

    private void OnQuickSpinToggle(bool isOn)
    {
        if (isOn)
        {
            gameManager.SetSpinSpeed(SpinSpeed.QuickSpin);
            if (turboToggle) turboToggle.isOn = false;
        }
        else
        {
            if (turboToggle && !turboToggle.isOn)
            {
                gameManager.SetSpinSpeed(SpinSpeed.Normal);
            }
        }
    }

    internal void OnAutoPlayStarted()
    {
        if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(true);
        UpdateAutoPlayCount();
        SetBetControlsEnabled(false);
        SetAutoPlayButtonEnabled(false);
        
        Debug.Log("[UIManager] Auto play started");
    }

    internal void OnAutoPlayStopped()
    {
        if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(false);
        
        // Reset spin button visual
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);
        
        // Re-enable controls
        SetBetControlsEnabled(true);    
        SetAutoPlayButtonEnabled(true);
        
        Debug.Log("[UIManager] Auto play stopped");
    }

    internal void UpdateAutoPlayCount()
    {
        if (autoPlayCountText)
            autoPlayCountText.text = $"{gameManager.autoPlayRemainingRounds}";
    }

    #endregion

    #region Free Spins

    internal void OnFreeSpinsStarted(int spins)
    {
        Debug.Log($"[UIManager] Free spins started: {spins}");
        
        totalFreeSpinsAwarded = spins;
        totalFreeSpinWin = 0;
        
        if (freeSpinPanel) freeSpinPanel.SetActive(true);
        UpdateFreeSpinCount();
        
        // Show start popup
        ShowFreeSpinStartPopup(spins);
    }

    internal void OnFreeSpinsEnded()
    {
        Debug.Log($"[UIManager] Free spins ended - Total win: {totalFreeSpinWin:F2}");
        
        if (freeSpinPanel) freeSpinPanel.SetActive(false);
        
        // Reset spin button visual (controls will be enabled when popup closes)
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);
        
        // Show end popup with total win
        ShowFreeSpinEndPopup(totalFreeSpinWin, totalFreeSpinsAwarded);
    }

    internal void UpdateFreeSpinCount()
    {
        if (freeSpinCountText)
            freeSpinCountText.text = $"{gameManager.freeSpinsRemaining}";
    }

    #endregion

    #region Free Spin Start Popup

    private void ShowFreeSpinStartPopup(int spins)
    {
        if (!freeSpinStartPopup || !freeSpinStartPopupRect) return;

        // Set the spin count display
        SetTwoDigitDisplay(freeSpinStartCountTens, freeSpinStartCountOnes, spins);

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
        
        Debug.Log($"[UIManager] Free spin start popup shown: {spins} spins");
    }

    private void CloseFreeSpinStartPopup()
    {
        if (!freeSpinStartPopup || !freeSpinStartPopupRect) return;

        AnimatePopupClose(freeSpinStartPopupRect, () => {
            freeSpinStartPopup.SetActive(false);
            
            // Only start free spin if actually in free spins mode (not during test)
            if (gameManager && gameManager.isInFreeSpins)
            {
                gameManager.StartFirstFreeSpin();
            }
        });
        
        Debug.Log("[UIManager] Free spin start popup closed");
    }

    #endregion

    #region Free Spin End Popup

    private void ShowFreeSpinEndPopup(double totalWin, int totalSpins)
    {
        if (!freeSpinEndPopup || !freeSpinEndPopupRect) return;

        // Set the total spins count
        SetTwoDigitDisplay(freeSpinEndCountTens, freeSpinEndCountOnes, totalSpins);
        
        // Set the win amount with proper decimal handling and spacing
        SetWinAmountDisplay(totalWin);

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
        
        Debug.Log($"[UIManager] Free spin end popup shown: {totalWin:F2} total win, {totalSpins} spins");
    }

    private void CloseFreeSpinEndPopup()
    {
        if (!freeSpinEndPopup || !freeSpinEndPopupRect) return;

        AnimatePopupClose(freeSpinEndPopupRect, () => {
            freeSpinEndPopup.SetActive(false);
            
            // Re-enable all controls after free spin end popup closes
            SetBetControlsEnabled(true);
            SetAutoPlayButtonEnabled(true);
            
            Debug.Log("[UIManager] Controls re-enabled after free spin end popup close");
        });
        
        Debug.Log("[UIManager] Free spin end popup closed");
    }

    private void SetTwoDigitDisplay(Image tensImage, Image onesImage, int number)
    {
        if (numberSprites == null || numberSprites.Length < 10)
        {
            Debug.LogError("[UIManager] Number sprites not assigned!");
            return;
        }

        int tens = number / 10;
        int ones = number % 10;

        if (tensImage != null)
        {
            if (tens > 0)
            {
                tensImage.gameObject.SetActive(true);
                tensImage.sprite = numberSprites[tens];
            }
            else
            {
                tensImage.gameObject.SetActive(false);
            }
        }

        if (onesImage != null)
        {
            onesImage.gameObject.SetActive(true);
            onesImage.sprite = numberSprites[ones];
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

        // Deactivate all digits first
        foreach (var digit in winAmountDigits)
        {
            if (digit) digit.gameObject.SetActive(false);
        }

        // Hide decimal point initially
        if (decimalPointObject) decimalPointObject.SetActive(false);

        // Check if amount has decimals
        bool hasDecimal = (amount % 1) != 0;

        string amountStr;
        if (hasDecimal)
        {
            // Format with 2 decimals
            amountStr = amount.ToString("F2");
        }
        else
        {
            // No decimal needed
            amountStr = amount.ToString("F0");
        }

        // Count total objects needed (digits + decimal point if applicable)
        int totalObjects = amountStr.Replace(".", "").Length;
        if (hasDecimal) totalObjects++; // Add 1 for decimal point

        // Adjust horizontal layout spacing based on total objects
        AdjustWinAmountLayoutSpacing(totalObjects);

        // Calculate total needed digits
        int digitCount = amountStr.Replace(".", "").Length;
        
        Debug.Log($"[UIManager] Displaying win: {amountStr}, Has Decimal: {hasDecimal}, Digit Count: {digitCount}, Total Objects: {totalObjects}");

        // Position in array from right to left (index 5 is rightmost)
        int arrayIndex = winAmountDigits.Length - 1;
        
        // Process string from right to left
        for (int charIndex = amountStr.Length - 1; charIndex >= 0 && arrayIndex >= 0; charIndex--)
        {
            char c = amountStr[charIndex];

            if (c == '.')
            {
                // Show decimal point at current position
                if (decimalPointObject)
                {
                    decimalPointObject.SetActive(true);
                }
            }
            else if (char.IsDigit(c))
            {
                int num = int.Parse(c.ToString());
                
                if (winAmountDigits[arrayIndex])
                {
                    winAmountDigits[arrayIndex].gameObject.SetActive(true);
                    winAmountDigits[arrayIndex].sprite = numberSprites[num];
                }
                
                arrayIndex--;
            }
        }

        Debug.Log($"[UIManager] Win amount display set: {amountStr}");
    }

    /// <summary>
    /// Adjust horizontal layout group spacing based on number of objects displayed
    /// Follows the rules: 6 objects (e.g., 123.45) = 0 spacing
    ///                    5 objects (e.g., 23.34) = -40 spacing
    ///                    4 objects (e.g., 3.43) = -90 spacing
    ///                    3 objects (e.g., 2.2) = -140 spacing
    ///                    1-2 objects (single/double digit) = 0 spacing
    /// </summary>
    private void AdjustWinAmountLayoutSpacing(int objectCount)
    {
        if (winAmountLayoutGroup == null)
        {
            Debug.LogWarning("[UIManager] Win amount layout group not assigned!");
            return;
        }

        float spacing = 0f;

        switch (objectCount)
        {
            case 6:
                spacing = 0f;
                break;
            case 5:
                spacing = -40f;
                break;
            case 4:
                spacing = -90f;
                break;
            case 3:
                spacing = -140f;
                break;
            case 1:
            case 2:
                spacing = 0f;
                break;
            default:
                spacing = 0f;
                Debug.LogWarning($"[UIManager] Unexpected object count: {objectCount}");
                break;
        }

        winAmountLayoutGroup.spacing = spacing;
        Debug.Log($"[UIManager] Layout spacing set to {spacing} for {objectCount} objects");
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
        
        // Re-apply min/max button states if enabling
        if (enabled)
        {
            UpdateBetButtonStates();
        }
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

    private IEnumerator TestFreeSpinSequence()
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