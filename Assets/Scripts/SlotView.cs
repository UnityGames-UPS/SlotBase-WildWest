using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Symbol Sprites - Assign by Name")]
    [Tooltip("Symbol sprites assigned by name. Array order: Sheriff, CowboyBlue, CowgirlGreen, CowgirlRed, Treasure, Gun, A, K, Q, J, Ten, Wild, Scatter, Wild2x, Wild3x, Wild5x")]
    [SerializeField] private Sprite spriteSheriff;           // ID: 0
    [SerializeField] private Sprite spriteCowboyBlue;        // ID: 1
    [SerializeField] private Sprite spriteCowgirlGreen;      // ID: 2
    [SerializeField] private Sprite spriteCowgirlRed;        // ID: 3
    [SerializeField] private Sprite spriteTreasure;          // ID: 4
    [SerializeField] private Sprite spriteGun;               // ID: 5
    [SerializeField] private Sprite spriteA;                 // ID: 6
    [SerializeField] private Sprite spriteK;                 // ID: 7
    [SerializeField] private Sprite spriteQ;                 // ID: 8
    [SerializeField] private Sprite spriteJ;                 // ID: 9
    [SerializeField] private Sprite spriteTen;               // ID: 10
    [SerializeField] private Sprite spriteWild;              // ID: 11 (1x multiplier)
    [SerializeField] private Sprite spriteScatter;           // ID: 12
    [SerializeField] private Sprite spriteWild2x;            // ID: 13 (2x multiplier)
    [SerializeField] private Sprite spriteWild3x;            // ID: 14 (3x multiplier)
    [SerializeField] private Sprite spriteWild5x;            // ID: 15 (5x multiplier)

    // Internal array built from named sprites
    private Sprite[] symbolSprites;

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
    [SerializeField] private int scatterSymbolId = 12;
    [SerializeField] private float anticipationExtraSpins = 3f;
    [SerializeField] private float anticipationSpeedMultiplier = 1.5f;

    [Header("Win Animation Settings")]
    [SerializeField] private float winPopScale = 1.3f;
    [SerializeField] private float winPopDuration = 0.4f;
    [SerializeField] private int winPopRepeat = 3;

    [Header("Win Box Overlays — Col 0..4  (each has 4 rows: 0=top .. 3=bottom)")]
    [SerializeField] private ColumnOverlays[] winBoxColumns = new ColumnOverlays[5];

    [Header("Scatter / Badge Overlays — Col 0..4  (each has 4 rows)")]
    [SerializeField] private ColumnOverlays[] scatterStarColumns = new ColumnOverlays[5];

    [Header("Sticky Wild Overlays — Col 0..4  (each has 4 rows)")]
    [SerializeField] private ColumnOverlays[] stickyWildColumns = new ColumnOverlays[5];

    [SerializeField] private GameObject anticipationFrame;


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
        BuildSymbolSpriteArray();
        InitializeReels();
        DisableAllOverlays();
    }

    private void DisableAllOverlays()
    {
        DisableColumns(winBoxColumns);
        DisableColumns(scatterStarColumns);
        DisableColumns(stickyWildColumns);
        if (anticipationFrame) anticipationFrame.SetActive(false);
    }

    // Disables every row GameObject in all 5 columns of an overlay array
    private static void DisableColumns(ColumnOverlays[] cols)
    {
        if (cols == null) return;
        foreach (var col in cols)
            if (col?.rows != null)
                foreach (var go in col.rows)
                    if (go) go.SetActive(false);
    }

    // Direct cell access helpers
    private static GameObject WinBox(ColumnOverlays[] cols, int col, int row)
        => (col >= 0 && col < cols?.Length && cols[col]?.rows != null && row >= 0 && row < cols[col].rows.Length)
            ? cols[col].rows[row] : null;

    private void BuildSymbolSpriteArray()
    {
        // Build the symbol sprite array from named sprite fields
        symbolSprites = new Sprite[16];
        symbolSprites[0] = spriteSheriff;
        symbolSprites[1] = spriteCowboyBlue;
        symbolSprites[2] = spriteCowgirlGreen;
        symbolSprites[3] = spriteCowgirlRed;
        symbolSprites[4] = spriteTreasure;
        symbolSprites[5] = spriteGun;
        symbolSprites[6] = spriteA;
        symbolSprites[7] = spriteK;
        symbolSprites[8] = spriteQ;
        symbolSprites[9] = spriteJ;
        symbolSprites[10] = spriteTen;
        symbolSprites[11] = spriteWild;
        symbolSprites[12] = spriteScatter;
        symbolSprites[13] = spriteWild2x;
        symbolSprites[14] = spriteWild3x;
        symbolSprites[15] = spriteWild5x;

        // Validate
        for (int i = 0; i < symbolSprites.Length; i++)
        {
            if (symbolSprites[i] == null)
            {
                Debug.LogError($"[SlotView] Symbol sprite at index {i} is not assigned in inspector!");
            }
        }

        Debug.Log($"[SlotView] Built symbol sprite array with {symbolSprites.Length} sprites");
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
        if (columnIndex >= reelImagesList.Count)
        {
            Debug.LogError($"SetReelSymbols: Invalid column index {columnIndex}, max is {reelImagesList.Count - 1}");
            return;
        }

        if (visibleSymbolIds == null || visibleSymbolIds.Count != 4)
        {
            Debug.LogError($"SetReelSymbols: Invalid visibleSymbolIds count {visibleSymbolIds?.Count}, expected 4");
            return;
        }

        var reel = reelImagesList[columnIndex];

        if (reel.images == null || reel.images.Count != 16)
        {
            Debug.LogError($"SetReelSymbols: Reel {columnIndex} has invalid image count {reel.images?.Count}, expected 16");
            return;
        }

        for (int row = 0; row < 4; row++)
        {
            int imageIndex = 6 + row;
            int symbolId = visibleSymbolIds[row];
            reel.images[imageIndex].sprite = GetSymbolSprite(symbolId);
        }

        for (int i = 0; i < 6; i++)
        {
            reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 11));
        }

        for (int i = 10; i < 16; i++)
        {
            reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 11));
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
        // Validate symbolId range (0-15)
        if (symbolId < 0 || symbolId >= symbolSprites.Length)
        {
            Debug.LogWarning($"[SlotView] Invalid symbolId {symbolId}, using default sprite 0. Total sprites: {symbolSprites.Length}");
            return symbolSprites[0];
        }

        if (symbolSprites[symbolId] == null)
        {
            Debug.LogError($"[SlotView] Symbol sprite for ID {symbolId} is null!");
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

        DisableAllOverlays();

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

        int actualScatterId = gameManager.gameConfig != null ? gameManager.gameConfig.scatterSymbolId : scatterSymbolId;
        int scatterCount = 0;
        for (int col = 0; col < 4; col++)
        {
            for (int row = 0; row < resultMatrix[col].Count; row++)
            {
                if (resultMatrix[col][row] == actualScatterId)
                {
                    scatterCount++;
                    break;
                }
            }
        }

        if (scatterCount >= 2 && !isQuickStop)
        {
            scatterAnticipationActive = true;
            if (anticipationFrame) anticipationFrame.SetActive(true);
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
        if (anticipationFrame) anticipationFrame.SetActive(false);

        var currentResult = gameManager.lastResult;
        if (currentResult != null)
        {
            // Sticky wilds — server key format: "col_row"
            if (currentResult.stickyWilds != null)
            {
                foreach (var kvp in currentResult.stickyWilds)
                {
                    string[] parts = kvp.Key.Split('_');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int col) &&
                        int.TryParse(parts[1], out int row))
                    {
                        var go = WinBox(stickyWildColumns, col, row);
                        if (go) go.SetActive(true);
                    }
                }
            }

            // Overlay scatter badges — server pos format: [row, col]
            if (currentResult.overlayScatterData != null && currentResult.overlayScatterData.isTriggered)
            {
                foreach (var pos in currentResult.overlayScatterData.positions)
                {
                    if (pos.Count >= 2)
                    {
                        int row = pos[0];
                        int col = pos[1];
                        var go = WinBox(scatterStarColumns, col, row);
                        if (go) go.SetActive(true);
                    }
                }
            }
        }

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
        Debug.Log($"[ShowWinLineAnimation] Called with {winLines?.Count ?? 0} win lines");

        if (winLines == null || winLines.Count == 0)
        {
            Debug.Log("[ShowWinLineAnimation] No win lines to animate");
            onComplete?.Invoke();
            return;
        }

        for (int i = 0; i < winLines.Count; i++)
        {
            var line = winLines[i];
            Debug.Log($"[ShowWinLineAnimation] Line {i}: lineId={line.lineId}, symbolId={line.symbolId}, positions={string.Join(",", line.positions)}");
        }

        KillWinTweens();
        StartCoroutine(PlayWinLinesSequentially(winLines, onComplete));
    }

    /// <summary>
    /// Plays each winning line animation one at a time:
    ///   1. Enable win boxes for this line's positions
    ///   2. Pop-animate each symbol winPopRepeat times
    ///   3. Wait for the animation to finish
    ///   4. Disable only the previous line's cells (not all) and reset their scales
    ///   5. Move to next line — invoke onComplete after all lines done
    /// </summary>
    private IEnumerator PlayWinLinesSequentially(List<WinLine> winLines, System.Action onComplete)
    {
        float lineDuration = (winPopDuration * winPopRepeat) + (0.1f * (winPopRepeat - 1));

        List<int> prevPositions = null;

        Debug.Log($"[PlayWinLinesSequentially] Starting win animation for {winLines.Count} lines");

        foreach (var winLine in winLines)
        {
            if (winLine.positions == null || winLine.positions.Count == 0) continue;

            Debug.Log($"[PlayWinLinesSequentially] Processing winLine ID: {winLine.lineId}, symbolId: {winLine.symbolId}, positions count: {winLine.positions.Count}");

            if (prevPositions != null)
            {
                KillWinTweens();
                foreach (int flatIdx in prevPositions)
                {
                    int r = flatIdx / 5;
                    int c = flatIdx % 5;
                    Debug.Log($"[PlayWinLinesSequentially] Disabling previous flatIdx: {flatIdx} -> col: {c}, row: {r}");
                    DisableWinBox(c, r);
                    ResetSymbolScale(c, r);
                }
            }

            foreach (int flatIndex in winLine.positions)
            {
                int row = flatIndex / 5;
                int col = flatIndex % 5;

                Debug.Log($"[PlayWinLinesSequentially] flatIndex: {flatIndex} -> col: {col}, row: {row}");

                if (col < 0 || col >= 5 || row < 0 || row >= 4)
                {
                    Debug.LogWarning($"[PlayWinLinesSequentially] Invalid position! col: {col}, row: {row}");
                    continue;
                }

                Debug.Log($"[PlayWinLinesSequentially] Enabling win box at col: {col}, row: {row}");
                EnableWinBox(col, row);

                Debug.Log($"[PlayWinLinesSequentially] Animating symbol at col: {col}, row: {row}");
                AnimateWinSymbol(col, row);
            }

            prevPositions = new List<int>(winLine.positions);

            yield return new WaitForSeconds(lineDuration);
        }

        KillWinTweens();

        onComplete?.Invoke();
    }

    /// <summary>
    /// Enables the win-box overlay at the given (col, row) cell.
    /// Handles both column-organised and row-organised reelOverlaysList.
    /// </summary>
    private void EnableWinBox(int col, int row)
    {
        var go = WinBox(winBoxColumns, col, row);
        if (go)
        {
            go.SetActive(true);
            Debug.Log($"[EnableWinBox] Enabled win box at col: {col}, row: {row}, GameObject: {go.name}");
        }
        else
        {
            Debug.LogError($"[EnableWinBox] WinBox GameObject is NULL at col: {col}, row: {row}");
        }
    }

    private void DisableWinBox(int col, int row)
    {
        var go = WinBox(winBoxColumns, col, row);
        if (go) go.SetActive(false);
    }

    /// <summary>
    /// Resets the scale of the symbol image at (col, row) to Vector3.one.
    /// </summary>
    private void ResetSymbolScale(int col, int row)
    {
        if (col >= reelImagesList.Count) return;
        var reel = reelImagesList[col];
        if (reel.images == null) return;
        int imageIndex = 6 + row;
        if (imageIndex >= reel.images.Count) return;
        if (reel.images[imageIndex] != null)
            reel.images[imageIndex].transform.localScale = Vector3.one;
    }


    private void AnimateWinSymbol(int column, int row)
    {
        Debug.Log($"[AnimateWinSymbol] Called for col: {column}, row: {row}");

        if (column >= reelImagesList.Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Invalid column {column}, max is {reelImagesList.Count - 1}");
            return;
        }

        var reel = reelImagesList[column];
        if (reel.images == null || reel.images.Count < 10)
        {
            Debug.LogError($"[AnimateWinSymbol] Reel {column} has invalid images list");
            return;
        }

        int imageIndex = 6 + row;
        if (imageIndex >= reel.images.Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Image index {imageIndex} out of range for reel {column}");
            return;
        }

        Image symbolImage = reel.images[imageIndex];
        if (symbolImage == null)
        {
            Debug.LogError($"[AnimateWinSymbol] Symbol image is NULL at col: {column}, row: {row}, imageIndex: {imageIndex}");
            return;
        }

        Debug.Log($"[AnimateWinSymbol] Animating symbol at col: {column}, row: {row}, imageIndex: {imageIndex}, GameObject: {symbolImage.gameObject.name}");

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

        DisableColumns(winBoxColumns);

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

/// <summary>
/// One column of overlay GameObjects.
/// rows[0] = top visible row, rows[3] = bottom visible row.
/// Assign in Inspector: 5 entries of this class (Col0..Col4),
/// each with exactly 4 row slots.
/// </summary>
[System.Serializable]
public class ColumnOverlays
{
    [Tooltip("Row 0 = top, Row 1, Row 2, Row 3 = bottom")]
    public GameObject[] rows = new GameObject[4];
}