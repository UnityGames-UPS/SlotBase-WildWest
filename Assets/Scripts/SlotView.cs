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
        [SerializeField] private float reelStopDelay = 0.2f; 

        [Header("Win Animation Settings - NEW")]
        [SerializeField] private float winPopScale = 1.3f; 
        [SerializeField] private float winPopDuration = 0.4f; 
        [SerializeField] private int winPopRepeat = 3; 


        private float middlePosition = 0f;
        private float cycleDistance;
        

        private List<Tween> spinTweens = new List<Tween>();
        private List<Tween> winTweens = new List<Tween>(); 
        

        internal List<List<int>> currentDisplayMatrix;

        private bool isSpinning;

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

            for (int col = 0; col < 5; col++)
            {
                StartReelCycleWithDelay(col, col * 0.15f); 
            }

            Debug.Log("[SlotView]  All reels spinning with staggered start");
        }

        private void StartReelCycleWithDelay(int columnIndex, float delay)
        {
            if (columnIndex >= reelTransforms.Length) return;

            Transform slotTransform = reelTransforms[columnIndex];

            Sequence startSequence = DOTween.Sequence();

            if (delay > 0)
            {
                startSequence.AppendInterval(delay);
            }

            startSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition + 25f, 0.15f)
                    .SetEase(Ease.OutCubic)
            );

            startSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - 10f, 0.12f)
                    .SetEase(Ease.InCubic)
            );

            startSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, 0.18f)
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


        private void StartReelCycle(int columnIndex)
        {
            if (columnIndex >= reelTransforms.Length) return;

            Transform slotTransform = reelTransforms[columnIndex];
            var reel = reelImagesList[columnIndex];

            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

            Sequence cycleSequence = DOTween.Sequence();
            
            cycleSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - cycleDistance, spinSpeed)
                    .SetEase(Ease.Linear)
            );
            
            cycleSequence.OnComplete(() => {
                if (isSpinning)
                {
                    CycleReelSymbols(columnIndex);
                    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);
                    StartReelCycle(columnIndex);
                }
            });

            cycleSequence.Play();
            
            if (spinTweens.Count <= columnIndex)
                spinTweens.Add(cycleSequence);
            else
                spinTweens[columnIndex] = cycleSequence;
        }

        private void CycleReelSymbols(int columnIndex)
        {
            var reel = reelImagesList[columnIndex];
            if (reel.images == null || reel.images.Count != 16) return;

  
            Sprite bottomSprite = reel.images[15].sprite;
            

            for (int i = 15; i > 0; i--)
            {
                reel.images[i].sprite = reel.images[i - 1].sprite;
            }
            

            reel.images[0].sprite = GetSymbolSprite(Random.Range(0, 16));
        }

        internal void StopSpin(List<List<int>> resultMatrix, System.Action onComplete)
        {
            if (!isSpinning)
            {
                Debug.LogWarning("[SlotView] Not spinning!");
                return;
            }

            LogMatrix("RESULT", resultMatrix);
            StartCoroutine(StopSpinSequence(resultMatrix, onComplete));
        }

        private IEnumerator StopSpinSequence(List<List<int>> resultMatrix, System.Action onComplete)
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
            Debug.Log("[SlotView]  All reels stopped");
            
            onComplete?.Invoke();
        }

        private IEnumerator StopSingleReel(int columnIndex, List<int> targetSymbols)
        {
            if (columnIndex >= spinTweens.Count || columnIndex >= reelTransforms.Length)
                yield break;

            Transform slotTransform = reelTransforms[columnIndex];

            if (spinTweens[columnIndex] != null)
            {
                spinTweens[columnIndex].Kill();
            }

            SetReelSymbols(columnIndex, targetSymbols, false);

            Sequence stopSequence = DOTween.Sequence();
            
            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - 50f, 0.15f)
                    .SetEase(Ease.InCubic)
            );
            
            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition + 15f, 0.25f)
                    .SetEase(Ease.OutCubic)
            );

            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, 0.35f)
                    .SetEase(Ease.OutBounce)
            );

            spinTweens[columnIndex] = stopSequence;
            
            yield return new WaitForSeconds(0.75f); 
        }

        #endregion

        #region Quick Spin

        internal void QuickStop(List<List<int>> resultMatrix)
        {
            Debug.Log("[SlotView]  Quick stop");
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

        internal void ShowWinLineAnimation(List<WinLine> winLines, System.Action onComplete)
        {
            if (winLines == null || winLines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[SlotView]  Showing {winLines.Count} win line animations");

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