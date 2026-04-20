using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private UIManager uiManager;
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
    internal bool waitingForFreeSpinStart; // Flag for popup handling

    private Coroutine spinCoroutine;
    private bool stopRequested;

    #region Initialization

    private void Start()
    {
        currentState = GameState.Initializing;
        currentSpinSpeed = SpinSpeed.Normal;
        waitingForFreeSpinStart = false;
    }

    internal void OnInitDataReceived(InitData initData)
    {
        Debug.Log("[GameManager] Init data received");

        gameConfig = initData.gameConfig;
        playerData = initData.playerData;
        currentBetIndex = playerData.currentBetIndex;
        UpdateBetAmount();

        if (initData.initialMatrix != null && slotView != null)
        {
            slotView.SetInitialMatrix(initData.initialMatrix);
        }

        currentState = GameState.Idle;
        
        uiManager.OnGameInitialized();
    }

    #endregion

    #region Bet Management

    internal void IncreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;

        if (currentBetIndex < gameConfig.availableBets.Count - 1)
        {
            currentBetIndex++;
            UpdateBetAmount();
            uiManager.UpdateBetDisplay();
        }
    }

    internal void DecreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;

        if (currentBetIndex > 0)
        {
            currentBetIndex--;
            UpdateBetAmount();
            uiManager.UpdateBetDisplay();
        }
    }

    private void UpdateBetAmount()
    {
        currentBetAmount = gameConfig.availableBets[currentBetIndex];
    }

    #endregion

    #region Spin Control

    internal void RequestSpin()
    {
        // Don't allow spin if waiting for free spin popup to close
        if (waitingForFreeSpinStart) 
        {
            Debug.Log("[GameManager] Waiting for free spin popup to close");
            return;
        }

        if (currentState != GameState.Idle) return;
        if (!socketManager.isConnected && !socketManager.useDemoMode) return;

        // Check balance
        if (!isInFreeSpins && playerData.balance < currentBetAmount)
        {
            uiManager.ShowLowBalancePopup();
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
            // Disable controls during win animation
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
        // Re-enable controls after win animation
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
        Debug.Log($"[GameManager] Result received - Win: {result.winAmount:F2}");
        lastResult = result;
    }

    private void ProcessSpinResult()
    {
        playerData = lastResult.playerData;
        
        uiManager.OnSpinCompleted(lastResult);

        // Check if free spins are triggered
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
            freeSpinsRemaining--;
            
            // Update free spin count display on game screen
            uiManager.UpdateFreeSpinCount(freeSpinsRemaining);

            if (freeSpinsRemaining <= 0)
            {
                EndFreeSpins();
            }
            else
            {
                currentState = GameState.Idle;
                // Continue free spins automatically
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
        Debug.Log($"[GameManager] Spin speed changed to: {speed}");
    }

    #endregion

    #region Auto Play

    internal void StartAutoPlay(int rounds)
    {
        if (currentState != GameState.Idle) return;

        isAutoPlaying = true;
        autoPlayTotalRounds = rounds;
        autoPlayRemainingRounds = rounds;

        Debug.Log($"[GameManager] Auto play started: {rounds} rounds, {currentSpinSpeed} speed");

        uiManager.OnAutoPlayStarted();
        RequestSpin();
    }

    internal void StopAutoPlay()
    {
        Debug.Log("[GameManager] Stopping auto play");
        
        isAutoPlaying = false;
        autoPlayRemainingRounds = 0;
        
        uiManager.OnAutoPlayStopped();
    }

    #endregion

    #region Free Spins

    private void StartFreeSpins(int spins)
    {
        Debug.Log($"[GameManager] Starting {spins} free spins");
        
        isInFreeSpins = true;
        freeSpinsRemaining = spins;
        waitingForFreeSpinStart = true; // Block spins until popup is closed

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        // UIManager will show the popup
        uiManager.OnFreeSpinsStarted(spins);

        currentState = GameState.Idle;

        // Don't auto-start first spin - wait for popup close
        // The UIManager's close button will call StartFirstFreeSpin()
    }

    /// <summary>
    /// Called by UIManager when free spin start popup is closed AND intro animation finishes
    /// </summary>
    internal void StartFirstFreeSpin()
    {
        Debug.Log("[GameManager] Starting first free spin after popup close and intro animation");
        waitingForFreeSpinStart = false;
        
        StartCoroutine(DelayBeforeFirstFreeSpin());
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

    private void EndFreeSpins()
    {
        Debug.Log("[GameManager] Free spins ended");
        
        isInFreeSpins = false;
        freeSpinsRemaining = 0;
        
        // UIManager will show end popup
        uiManager.OnFreeSpinsEnded();
        
        currentState = GameState.Idle;
    }

    #endregion

    #region Connection Events

    internal void OnDisconnected()
    {
        Debug.LogWarning("[GameManager] Disconnected!");
        
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
        uiManager.ShowDisconnectionPopup();
    }

    internal void ExitGame()
    {
        Debug.Log("[GameManager] Exiting game");
        socketManager.CloseSocket();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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