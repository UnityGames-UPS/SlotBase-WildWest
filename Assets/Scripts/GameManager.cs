using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private SlotView slotView;

    [Header("Spin Settings")]
    [SerializeField] private float normalSpinDuration = 3.5f;
    [SerializeField] private float turboSpinDuration = 2.0f;
    [SerializeField] private float quickSpinCycleDuration = 0.8f;

    internal GameConfig gameConfig;
    internal PlayerData playerData;
    internal SpinResult lastResult;

    internal GameState currentState;
    internal SpinSpeed currentSpinSpeed;

    internal int currentBetIndex;
    internal double currentBetAmount;

    internal bool isAutoPlaying;
    internal int autoPlayTotalRounds;
    internal int autoPlayRemainingRounds;

    internal bool isInFreeSpins;
    internal int freeSpinsRemaining;
    internal bool waitingForFreeSpinStart;

    // Buy Feature
    internal int buyFeatureBetIndex;      // Bet index selected inside the buy panel
    internal bool isBuyingFeature;        // True while waiting for server response

    internal bool isInitialized;
    internal bool initializationFailed;

    private Coroutine spinCoroutine;
    private bool stopRequested;

    #region Initialization

    private void Start()
    {
        currentState = GameState.Initializing;
        currentSpinSpeed = SpinSpeed.Normal;
        waitingForFreeSpinStart = false;
        isBuyingFeature = false;
        isInitialized = false;
        initializationFailed = false;
    }

    internal void OnInitDataReceived(GameConfig config, PlayerData player, List<List<int>> initialMatrix)
    {
        gameConfig = config;
        playerData = player;
        currentBetIndex = playerData.currentBetIndex;
        UpdateBetAmount();
        buyFeatureBetIndex = currentBetIndex; // initialise buy panel to same bet

        if (initialMatrix != null && slotView != null)
        {
            slotView.SetInitialMatrix(initialMatrix);
        }

        isInitialized = true;
        currentState = GameState.Idle;

        uiManager.OnGameInitialized();
    }

    #endregion

    #region Bet Management

    internal void IncreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;

        currentBetIndex = (currentBetIndex + 1) % gameConfig.availableBets.Count;
        UpdateBetAmount();
        buyFeatureBetIndex = currentBetIndex;
        uiManager.UpdateBetDisplay();
    }

    internal void DecreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;

        currentBetIndex = (currentBetIndex - 1 + gameConfig.availableBets.Count) % gameConfig.availableBets.Count;
        UpdateBetAmount();
        buyFeatureBetIndex = currentBetIndex;
        uiManager.UpdateBetDisplay();
    }

    private void UpdateBetAmount()
    {
        currentBetAmount = gameConfig.availableBets[currentBetIndex];
    }

    #endregion

    #region Spin Control

    internal void RequestSpin()
    {
        if (waitingForFreeSpinStart) return;

        if (currentState != GameState.Idle) return;
        if (!socketManager.isConnected) return;

        if (!isInFreeSpins && playerData.balance < currentBetAmount)
        {
            if (popupManager != null)
            {
                popupManager.ShowInsufficientFundsError();
            }
            return;
        }

        StartSpin();
    }

    internal void RequestStop()
    {
        if (currentState == GameState.Spinning)
        {
            if (isAutoPlaying)
            {
                StopAutoPlay();
            }
            else
            {
                stopRequested = true;
            }
        }
    }

    private void StartSpin()
    {
        currentState = GameState.Spinning;
        stopRequested = false;

        uiManager.OnSpinStarted();

        if (slotView != null)
        {
            slotView.StartSpin();
        }

        socketManager.SendSpinRequest(currentBetIndex, isInFreeSpins);

        if (spinCoroutine != null)
            StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        float spinDuration = GetSpinDuration();
        float elapsed = 0f;

        while (elapsed < spinDuration && !stopRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (lastResult == null)
        {
            yield return null;
        }

        currentState = GameState.Stopping;

        if (slotView != null && lastResult.resultMatrix != null)
        {
            if (currentSpinSpeed == SpinSpeed.QuickSpin || stopRequested)
            {
                slotView.QuickStop(lastResult.resultMatrix);

                float quickStopWaitTime = 0.5f;
                yield return new WaitForSeconds(quickStopWaitTime);

                OnReelsStoppedComplete();
            }
            else
            {
                slotView.StopSpin(lastResult.resultMatrix, OnReelsStoppedComplete);
            }
        }
        else
        {
            OnReelsStoppedComplete();
        }
    }

    private void OnReelsStoppedComplete()
    {
        if (lastResult.winAmount > 0 && lastResult.winLines != null && lastResult.winLines.Count > 0)
        {
            uiManager.DisableControlsDuringWinAnimation();

            slotView.ShowWinLineAnimation(lastResult.winLines, OnWinAnimationComplete);
        }
        else
        {
            OnWinAnimationComplete();
        }
    }

    private void OnWinAnimationComplete()
    {
        uiManager.EnableControlsAfterWinAnimation();

        uiManager.OnSpinStopping(lastResult);

        if (isAutoPlaying || isInFreeSpins)
        {
            StartCoroutine(DelayBeforeNextRound());
        }
        else
        {
            ProcessSpinResult();
        }
    }

    private IEnumerator DelayBeforeNextRound()
    {
        float delayTime = currentSpinSpeed == SpinSpeed.QuickSpin ? 0.3f : 0.5f;
        yield return new WaitForSeconds(delayTime);
        ProcessSpinResult();
    }

    private float GetSpinDuration()
    {
        return currentSpinSpeed switch
        {
            SpinSpeed.Normal => normalSpinDuration,
            SpinSpeed.Turbo => turboSpinDuration,
            SpinSpeed.QuickSpin => quickSpinCycleDuration,
            _ => normalSpinDuration
        };
    }

    internal void OnSpinResultReceived(SpinResult result)
    {
        lastResult = result;


        if (result.winLines != null)
        {
            for (int i = 0; i < result.winLines.Count; i++)
            {
                var line = result.winLines[i];

            }
        }
    }

    private void ProcessSpinResult()
    {
        playerData = lastResult.playerData;

        uiManager.OnSpinCompleted(lastResult);

        // Extract server-authoritative values before nullifying lastResult
        int serverSpinsRemaining = lastResult.serverSpinsRemaining;
        int serverSpinsUsed = lastResult.serverSpinsUsed;
        double serverTotalRoundWin = lastResult.serverTotalRoundWin;
        bool isRoundOver = lastResult.isRoundOver;

        if (isInFreeSpins)
        {
            freeSpinsRemaining = serverSpinsRemaining;
        }

        // Show overlay scatter extra spins popup (display only — server already updated spinsRemaining)
        if (lastResult.overlayScatterData != null && lastResult.overlayScatterData.isTriggered)
        {
            if (isInFreeSpins)
            {
                uiManager.ShowExtraFreeSpinsPopup(lastResult.overlayScatterData.extraSpins);

                // Wait for user to close popup before continuing
                lastResult = null;
                currentState = GameState.Idle;
                return;
            }
        }

        // Check if free spins were just triggered (initial trigger)
        if (lastResult.freeSpinData != null && lastResult.freeSpinData.isTriggered)
        {
            StartFreeSpins(lastResult.freeSpinData.spinsAwarded);
            lastResult = null;
            return;
        }

        lastResult = null;

        if (isAutoPlaying && !isInFreeSpins)
        {
            autoPlayRemainingRounds--;

            uiManager.UpdateAutoPlayCount();

            if (autoPlayRemainingRounds <= 0)
            {
                StopAutoPlay();
                currentState = GameState.Idle;
            }
            else
            {
                currentState = GameState.Idle;
                RequestSpin();
            }
        }
        else if (isInFreeSpins)
        {
            uiManager.UpdateFreeSpinCount(freeSpinsRemaining);

            if (isRoundOver || freeSpinsRemaining <= 0)
            {
                EndFreeSpins(serverTotalRoundWin, serverSpinsUsed);
            }
            else
            {
                currentState = GameState.Idle;
                StartCoroutine(DelayBeforeNextFreeSpin());
            }
        }
        else
        {
            currentState = GameState.Idle;
        }
    }

    #endregion

    #region Spin Speed Control

    internal void SetSpinSpeed(SpinSpeed speed)
    {
        currentSpinSpeed = speed;
    }

    #endregion

    #region Buy Feature

    internal void IncreaseBuyFeatureBet()
    {
        buyFeatureBetIndex = (buyFeatureBetIndex + 1) % gameConfig.availableBets.Count;
        uiManager.UpdateBuyFeatureCostDisplay();
    }

    internal void DecreaseBuyFeatureBet()
    {
        buyFeatureBetIndex = (buyFeatureBetIndex - 1 + gameConfig.availableBets.Count) % gameConfig.availableBets.Count;
        uiManager.UpdateBuyFeatureCostDisplay();
    }

    internal double GetBuyFeatureCost(int betIndex = -1)
    {
        int idx = betIndex < 0 ? buyFeatureBetIndex : betIndex;
        return gameConfig.availableBets[idx] * gameConfig.buyFeatureCostMultiplier;
    }

    internal void RequestBuyFeature()
    {
        if (currentState != GameState.Idle) return;
        if (!socketManager.isConnected) return;
        if (isBuyingFeature) return;

        double cost = GetBuyFeatureCost();
        if (playerData.balance < cost)
        {
            if (popupManager != null)
            {
                popupManager.ShowInsufficientFundsError();
            }
            return;
        }

        isBuyingFeature = true;
        currentState = GameState.Spinning;

        uiManager.OnBuyFeatureConfirmed();
        socketManager.SendBuyFeatureRequest(buyFeatureBetIndex);

        // Reuse the existing spin coroutine / result flow — the server returns a
        // "result" event identical to a normal free-spin trigger, so OnSpinResultReceived
        // will be called automatically and StartFreeSpins will handle the rest.
        if (slotView != null) slotView.StartSpin();

        if (spinCoroutine != null) StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(BuyFeatureRoutine());
    }

    private IEnumerator BuyFeatureRoutine()
    {
        // Wait until the server result arrives (lastResult set by OnSpinResultReceived)
        while (lastResult == null)
        {
            yield return null;
        }

        isBuyingFeature = false;
        currentState = GameState.Stopping;

        if (slotView != null && lastResult.resultMatrix != null)
        {
            slotView.QuickStop(lastResult.resultMatrix);
            yield return new WaitForSeconds(0.5f);
        }

        // No win lines for a buy-feature trigger — go straight to result processing
        OnWinAnimationComplete();
    }

    #endregion

    #region Auto Play

    internal void StartAutoPlay(int rounds)
    {
        if (currentState != GameState.Idle) return;

        isAutoPlaying = true;
        autoPlayTotalRounds = rounds;
        autoPlayRemainingRounds = rounds;

        uiManager.OnAutoPlayStarted();
        RequestSpin();
    }

    internal void StopAutoPlay()
    {
        isAutoPlaying = false;
        autoPlayRemainingRounds = 0;

        uiManager.OnAutoPlayStopped();
    }

    #endregion

    #region Free Spins

    private void StartFreeSpins(int spins)
    {
        isInFreeSpins = true;
        freeSpinsRemaining = spins;
        waitingForFreeSpinStart = true;

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        uiManager.OnFreeSpinsStarted(spins);

        currentState = GameState.Idle;
    }

    internal void StartFirstFreeSpin()
    {
        waitingForFreeSpinStart = false;

        StartCoroutine(DelayBeforeFirstFreeSpin());
    }

    internal void ResumeAfterExtraSpinsPopup()
    {
        // Resume free spin cycle after extra spins popup is closed
        // Bypass intro animation and continue to next spin
        StartCoroutine(DelayBeforeNextFreeSpin());
    }

    private IEnumerator DelayBeforeFirstFreeSpin()
    {
        yield return new WaitForSeconds(0.5f);
        RequestSpin();
    }

    private IEnumerator DelayBeforeNextFreeSpin()
    {
        yield return new WaitForSeconds(0.3f);
        RequestSpin();
    }

    private void EndFreeSpins(double totalRoundWin, int totalSpinsUsed)
    {
        isInFreeSpins = false;
        freeSpinsRemaining = 0;

        // Clear sticky wild overlays and stored state
        if (slotView != null)
        {
            slotView.ClearStickyWilds();
        }

        uiManager.OnFreeSpinsEnded(totalRoundWin, totalSpinsUsed);

        currentState = GameState.Idle;
    }

    #endregion

    #region Connection Events

    internal void OnDisconnected()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        currentState = GameState.Idle;
        
        if (popupManager != null)
        {
            popupManager.ShowDisconnectionPopup();
        }
    }

    internal void ExitGame()
    {
        socketManager.CloseSocket();

    }

    #endregion

    #region Helper Methods

    internal bool CanAffordBet()
    {
        return playerData.balance >= currentBetAmount;
    }

    internal bool IsSpinning()
    {
        return currentState == GameState.Spinning || currentState == GameState.Stopping;
    }

    #endregion
}