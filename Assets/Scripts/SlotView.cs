using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

    public class SlotView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("Symbol Sprites")]
        [SerializeField] private Sprite[] symbolSprites; 

        [Header("Reel Containers")]
        [SerializeField] private Transform[] reelTransforms; 
        
        [Header("Reel Images - 16 images per reel")]
        [SerializeField] private List<ReelImages> reelImagesList; 

        [Header("Spin Settings")]
        [SerializeField] private float symbolHeight = 100f; 
        [SerializeField] private float spinSpeed = 0.05f; 
        [SerializeField] private float reelStartStagger = 0.08f; // Stagger between reel starts
        [SerializeField] private float reelStopStagger = 0.12f; // Stagger between reel stops (same cycle)

        [Header("Animation Settings - Casino Style")]
        [SerializeField] private float anticipationUpDistance = 30f; // How far up reels go before dropping
        [SerializeField] private float anticipationUpDuration = 0.15f;
        [SerializeField] private float dropDownDistance = 15f; // Brief drop before settling
        [SerializeField] private float dropDownDuration = 0.12f;
        [SerializeField] private float settleBounceDuration = 0.18f;
        
        [Header("Stop Animation Settings")]
        [SerializeField] private float stopOvershootDistance = 50f; // How far past center on stop
        [SerializeField] private float stopOvershootDuration = 0.15f;
        [SerializeField] private float stopBounceBackDistance = 15f; // Bounce back distance
        [SerializeField] private float stopBounceBackDuration = 0.25f;
        [SerializeField] private float stopSettleDuration = 0.35f;

        [Header("Quick Spin Settings")]
        [SerializeField] private float quickStopStagger = 0.06f; // Faster stagger for quick stop
        [SerializeField] private float quickStopOvershoot = 20f; // Smaller overshoot
        [SerializeField] private float quickStopDuration = 0.2f; // Total quick stop time per reel
        [SerializeField] private int minSpinCyclesBeforeStop = 3; // Minimum full cycles before allowing stop

        [Header("Scatter Anticipation Settings")]
        [SerializeField] private int scatterSymbolId = 4; // ID of scatter symbol
        [SerializeField] private float anticipationExtraSpins = 3f; // Extra cycles for 5th reel
        [SerializeField] private float anticipationSpeedMultiplier = 1.5f; // Speed up factor for 5th reel

        [Header("Win Animation Settings")]
        [SerializeField] private float winPopScale = 1.3f; 
        [SerializeField] private float winPopDuration = 0.4f; 
        [SerializeField] private int winPopRepeat = 3; 


        private float middlePosition = 0f;
        private float cycleDistance;
        

        private List<Tween> spinTweens = new List<Tween>();
        private List<Tween> winTweens = new List<Tween>(); 
        private List<int> reelCycleCount = new List<int>(); // Track cycles per reel
        

        internal List<List<int>> currentDisplayMatrix;

        private bool isSpinning;
        private bool scatterAnticipationActive = false;

        #region Initialization

        private void Start()
        {
            InitializeReels();
        }

        private void InitializeReels()
        {
            cycleDistance = symbolHeight;
            
            middlePosition = 0f;

            currentDisplayMatrix = new List<List<int>>();
            for (int col = 0; col < 5; col++)
            {
                currentDisplayMatrix.Add(new List<int> { 0, 0, 0, 0 });
                reelCycleCount.Add(0);
            }

            Debug.Log($"[SlotView] Initialized - Symbol Height: {symbolHeight}, Cycle Distance: {cycleDistance}");
        }

        internal void SetInitialMatrix(List<List<int>> matrix)
        {
            if (matrix == null || matrix.Count != 5)
            {
                Debug.LogWarning("[SlotView] Invalid initial matrix!");
                return;
            }

            for (int col = 0; col < 5; col++)
            {
                if (matrix[col].Count != 4)
                {
                    Debug.LogWarning($"[SlotView] Column {col} doesn't have 4 rows!");
                    return;
                }
            }

            currentDisplayMatrix = matrix;

            for (int col = 0; col < 5; col++)
            {
                SetReelSymbols(col, matrix[col], true);
            }

            Debug.Log("[SlotView] Initial matrix set");
            LogMatrix("INIT", matrix);
        }

        #endregion

        #region Symbol Display


        private void SetReelSymbols(int columnIndex, List<int> visibleSymbolIds, bool isInitial = false)
        {
            if (columnIndex >= reelImagesList.Count) return;

            var reel = reelImagesList[columnIndex];
            
            if (reel.images == null || reel.images.Count != 16)
            {
                Debug.LogError($"[SlotView] Reel {columnIndex} doesn't have 16 images!");
                return;
            }

            for (int row = 0; row < 4; row++)
            {
                int imageIndex = 6 + row; // Middle positions
                int symbolId = visibleSymbolIds[row];
                reel.images[imageIndex].sprite = GetSymbolSprite(symbolId);
            }

            for (int i = 0; i < 6; i++)
            {
                reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 16));
            }
            
            for (int i = 10; i < 16; i++)
            {
                reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 16));
            }

            if (isInitial && reelTransforms[columnIndex] != null)
            {
                reelTransforms[columnIndex].localPosition = new Vector3(
                    reelTransforms[columnIndex].localPosition.x, 
                    middlePosition, 
                    0
                );
            }
        }

        private Sprite GetSymbolSprite(int symbolId)
        {
            if (symbolId < 0 || symbolId >= symbolSprites.Length)
            {
                Debug.LogWarning($"[SlotView] Invalid symbol ID: {symbolId}, using default");
                return symbolSprites[0];
            }
            return symbolSprites[symbolId];
        }

        #endregion

        #region Spin Animation - SMOOTH SPINNING (OLD) + SCATTER ANTICIPATION (NEW)

        internal void StartSpin()
        {
            if (isSpinning)
            {
                Debug.LogWarning("[SlotView] Already spinning!");
                return;
            }

            isSpinning = true;
            scatterAnticipationActive = false;
            KillAllTweens();

            // Reset cycle counters
            for (int i = 0; i < reelCycleCount.Count; i++)
            {
                reelCycleCount[i] = 0;
            }

            // Start each reel with stagger delay
            for (int col = 0; col < 5; col++)
            {
                StartReelCycleWithDelay(col, col * reelStartStagger); 
            }

            Debug.Log("[SlotView] All reels spinning with smooth staggered start");
        }

        private void StartReelCycleWithDelay(int columnIndex, float delay)
        {
            if (columnIndex >= reelTransforms.Length) return;

            Transform slotTransform = reelTransforms[columnIndex];

            Sequence startSequence = DOTween.Sequence();

            // Wait for stagger delay
            if (delay > 0)
            {
                startSequence.AppendInterval(delay);
            }

            // Casino-style anticipation: Go up first
            startSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition + anticipationUpDistance, anticipationUpDuration)
                    .SetEase(Ease.OutCubic)
            );

            // Drop down with momentum
            startSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - dropDownDistance, dropDownDuration)
                    .SetEase(Ease.InCubic)
            );

            // Settle with subtle bounce
            startSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, settleBounceDuration)
                    .SetEase(Ease.OutBounce)
            );

            startSequence.OnComplete(() => {
                if (isSpinning)
                {
                    StartReelCycle(columnIndex);
                }
            });

            startSequence.Play();
            
            if (spinTweens.Count <= columnIndex)
                spinTweens.Add(startSequence);
            else
                spinTweens[columnIndex] = startSequence;
        }

        // 🔥 MERGED: Smooth cycling from OLD + Speed control from NEW
        private void StartReelCycle(int columnIndex)
        {
            if (columnIndex >= reelTransforms.Length) return;
            if (!isSpinning) return;

            Transform slotTransform = reelTransforms[columnIndex];

            // Reset to middle position for clean loop (FROM OLD VERSION)
            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

            // Apply anticipation speed boost for 5th reel if scatters detected (FROM NEW VERSION)
            float currentSpeed = spinSpeed;
            if (scatterAnticipationActive && columnIndex == 4)
            {
                currentSpeed = spinSpeed / anticipationSpeedMultiplier;
            }

            // Move down one cycle distance
            Sequence cycleSequence = DOTween.Sequence();
            
            cycleSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - cycleDistance, currentSpeed)
                    .SetEase(Ease.Linear)
            );
            
            cycleSequence.OnComplete(() => {
                if (isSpinning)
                {
                    // 🔥 CRITICAL: Cycle sprites physically (FROM OLD VERSION - THIS WAS MISSING IN NEW!)
                    CycleReelSymbols(columnIndex);
                    
                    // Reset position for next cycle
                    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);
                    
                    // Increment cycle count (FROM NEW VERSION)
                    if (columnIndex < reelCycleCount.Count)
                    {
                        reelCycleCount[columnIndex]++;
                    }
                    
                    // Continue spinning
                    StartReelCycle(columnIndex);
                }
            });

            cycleSequence.Play();
            
            if (spinTweens.Count <= columnIndex)
                spinTweens.Add(cycleSequence);
            else
                spinTweens[columnIndex] = cycleSequence;
        }

        // 🔥 FROM OLD VERSION - This method was MISSING in new version!
        private void CycleReelSymbols(int columnIndex)
        {
            var reel = reelImagesList[columnIndex];
            if (reel.images == null || reel.images.Count != 16) return;

            // Shift all sprites down by one position
            Sprite bottomSprite = reel.images[15].sprite;
            
            for (int i = 15; i > 0; i--)
            {
                reel.images[i].sprite = reel.images[i - 1].sprite;
            }
            
            // Add new random sprite at top
            reel.images[0].sprite = GetSymbolSprite(Random.Range(0, 16));
        }

        #endregion

        #region Stop Spin - All Reels Stop in Same Cycle (FROM NEW VERSION)

        internal void StopSpin(List<List<int>> resultMatrix, System.Action onComplete)
        {
            Debug.Log("[SlotView] Stop spin with casino animation");
            LogMatrix("RESULT", resultMatrix);

            if (!isSpinning)
            {
                Debug.LogWarning("[SlotView] Not spinning, setting symbols directly");
                currentDisplayMatrix = resultMatrix;
                for (int col = 0; col < 5; col++)
                {
                    SetReelSymbols(col, resultMatrix[col], false);
                }
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(StopSpinSequence(resultMatrix, onComplete, false));
        }

        private IEnumerator StopSpinSequence(List<List<int>> resultMatrix, System.Action onComplete, bool isQuickStop)
        {
            currentDisplayMatrix = resultMatrix;

            // 🔥 SCATTER ANTICIPATION DETECTION (FROM NEW VERSION)
            int scatterCount = 0;
            for (int col = 0; col < 4; col++)
            {
                for (int row = 0; row < resultMatrix[col].Count; row++)
                {
                    if (resultMatrix[col][row] == scatterSymbolId)
                    {
                        scatterCount++;
                        break; // Only count one scatter per reel
                    }
                }
            }

            // Activate scatter anticipation if 2 scatters found
            if (scatterCount >= 2 && !isQuickStop)
            {
                scatterAnticipationActive = true;
                Debug.Log("[SlotView] 🎰 SCATTER ANTICIPATION ACTIVATED! 2 scatters detected!");
            }

            // Wait for minimum cycles on all reels
            while (true)
            {
                bool allReelsReady = true;
                for (int col = 0; col < 5; col++)
                {
                    // For 5th reel with anticipation, require extra cycles
                    int requiredCycles = minSpinCyclesBeforeStop;
                    if (scatterAnticipationActive && col == 4)
                    {
                        requiredCycles += Mathf.RoundToInt(anticipationExtraSpins);
                    }

                    if (reelCycleCount[col] < requiredCycles)
                    {
                        allReelsReady = false;
                        break;
                    }
                }

                if (allReelsReady) break;
                yield return null;
            }

            // Stop all reels in the SAME cycle with staggered timing
            float stagger = isQuickStop ? quickStopStagger : reelStopStagger;

            for (int col = 0; col < 5; col++)
            {
                // Calculate delay for this reel
                float delay = col * stagger;
                
                // Start the stop animation for this reel
                StartCoroutine(StopSingleReel(col, resultMatrix[col], delay, isQuickStop));
            }

            // Wait for all reels to complete stopping
            float longestStopTime;
            if (isQuickStop)
            {
                longestStopTime = (4 * stagger) + quickStopDuration;
            }
            else
            {
                longestStopTime = (4 * stagger) + stopOvershootDuration + stopBounceBackDuration + stopSettleDuration;
            }

            yield return new WaitForSeconds(longestStopTime);

            isSpinning = false;
            scatterAnticipationActive = false;
            
            Debug.Log("[SlotView] All reels stopped");
            onComplete?.Invoke();
        }

        private IEnumerator StopSingleReel(int columnIndex, List<int> targetSymbols, float delay, bool isQuickStop)
        {
            // Wait for stagger delay
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            // Kill current cycling animation
            if (columnIndex < spinTweens.Count && spinTweens[columnIndex] != null)
            {
                spinTweens[columnIndex].Kill();
            }

            Transform slotTransform = reelTransforms[columnIndex];

            // Set final symbols
            SetReelSymbols(columnIndex, targetSymbols, false);

            // Align to middle position
            float currentY = slotTransform.localPosition.y;
            float targetY = middlePosition;
            float offset = (currentY - targetY) % cycleDistance;
            if (offset < 0) offset += cycleDistance;

            slotTransform.localPosition = new Vector3(
                slotTransform.localPosition.x,
                targetY + offset,
                0
            );

            // Apply stop animation
            if (isQuickStop)
            {
                // Quick stop animation
                Sequence quickStopSequence = DOTween.Sequence();
                
                quickStopSequence.Append(
                    slotTransform.DOLocalMoveY(middlePosition - quickStopOvershoot, quickStopDuration * 0.3f)
                        .SetEase(Ease.InCubic)
                );
                
                quickStopSequence.Append(
                    slotTransform.DOLocalMoveY(middlePosition, quickStopDuration * 0.7f)
                        .SetEase(Ease.OutBack, 1.2f)
                );

                spinTweens[columnIndex] = quickStopSequence;
            }
            else
            {
                // Full casino-style stop animation
                Sequence stopSequence = DOTween.Sequence();
                
                // Overshoot downward (momentum)
                stopSequence.Append(
                    slotTransform.DOLocalMoveY(middlePosition - stopOvershootDistance, stopOvershootDuration)
                        .SetEase(Ease.InCubic)
                );
                
                // Bounce back up past center
                stopSequence.Append(
                    slotTransform.DOLocalMoveY(middlePosition + stopBounceBackDistance, stopBounceBackDuration)
                        .SetEase(Ease.OutCubic)
                );

                // Settle to final position with bounce
                stopSequence.Append(
                    slotTransform.DOLocalMoveY(middlePosition, stopSettleDuration)
                        .SetEase(Ease.OutBounce)
                );

                spinTweens[columnIndex] = stopSequence;
            }
        }

        #endregion

        #region Quick Spin

        internal void QuickStop(List<List<int>> resultMatrix)
        {
            Debug.Log("[SlotView] Quick stop with fast animation");
            LogMatrix("QUICK_RESULT", resultMatrix);

            if (!isSpinning)
            {
                // Already stopped, just set symbols
                currentDisplayMatrix = resultMatrix;
                for (int col = 0; col < 5; col++)
                {
                    if (col < reelTransforms.Length)
                    {
                        SetReelSymbols(col, resultMatrix[col], false);
                        reelTransforms[col].localPosition = new Vector3(
                            reelTransforms[col].localPosition.x, 
                            middlePosition, 
                            0
                        );
                    }
                }
                return;
            }

            // Use quick stop sequence with animations
            StartCoroutine(StopSpinSequence(resultMatrix, null, true));
        }

        #endregion

        #region Win Line Animation

        internal void ShowWinLineAnimation(List<WinLine> winLines, System.Action onComplete)
        {
            if (winLines == null || winLines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[SlotView] Showing {winLines.Count} win line animations");

            KillWinTweens();

            HashSet<Vector2Int> winningPositions = new HashSet<Vector2Int>();
            
            foreach (var winLine in winLines)
            {
                if (winLine.positions == null) continue;

                foreach (int position in winLine.positions)
                {
                    int col = position / 4; 
                    int row = position % 4; 
                    
                    if (col >= 0 && col < 5 && row >= 0 && row < 4)
                    {
                        winningPositions.Add(new Vector2Int(col, row));
                    }
                }
            }

            float totalDuration = (winPopDuration * winPopRepeat) + (0.1f * (winPopRepeat - 1));
            
            foreach (var pos in winningPositions)
            {
                AnimateWinSymbol(pos.x, pos.y);
            }

            StartCoroutine(WaitForWinAnimationComplete(totalDuration, onComplete));
        }

        private IEnumerator WaitForWinAnimationComplete(float duration, System.Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }

        private void AnimateWinSymbol(int column, int row)
        {
            if (column >= reelImagesList.Count) return;

            var reel = reelImagesList[column];
            if (reel.images == null || reel.images.Count < 10) return;

    
            int imageIndex = 6 + row;
            if (imageIndex >= reel.images.Count) return;

            Image symbolImage = reel.images[imageIndex];
            if (symbolImage == null) return;

            symbolImage.transform.localScale = Vector3.one;

            Sequence winSequence = DOTween.Sequence();

            for (int i = 0; i < winPopRepeat; i++)
            {
                winSequence.Append(
                    symbolImage.transform.DOScale(winPopScale, winPopDuration / 2)
                        .SetEase(Ease.OutBack, 1.5f)
                );
                
                winSequence.Append(
                    symbolImage.transform.DOScale(1f, winPopDuration / 2)
                        .SetEase(Ease.InBack, 1.5f)
                );

                if (i < winPopRepeat - 1)
                {
                    winSequence.AppendInterval(0.1f);
                }
            }

            winSequence.OnComplete(() => {
                if (symbolImage != null)
                    symbolImage.transform.localScale = Vector3.one;
            });

            winTweens.Add(winSequence);
        }


        private void KillWinTweens()
        {
            foreach (var tween in winTweens)
            {
                tween?.Kill();
            }
            winTweens.Clear();

            foreach (var reel in reelImagesList)
            {
                if (reel.images != null)
                {
                    foreach (var image in reel.images)
                    {
                        if (image != null)
                            image.transform.localScale = Vector3.one;
                    }
                }
            }
        }

        #endregion

        #region Debug Logging

        private void LogMatrix(string label, List<List<int>> matrix)
        {
            string matrixStr = $"[SlotView] {label} Matrix:\n";
            for (int row = 0; row < 4; row++)
            {
                matrixStr += "  [";
                for (int col = 0; col < 5; col++)
                {
                    matrixStr += matrix[col][row].ToString("D2");
                    if (col < 4) matrixStr += ", ";
                }
                matrixStr += "]\n";
            }
            Debug.Log(matrixStr);
        }

        #endregion

        #region Helper Methods

        internal List<List<int>> GetCurrentDisplayMatrix()
        {
            return currentDisplayMatrix;
        }

        internal bool IsSpinning()
        {
            return isSpinning;
        }

        private void KillAllTweens()
        {
            foreach (var tween in spinTweens)
            {
                tween?.Kill();
            }
            spinTweens.Clear();

            KillWinTweens();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            KillAllTweens();
        }

        #endregion
    }

    [System.Serializable]
    public class ReelImages
    {
        public List<Image> images = new List<Image>(16);
    }