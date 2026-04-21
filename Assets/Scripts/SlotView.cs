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
        [SerializeField] private float reelStartStagger = 0.08f;
        [SerializeField] private float reelStopStagger = 0.12f;

        [Header("Animation Settings - Casino Style")]
        [SerializeField] private float anticipationUpDistance = 30f;
        [SerializeField] private float anticipationUpDuration = 0.15f;
        [SerializeField] private float dropDownDistance = 15f;
        [SerializeField] private float dropDownDuration = 0.12f;
        [SerializeField] private float settleBounceDuration = 0.18f;
        
        [Header("Stop Animation Settings")]
        [SerializeField] private float stopOvershootDistance = 50f;
        [SerializeField] private float stopOvershootDuration = 0.15f;
        [SerializeField] private float stopBounceBackDistance = 15f;
        [SerializeField] private float stopBounceBackDuration = 0.25f;
        [SerializeField] private float stopSettleDuration = 0.35f;

        [Header("Quick Spin Settings")]
        [SerializeField] private float quickStopStagger = 0.06f;
        [SerializeField] private float quickStopOvershoot = 20f;
        [SerializeField] private float quickStopDuration = 0.2f;
        [SerializeField] private int minSpinCyclesBeforeStop = 3;

        [Header("Scatter Anticipation Settings")]
        [SerializeField] private int scatterSymbolId = 4;
        [SerializeField] private float anticipationExtraSpins = 3f;
        [SerializeField] private float anticipationSpeedMultiplier = 1.5f;

        [Header("Win Animation Settings")]
        [SerializeField] private float winPopScale = 1.3f; 
        [SerializeField] private float winPopDuration = 0.4f; 
        [SerializeField] private int winPopRepeat = 3; 


        private float middlePosition = 0f;
        private float cycleDistance;
        

        private List<Tween> spinTweens = new List<Tween>();
        private List<Tween> winTweens = new List<Tween>(); 
        private List<int> reelCycleCount = new List<int>();
        

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
        }

        internal void SetInitialMatrix(List<List<int>> matrix)
        {
            if (matrix == null || matrix.Count != 5) return;

            for (int col = 0; col < 5; col++)
            {
                if (matrix[col].Count != 4) return;
            }

            currentDisplayMatrix = matrix;

            for (int col = 0; col < 5; col++)
            {
                SetReelSymbols(col, matrix[col], true);
            }
        }

        #endregion

        #region Symbol Display

        private void SetReelSymbols(int columnIndex, List<int> visibleSymbolIds, bool isInitial = false)
        {
            if (columnIndex >= reelImagesList.Count) return;

            var reel = reelImagesList[columnIndex];
            
            if (reel.images == null || reel.images.Count != 16) return;

            for (int row = 0; row < 4; row++)
            {
                int imageIndex = 6 + row;
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
                return symbolSprites[0];
            }
            return symbolSprites[symbolId];
        }

        #endregion

        #region Spin Animation

        internal void StartSpin()
        {
            if (isSpinning) return;

            isSpinning = true;
            scatterAnticipationActive = false;
            KillAllTweens();

            for (int i = 0; i < reelCycleCount.Count; i++)
            {
                reelCycleCount[i] = 0;
            }

            for (int col = 0; col < 5; col++)
            {
                StartReelCycleWithDelay(col, col * reelStartStagger); 
            }
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
                slotTransform.DOLocalMoveY(middlePosition + anticipationUpDistance, anticipationUpDuration)
                    .SetEase(Ease.OutCubic)
            );

            startSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - dropDownDistance, dropDownDuration)
                    .SetEase(Ease.InCubic)
            );

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

        private void StartReelCycle(int columnIndex)
        {
            if (columnIndex >= reelTransforms.Length) return;
            if (!isSpinning) return;

            Transform slotTransform = reelTransforms[columnIndex];

            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

            float currentSpeed = spinSpeed;
            if (scatterAnticipationActive && columnIndex == 4)
            {
                currentSpeed = spinSpeed / anticipationSpeedMultiplier;
            }

            Sequence cycleSequence = DOTween.Sequence();
            
            cycleSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - cycleDistance, currentSpeed)
                    .SetEase(Ease.Linear)
            );
            
            cycleSequence.OnComplete(() => {
                if (isSpinning)
                {
                    CycleReelSymbols(columnIndex);
                    
                    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);
                    
                    if (columnIndex < reelCycleCount.Count)
                    {
                        reelCycleCount[columnIndex]++;
                    }
                    
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

        #endregion

        #region Stop Spin

        internal void StopSpin(List<List<int>> resultMatrix, System.Action onComplete)
        {
            if (!isSpinning)
            {
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

            int scatterCount = 0;
            for (int col = 0; col < 4; col++)
            {
                for (int row = 0; row < resultMatrix[col].Count; row++)
                {
                    if (resultMatrix[col][row] == scatterSymbolId)
                    {
                        scatterCount++;
                        break;
                    }
                }
            }

            if (scatterCount >= 2 && !isQuickStop)
            {
                scatterAnticipationActive = true;
            }

            while (true)
            {
                bool allReelsReady = true;
                for (int col = 0; col < 5; col++)
                {
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

            float stagger = isQuickStop ? quickStopStagger : reelStopStagger;

            for (int col = 0; col < 5; col++)
            {
                float delay = col * stagger;
                StartCoroutine(StopSingleReel(col, resultMatrix[col], delay, isQuickStop));
            }

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
            
            onComplete?.Invoke();
        }

        private IEnumerator StopSingleReel(int columnIndex, List<int> targetSymbols, float delay, bool isQuickStop)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            if (columnIndex < spinTweens.Count && spinTweens[columnIndex] != null)
            {
                spinTweens[columnIndex].Kill();
            }

            Transform slotTransform = reelTransforms[columnIndex];

            SetReelSymbols(columnIndex, targetSymbols, false);

            float currentY = slotTransform.localPosition.y;
            float targetY = middlePosition;
            float offset = (currentY - targetY) % cycleDistance;
            if (offset < 0) offset += cycleDistance;

            slotTransform.localPosition = new Vector3(
                slotTransform.localPosition.x,
                targetY + offset,
                0
            );

            if (isQuickStop)
            {
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
                Sequence stopSequence = DOTween.Sequence();
                
                stopSequence.Append(
                    slotTransform.DOLocalMoveY(middlePosition - stopOvershootDistance, stopOvershootDuration)
                        .SetEase(Ease.InCubic)
                );
                
                stopSequence.Append(
                    slotTransform.DOLocalMoveY(middlePosition + stopBounceBackDistance, stopBounceBackDuration)
                        .SetEase(Ease.OutCubic)
                );

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
            if (!isSpinning)
            {
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