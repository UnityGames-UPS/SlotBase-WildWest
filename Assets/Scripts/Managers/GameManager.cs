using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] internal UIManager uiManager;
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
    internal int freeSpinsUsed;
    internal bool waitingForFreeSpinStart;

    // Buy Feature
    internal bool isBuyingFeature;        // True while waiting for server response

    internal bool isInitialized;
    internal bool initializationFailed;

    private Coroutine spinCoroutine;
    private bool stopRequested;
    private bool waitingForSpecialWin;

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
        SetBetIndex((currentBetIndex + 1) % gameConfig.availableBets.Count);
    }

    internal void DecreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;
        SetBetIndex((currentBetIndex - 1 + gameConfig.availableBets.Count) % gameConfig.availableBets.Count);
    }

    internal void SetBetIndex(int index)
    {
        currentBetIndex = index;
        UpdateBetAmount();
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

        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        if (!isInFreeSpins && playerData.balance < totalBet)
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
            else if (!isInFreeSpins)
            {
                stopRequested = true;
                uiManager.DisableSpinButtonDuringStop();
            }
        }
    }

    private void StartSpin()
    {
        if (lastResult != null)
        {
            ProcessSpinResult();
        }

        lastResult = null;
        currentState = GameState.Spinning;
        stopRequested = false;

        // Server handles spin counting - we just display what server sends
        // No client-side freeSpinsUsed++ or freeSpinsRemaining-- here

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

        // Player pressed Stop manually — hold for 0.5s so the reels keep
        // spinning briefly before snapping, giving clear visual feedback.
        if (stopRequested)
        {
            yield return new WaitForSeconds(0.5f);
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

                // Wait for the snap animation to settle before processing result
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
        if (lastResult.freeSpinData != null && lastResult.freeSpinData.isTriggered && !isInFreeSpins)
        {
            StartCoroutine(DelayScatterTriggerResult());
            return;
        }

        if (lastResult.winAmount > 0 && lastResult.winLines != null && lastResult.winLines.Count > 0)
        {
            double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
            double multiplier = totalBet > 0 ? (lastResult.winAmount / totalBet) : 0;

            if (multiplier >= 5)
            {
                uiManager.DisableControlsDuringWinAnimation();
                currentState = GameState.Idle;
            }
            else
            {
                // For normal wins, trigger UI update immediately and enable controls
                uiManager.OnSpinStopping(lastResult);
                uiManager.EnableControlsAfterWinAnimation();
                uiManager.OnSpinCompleted(lastResult);
                currentState = GameState.Idle;
            }

            slotView.ShowWinLineAnimation(lastResult.winLines, OnWinAnimationComplete);
            StartCoroutine(TriggerWinPopupWithDelay(1.5f, lastResult));
        }
        else
        {
            uiManager.OnSpinStopping(lastResult);
            currentState = GameState.Idle;
            OnWinAnimationComplete();
        }
    }

    private IEnumerator TriggerWinPopupWithDelay(float delay, SpinResult result)
    {
        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        double multiplier = totalBet > 0 ? (result.winAmount / totalBet) : 0;
        bool skipScreen = uiManager.skipScreenToggle != null && uiManager.skipScreenToggle.isOn;

        if (multiplier >= 5 && !skipScreen)
        {
            waitingForSpecialWin = true;
        }
        else
        {
            waitingForSpecialWin = false;
        }

        yield return new WaitForSeconds(delay);

        if (lastResult == result)
        {
            uiManager.TriggerBigWinPopupEarly(result, () =>
            {
                waitingForSpecialWin = false;
            });
        }
        else
        {
            waitingForSpecialWin = false;
        }
    }

    private void OnWinAnimationComplete()
    {
        if (lastResult != null)
        {
            double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
            double multiplier = totalBet > 0 ? (lastResult.winAmount / totalBet) : 0;

            // Only update UI here if it wasn't already updated in OnReelsStoppedComplete (multiplier < 5)
            if (multiplier >= 5)
            {
                uiManager.OnSpinStopping(lastResult);
            }
        }

        if (isAutoPlaying || isInFreeSpins)
        {
            StartCoroutine(DelayBeforeNextRound());
        }
        else
        {
            ProcessSpinResult();
        }
    }

    private IEnumerator DelayScatterTriggerResult()
    {
        // Play special 3-scatter trigger sound AFTER all reels have stopped
        AudioManager.Instance?.Play3ScatterHit();

        // Start scatter animations together AFTER all reels have stopped
        // Using 4 loops to match the 6-second delay (4 * 1.5s = 6s)
        slotView.AnimateAllScatters(4);

        // Wait for scatter hit animations to play
        yield return new WaitForSeconds(3.5f);
        ProcessSpinResult();
    }

    private IEnumerator DelayBeforeNextRound()
    {
        float delayTime = currentSpinSpeed == SpinSpeed.QuickSpin ? 0.3f : 0.5f;
        yield return new WaitForSeconds(delayTime);

        // Wait for special win popup using the flag and active state
        while (waitingForSpecialWin || uiManager.IsSpecialWinActive)
        {
            yield return null;
        }

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

        // CRITICAL FIX: Update free spin counter IMMEDIATELY when server response arrives
        // This ensures the display shows the exact server-authoritative count without lag
        if (isInFreeSpins && result.serverSpinsRemaining >= 0)
        {
            freeSpinsRemaining = result.serverSpinsRemaining;
            uiManager.UpdateFreeSpinCount(freeSpinsRemaining);
        }

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

        // Note: freeSpinsRemaining already updated in OnSpinResultReceived
        // Keeping this for safety in case OnSpinResultReceived wasn't called
        if (isInFreeSpins && freeSpinsRemaining != serverSpinsRemaining)
        {
            freeSpinsRemaining = serverSpinsRemaining;
        }

        // Show overlay scatter extra spins popup ONLY if round is NOT over
        // When isRoundOver=true, go directly to end popup regardless of overlay scatter
        if (lastResult.overlayScatterData != null && lastResult.overlayScatterData.isTriggered && !isRoundOver)
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
                currentState = GameState.Idle;
                StopAutoPlay();
            }
            else
            {
                // Before requesting the next spin, verify the player can still afford it.
                // If not, stop autoplay (restores all UI) then show the popup.
                double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
                if (playerData.balance < totalBet)
                {
                    currentState = GameState.Idle;
                    StopAutoPlay();
                    if (popupManager != null) popupManager.ShowInsufficientFundsError();
                }
                else
                {
                    currentState = GameState.Idle;
                    RequestSpin();
                }
            }
        }
        else if (isInFreeSpins)
        {
            // Free spin counter already updated in OnSpinResultReceived
            // No need to update again here

            if (isRoundOver || freeSpinsRemaining <= 0)
            {
                // Always use server-authoritative spinsUsed
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

    internal double GetBuyFeatureCost(int betIndex = -1)
    {
        int idx = betIndex < 0 ? currentBetIndex : betIndex;
        return gameConfig.availableBets[idx] * gameConfig.betMultiplier * gameConfig.buyFeatureCostMultiplier;
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
        socketManager.SendBuyFeatureRequest(currentBetIndex);

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

        // Wait for reels to fully land before starting the scatter sequence.
        // We use a callback flag so we react exactly when StopSpinSequence finishes
        // rather than relying on a fixed time guess.
        if (slotView != null && lastResult.resultMatrix != null)
        {
            bool reelsStopped = false;
            slotView.QuickStop(lastResult.resultMatrix, () => reelsStopped = true);
            while (!reelsStopped)
            {
                yield return null;
            }
        }
      // Update balance display from server result immediately.
        // BuyFeature always has totalWin=0, so OnWinAnimationComplete's multiplier
        // guard (>= 5) would skip OnSpinStopping entirely — call it explicitly here.
        uiManager.OnSpinStopping(lastResult);
        // --- Scatter sound + animation sequence ---
        // Only play if scatter symbols are actually present in the result matrix.
        if (ResultMatrixHasScatter(lastResult.resultMatrix))
        {
            AudioManager.Instance?.Play3ScatterHit();
            if (slotView != null) slotView.AnimateAllScatters(4);
            yield return new WaitForSeconds(3.5f);
        }
        // -----------------------------------------

  

        // No win lines for a buy-feature trigger — go straight to result processing
        OnWinAnimationComplete();
    }

    #endregion

    #region Auto Play

    internal void StartAutoPlay(int rounds)
    {
        if (currentState != GameState.Idle) return;

        // Check balance BEFORE locking any UI — if insufficient, show popup and bail.
        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        if (playerData.balance < totalBet)
        {
            if (popupManager != null) popupManager.ShowInsufficientFundsError();
            return;
        }

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
        freeSpinsUsed = 0;
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

        // Wait for special win popup if it's still active or pending
        while (waitingForSpecialWin || uiManager.IsSpecialWinActive)
        {
            yield return null;
        }

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
        // Note: The disconnection popup is shown by SocketIOManager.OnSocketDisconnected()
        // to avoid duplicates. GameManager only cleans up state here.
    }

    internal void ExitGame()
    {
        socketManager.CloseSocket();

    }

    #endregion

    #region Helper Methods

    internal bool CanAffordBet()
    {
        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        return playerData.balance >= totalBet;
    }

    internal bool IsSpinning()
    {
        return currentState == GameState.Spinning || currentState == GameState.Stopping;
    }

    /// <summary>
    /// Returns true if at least one scatter symbol appears anywhere in the result matrix.
    /// Uses the server-configured scatterSymbolId (default 12) as the reference ID.
    /// </summary>
    private bool ResultMatrixHasScatter(List<List<int>> matrix)
    {
        if (matrix == null) return false;

        int scatterId = gameConfig != null ? gameConfig.scatterSymbolId : 12;

        foreach (var col in matrix)
        {
            if (col == null) continue;
            foreach (int sym in col)
            {
                if (sym == scatterId) return true;
            }
        }

        return false;
    }

    #endregion
}