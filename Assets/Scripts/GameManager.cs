using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlotGame
{
    /// <summary>
    /// Main game controller - handles all game logic
    /// Controls: Slots, Bets, Auto Play, Free Spins
    /// FIXED: AutoPlay counter hiding issue
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SocketIOManager socketManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SlotView slotView;

        [Header("Spin Settings")]
        [SerializeField] private float normalSpinDuration = 3.5f;
        [SerializeField] private float turboSpinDuration = 2.0f;
        [SerializeField] private float quickSpinCycleDuration = 1.0f;

        // Game data
        internal GameConfig gameConfig;
        internal PlayerData playerData;
        internal SpinResult lastResult;

        // Game state
        internal GameState currentState;
        internal SpinSpeed currentSpinSpeed;

        // Bet management
        internal int currentBetIndex;
        internal double currentBetAmount;

        // Auto play
        internal bool isAutoPlaying;
        internal int autoPlayTotalRounds;
        internal int autoPlayRemainingRounds;

        // Free spins
        internal bool isInFreeSpins;
        internal int freeSpinsRemaining;

        // Spin control
        private Coroutine spinCoroutine;
        private bool stopRequested;

        #region Initialization

        private void Start()
        {
            currentState = GameState.Initializing;
            currentSpinSpeed = SpinSpeed.Normal;
        }

        internal void OnInitDataReceived(InitData initData)
        {
            Debug.Log("[GameManager] Init data received");

            gameConfig = initData.gameConfig;
            playerData = initData.playerData;
            currentBetIndex = playerData.currentBetIndex;
            UpdateBetAmount();

            // Set initial slot display
            if (initData.initialMatrix != null && slotView != null)
            {
                slotView.SetInitialMatrix(initData.initialMatrix);
            }

            currentState = GameState.Idle;
            
            // Update UI
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

            // Update UI
            uiManager.OnSpinStarted();

            // Start visual slot spin
            if (slotView != null)
            {
                slotView.StartSpin();
            }

            // Send request to server
            socketManager.SendSpinRequest(currentBetIndex, isInFreeSpins);

            // Start spin animation
            if (spinCoroutine != null)
                StopCoroutine(spinCoroutine);
            spinCoroutine = StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            float spinDuration = GetSpinDuration();
            float elapsed = 0f;

            // Spin until duration complete or stop requested
            while (elapsed < spinDuration && !stopRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Wait for result if not received yet
            while (lastResult == null)
            {
                yield return null;
            }

            // Stop spinning
            currentState = GameState.Stopping;
            
            // Stop slot visuals based on speed mode
            if (slotView != null && lastResult.resultMatrix != null)
            {
                if (currentSpinSpeed == SpinSpeed.QuickSpin || stopRequested)
                {
                    // Instant stop
                    slotView.QuickStop(lastResult.resultMatrix);
                    yield return new WaitForSeconds(0.3f);
                }
                else
                {
                    // Normal/Turbo - sequential stop
                    slotView.StopSpin(lastResult.resultMatrix);
                    // Wait for all reels to stop (5 reels × 0.2s delay + 0.5s elastic = 1.5s)
                    yield return new WaitForSeconds(1.5f);
                }
            }

            // Let UI handle result display
            uiManager.OnSpinStopping(lastResult);

            // Process result
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
            // Update player data
            playerData = lastResult.playerData;
            
            // Update UI with result
            uiManager.OnSpinCompleted(lastResult);

            // Check for free spins FIRST
            if (lastResult.freeSpinData != null && lastResult.freeSpinData.isTriggered)
            {
                StartFreeSpins(lastResult.freeSpinData.spinsAwarded);
                // Clear result and return - free spins will handle next spin
                lastResult = null;
                return;
            }

            // Clear result
            lastResult = null;

            // Handle auto play continuation
            if (isAutoPlaying && !isInFreeSpins)
            {
                // CRITICAL FIX: Decrement AFTER updating UI, BEFORE checking if should stop
                autoPlayRemainingRounds--;
                
                // Update counter display
                uiManager.UpdateAutoPlayCount();

                if (autoPlayRemainingRounds <= 0)
                {
                    // Stop auto play - counter will be hidden in StopAutoPlay()
                    StopAutoPlay();
                    currentState = GameState.Idle;
                }
                else
                {
                    // Continue auto play
                    currentState = GameState.Idle;
                    StartCoroutine(DelayedAutoSpin());
                }
            }
            else if (isInFreeSpins)
            {
                // Handle free spin continuation
                freeSpinsRemaining--;
                
                // Update free spin counter
                uiManager.UpdateFreeSpinCount();

                if (freeSpinsRemaining <= 0)
                {
                    EndFreeSpins();
                }
                else
                {
                    // Continue free spins
                    currentState = GameState.Idle;
                    StartCoroutine(DelayedAutoSpin());
                }
            }
            else
            {
                // Normal spin complete
                currentState = GameState.Idle;
            }
        }

        private IEnumerator DelayedAutoSpin()
        {
            yield return new WaitForSeconds(0.5f);
            RequestSpin();
        }

        #endregion

        #region Auto Play

        internal void StartAutoPlay(int rounds, SpinSpeed speed)
        {
            if (currentState != GameState.Idle) return;

            isAutoPlaying = true;
            autoPlayTotalRounds = rounds;
            autoPlayRemainingRounds = rounds;
            currentSpinSpeed = speed;

            Debug.Log($"[GameManager] Auto play started: {rounds} rounds, {speed} speed");

            uiManager.OnAutoPlayStarted();
            RequestSpin();
        }

        internal void StopAutoPlay()
        {
            Debug.Log("[GameManager] Stopping auto play");
            
            isAutoPlaying = false;
            autoPlayRemainingRounds = 0;
            currentSpinSpeed = SpinSpeed.Normal;
            
            uiManager.OnAutoPlayStopped();
        }

        #endregion

        #region Free Spins

        private void StartFreeSpins(int spins)
        {
            Debug.Log($"[GameManager] Starting {spins} free spins");
            
            isInFreeSpins = true;
            freeSpinsRemaining = spins;

            // Stop auto play if running
            if (isAutoPlaying)
            {
                StopAutoPlay();
            }

            uiManager.OnFreeSpinsStarted(spins);

            // Set state back to idle for free spin to start
            currentState = GameState.Idle;

            // Auto start first free spin after delay
            StartCoroutine(DelayedAutoSpin());
        }

        private void EndFreeSpins()
        {
            Debug.Log("[GameManager] Free spins ended");
            
            isInFreeSpins = false;
            freeSpinsRemaining = 0;
            
            uiManager.OnFreeSpinsEnded();
            
            currentState = GameState.Idle;
        }

        #endregion

        #region Connection Events

        internal void OnDisconnected()
        {
            Debug.LogWarning("[GameManager] Disconnected!");
            
            // Stop any active spins/auto play
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
}
