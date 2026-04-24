using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HistoryController historyController;

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
    [SerializeField] private float minStopPosition = 0.2f;
    [SerializeField] private float maxStopPosition = 0.9f;
    [SerializeField] private float minStopDuration = 0.2f;
    [SerializeField] private float maxStopDuration = 0.5f;
    [SerializeField] private float loadingSpeed = 0.5f;
    [SerializeField] private float introAnimDuration = 2f;

    [Header("Backgrounds")]
    [SerializeField] private GameObject normalSpinBackground;
    [SerializeField] private GameObject freeSpinBackground;

    [Header("Bet Controls")]
    [SerializeField] private TMP_Text betAmountText;
    [SerializeField] private Button betPlusButton;
    [SerializeField] private Button betMinusButton;

    [Header("Max Bet Indicator")]
    [SerializeField] private GameObject maxBetObject;
    [SerializeField] private float maxBetDisplayDuration = 1f;

    [Header("Balance & Win")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text winAmountText;

    [Header("Display Panel - Game Rules & Win")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject gameRuleObject;
    [SerializeField] private GameObject winDisplayObject;
    [SerializeField] private TMP_Text winDisplayText;
    [SerializeField] private float winDisplayDuration = 1f;

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

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform settingsPanelRect;
    [SerializeField] private Button settingsOpenButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button gameQuitButton;
    [SerializeField] private Button historyOpenButton; // New: Opens bet history from settings

    [Header("Settings - Spin Speed Toggles (mirrored in AutoPlay)")]
    [SerializeField] private Toggle settingsTurboToggle;
    [SerializeField] private Toggle settingsQuickSpinToggle;

    [Header("Game Rules Panel")]
    [SerializeField] private GameObject gameRulesPanel;
    [SerializeField] private RectTransform gameRulesPanelRect;
    [SerializeField] private Button gameRulesOpenButton;
    [SerializeField] private Button gameRulesBackButton;
    [SerializeField] private Button gameRulesNextPageButton;
    [SerializeField] private Button gameRulesPrevPageButton;

    [Tooltip("Assign exactly 6 page RectTransforms that live inside the panel.")]
    [SerializeField] private RectTransform[] gameRulePages;
    [SerializeField] private float pageSlideWidth = 800f;
    [SerializeField] private GameObject[] rulePageIndicators;

    [Header("Free Spin Count Display - Game Screen")]
    [SerializeField] private GameObject freeSpinCountContainer;
    [SerializeField] private Image freeSpinCountTens;
    [SerializeField] private Image freeSpinCountOnes;
    [SerializeField] private Sprite[] freeSpinNumberSprites;
    [SerializeField] private GameObject lastSpinLeftObject;
    [SerializeField] private GameObject buyFreeSpinObject;
    [SerializeField] private TMP_Text buyFreeSpinButtonCostText;

    [Header("Buy Free Spin Panel")]
    [SerializeField] private GameObject buyFreeSpinPanel;
    [SerializeField] private RectTransform buyFreeSpinPanelRect;
    [SerializeField] private Button buyFreeSpinOpenButton;        
    [SerializeField] private Button buyFreeSpinCancelButton;  
    [SerializeField] private Button buyFreeSpinConfirmButton;      
    [SerializeField] private Button buyFreeSpinBetPlusButton;      
    [SerializeField] private Button buyFreeSpinBetMinusButton;     
    [SerializeField] private TMP_Text buyFeatureCostText;
    [SerializeField] private Image[] buyFeatureBetDigits;
    [SerializeField] private HorizontalLayoutGroup buyFeatureBetLayoutGroup;
    [SerializeField] private GameObject buyFeatureBetDecimalPoint;
    [SerializeField] private Sprite[] buyFeatureNumberSprites;

    [Header("Free Spin Start Popup")]
    [SerializeField] private GameObject freeSpinStartPopup;
    [SerializeField] private RectTransform freeSpinStartPopupRect;
    [SerializeField] private Image freeSpinStartPopImage;
    [SerializeField] private Button freeSpinStartCloseButton;
    [SerializeField] private Image freeSpinStartCountTens;
    [SerializeField] private Image freeSpinStartCountOnes;
    [SerializeField] private Sprite[] numberSprites;
    [SerializeField] private GameObject freeSpinStartPlusIcon;

    [Header("Free Spin End Popup")]
    [SerializeField] private GameObject freeSpinEndPopup;
    [SerializeField] private RectTransform freeSpinEndPopupRect;
    [SerializeField] private Image freeSpinEndPopImage;
    [SerializeField] private Button freeSpinEndCloseButton;

    [Header("Free Spin End - Win Amount Display")]
    [SerializeField] private Transform winAmountContainer;
    [SerializeField] private HorizontalLayoutGroup winAmountLayoutGroup;
    [SerializeField] private Image[] winAmountDigits;
    [SerializeField] private GameObject decimalPointObject;

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
    private int initialFreeSpins = 0;
    private Coroutine maxBetCoroutine;
    private Coroutine winDisplayCoroutine;

    private int currentRulesPage = 0;
    private bool isPageAnimating = false;
    private bool isSyncingToggles = false;

    #region Initialization

    private void Start()
    {
        SetupButtons();
        SetupAutoPlayPanel();
        SetupSettingsPanel();
        SetupGameRulesPanel();

        InitializeDisplayPanel();

        if (maxBetObject) maxBetObject.SetActive(false);

        InitializeBackgrounds();
        StartCoroutine(LoadingSequence());
    }

    private void InitializeBackgrounds()
    {
        if (normalSpinBackground) normalSpinBackground.SetActive(true);
        if (freeSpinBackground) freeSpinBackground.SetActive(false);
    }

    private void InitializeUI()
    {
        if (spinNormalImage) spinNormalImage.SetActive(true);
        if (spinStopImage) spinStopImage.SetActive(false);

        if (autoPlayPanel) autoPlayPanel.SetActive(false);
        if (autoPlayCountDisplay) autoPlayCountDisplay.SetActive(false);

        if (settingsPanel) settingsPanel.SetActive(false);
        if (gameRulesPanel) gameRulesPanel.SetActive(false);

        if (lowBalancePopup) lowBalancePopup.SetActive(false);
        if (disconnectionPopup) disconnectionPopup.SetActive(false);

        if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
        if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
        if (buyFreeSpinObject) buyFreeSpinObject.SetActive(true);
        if (buyFreeSpinPanel) buyFreeSpinPanel.SetActive(false);

        if (freeSpinStartPopup) freeSpinStartPopup.SetActive(false);
        if (freeSpinEndPopup) freeSpinEndPopup.SetActive(false);

        if (reconnectionPopup) reconnectionPopup.SetActive(false);
        if (anotherDevicePopup) anotherDevicePopup.SetActive(false);

        UpdateAutoPlayButtonText();
    }

    private void InitializeDisplayPanel()
    {
        if (gameRuleObject) gameRuleObject.SetActive(true);
        if (winDisplayObject) winDisplayObject.SetActive(false);
    }

    #endregion

    #region Loading & Intro Sequence

    private IEnumerator LoadingSequence()
    {
        if (loadingScreen) loadingScreen.SetActive(true);
        if (gameScreen) gameScreen.SetActive(false);
        if (introAnimationObject) introAnimationObject.SetActive(false);

        if (loadingBarFill)
        {
            loadingBarFill.fillAmount = 0f;
            UpdateLoadingBarEndMarker(0f);
        }

        int numberOfStops = Random.Range(minStops, maxStops + 1);
        float[] stopPoints = GenerateRandomStopPoints(numberOfStops);

        float currentFill = 0f;
        for (int i = 0; i < stopPoints.Length; i++)
        {
            yield return StartCoroutine(FillLoadingBar(currentFill, stopPoints[i]));
            float pauseDuration = Random.Range(minStopDuration, maxStopDuration);
            yield return new WaitForSeconds(pauseDuration);
            currentFill = stopPoints[i];
        }

        yield return StartCoroutine(FillLoadingBar(currentFill, 1f));

        if (introAnimationObject) introAnimationObject.SetActive(true);
        yield return new WaitForSeconds(introAnimDuration);

        if (loadingScreen) loadingScreen.SetActive(false);
        if (introAnimationObject) introAnimationObject.SetActive(false);

        if (gameScreen) gameScreen.SetActive(true);
        InitializeUI();
    }

    private float[] GenerateRandomStopPoints(int count)
    {
        float[] stops = new float[count];
        for (int i = 0; i < count; i++)
            stops[i] = Random.Range(minStopPosition, maxStopPosition);
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
        RectTransform barRect = loadingBarFill.rectTransform;
        float barWidth = barRect.rect.width;
        float xPosition = (fillAmount - 0.5f) * barWidth;
        loadingBarEndMarker.anchoredPosition = new Vector2(xPosition, loadingBarEndMarker.anchoredPosition.y);
    }

    private IEnumerator ShowFreeSpinIntroAnimation(System.Action onComplete)
    {
        if (freeSpinIntroAnimation) freeSpinIntroAnimation.SetActive(true);
        yield return new WaitForSeconds(freeSpinIntroDuration);
        if (freeSpinIntroAnimation) freeSpinIntroAnimation.SetActive(false);
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

        if (lowBalanceCloseButton)
            lowBalanceCloseButton.onClick.AddListener(() => { if (lowBalancePopup) lowBalancePopup.SetActive(false); });

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
        if(gameQuitButton) gameQuitButton.onClick.AddListener(OnExitButtonPressed);
        if(historyOpenButton) historyOpenButton.onClick.AddListener(OnHistoryButtonPressed); // New: History button

        if (testFreeSpinButton) testFreeSpinButton.onClick.AddListener(TestFreeSpinPopups);

        if (buyFreeSpinOpenButton) buyFreeSpinOpenButton.onClick.AddListener(OpenBuyFreeSpinPanel);
        if (buyFreeSpinCancelButton) buyFreeSpinCancelButton.onClick.AddListener(CloseBuyFreeSpinPanel);
        if (buyFreeSpinConfirmButton) buyFreeSpinConfirmButton.onClick.AddListener(OnBuyFreeSpinConfirmed);
        if (buyFreeSpinBetPlusButton) buyFreeSpinBetPlusButton.onClick.AddListener(OnBuyFeatureBetPlus);
        if (buyFreeSpinBetMinusButton) buyFreeSpinBetMinusButton.onClick.AddListener(OnBuyFeatureBetMinus);
    }

    private void SetupAutoPlayPanel()
    {
        if (turboToggle) turboToggle.onValueChanged.AddListener(OnTurboToggleChanged);
        if (quickSpinToggle) quickSpinToggle.onValueChanged.AddListener(OnQuickSpinToggleChanged);

        RefreshToggleBgAlpha(turboToggle);
        RefreshToggleBgAlpha(quickSpinToggle);

        foreach (var roundButton in roundButtons)
        {
            if (roundButton.button != null)
            {
                int rounds = roundButton.rounds;
                roundButton.button.onClick.AddListener(() => SelectAutoPlayRounds(rounds));
            }
        }
    }

    private void SetupSettingsPanel()
    {
        if (settingsOpenButton) settingsOpenButton.onClick.AddListener(OpenSettingsPanel);
        if (settingsCloseButton) settingsCloseButton.onClick.AddListener(CloseSettingsPanel);

        if (settingsTurboToggle)
        {
            settingsTurboToggle.onValueChanged.AddListener(OnSettingsTurboToggleChanged);
            RefreshToggleBgAlpha(settingsTurboToggle);
        }
        if (settingsQuickSpinToggle)
        {
            settingsQuickSpinToggle.onValueChanged.AddListener(OnSettingsQuickSpinToggleChanged);
            RefreshToggleBgAlpha(settingsQuickSpinToggle);
        }
    }

    private void SetupGameRulesPanel()
    {
        if (gameRulesOpenButton) gameRulesOpenButton.onClick.AddListener(OpenGameRulesPanel);
        if (gameRulesBackButton) gameRulesBackButton.onClick.AddListener(CloseGameRulesPanel);
        if (gameRulesNextPageButton) gameRulesNextPageButton.onClick.AddListener(NextRulesPage);
        if (gameRulesPrevPageButton) gameRulesPrevPageButton.onClick.AddListener(PrevRulesPage);
    }

    #endregion

    #region Game Events

    internal void OnGameInitialized()
    {
        UpdateBetDisplay();
        UpdateBalanceDisplay();
        UpdateWinDisplay(0);
        UpdateBuyFeatureCostDisplay(); // refresh button cost text & show/hide button

        // Show buy button only if feature is enabled
        bool buyEnabled = gameManager.gameConfig.buyFeatureEnabled;
        if (buyFreeSpinObject) buyFreeSpinObject.SetActive(buyEnabled);
    }

    internal void OnSpinStarted()
    {
        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(true);

        SetBetControlsEnabled(false);
        if (autoPlayOpenButton) autoPlayOpenButton.interactable = false;

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
        AnimateBalanceUpdate(result.playerData.balance);

        if (result.winAmount > 0)
        {
            AnimateWinUpdate(result.winAmount);
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
        SetBetControlsEnabled(false);
        if (spinButton) spinButton.interactable = false;
        if (spinNormalImage) spinNormalImage.SetActive(false);
        if (spinStopImage) spinStopImage.SetActive(false);
    }

    internal void EnableControlsAfterWinAnimation()
    {
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
        if (winDisplayCoroutine != null) StopCoroutine(winDisplayCoroutine);
        winDisplayCoroutine = StartCoroutine(ShowWinDisplayCoroutine(winAmount));
    }

    private IEnumerator ShowWinDisplayCoroutine(double winAmount)
    {
        if (gameRuleObject) gameRuleObject.SetActive(false);
        if (winDisplayObject) winDisplayObject.SetActive(true);

        if (winDisplayText) winDisplayText.text = $"WIN {winAmount:F2}";

        yield return new WaitForSeconds(winDisplayDuration);

        if (winDisplayObject) winDisplayObject.SetActive(false);
        if (gameRuleObject) gameRuleObject.SetActive(true);
        winDisplayCoroutine = null;
    }

    #endregion

    #region Spin Button

    private void OnSpinButtonPressed()
    {
        if (gameManager.IsSpinning())
            gameManager.RequestStop();
        else
            gameManager.RequestSpin();
    }

    #endregion

    #region Bet Controls

    internal void UpdateBetDisplay()
    {
        if (betAmountText)
            betAmountText.text = gameManager.currentBetAmount.ToString("F2");
        UpdateBetButtonStates();
        CheckMaxBetIndicator();
        UpdateBuyFeatureCostDisplay(); // keep button cost in sync
    }

    private void UpdateBetButtonStates()
    {
        if (betMinusButton) betMinusButton.interactable = true;
        if (betPlusButton) betPlusButton.interactable = true;
    }

    private void CheckMaxBetIndicator()
    {
        bool isMaxBet = gameManager.currentBetIndex >= gameManager.gameConfig.availableBets.Count - 1;

        if (isMaxBet && maxBetObject && !maxBetObject.activeSelf)
        {
            if (maxBetCoroutine != null) StopCoroutine(maxBetCoroutine);
            maxBetCoroutine = StartCoroutine(ShowMaxBetIndicator());
        }
        else if (!isMaxBet && maxBetObject && maxBetObject.activeSelf)
        {
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

    #region Auto Play Panel

    private void OpenAutoPlayPanel()
    {
        if (settingsPanel && settingsPanel.activeSelf)
            CloseSettingsPanelImmediate();

        if (autoPlayPanel) autoPlayPanel.SetActive(true);
        AnimatePopupOpen(autoPlayPanelRect);
    }

    private void CloseAutoPlayPanel()
    {
        AnimatePopupClose(autoPlayPanelRect, () =>
        {
            if (autoPlayPanel) autoPlayPanel.SetActive(false);
        });
    }

    private void SelectAutoPlayRounds(int rounds)
    {
        selectedRounds = rounds;
        foreach (var roundButton in roundButtons)
        {
            if (roundButton.selectedIndicator)
                roundButton.selectedIndicator.SetActive(roundButton.rounds == selectedRounds);
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
            autoPlayCountText.text = $"{gameManager.autoPlayRemainingRounds}";
    }

    #endregion

    #region Spin Speed Toggle Logic

    private void OnTurboToggleChanged(bool isOn)
    {
        if (isSyncingToggles) return;
        isSyncingToggles = true;

        if (isOn && quickSpinToggle && quickSpinToggle.isOn)
            quickSpinToggle.isOn = false;

        if (settingsTurboToggle && settingsTurboToggle.isOn != isOn)
            settingsTurboToggle.isOn = isOn;
        if (isOn && settingsQuickSpinToggle && settingsQuickSpinToggle.isOn)
            settingsQuickSpinToggle.isOn = false;

        isSyncingToggles = false;

        RefreshAllToggleBgAlpha();
        ApplySpinSpeedFromToggles();
    }

    private void OnQuickSpinToggleChanged(bool isOn)
    {
        if (isSyncingToggles) return;
        isSyncingToggles = true;

        if (isOn && turboToggle && turboToggle.isOn)
            turboToggle.isOn = false;

        if (settingsQuickSpinToggle && settingsQuickSpinToggle.isOn != isOn)
            settingsQuickSpinToggle.isOn = isOn;
        if (isOn && settingsTurboToggle && settingsTurboToggle.isOn)
            settingsTurboToggle.isOn = false;

        isSyncingToggles = false;

        RefreshAllToggleBgAlpha();
        ApplySpinSpeedFromToggles();
    }

    private void OnSettingsTurboToggleChanged(bool isOn)
    {
        if (isSyncingToggles) return;
        isSyncingToggles = true;

        if (isOn && settingsQuickSpinToggle && settingsQuickSpinToggle.isOn)
            settingsQuickSpinToggle.isOn = false;

        if (turboToggle && turboToggle.isOn != isOn)
            turboToggle.isOn = isOn;
        if (isOn && quickSpinToggle && quickSpinToggle.isOn)
            quickSpinToggle.isOn = false;

        isSyncingToggles = false;

        RefreshAllToggleBgAlpha();
        ApplySpinSpeedFromToggles();
    }

    private void OnSettingsQuickSpinToggleChanged(bool isOn)
    {
        if (isSyncingToggles) return;
        isSyncingToggles = true;

        if (isOn && settingsTurboToggle && settingsTurboToggle.isOn)
            settingsTurboToggle.isOn = false;

        if (quickSpinToggle && quickSpinToggle.isOn != isOn)
            quickSpinToggle.isOn = isOn;
        if (isOn && turboToggle && turboToggle.isOn)
            turboToggle.isOn = false;

        isSyncingToggles = false;

        RefreshAllToggleBgAlpha();
        ApplySpinSpeedFromToggles();
    }

    private void ApplySpinSpeedFromToggles()
    {
        bool turboOn = (turboToggle != null && turboToggle.isOn) ||
                           (settingsTurboToggle != null && settingsTurboToggle.isOn);
        bool quickSpinOn = (quickSpinToggle != null && quickSpinToggle.isOn) ||
                           (settingsQuickSpinToggle != null && settingsQuickSpinToggle.isOn);

        if (turboOn)
            gameManager.SetSpinSpeed(SpinSpeed.Turbo);
        else if (quickSpinOn)
            gameManager.SetSpinSpeed(SpinSpeed.QuickSpin);
        else
            gameManager.SetSpinSpeed(SpinSpeed.Normal);
    }

    private void RefreshAllToggleBgAlpha()
    {
        RefreshToggleBgAlpha(turboToggle);
        RefreshToggleBgAlpha(quickSpinToggle);
        RefreshToggleBgAlpha(settingsTurboToggle);
        RefreshToggleBgAlpha(settingsQuickSpinToggle);
    }

    // Reads the background Image directly from Toggle.targetGraphic.
    // Sets alpha to 0 when the toggle is ON so the checkmark is not obscured,
    // and restores full alpha when the toggle is OFF.
    private static void RefreshToggleBgAlpha(Toggle toggle)
    {
        if (toggle == null) return;
        Image bgImage = toggle.targetGraphic as Image;
        if (bgImage == null) return;
        Color c = bgImage.color;
        c.a = toggle.isOn ? 0f : 1f;
        bgImage.color = c;
    }

    #endregion

    #region Settings Panel

    private void OpenSettingsPanel()
    {
        if (autoPlayPanel && autoPlayPanel.activeSelf)
            CloseAutoPlayPanelImmediate();

        if (settingsPanel) settingsPanel.SetActive(true);
        AnimatePopupOpen(settingsPanelRect);
    }

    private void CloseSettingsPanel()
    {
        AnimatePopupClose(settingsPanelRect, () =>
        {
            if (settingsPanel) settingsPanel.SetActive(false);
        });
    }

    private void CloseSettingsPanelImmediate()
    {
        if (settingsPanelRect) settingsPanelRect.localScale = Vector3.one;
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void CloseAutoPlayPanelImmediate()
    {
        if (autoPlayPanelRect) autoPlayPanelRect.localScale = Vector3.one;
        if (autoPlayPanel) autoPlayPanel.SetActive(false);
    }

    private void OnHistoryButtonPressed()
    {
        if (historyController != null)
        {
            // Close settings panel first
            if (settingsPanel && settingsPanel.activeSelf)
            {
                CloseSettingsPanel();
            }

            // Open history panel
            historyController.OpenHistoryPanel();
        }
        else
        {
            Debug.LogWarning("[UIManager] HistoryController not assigned");
        }
    }

    #endregion

    #region Game Rules Panel

    private void OpenGameRulesPanel()
    {
        if (settingsPanel && settingsPanel.activeSelf)
        {
            CloseSettingsPanel();
            StartCoroutine(OpenGameRulesPanelAfterDelay(0.15f));
        }
        else
        {
            ShowGameRulesPanel();
        }
    }

    private IEnumerator OpenGameRulesPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowGameRulesPanel();
    }

    private void ShowGameRulesPanel()
    {
        if (gameRulesPanel == null) return;

        currentRulesPage = 0;
        isPageAnimating = false;

        if (gameRulePages != null)
        {
            for (int i = 0; i < gameRulePages.Length; i++)
            {
                if (gameRulePages[i] == null) continue;
                gameRulePages[i].gameObject.SetActive(true);
                gameRulePages[i].anchoredPosition = new Vector2(i * pageSlideWidth, 0f);
            }
        }

        UpdateRulePageIndicators(currentRulesPage);

        gameRulesPanel.SetActive(true);

        if (gameRulesPanelRect)
        {
            gameRulesPanelRect.anchoredPosition = new Vector2(Screen.width, gameRulesPanelRect.anchoredPosition.y);
            gameRulesPanelRect.DOAnchorPosX(0f, 0.35f).SetEase(Ease.OutCubic);
        }
    }

    private void CloseGameRulesPanel()
    {
        if (gameRulesPanel == null || !gameRulesPanel.activeSelf) return;

        if (gameRulesPanelRect)
        {
            gameRulesPanelRect.DOAnchorPosX(Screen.width, 0.35f)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    gameRulesPanel.SetActive(false);
                    if (gameRulesPanelRect)
                        gameRulesPanelRect.anchoredPosition = new Vector2(0f, gameRulesPanelRect.anchoredPosition.y);
                });
        }
        else
        {
            gameRulesPanel.SetActive(false);
        }
    }

    private void NextRulesPage()
    {
        if (isPageAnimating || gameRulePages == null || gameRulePages.Length == 0) return;
        int next = (currentRulesPage + 1) % gameRulePages.Length;
        SlideToPage(currentRulesPage, next, slideLeft: true);
    }

    private void PrevRulesPage()
    {
        if (isPageAnimating || gameRulePages == null || gameRulePages.Length == 0) return;
        int prev = (currentRulesPage - 1 + gameRulePages.Length) % gameRulePages.Length;
        SlideToPage(currentRulesPage, prev, slideLeft: false);
    }

    private void SlideToPage(int fromIndex, int toIndex, bool slideLeft)
    {
        if (gameRulePages == null) return;
        if (fromIndex < 0 || fromIndex >= gameRulePages.Length) return;
        if (toIndex < 0 || toIndex >= gameRulePages.Length) return;

        RectTransform fromPage = gameRulePages[fromIndex];
        RectTransform toPage = gameRulePages[toIndex];
        if (fromPage == null || toPage == null) return;

        isPageAnimating = true;

        float direction = slideLeft ? 1f : -1f;

        toPage.anchoredPosition = new Vector2(direction * pageSlideWidth, 0f);
        toPage.gameObject.SetActive(true);
        fromPage.anchoredPosition = new Vector2(0f, 0f);

        float slideDuration = 0.35f;

        fromPage.DOAnchorPosX(-direction * pageSlideWidth, slideDuration).SetEase(Ease.InOutCubic);

        toPage.DOAnchorPosX(0f, slideDuration)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() =>
            {
                fromPage.anchoredPosition = new Vector2(direction * pageSlideWidth, 0f);
                currentRulesPage = toIndex;
                isPageAnimating = false;
                UpdateRulePageIndicators(currentRulesPage);
            });
    }

    private void UpdateRulePageIndicators(int activeIndex)
    {
        if (rulePageIndicators == null || rulePageIndicators.Length == 0) return;
        for (int i = 0; i < rulePageIndicators.Length; i++)
        {
            if (rulePageIndicators[i] == null) continue;
            // Enable the first child for the active index, disable for others
            if (rulePageIndicators[i].transform.childCount > 0)
            {
                rulePageIndicators[i].transform.GetChild(0).gameObject.SetActive(i == activeIndex);
            }
        }
    }

    #endregion

    #region Buy Free Spin Panel

    /// <summary>
    /// Updates the cost plain-text inside the buy panel and refreshes the
    /// sprite-digit bet-value display. Also keeps the open-panel button label in sync.
    /// </summary>
    internal void UpdateBuyFeatureCostDisplay()
    {
        if (gameManager.gameConfig == null) return;

        double cost = gameManager.GetBuyFeatureCost();

        // Plain-text label on the open-panel button (outside the panel)
        if (buyFreeSpinButtonCostText)
            buyFreeSpinButtonCostText.text = FormatCostText(cost);

        // Plain-text cost label inside the panel
        if (buyFeatureCostText)
            buyFeatureCostText.text = FormatCostText(cost);

        // Sprite-digit display for the current bet value inside the panel
        double betValue = gameManager.gameConfig.availableBets[gameManager.buyFeatureBetIndex];
        SetBuyFeatureBetDisplay(betValue);
    }

    /// <summary>
    /// Renders the bet value using a sprite-digit Image array with decimal support,
    /// matching the style of the Free Spin Total Win display.
    /// Supports full 6-digit format (e.g. 123.23).
    /// </summary>
    private void SetBuyFeatureBetDisplay(double betValue)
    {
        if (buyFeatureBetDigits == null || buyFeatureBetDigits.Length == 0) return;
        if (buyFeatureNumberSprites == null || buyFeatureNumberSprites.Length < 10) return;

        // Hide all digit images first
        foreach (var digit in buyFeatureBetDigits)
            if (digit) digit.gameObject.SetActive(false);

        if (buyFeatureBetDecimalPoint) buyFeatureBetDecimalPoint.SetActive(false);

        // Determine format: use decimals only if the value has a fractional part
        bool hasDecimal = (betValue % 1) != 0;
        string betStr = hasDecimal ? betValue.ToString("F2") : betValue.ToString("F0");

        // Count total visible objects (digits + decimal point) for spacing adjustment
        int totalObjects = betStr.Replace(".", "").Length;
        if (hasDecimal) totalObjects++;

        AdjustBuyFeatureBetLayoutSpacing(totalObjects);

        // Fill from right to left (same approach as SetWinAmountDisplay)
        int arrayIndex = buyFeatureBetDigits.Length - 1;

        for (int charIndex = betStr.Length - 1; charIndex >= 0 && arrayIndex >= 0; charIndex--)
        {
            char c = betStr[charIndex];
            if (c == '.')
            {
                if (buyFeatureBetDecimalPoint) buyFeatureBetDecimalPoint.SetActive(true);
            }
            else if (char.IsDigit(c))
            {
                int num = int.Parse(c.ToString());
                if (buyFeatureBetDigits[arrayIndex])
                {
                    buyFeatureBetDigits[arrayIndex].gameObject.SetActive(true);
                    buyFeatureBetDigits[arrayIndex].sprite = buyFeatureNumberSprites[num];
                }
                arrayIndex--;
            }
        }
    }

    /// <summary>
    /// Adjusts the HorizontalLayoutGroup spacing for the buy feature bet display
    /// based on how many objects are visible, matching the Free Spin Total Win style.
    /// </summary>
    private void AdjustBuyFeatureBetLayoutSpacing(int objectCount)
    {
        if (buyFeatureBetLayoutGroup == null) return;

        buyFeatureBetLayoutGroup.spacing = objectCount switch
        {
            6 => 0f,
            5 => 0f,
            4 => -40f,
            3 => -80f,
            2 => -120f,
            _ => 0f
        };
    }

    /// <summary>Returns "1,234.00" or "0.50" style string for cost labels.</summary>
    private string FormatCostText(double cost)
    {
        return cost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Increases the buy-panel bet, then mirrors the change to the main bet index
    /// so the outside bet display stays in sync.
    /// </summary>
    private void OnBuyFeatureBetPlus()
    {
        gameManager.IncreaseBuyFeatureBet();
        // Mirror to main bet
        gameManager.currentBetIndex = gameManager.buyFeatureBetIndex;
        gameManager.currentBetAmount = gameManager.gameConfig.availableBets[gameManager.currentBetIndex];
        UpdateBetDisplay(); // refreshes outside betAmountText + calls UpdateBuyFeatureCostDisplay
    }

    /// <summary>
    /// Decreases the buy-panel bet, then mirrors the change to the main bet index
    /// so the outside bet display stays in sync.
    /// </summary>
    private void OnBuyFeatureBetMinus()
    {
        gameManager.DecreaseBuyFeatureBet();
        // Mirror to main bet
        gameManager.currentBetIndex = gameManager.buyFeatureBetIndex;
        gameManager.currentBetAmount = gameManager.gameConfig.availableBets[gameManager.currentBetIndex];
        UpdateBetDisplay(); // refreshes outside betAmountText + calls UpdateBuyFeatureCostDisplay
    }

    private void OpenBuyFreeSpinPanel()
    {
        if (gameManager.currentState != GameState.Idle) return;
        if (!gameManager.gameConfig.buyFeatureEnabled) return;

        // Sync buy panel bet index to current main bet before opening
        gameManager.buyFeatureBetIndex = gameManager.currentBetIndex;
        UpdateBuyFeatureCostDisplay();

        if (buyFreeSpinPanel) buyFreeSpinPanel.SetActive(true);
        AnimatePopupOpen(buyFreeSpinPanelRect);
    }

    private void CloseBuyFreeSpinPanel()
    {
        AnimatePopupClose(buyFreeSpinPanelRect, () =>
        {
            if (buyFreeSpinPanel) buyFreeSpinPanel.SetActive(false);
        });
    }

    private void OnBuyFreeSpinConfirmed()
    {
        // Close panel immediately, then trigger purchase
        if (buyFreeSpinPanelRect) buyFreeSpinPanelRect.localScale = Vector3.one;
        if (buyFreeSpinPanel) buyFreeSpinPanel.SetActive(false);

        gameManager.RequestBuyFeature();
    }

    /// <summary>Called by GameManager after BUY_FEATURE is sent — disable controls.</summary>
    internal void OnBuyFeatureConfirmed()
    {
        SetBetControlsEnabled(false);
        if (spinButton) spinButton.interactable = false;
        if (buyFreeSpinObject) buyFreeSpinObject.SetActive(false);
        if (autoPlayOpenButton) autoPlayOpenButton.interactable = false;
    }

    #endregion

    #region Free Spins

    internal void OnFreeSpinsStarted(int spinsAwarded)
    {
        initialFreeSpins = spinsAwarded;
        totalFreeSpinsAwarded = spinsAwarded;
        if (buyFreeSpinObject) buyFreeSpinObject.SetActive(false);
        ShowFreeSpinStartPopup(spinsAwarded, false);
    }

    internal void OnFreeSpinsEnded(double serverTotalRoundWin, int serverTotalSpinsUsed)
    {
        // Use server spinsUsed if available, otherwise fall back to totalFreeSpinsAwarded
        // (on the last spin, freeSpinState can be null so spinsUsed defaults to 0)
        int totalSpins = serverTotalSpinsUsed > 0 ? serverTotalSpinsUsed : totalFreeSpinsAwarded;
        ShowFreeSpinEndPopup(serverTotalRoundWin, totalSpins);
    }

    internal void UpdateFreeSpinCount(int remainingSpins)
    {
        if (remainingSpins == 1)
        {
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(true);
            if (buyFreeSpinObject) buyFreeSpinObject.SetActive(false);
        }
        else if (remainingSpins == 0)
        {
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
            if (buyFreeSpinObject) buyFreeSpinObject.SetActive(true);
        }
        else
        {
            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(true);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);
            if (buyFreeSpinObject) buyFreeSpinObject.SetActive(false);
            SetFreeSpinCountImages(remainingSpins);
        }
    }

    private void SetFreeSpinCountImages(int count)
    {
        if (freeSpinNumberSprites == null || freeSpinNumberSprites.Length < 10) return;

        // Use alpha to hide tens digit when count is single-digit
        if (freeSpinCountTens)
        {
            if (count >= 10)
            {
                freeSpinCountTens.color = Color.white;
                freeSpinCountTens.sprite = freeSpinNumberSprites[count / 10];
            }
            else
            {
                freeSpinCountTens.color = new Color(1f, 1f, 1f, 0f);
            }
        }
        if (freeSpinCountOnes) freeSpinCountOnes.sprite = freeSpinNumberSprites[count % 10];
    }

    #endregion

    #region Free Spin Start Popup

    private bool isClosingExtraSpins = false;
    private Coroutine hideExtraSpinsCoroutine;
    private bool pausedForExtraSpins = false;

    internal void ShowExtraFreeSpinsPopup(int extraSpins)
    {
        isClosingExtraSpins = true;
        pausedForExtraSpins = true;
        totalFreeSpinsAwarded += extraSpins;
        ShowFreeSpinStartPopup(extraSpins, true);
    }

    private void ShowFreeSpinStartPopup(int spinsAwarded, bool isExtraSpins = false)
    {
        if (!freeSpinStartPopup || !freeSpinStartPopupRect) return;

        if (!isExtraSpins)
        {
            isClosingExtraSpins = false;
            pausedForExtraSpins = false;
        }

        if (hideExtraSpinsCoroutine != null)
        {
            StopCoroutine(hideExtraSpinsCoroutine);
            hideExtraSpinsCoroutine = null;
        }

        // Activate popup first so child modifications happen in active hierarchy
        freeSpinStartPopup.SetActive(true);

        // Set plus icon visibility (show "+" for extra spins only)
        if (freeSpinStartPlusIcon) freeSpinStartPlusIcon.SetActive(isExtraSpins);

        // Set count digit images
        SetCountImages(spinsAwarded, freeSpinStartCountTens, freeSpinStartCountOnes);

        freeSpinStartPopupRect.anchoredPosition = new Vector2(freeSpinStartPopupRect.anchoredPosition.x, popupAppearY);
        freeSpinStartPopupRect.localScale = Vector3.one;
        freeSpinStartPopupRect.DOAnchorPosY(popupFinalY, popupDropDuration).SetEase(Ease.OutBounce);

        // Extra spins now require manual close - no auto-hide
        // User must click the close button
    }

    private IEnumerator HideExtraFreeSpinsPopup()
    {
        yield return new WaitForSeconds(2.0f);
        if (freeSpinStartPopup && freeSpinStartPopup.activeSelf && isClosingExtraSpins)
        {
            CloseFreeSpinStartPopup();
        }
    }

    private void CloseFreeSpinStartPopup()
    {
        if (!freeSpinStartPopup) return;

        bool wasExtraSpins = isClosingExtraSpins;
        isClosingExtraSpins = false;

        AnimatePopupClose(freeSpinStartPopupRect, () =>
        {
            freeSpinStartPopup.SetActive(false);

            if (wasExtraSpins)
            {
                // Resume spinning after extra spins popup closed - bypass intro animation
                pausedForExtraSpins = false;
                if (gameManager.isInFreeSpins)
                {
                    // Update the free spin count display to show new total
                    UpdateFreeSpinCount(gameManager.freeSpinsRemaining);
                    gameManager.ResumeAfterExtraSpinsPopup();
                }
            }
            else
            {
                if (normalSpinBackground) normalSpinBackground.SetActive(false);
                if (freeSpinBackground) freeSpinBackground.SetActive(true);

                StartCoroutine(ShowFreeSpinIntroAnimation(() =>
                {
                    UpdateFreeSpinCount(gameManager.freeSpinsRemaining);
                    gameManager.StartFirstFreeSpin();
                }));
            }
        });
    }

    #endregion

    #region Free Spin End Popup

    private void ShowFreeSpinEndPopup(double totalWin, int totalSpins)
    {
        if (!freeSpinEndPopup || !freeSpinEndPopupRect) return;

        SetCountImages(totalSpins, freeSpinEndCountTens, freeSpinEndCountOnes);
        SetWinAmountDisplay(totalWin);

        freeSpinEndPopup.SetActive(true);
        freeSpinEndPopupRect.anchoredPosition = new Vector2(freeSpinEndPopupRect.anchoredPosition.x, popupAppearY);
        freeSpinEndPopupRect.localScale = Vector3.one;
        freeSpinEndPopupRect.DOAnchorPosY(popupFinalY, popupDropDuration).SetEase(Ease.OutBounce);
    }

    private void CloseFreeSpinEndPopup()
    {
        if (!freeSpinEndPopup) return;

        // Reset free spin tracking
        initialFreeSpins = 0;
        totalFreeSpinsAwarded = 0;

        AnimatePopupClose(freeSpinEndPopupRect, () =>
        {
            freeSpinEndPopup.SetActive(false);
            if (freeSpinBackground) freeSpinBackground.SetActive(false);
            if (normalSpinBackground) normalSpinBackground.SetActive(true);

            if (freeSpinCountContainer) freeSpinCountContainer.SetActive(false);
            if (lastSpinLeftObject) lastSpinLeftObject.SetActive(false);

            StartCoroutine(ShowFreeSpinIntroAnimation(() =>
            {
                if (buyFreeSpinObject) buyFreeSpinObject.SetActive(true);
            }));
        });
    }

    #endregion



    #region Free Spin Popup Helpers

    private void SetCountImages(int count, Image tensImage, Image onesImage)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        // Properly handle tens digit visibility
        if (tensImage)
        {
            if (count >= 10)
            {
                tensImage.gameObject.SetActive(true);
                tensImage.color = Color.white;
                tensImage.sprite = numberSprites[count / 10];
            }
            else
            {
                // Disable completely for single digits
                tensImage.gameObject.SetActive(false);
                tensImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        if (onesImage)
        {
            onesImage.gameObject.SetActive(true);
            onesImage.color = Color.white;
            onesImage.sprite = numberSprites[count % 10];
        }
    }

    private void SetWinAmountDisplay(double amount)
    {
        if (winAmountDigits == null || winAmountDigits.Length == 0) return;
        if (numberSprites == null || numberSprites.Length < 10) return;

        foreach (var digit in winAmountDigits)
            if (digit) digit.gameObject.SetActive(false);

        if (decimalPointObject) decimalPointObject.SetActive(false);

        bool hasDecimal = (amount % 1) != 0;
        string amountStr = hasDecimal ? amount.ToString("F2") : amount.ToString("F0");

        int totalObjects = amountStr.Replace(".", "").Length;
        if (hasDecimal) totalObjects++;

        AdjustWinAmountLayoutSpacing(totalObjects);

        int arrayIndex = winAmountDigits.Length - 1;

        for (int charIndex = amountStr.Length - 1; charIndex >= 0 && arrayIndex >= 0; charIndex--)
        {
            char c = amountStr[charIndex];
            if (c == '.')
            {
                if (decimalPointObject) decimalPointObject.SetActive(true);
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

    private void AdjustWinAmountLayoutSpacing(int objectCount)
    {
        if (winAmountLayoutGroup == null) return;

        winAmountLayoutGroup.spacing = objectCount switch
        {
            6 => 0f,
            5 => -40f,
            4 => -90f,
            3 => -140f,
            _ => 0f
        };
    }
  
    #endregion

        #region Popup Animations (Generic)

    private void AnimatePopupOpen(RectTransform popupRect)
    {
        if (!popupRect) return;
        popupRect.localScale = Vector3.zero;
        popupRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void AnimatePopupClose(RectTransform popupRect, System.Action onComplete)
    {
        if (!popupRect) return;

        Sequence closeSeq = DOTween.Sequence();
        closeSeq.Append(popupRect.DOScale(1.1f, 0.1f));
        closeSeq.Append(popupRect.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        closeSeq.OnComplete(() =>
        {
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
            x => { if (balanceText != null) balanceText.text = x.ToString("F2"); },
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
            ).SetEase(Ease.OutCubic)
             .OnComplete(() => UpdateWinDisplay(winAmount));
        }
        else
        {
            UpdateWinDisplay(0);
        }
    }

    #endregion

    #region Helper Methods

    internal bool IsPausedForExtraSpins()
    {
        return pausedForExtraSpins;
    }

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
        ShowFreeSpinStartPopup(1, false);
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

    #region Connection Popup Management

    internal void ReconnectionPopup()
    {
        if (reconnectionPopup != null) reconnectionPopup.SetActive(true);
    }

    internal void DisconnectionPopup()
    {
        if (reconnectionPopup != null) reconnectionPopup.SetActive(false);
        if (disconnectionPopup != null) disconnectionPopup.SetActive(true);
    }

    internal void CheckAndClosePopups()
    {
        if (reconnectionPopup != null && reconnectionPopup.activeSelf) reconnectionPopup.SetActive(false);
        if (disconnectionPopup != null && disconnectionPopup.activeSelf) disconnectionPopup.SetActive(false);
    }

    internal void AnotherDevicePopup()
    {
        if (anotherDevicePopup != null) anotherDevicePopup.SetActive(true);
    }

    private void OnExitButtonPressed()
    {
        if (gameManager != null) gameManager.ExitGame();
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