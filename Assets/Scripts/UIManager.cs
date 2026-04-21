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
    [SerializeField] private GameObject freeSpinIntroAnimation;
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

    [Header("Backgrounds")]
    [SerializeField] private GameObject normalSpinBackground; // Normal game background
    [SerializeField] private GameObject freeSpinBackground; // Free spin mode background

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
    [SerializeField] private RectTransform autoPlayPanelRect;
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

    [Header("Free Spin Count Display - Game Screen")]
    [SerializeField] private GameObject freeSpinCountContainer; // Container for count display
    [SerializeField] private Image freeSpinCountTens; // Tens digit
    [SerializeField] private Image freeSpinCountOnes; // Ones digit
    [SerializeField] private Sprite[] freeSpinNumberSprites; // 0-9 sprites for game screen (different from popup)
    [SerializeField] private GameObject lastSpinLeftObject; // Shows on last spin
    [SerializeField] private GameObject buyFreeSpinObject; // Shows after free spins end


    [Header("Free Spin Start Popup")]
    [SerializeField] private GameObject freeSpinStartPopup;
    [SerializeField] private RectTransform freeSpinStartPopupRect;
    [SerializeField] private Image freeSpinStartPopImage;
    [SerializeField] private Button freeSpinStartCloseButton;
    [SerializeField] private Image freeSpinStartCountTens;
    [SerializeField] private Image freeSpinStartCountOnes;
    [SerializeField] private Sprite[] numberSprites; // 0-9 sprites for popups

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

    [Header("Connection Popups")]
    [SerializeField] private GameObject reconnectionPopup;
    [SerializeField] private GameObject anotherDevicePopup;
    [SerializeField] private Button anotherDeviceCloseButton;

    [SerializeField] private SocketIOManager socketManager;

    [Header("Animation Settings")]
    [SerializeField] private float winCountDuration = 0.25f;
    [SerializeField] private float balanceCountDuration = 1.0f;
    [SerializeField] private float popupAppearY = 555f;
    [SerializeField] private float popupFinalY = 165f;
    [SerializeField] private float popupDropDuration = 0.8f;
    [SerializeField] private int popupBounceCount = 2;
    [SerializeField] private float freeSpinIntroDuration = 2f;

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

        // Initialize backgrounds
        InitializeBackgrounds();

        // Start with loading sequence
        StartCoroutine(LoadingSequence());
    }

    private void InitializeBackgrounds()
    {
        // Start with normal background active, free spin background hidden
        if (normalSpinBackground) normalSpinBackground.SetActive(true);
        if (freeSpinBackground) freeSpinBackground.SetActive(false);

    }

    private void InitializeUI()
    {
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);

        if (autoPlayPanel) autoPlayPanel.SetActive(false);
        if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(false);

        if (lowBalancePopup) lowBalancePopup.SetActive(false);
        if (disconnectionPopup) disconnectionPopup.SetActive(false);

        // Initialize free spin count display on game screen
        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
        if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
        if (buyFreeSpinObject) buyFreeSpinObject.SetActive(false);

        // Initialize free spin popups
        if (freeSpinStartPopup) freeSpinStartPopup.SetActive(false);
        if (freeSpinEndPopup) freeSpinEndPopup.SetActive(false);

        if (reconnectionPopup) reconnectionPopup.SetActive(false);
        if (anotherDevicePopup) anotherDevicePopup.SetActive(false);

        UpdateAutoPlayButtonText();

    }

    private void InitializeDisplayPanel()
    {
        // Show game rules by default, hide win display
        if (gameRuleObject) gameRuleObject.SetActive(true);
        if (winDisplayObject) winDisplayObject.SetActive(false);

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


        // Generate random stop points
        int numberOfStops = Random.Range(minStops, maxStops + 1);
        float[] stopPoints = GenerateRandomStopPoints(numberOfStops);


        // Fill to each stop point
        float currentFill = 0f;
        for (int i = 0; i < stopPoints.Length; i++)
        {
            yield return StartCoroutine(FillLoadingBar(currentFill, stopPoints[i]));

            // Pause at stop point with random duration
            float pauseDuration = Random.Range(minStopDuration, maxStopDuration);
            yield return new WaitForSeconds(pauseDuration);

            currentFill = stopPoints[i];
        }

        // Final fill to 100%
        yield return StartCoroutine(FillLoadingBar(currentFill, 1f));


        // Show intro animation
        if (introAnimationObject)
        {
            introAnimationObject.SetActive(true);
        }

        yield return new WaitForSeconds(introAnimDuration);

        // Hide loading screen and intro animation
        if (loadingScreen) loadingScreen.SetActive(false);
        if (introAnimationObject) introAnimationObject.SetActive(false);

        // Enable game screen
        if (gameScreen) gameScreen.SetActive(true);

        // Initialize UI
        InitializeUI();

    }

    private float[] GenerateRandomStopPoints(int count)
    {
        float[] stops = new float[count];

        for (int i = 0; i < count; i++)
        {
            stops[i] = Random.Range(minStopPosition, maxStopPosition);
        }

        // Sort to ensure increasing order
        System.Array.Sort(stops);

        return stops;
    }

    private IEnumerator FillLoadingBar(float fromFill, float toFill)
    {
        float currentFill = fromFill;

        while (currentFill < toFill)
        {
            currentFill += loadingSpeed * Time.deltaTime;
            currentFill = Mathf.Min(currentFill, toFill);

            if (loadingBarFill)
            {
                loadingBarFill.fillAmount = currentFill;
                UpdateLoadingBarEndMarker(currentFill);
            }

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

    /// <summary>
    /// Show intro animation for 2 seconds (called from free spin flows)
    /// </summary>
    private IEnumerator ShowFreeSpinIntroAnimation(System.Action onComplete)
    {
        if (freeSpinIntroAnimation)
        {
            freeSpinIntroAnimation.SetActive(true);
        }

        yield return new WaitForSeconds(freeSpinIntroDuration);

        if (freeSpinIntroAnimation)
        {
            freeSpinIntroAnimation.SetActive(false);
        }

        onComplete?.Invoke();
    }

    #endregion

    #region Button Setup

    private void SetupButtons()
    {
        if (betPlusButton) betPlusButton.onClick.AddListener(() => gameManager.IncreaseBet());
        if (betMinusButton) betMinusButton.onClick.AddListener(() => gameManager.DecreaseBet());
        if (spinButton) spinButton.onClick.AddListener(OnSpinButtonPressed);

        if (autoPlayOpenButton) autoPlayOpenButton.onClick.AddListener(OpenAutoPlayPanel);
        if (autoPlayCloseButton) autoPlayCloseButton.onClick.AddListener(CloseAutoPlayPanel);
        if (autoPlayStartButton) autoPlayStartButton.onClick.AddListener(OnAutoPlayStart);

        if (freeSpinStartCloseButton) freeSpinStartCloseButton.onClick.AddListener(CloseFreeSpinStartPopup);
        if (freeSpinEndCloseButton) freeSpinEndCloseButton.onClick.AddListener(CloseFreeSpinEndPopup);

        if (lowBalanceCloseButton) lowBalanceCloseButton.onClick.AddListener(() => {
            if (lowBalancePopup) lowBalancePopup.SetActive(false);
        });

        if (disconnectionCloseButton) disconnectionCloseButton.onClick.AddListener(() => {
            if (disconnectionPopup) disconnectionPopup.SetActive(false);
        });

        if (testFreeSpinButton) testFreeSpinButton.onClick.AddListener(TestFreeSpinPopups);
        // Connection popup buttons
        if (disconnectionCloseButton)
        {
            disconnectionCloseButton.onClick.RemoveAllListeners();
            disconnectionCloseButton.onClick.AddListener(OnExitButtonPressed);
        }

        if (anotherDeviceCloseButton)
        {
            anotherDeviceCloseButton.onClick.RemoveAllListeners();
            anotherDeviceCloseButton.onClick.AddListener(OnExitButtonPressed);
        }
    }

    private void SetupAutoPlayPanel()
    {
        if (turboToggle) turboToggle.onValueChanged.AddListener(OnTurboToggleChanged);
        if (quickSpinToggle) quickSpinToggle.onValueChanged.AddListener(OnQuickSpinToggleChanged);

        foreach (var roundButton in roundButtons)
        {
            if (roundButton.button != null)
            {
                int rounds = roundButton.rounds;
                roundButton.button.onClick.AddListener(() => SelectAutoPlayRounds(rounds));
            }
        }
    }

    #endregion

    #region Game Events

    internal void OnGameInitialized()
    {
        UpdateBetDisplay();
        UpdateBalanceDisplay();
        UpdateWinDisplay(0);
    }

    internal void OnSpinStarted()
    {
        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(true);

        SetBetControlsEnabled(false);
        if (autoPlayOpenButton) autoPlayOpenButton.interactable = false;

        // Hide win display, show game rules
        if (winDisplayCoroutine != null)
        {
            StopCoroutine(winDisplayCoroutine);
            winDisplayCoroutine = null;
        }
        if (winDisplayObject) winDisplayObject.SetActive(false);
        if (gameRuleObject) gameRuleObject.SetActive(true);
    }

    internal void OnSpinStopping(SpinResult result)
    {
        // Update balance with animation
        AnimateBalanceUpdate(result.playerData.balance);

        // Update win amount with animation
        if (result.winAmount > 0)
        {
            AnimateWinUpdate(result.winAmount);

            // Show win display temporarily
            ShowWinDisplay(result.winAmount);
        }
        else
        {
            UpdateWinDisplay(0);
        }
    }

    internal void OnSpinCompleted(SpinResult result)
    {
        if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
        {
            if (spinNormalImage) spinNormalImage.SetActive(true);
            if (spinStopImage) spinStopImage.SetActive(false);

            SetBetControlsEnabled(true);
            if (autoPlayOpenButton) autoPlayOpenButton.interactable = true;
        }
    }

    internal void DisableControlsDuringWinAnimation()
    {
        // Keep controls disabled during win line animation
        SetBetControlsEnabled(false);
        if (spinButton) spinButton.interactable = false;
        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(false);
    }

    internal void EnableControlsAfterWinAnimation()
    {
        // Re-enable controls after win animation
        if (!gameManager.isAutoPlaying && !gameManager.isInFreeSpins)
        {
            SetBetControlsEnabled(true);
            if (spinButton) spinButton.interactable = true;
            if (autoPlayOpenButton) autoPlayOpenButton.interactable = true;
            if (spinNormalImage) spinNormalImage.SetActive(true);
            if (spinStopImage) spinStopImage.SetActive(false);
        }
    }

    private void ShowWinDisplay(double winAmount)
    {
        if (winDisplayCoroutine != null)
            StopCoroutine(winDisplayCoroutine);

        winDisplayCoroutine = StartCoroutine(ShowWinDisplayCoroutine(winAmount));
    }

    private IEnumerator ShowWinDisplayCoroutine(double winAmount)
    {
        // Hide game rules, show win display
        if (gameRuleObject) gameRuleObject.SetActive(false);
        if (winDisplayObject) winDisplayObject.SetActive(true);

        if (winDisplayText)
        {
            winDisplayText.text = $"WIN {winAmount:F2}";
        }

        yield return new WaitForSeconds(winDisplayDuration);

        // Switch back to game rules
        if (winDisplayObject) winDisplayObject.SetActive(false);
        if (gameRuleObject) gameRuleObject.SetActive(true);

        winDisplayCoroutine = null;
    }

    #endregion

    #region Spin Button

    private void OnSpinButtonPressed()
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

    #endregion

    #region Bet Controls

    internal void UpdateBetDisplay()
    {
        if (betAmountText)
            betAmountText.text = gameManager.currentBetAmount.ToString("F2");

        UpdateBetButtonStates();
        CheckMaxBetIndicator();
    }

    private void UpdateBetButtonStates()
    {
        // Buttons always interactable - bet wraps cyclically
        if (betMinusButton) betMinusButton.interactable = true;
        if (betPlusButton) betPlusButton.interactable = true;
    }

    private void CheckMaxBetIndicator()
    {
        bool isMaxBet = gameManager.currentBetIndex >= gameManager.gameConfig.availableBets.Count - 1;

        if (isMaxBet && maxBetObject && !maxBetObject.activeSelf)
        {
            // Show max bet indicator
            if (maxBetCoroutine != null)
                StopCoroutine(maxBetCoroutine);

            maxBetCoroutine = StartCoroutine(ShowMaxBetIndicator());
        }
        else if (!isMaxBet && maxBetObject && maxBetObject.activeSelf)
        {
            // Hide immediately if user decreases bet
            maxBetObject.SetActive(false);

            if (maxBetCoroutine != null)
            {
                StopCoroutine(maxBetCoroutine);
                maxBetCoroutine = null;
            }
        }
    }

    private IEnumerator ShowMaxBetIndicator()
    {
        if (maxBetObject) maxBetObject.SetActive(true);

        yield return new WaitForSeconds(maxBetDisplayDuration);

        if (maxBetObject) maxBetObject.SetActive(false);
        maxBetCoroutine = null;
    }

    #endregion

    #region Auto Play

    private void OpenAutoPlayPanel()
    {
        if (autoPlayPanel) autoPlayPanel.SetActive(true);
        AnimatePopupOpen(autoPlayPanelRect);
    }

    private void CloseAutoPlayPanel()
    {
        AnimatePopupClose(autoPlayPanelRect, () => {
            if (autoPlayPanel) autoPlayPanel.SetActive(false);
        });
    }

    private void SelectAutoPlayRounds(int rounds)
    {
        selectedRounds = rounds;

        // Update selected indicators
        foreach (var roundButton in roundButtons)
        {
            if (roundButton.selectedIndicator)
            {
                roundButton.selectedIndicator.SetActive(roundButton.rounds == selectedRounds);
            }
        }

        UpdateAutoPlayButtonText();
    }

    private void UpdateAutoPlayButtonText()
    {
        if (autoPlayStartButtonText)
            autoPlayStartButtonText.text = $"START AUTOPLAY({selectedRounds})";
    }

    private void OnAutoPlayStart()
    {
        CloseAutoPlayPanel();
        gameManager.StartAutoPlay(selectedRounds);
    }

    internal void OnAutoPlayStarted()
    {
        if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(true);
        UpdateAutoPlayCount();

        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(true);

        SetBetControlsEnabled(false);

    }

    internal void OnAutoPlayStopped()
    {
        if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(false);

        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);

        SetBetControlsEnabled(true);
        if (autoPlayOpenButton) autoPlayOpenButton.interactable = true;

    }

    internal void UpdateAutoPlayCount()
    {
        if (autoPlayCountText)
            autoPlayCountText.text = $"{gameManager.autoPlayRemainingRounds}/{gameManager.autoPlayTotalRounds}";
    }

    private void OnTurboToggleChanged(bool isOn)
    {
        if (isOn && quickSpinToggle && quickSpinToggle.isOn)
        {
            quickSpinToggle.isOn = false;
        }

        gameManager.SetSpinSpeed(isOn ? SpinSpeed.Turbo : SpinSpeed.Normal);
    }

    private void OnQuickSpinToggleChanged(bool isOn)
    {
        if (isOn && turboToggle && turboToggle.isOn)
        {
            turboToggle.isOn = false;
        }

        gameManager.SetSpinSpeed(isOn ? SpinSpeed.QuickSpin : SpinSpeed.Normal);
    }

    #endregion

    #region Free Spins

    internal void OnFreeSpinsStarted(int spinsAwarded)
    {

        totalFreeSpinWin = 0;
        totalFreeSpinsAwarded = spinsAwarded;

        // Show free spin start popup
        ShowFreeSpinStartPopup(spinsAwarded);

        // NOTE: Free spin background is NOT enabled here
        // It will be enabled in CloseFreeSpinStartPopup after popup closes
    }

    internal void OnFreeSpinsEnded()
    {

        // Show free spin end popup with total win
        ShowFreeSpinEndPopup(totalFreeSpinWin, totalFreeSpinsAwarded);

        // NOTE: Free spin background is NOT disabled here
        // It will be disabled in CloseFreeSpinEndPopup after popup closes
    }

    /// <summary>
    /// Update free spin count display on game screen
    /// </summary>
    internal void UpdateFreeSpinCount(int remainingSpins)
    {

        if (remainingSpins == 1)
        {
            // Last spin - hide count, show "last spin left"
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(true);
        }
        else if (remainingSpins == 0)
        {
            // After last spin - hide both, show buy free spin
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
            if (buyFreeSpinObject) buyFreeSpinObject.SetActive(true);
        }
        else
        {
            // Normal count display
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(true);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
            if (buyFreeSpinObject) buyFreeSpinObject.SetActive(false);

            SetFreeSpinCountImages(remainingSpins);
        }
    }

    /// <summary>
    /// Set free spin count images on game screen (uses freeSpinNumberSprites)
    /// </summary>
    private void SetFreeSpinCountImages(int count)
    {
        if (freeSpinNumberSprites == null || freeSpinNumberSprites.Length < 10)
        {
            return;
        }

        int tens = count / 10;
        int ones = count % 10;

        if (freeSpinCountTens)
        {
            freeSpinCountTens.sprite = freeSpinNumberSprites[tens];
        }

        if (freeSpinCountOnes)
        {
            freeSpinCountOnes.sprite = freeSpinNumberSprites[ones];
        }

    }

    #endregion

    #region Free Spin Start Popup

    private void ShowFreeSpinStartPopup(int spinsAwarded)
    {
        if (!freeSpinStartPopup || !freeSpinStartPopupRect) return;

        // Set count images
        SetCountImages(spinsAwarded, freeSpinStartCountTens, freeSpinStartCountOnes);

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
            });

    }

    private void CloseFreeSpinStartPopup()
    {
        if (!freeSpinStartPopup) return;

        AnimatePopupClose(freeSpinStartPopupRect, () => {
            freeSpinStartPopup.SetActive(false);

            // Enable free spin background AFTER popup closes
            if (normalSpinBackground) normalSpinBackground.SetActive(false);
            if (freeSpinBackground) freeSpinBackground.SetActive(true);

            // Show intro animation for 2 seconds, then start free spins
            StartCoroutine(ShowFreeSpinIntroAnimation(() => {
                // Initialize free spin count display
                UpdateFreeSpinCount(gameManager.freeSpinsRemaining);

                // Notify GameManager to start first free spin
                gameManager.StartFirstFreeSpin();
            }));
        });
    }

    #endregion

    #region Free Spin End Popup

    private void ShowFreeSpinEndPopup(double totalWin, int totalSpins)
    {
        if (!freeSpinEndPopup || !freeSpinEndPopupRect) return;

        // Set total spin count images
        SetCountImages(totalSpins, freeSpinEndCountTens, freeSpinEndCountOnes);

        // Set win amount display
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
            });

    }

    private void CloseFreeSpinEndPopup()
    {
        if (!freeSpinEndPopup) return;

        AnimatePopupClose(freeSpinEndPopupRect, () => {
            freeSpinEndPopup.SetActive(false);

            // Disable free spin background AFTER popup closes
            if (freeSpinBackground) freeSpinBackground.SetActive(false);
            if (normalSpinBackground) normalSpinBackground.SetActive(true);

            // Hide free spin count display
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);

            // Show free spin intro animation for 2 seconds
            StartCoroutine(ShowFreeSpinIntroAnimation(() => {
                // After intro animation, show buy free spin button
                if (buyFreeSpinObject) buyFreeSpinObject.SetActive(true);
            }));
        });
    }

    #endregion

    #region Free Spin Popup Helpers

    /// <summary>
    /// Set count images for popups (uses numberSprites)
    /// </summary>
    private void SetCountImages(int count, Image tensImage, Image onesImage)
    {
        if (numberSprites == null || numberSprites.Length < 10)
        {
            return;
        }

        int tens = count / 10;
        int ones = count % 10;

        if (tensImage)
        {
            tensImage.sprite = numberSprites[tens];
        }

        if (onesImage)
        {
            onesImage.sprite = numberSprites[ones];
        }

    }

    private void SetWinAmountDisplay(double amount)
    {
        if (winAmountDigits == null || winAmountDigits.Length == 0)
        {
            return;
        }

        if (numberSprites == null || numberSprites.Length < 10)
        {
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
                break;
        }

        winAmountLayoutGroup.spacing = spacing;
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

        // Add to total free spin win if in free spins
        if (gameManager.isInFreeSpins)
        {
            totalFreeSpinWin += winAmount;
        }

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
        if (lowBalancePopup) lowBalancePopup.SetActive(true);
    }

    internal void ShowDisconnectionPopup()
    {
        if (disconnectionPopup) disconnectionPopup.SetActive(true);
    }

    #endregion

    #region Test Functions

    private void TestFreeSpinPopups()
    {

        ShowFreeSpinStartPopup(1);
    }

    /*private IEnumerator TestFreeSpinSequence()
    {
        
        // Step 1: Show start popup with 1 spin
        ShowFreeSpinStartPopup(1);
        
        // Wait 2 seconds to view the popup
        yield return new WaitForSeconds(2f);
        
        // Step 2: Close start popup (triggers background change and intro animation)
        
        // Manually simulate popup close
        if (freeSpinStartPopup) freeSpinStartPopup.SetActive(false);
        
        // Step 3: Switch to free spin background
        if (normalSpinBackground) normalSpinBackground.SetActive(false);
        if (freeSpinBackground) freeSpinBackground.SetActive(true);
        
        // Step 4: Play free spin intro animation
        if (freeSpinIntroAnimation)
        {
            freeSpinIntroAnimation.SetActive(true);
        }
        
        yield return new WaitForSeconds(2f);
        
        if (freeSpinIntroAnimation)
        {
            freeSpinIntroAnimation.SetActive(false);
        }
        
        // Step 5: Simulate 1 spin happening (wait to represent spin duration)
        
        // Show free spin count display (1 spin remaining)
        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false); // Hide count for last spin
        if (lastSpinLeftObject) lastSpinLeftObject.SetActive(true);
        
        yield return new WaitForSeconds(3f); // Simulate spin duration
        
        // Hide last spin indicator
        if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
        
        
        // Step 6: Show end popup with total win (example: 125.50)
        ShowFreeSpinEndPopup(125.50, 1);
        
        // Wait 3 seconds to view the popup
        yield return new WaitForSeconds(3f);
        
        // Step 7: Close end popup (triggers background restore and intro animation)
        
        // Manually simulate popup close
        if (freeSpinEndPopup) freeSpinEndPopup.SetActive(false);
        
        // Step 8: Switch back to normal background
        if (freeSpinBackground) freeSpinBackground.SetActive(false);
        if (normalSpinBackground) normalSpinBackground.SetActive(true);
        
        // Step 9: Play free spin intro animation again (transition back)
        if (freeSpinIntroAnimation)
        {
            freeSpinIntroAnimation.SetActive(true);
        }
        
        yield return new WaitForSeconds(2f);
        
        if (freeSpinIntroAnimation)
        {
            freeSpinIntroAnimation.SetActive(false);
        }
        */


    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (balanceTween != null) balanceTween.Kill();
        if (winTween != null) winTween.Kill();
        DOTween.KillAll();
    }

    #endregion
    #region Connection Popup Management

    /// <summary>
    /// Show reconnection popup when 2 pongs are missed (warning state)
    /// </summary>
    internal void ReconnectionPopup()
    {

        if (reconnectionPopup != null)
        {
            reconnectionPopup.SetActive(true);
        }
    }

    /// <summary>
    /// Show disconnection popup when 5 pongs are missed (disconnect state)
    /// Auto-closes reconnection popup if showing
    /// </summary>
    internal void DisconnectionPopup()
    {

        // Close reconnection popup if it's showing
        if (reconnectionPopup != null)
        {
            reconnectionPopup.SetActive(false);
        }

        if (disconnectionPopup != null)
        {
            disconnectionPopup.SetActive(true);
        }
    }

    /// <summary>
    /// Check and close connection-related popups when connection recovers
    /// Called from SocketIOManager when pong is received after missed pongs
    /// </summary>
    internal void CheckAndClosePopups()
    {

        if (reconnectionPopup != null && reconnectionPopup.activeSelf)
        {
            reconnectionPopup.SetActive(false);
        }

        if (disconnectionPopup != null && disconnectionPopup.activeSelf)
        {
            disconnectionPopup.SetActive(false);
        }
    }

    /// <summary>
    /// Show another device popup when user logs in from another device
    /// </summary>
    internal void AnotherDevicePopup()
    {

        if (anotherDevicePopup != null)
        {
            anotherDevicePopup.SetActive(true);
        }
    }

    /// <summary>
    /// Exit game and return to platform
    /// </summary>
    private void OnExitButtonPressed()
    {

        if (gameManager != null)
        {
            gameManager.ExitGame();
        }
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