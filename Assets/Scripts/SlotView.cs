using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SlotGame
{
    /// <summary>
    /// Handles all slot visuals - smooth cyclic icon animation
    /// 5 reels × 4 rows = 20 visible symbols
    /// Icons cycle seamlessly, no empty space visible
    /// MODIFICATIONS:
    /// - Added win line pop animation
    /// - Smoother elastic stopping animation
    /// </summary>
    public class SlotView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("Symbol Sprites")]
        [SerializeField] private Sprite[] symbolSprites; // 11 symbols (index 0-10)

        [Header("Reel Containers")]
        [SerializeField] private Transform[] reelTransforms; // 5 reels
        
        [Header("Reel Images - 16 images per reel")]
        [SerializeField] private List<ReelImages> reelImagesList; // 5 reels, each with 16 images

        [Header("Spin Settings")]
        [SerializeField] private float symbolHeight = 100f; // Height of each symbol
        [SerializeField] private float spinSpeed = 0.05f; // Time per symbol movement (lower = faster)
        [SerializeField] private float reelStopDelay = 0.2f; // Delay between reel stops

        [Header("Win Animation Settings - NEW")]
        [SerializeField] private float winPopScale = 1.3f; // Scale multiplier for win pop
        [SerializeField] private float winPopDuration = 0.4f; // Duration of single pop
        [SerializeField] private int winPopRepeat = 3; // Number of times to repeat animation

        // Reel positioning
        private float middlePosition = 0f; // Middle position where 4 icons are visible
        private float cycleDistance; // Distance to move before cycling back
        
        // Tween tracking
        private List<Tween> spinTweens = new List<Tween>();
        private List<Tween> winTweens = new List<Tween>(); // NEW: Track win animations
        
        // Current display matrix
        internal List<List<int>> currentDisplayMatrix;

        // Spin state
        private bool isSpinning;

        #region Initialization

        private void Start()
        {
            InitializeReels();
        }

        private void InitializeReels()
        {
            // Calculate cycle distance: move 1 symbol down, then reset
            cycleDistance = symbolHeight;
            
            // Middle position keeps center 4 icons visible
            middlePosition = 0f;

            // Initialize display matrix
            currentDisplayMatrix = new List<List<int>>();
            for (int col = 0; col < 5; col++)
            {
                currentDisplayMatrix.Add(new List<int> { 0, 0, 0, 0 });
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

            // Set initial symbols (visible middle 4 + surrounding randoms)
            for (int col = 0; col < 5; col++)
            {
                SetReelSymbols(col, matrix[col], true);
            }

            Debug.Log("[SlotView] ✅ Initial matrix set");
            LogMatrix("INIT", matrix);
        }

        #endregion

        #region Symbol Display

        /// <summary>
        /// Sets symbols on a reel - middle 4 are the visible result symbols
        /// </summary>
        private void SetReelSymbols(int columnIndex, List<int> visibleSymbolIds, bool isInitial = false)
        {
            if (columnIndex >= reelImagesList.Count) return;

            var reel = reelImagesList[columnIndex];
            
            if (reel.images == null || reel.images.Count != 16)
            {
                Debug.LogError($"[SlotView] Reel {columnIndex} doesn't have 16 images!");
                return;
            }

            // Middle 4 visible symbols are at indices 6, 7, 8, 9
            // This keeps them centered in the view
            for (int row = 0; row < 4; row++)
            {
                int imageIndex = 6 + row; // Middle positions
                int symbolId = visibleSymbolIds[row];
                reel.images[imageIndex].sprite = GetSymbolSprite(symbolId);
            }

            // Fill surrounding positions with random symbols for seamless cycling
            // Top part (0-5)
            for (int i = 0; i < 6; i++)
            {
                reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 11));
            }
            
            // Bottom part (10-15)
            for (int i = 10; i < 16; i++)
            {
                reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 11));
            }

            // Reset position to middle
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

        #region Spin Animation - Cyclic Icons

        internal void StartSpin()
        {
            if (isSpinning)
            {
                Debug.LogWarning("[SlotView] Already spinning!");
                return;
            }

            isSpinning = true;
            KillAllTweens();

            // Start cyclic animation for all reels
            for (int col = 0; col < 5; col++)
            {
                StartReelCycle(col);
            }

            Debug.Log("[SlotView] 🎰 All reels spinning");
        }

        /// <summary>
        /// Cyclic icon animation - icons move down and cycle back seamlessly
        /// </summary>
        private void StartReelCycle(int columnIndex)
        {
            if (columnIndex >= reelTransforms.Length) return;

            Transform slotTransform = reelTransforms[columnIndex];
            var reel = reelImagesList[columnIndex];

            // Reset to middle position
            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

            // Create cyclic animation sequence
            Sequence cycleSequence = DOTween.Sequence();
            
            cycleSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - cycleDistance, spinSpeed)
                    .SetEase(Ease.Linear)
            );
            
            // On each cycle completion, shuffle top/bottom symbols and reset position
            cycleSequence.OnComplete(() => {
                if (isSpinning)
                {
                    CycleReelSymbols(columnIndex);
                    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);
                    StartReelCycle(columnIndex); // Restart cycle
                }
            });

            cycleSequence.Play();
            
            if (spinTweens.Count <= columnIndex)
                spinTweens.Add(cycleSequence);
            else
                spinTweens[columnIndex] = cycleSequence;
        }

        /// <summary>
        /// Shuffle symbols to create seamless infinite scroll effect
        /// </summary>
        private void CycleReelSymbols(int columnIndex)
        {
            var reel = reelImagesList[columnIndex];
            if (reel.images == null || reel.images.Count != 16) return;

            // Shift symbols: bottom becomes top
            Sprite bottomSprite = reel.images[15].sprite;
            
            // Shuffle all down by 1
            for (int i = 15; i > 0; i--)
            {
                reel.images[i].sprite = reel.images[i - 1].sprite;
            }
            
            // Put bottom at top
            reel.images[0].sprite = GetSymbolSprite(Random.Range(0, 11));
        }

        internal void StopSpin(List<List<int>> resultMatrix)
        {
            if (!isSpinning)
            {
                Debug.LogWarning("[SlotView] Not spinning!");
                return;
            }

            LogMatrix("RESULT", resultMatrix);
            StartCoroutine(StopSpinSequence(resultMatrix));
        }

        /// <summary>
        /// Sequential smooth stop with elastic bounce
        /// </summary>
        private IEnumerator StopSpinSequence(List<List<int>> resultMatrix)
        {
            currentDisplayMatrix = resultMatrix;

            for (int col = 0; col < 5; col++)
            {
                yield return StartCoroutine(StopSingleReel(col, resultMatrix[col]));
                
                if (col < 4)
                {
                    yield return new WaitForSeconds(reelStopDelay);
                }
            }

            isSpinning = false;
            Debug.Log("[SlotView] ✅ All reels stopped");
        }

        /// <summary>
        /// MODIFIED: Smoother elastic stop animation
        /// </summary>
        private IEnumerator StopSingleReel(int columnIndex, List<int> targetSymbols)
        {
            if (columnIndex >= spinTweens.Count || columnIndex >= reelTransforms.Length)
                yield break;

            Transform slotTransform = reelTransforms[columnIndex];
            
            // Kill the cycle animation
            if (spinTweens[columnIndex] != null)
            {
                spinTweens[columnIndex].Kill();
            }

            // Set target symbols
            SetReelSymbols(columnIndex, targetSymbols, false);

            // MODIFIED: Smoother stopping animation
            // Create a more polished sequence with better easing
            Sequence stopSequence = DOTween.Sequence();
            
            // Phase 1: Quick deceleration (simulate inertia)
            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - 40f, 0.12f)
                    .SetEase(Ease.OutQuad)
            );
            
            // Phase 2: Smoother elastic bounce to final position
            // MODIFIED: Better elastic parameters for smoother feel
            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, 0.45f)
                    .SetEase(Ease.OutBack, 1.2f) // OutBack gives smooth bounce without harsh spring
            );

            spinTweens[columnIndex] = stopSequence;
            
            yield return new WaitForSeconds(0.57f); // Total animation time
        }

        #endregion

        #region Quick Spin

        internal void QuickStop(List<List<int>> resultMatrix)
        {
            Debug.Log("[SlotView] ⚡ Quick stop");
            LogMatrix("QUICK_RESULT", resultMatrix);

            KillAllTweens();
            currentDisplayMatrix = resultMatrix;

            // Instant stop - no animation
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

            isSpinning = false;
        }

        #endregion

        #region Win Line Animation - NEW

        /// <summary>
        /// NEW: Show pop animation for winning symbols
        /// </summary>
        internal void ShowWinLineAnimation(List<WinLine> winLines)
        {
            if (winLines == null || winLines.Count == 0) return;

            Debug.Log($"[SlotView] 🎉 Showing {winLines.Count} win line animations");

            // Clear any existing win animations
            KillWinTweens();

            // Collect all winning positions to animate
            HashSet<Vector2Int> winningPositions = new HashSet<Vector2Int>();
            
            foreach (var winLine in winLines)
            {
                if (winLine.positions == null) continue;

                // Convert linear positions to column,row coordinates
                foreach (int position in winLine.positions)
                {
                    int col = position / 4; // Integer division gives column (0-4)
                    int row = position % 4; // Modulo gives row (0-3)
                    
                    if (col >= 0 && col < 5 && row >= 0 && row < 4)
                    {
                        winningPositions.Add(new Vector2Int(col, row));
                    }
                }
            }

            // Animate each winning symbol
            foreach (var pos in winningPositions)
            {
                AnimateWinSymbol(pos.x, pos.y);
            }
        }

        /// <summary>
        /// NEW: Animate a single winning symbol with pop effect
        /// </summary>
        private void AnimateWinSymbol(int column, int row)
        {
            if (column >= reelImagesList.Count) return;

            var reel = reelImagesList[column];
            if (reel.images == null || reel.images.Count < 10) return;

            // Get the image at the visible position (indices 6-9 for rows 0-3)
            int imageIndex = 6 + row;
            if (imageIndex >= reel.images.Count) return;

            Image symbolImage = reel.images[imageIndex];
            if (symbolImage == null) return;

            // Reset scale
            symbolImage.transform.localScale = Vector3.one;

            // Create pulsing pop animation
            Sequence winSequence = DOTween.Sequence();

            for (int i = 0; i < winPopRepeat; i++)
            {
                // Pop out
                winSequence.Append(
                    symbolImage.transform.DOScale(winPopScale, winPopDuration / 2)
                        .SetEase(Ease.OutBack, 1.5f)
                );
                
                // Pop back
                winSequence.Append(
                    symbolImage.transform.DOScale(1f, winPopDuration / 2)
                        .SetEase(Ease.InBack, 1.5f)
                );

                // Small delay between pops (except on last one)
                if (i < winPopRepeat - 1)
                {
                    winSequence.AppendInterval(0.1f);
                }
            }

            // Ensure scale is reset at the end
            winSequence.OnComplete(() => {
                if (symbolImage != null)
                    symbolImage.transform.localScale = Vector3.one;
            });

            winTweens.Add(winSequence);
        }

        /// <summary>
        /// NEW: Kill all win animation tweens
        /// </summary>
        private void KillWinTweens()
        {
            foreach (var tween in winTweens)
            {
                tween?.Kill();
            }
            winTweens.Clear();

            // Reset all symbol scales
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
            string matrixStr = $"[SlotView] 📊 {label} Matrix:\n";
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

            KillWinTweens(); // Also kill win animations
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
}