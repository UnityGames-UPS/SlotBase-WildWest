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

        // Reel positioning
        private float middlePosition = 0f; // Middle position where 4 icons are visible
        private float cycleDistance; // Distance to move before cycling back
        
        // Tween tracking
        private List<Tween> spinTweens = new List<Tween>();
        
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
        /// Smooth stop with elastic bounce - no jerky snap
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

            // Get current position
            float currentY = slotTransform.localPosition.y;

            // Smooth transition to middle position with bounce
            // First move slightly down, then bounce back to middle
            Sequence stopSequence = DOTween.Sequence();
            
            // Quick settle down
            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - 30f, 0.15f)
                    .SetEase(Ease.OutCubic)
            );
            
            // Elastic bounce to final position
            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, 0.35f)
                    .SetEase(Ease.OutElastic, 0.8f, 0.5f)
            );

            spinTweens[columnIndex] = stopSequence;
            
            yield return new WaitForSeconds(0.5f);
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