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

    [Header("Win Animation Sprite Arrays - One per Symbol ID")]
    [Tooltip("Animation sprite arrays for each symbol. Array index = symbol ID (0-15)")]
    [SerializeField] private List<Sprite> animSpritesSheriff;        // ID: 0
    [SerializeField] private List<Sprite> animSpritesCowboyBlue;     // ID: 1
    [SerializeField] private List<Sprite> animSpritesCowgirlGreen;   // ID: 2
    [SerializeField] private List<Sprite> animSpritesCowgirlRed;     // ID: 3
    [SerializeField] private List<Sprite> animSpritesTreasure;       // ID: 4
    [SerializeField] private List<Sprite> animSpritesGun;            // ID: 5
    [SerializeField] private List<Sprite> animSpritesA;              // ID: 6
    [SerializeField] private List<Sprite> animSpritesK;              // ID: 7
    [SerializeField] private List<Sprite> animSpritesQ;              // ID: 8
    [SerializeField] private List<Sprite> animSpritesJ;              // ID: 9
    [SerializeField] private List<Sprite> animSpritesTen;            // ID: 10
    [SerializeField] private List<Sprite> animSpritesWild;           // ID: 11
    [SerializeField] private List<Sprite> animSpritesScatter;        // ID: 12
    [SerializeField] private List<Sprite> animSpritesWild2x;         // ID: 13
    [SerializeField] private List<Sprite> animSpritesWild3x;         // ID: 14
    [SerializeField] private List<Sprite> animSpritesWild5x;         // ID: 15

    // Internal array of animation sprite lists
    private List<Sprite>[] animationSpriteArrays;

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

    [Header("Win Animation Settings")]
    [SerializeField] private float winPopDuration = 0.4f;
    [SerializeField] private int winPopRepeat = 3;


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
    [SerializeField] private float winAnimationDuration = 3.0f; // Total duration each win symbol animation plays
    [SerializeField] private float winSymbolLoopDuration = 1.5f;
    [SerializeField] private int winSymbolLoopCount = 3;
    [Tooltip("Delay between enabling winBox overlay and starting the ImageAnimation - for sync timing")]
    [SerializeField] private float winLineBoxToAnimationDelay = 0.05f;

    [Header("Win Box Overlays — Col 0..4  (each has 4 rows: 0=top .. 3=bottom)")]
    [SerializeField] private ColumnOverlays[] winBoxColumns = new ColumnOverlays[5];

    [Header("Win Animation Objects — Col 0..4  (each has 4 rows, contains ImageAnimation component)")]
    [Tooltip("GameObject references for win animations. Each should have an ImageAnimation component attached.")]
    [SerializeField] private ColumnOverlays[] winAnimationColumns = new ColumnOverlays[5];

    [Header("Scatter / Badge Overlays — Col 0..4  (each has 4 rows)")]
    [SerializeField] private ColumnOverlays[] scatterStarColumns = new ColumnOverlays[5];

    [Header("Sticky Wild Overlays — Col 0..4  (each has 4 rows)")]
    [SerializeField] private ColumnOverlays[] stickyWildColumns = new ColumnOverlays[5];

    [Header("Spin Mask System")]
    [SerializeField] private Image reelMask;
    [Tooltip("Non-display slot icon GameObjects per reel (indices 0-5 and 10-15)")]
    [SerializeField] private List<ReelNonDisplayIcons> reelNonDisplayIconsList;

    [SerializeField] private GameObject anticipationFrame;


    private float middlePosition = 0f;
    private float cycleDistance;


    private List<Tween> spinTweens = new List<Tween>();
    private List<Tween> winTweens = new List<Tween>();
    private List<int> reelCycleCount = new List<int>();
    private Coroutine winAnimationCoroutine;


    internal List<List<int>> currentDisplayMatrix;

    private bool isSpinning;
    private bool scatterAnticipationActive = false;

    // Sticky wild state persisted across spins during free spins
    private Dictionary<string, int> currentStickyWilds;

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
        DisableColumns(winAnimationColumns);
        DisableColumns(scatterStarColumns);
        DisableColumns(stickyWildColumns);
        if (anticipationFrame) anticipationFrame.SetActive(false);
        
        // Initialize masks as disabled and non-display icons as hidden
        DisableAllMasks();
        DisableAllNonDisplayIcons();
    }

    private static void DisableColumns(ColumnOverlays[] cols)
    {
        if (cols == null) return;
        foreach (var col in cols)
            if (col?.rows != null)
                foreach (var go in col.rows)
                    if (go) go.SetActive(false);
    }

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

        // Build the animation sprite arrays
        animationSpriteArrays = new List<Sprite>[16];
        animationSpriteArrays[0] = animSpritesSheriff;
        animationSpriteArrays[1] = animSpritesCowboyBlue;
        animationSpriteArrays[2] = animSpritesCowgirlGreen;
        animationSpriteArrays[3] = animSpritesCowgirlRed;
        animationSpriteArrays[4] = animSpritesTreasure;
        animationSpriteArrays[5] = animSpritesGun;
        animationSpriteArrays[6] = animSpritesA;
        animationSpriteArrays[7] = animSpritesK;
        animationSpriteArrays[8] = animSpritesQ;
        animationSpriteArrays[9] = animSpritesJ;
        animationSpriteArrays[10] = animSpritesTen;
        animationSpriteArrays[11] = animSpritesWild;
        animationSpriteArrays[12] = animSpritesScatter;
        animationSpriteArrays[13] = animSpritesWild2x;
        animationSpriteArrays[14] = animSpritesWild3x;
        animationSpriteArrays[15] = animSpritesWild5x;

        // Validate animation arrays
        for (int i = 0; i < animationSpriteArrays.Length; i++)
        {
            if (animationSpriteArrays[i] == null || animationSpriteArrays[i].Count == 0)
            {
                Debug.LogWarning($"[SlotView] Animation sprite array at index {i} is not assigned or empty!");
            }
        }

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

        // Re-enable sticky wilds so they stay visible during the spin
        if (currentStickyWilds != null && currentStickyWilds.Count > 0)
        {
            ApplyStickyWilds(currentStickyWilds);
        }

        // Enable masks and enable non-display icons when spinning starts
        EnableAllMasks();
        EnableAllNonDisplayIcons();

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
            
            // Ensure masks are disabled and non-display icons are hidden
            DisableAllMasks();
            DisableAllNonDisplayIcons();
            
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, onComplete, false));
    }

    private IEnumerator StopSpinSequence(List<List<int>> resultMatrix, System.Action onComplete, bool isQuickStop)
    {
        currentDisplayMatrix = resultMatrix;

        var currentResult = gameManager.lastResult;
        if (currentResult != null)
        {
            // Overlay scatter badges early so they appear while spinning
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

        int actualScatterId = gameManager.gameConfig != null ? gameManager.gameConfig.scatterSymbolId : scatterSymbolId;
        int scatterCount = 0;
        for (int col = 0; col < 4; col++) // Only check first 4 reels (0..3) for anticipation trigger
        {
            for (int row = 0; row < resultMatrix[col].Count; row++)
            {
                // Count ONLY the actual scatter ID, excluding wilds or any other symbols
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
            AudioManager.Instance?.PlayAnticipationFastSpin();
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

        // Disable masks and hide non-display icons when spinning stops
        DisableAllMasks();
        DisableAllNonDisplayIcons();

        if (currentResult != null)
        {
            // Sticky wilds — store state for next spin (overlays only shown during spinning)
            if (currentResult.stickyWilds != null)
            {
                currentStickyWilds = currentResult.stickyWilds;
                // Don't apply overlays here — they only show during spinning.
                // The reel symbols already display the correct wild sprite after stopping.
                DisableColumns(stickyWildColumns);
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

        // ── Play reel-stop sound immediately when symbols lock in ──────────
        AudioManager.Instance?.PlayReelStop();

        // Detect scatter / wild symbols in this column for hit sounds
        if (currentDisplayMatrix != null && columnIndex < currentDisplayMatrix.Count)
        {
            int actualScatterId = gameManager?.gameConfig != null
                ? gameManager.gameConfig.scatterSymbolId
                : scatterSymbolId;
            bool hasScatter = false;
            bool hasWild    = false;
            foreach (int sym in currentDisplayMatrix[columnIndex])
            {
                if (sym == actualScatterId)                              hasScatter = true;
                if (sym == 11 || sym == 13 || sym == 14 || sym == 15) hasWild    = true;
            }
            if (hasScatter) AudioManager.Instance?.PlayScatterHit();
            else if (hasWild) AudioManager.Instance?.PlayWildHit();
        }
        // ──────────────────────────────────────────────────────────────────

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

            quickStopSequence.OnComplete(() => PlayStopAnimationsForColumn(columnIndex));

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

            stopSequence.OnComplete(() => PlayStopAnimationsForColumn(columnIndex));

            spinTweens[columnIndex] = stopSequence;
        }
    }

    #endregion

    #region Quick Spin

    internal void QuickStop(List<List<int>> resultMatrix, System.Action onComplete = null)
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
                    PlayStopAnimationsForColumn(col);
                }
            }
            
            // Ensure masks are disabled and non-display icons are hidden
            DisableAllMasks();
            DisableAllNonDisplayIcons();

            onComplete?.Invoke();
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, onComplete, true));
    }

    #endregion




    #region Stop Symbol Animations

    private void PlayStopAnimationsForColumn(int col)
    {
        if (currentDisplayMatrix == null || col >= currentDisplayMatrix.Count) return;
        
        int actualScatterId = gameManager?.gameConfig != null ? gameManager.gameConfig.scatterSymbolId : scatterSymbolId;
        
        for (int row = 0; row < currentDisplayMatrix[col].Count; row++)
        {
            int symId = currentDisplayMatrix[col][row];
            bool isScatter = (symId == actualScatterId);
            bool isWild = (symId == 11 || symId == 13 || symId == 14 || symId == 15);
            
            if (isScatter || isWild)
            {
                AnimateSymbolSingleLoop(col, row, 1);
            }
        }
    }

    internal void AnimateAllScatters(int loopCount)
    {
        if (currentDisplayMatrix == null) return;

        // Clear any individual hit animations before starting the collective one
        KillWinTweens();

        int actualScatterId = gameManager?.gameConfig != null ? gameManager.gameConfig.scatterSymbolId : scatterSymbolId;
        
        for (int col = 0; col < 5; col++)
        {
            for (int row = 0; row < currentDisplayMatrix[col].Count; row++)
            {
                if (currentDisplayMatrix[col][row] == actualScatterId)
                {
                    AnimateSymbolSingleLoop(col, row, loopCount);
                }
            }
        }
    }

    private void AnimateSymbolSingleLoop(int column, int row, int loopCount = 1)
    {
        if (column >= reelImagesList.Count) return;

        var reel = reelImagesList[column];
        if (reel.images == null || reel.images.Count < 10) return;

        int imageIndex = 6 + row;
        if (imageIndex >= reel.images.Count) return;

        Image symbolImage = reel.images[imageIndex];
        if (symbolImage == null) return;

        var animGO = WinBox(winAnimationColumns, column, row);
        if (animGO == null) return;

        ImageAnimation imageAnim = animGO.GetComponent<ImageAnimation>();
        if (imageAnim == null) return;

        int symbolId = currentDisplayMatrix[column][row];
        if (symbolId < 0 || symbolId >= animationSpriteArrays.Length) return;

        List<Sprite> animSprites = animationSpriteArrays[symbolId];
        if (animSprites == null || animSprites.Count == 0) return;

        imageAnim.textureArray = animSprites;
        imageAnim.useDynamicFramerate = true;
        imageAnim.dynamicLoopDuration = winSymbolLoopDuration;

        Color originalColor = new Color(symbolImage.color.r, symbolImage.color.g, symbolImage.color.b, 1f);

        Sequence seq = DOTween.Sequence();
        
        seq.AppendCallback(() => {
            animGO.SetActive(true);
            Image animRenderer = imageAnim.rendererDelegate;
            if (animRenderer != null)
            {
                animRenderer.DOKill();
                Color c = animRenderer.color;
                animRenderer.color = new Color(c.r, c.g, c.b, 0f);
                animRenderer.DOFade(1f, 0.2f);
            }
            symbolImage.DOKill();
            symbolImage.DOFade(0f, 0.2f);
            
            imageAnim.StartAnimation();
        });

        seq.AppendInterval(winSymbolLoopDuration * loopCount);

        seq.AppendCallback(() => {
            Image animRenderer = imageAnim != null ? imageAnim.rendererDelegate : null;

            if (animRenderer != null)
            {
                animRenderer.DOKill();
                animRenderer.DOFade(0f, 0.2f).OnComplete(() => {
                    if (imageAnim != null) imageAnim.StopAnimation();
                    if (animGO != null) animGO.SetActive(false);
                });
            }
            else
            {
                if (imageAnim != null) imageAnim.StopAnimation();
                if (animGO != null) animGO.SetActive(false);
            }

            if (symbolImage != null)
            {
                symbolImage.DOKill();
                symbolImage.DOFade(originalColor.a, 0.2f);
            }
        });

        winTweens.Add(seq);
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

        for (int i = 0; i < winLines.Count; i++)
        {
            var line = winLines[i];
    
        }

        KillWinTweens();
        winAnimationCoroutine = StartCoroutine(PlayWinLinesSequentially(winLines, onComplete));
    }


    private IEnumerator PlayWinLinesSequentially(List<WinLine> winLines, System.Action onComplete)
    {
        bool skipScreen = gameManager != null && gameManager.uiManager != null && gameManager.uiManager.skipScreenToggle != null && gameManager.uiManager.skipScreenToggle.isOn;
        int loopCount = (gameManager != null && (gameManager.isInFreeSpins || gameManager.isAutoPlaying)) ? 1 : winSymbolLoopCount;
        float lineDuration = skipScreen ? 0.5f : winSymbolLoopDuration * loopCount;

        List<int> prevPositions = null;

        Debug.Log($"[PlayWinLinesSequentially] Starting win animation for {winLines.Count} lines");

        foreach (var winLine in winLines)
        {
            if (winLine.positions == null || winLine.positions.Count == 0) continue;

           
            if (prevPositions != null)
            {
                KillWinTweens(false);
                foreach (int flatIdx in prevPositions)
                {
                    int r = flatIdx / 5;
                    int c = flatIdx % 5;
                    DisableWinBox(c, r);
                    ResetSymbolScale(c, r);
                }
            }

            AudioManager.Instance?.PlayWinLine();

            foreach (int flatIndex in winLine.positions)
            {
                int row = flatIndex / 5;
                int col = flatIndex % 5;


                if (col < 0 || col >= 5 || row < 0 || row >= 4)
                {
                    Debug.LogWarning($"[PlayWinLinesSequentially] Invalid position! col: {col}, row: {row}");
                    continue;
                }

                EnableWinBox(col, row);

                AnimateWinSymbol(col, row);
            }

            prevPositions = new List<int>(winLine.positions);

            yield return new WaitForSeconds(lineDuration);
        }

        AudioManager.Instance?.StopWinLine();
        KillWinTweens(false);

        onComplete?.Invoke();
    }
    private void EnableWinBox(int col, int row)
    {
        var go = WinBox(winBoxColumns, col, row);
        if (go)
        {
            go.SetActive(true);
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

    private void ResetSymbolScale(int col, int row)
    {
        if (col >= reelImagesList.Count) return;
        var reel = reelImagesList[col];
        if (reel.images == null) return;
        int imageIndex = 6 + row;
        if (imageIndex >= reel.images.Count) return;
        if (reel.images[imageIndex] != null)
        {
            reel.images[imageIndex].DOKill();
            reel.images[imageIndex].transform.localScale = Vector3.one;
            // Restore alpha to full opacity
            Color c = reel.images[imageIndex].color;
            reel.images[imageIndex].color = new Color(c.r, c.g, c.b, 1f);
        }

        // Also ensure the corresponding animation object is disabled
        var animGO = WinBox(winAnimationColumns, col, row);
        if (animGO != null)
        {
            ImageAnimation imageAnim = animGO.GetComponent<ImageAnimation>();
            if (imageAnim != null)
            {
                if (imageAnim.rendererDelegate != null) imageAnim.rendererDelegate.DOKill();
                imageAnim.StopAnimation();
            }
            animGO.SetActive(false);
        }
    }


    private void AnimateWinSymbol(int column, int row)
    {

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

        bool skipScreen = gameManager != null && gameManager.uiManager != null && gameManager.uiManager.skipScreenToggle != null && gameManager.uiManager.skipScreenToggle.isOn;

        if (skipScreen)
        {
            Sequence popSeq = DOTween.Sequence();
            popSeq.AppendCallback(() => {
                symbolImage.DOKill();
                symbolImage.transform.localScale = Vector3.one;
            });
            popSeq.Append(symbolImage.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
            popSeq.Append(symbolImage.transform.DOScale(1f, 0.2f).SetEase(Ease.InBack));
            winTweens.Add(popSeq);
            return;
        }

        // Get the animation GameObject for this position
        var animGO = WinBox(winAnimationColumns, column, row);
        if (animGO == null)
        {
            Debug.LogError($"[AnimateWinSymbol] Animation GameObject is NULL at col: {column}, row: {row}");
            return;
        }

        // Get the ImageAnimation component
        ImageAnimation imageAnim = animGO.GetComponent<ImageAnimation>();
        if (imageAnim == null)
        {
            Debug.LogError($"[AnimateWinSymbol] ImageAnimation component not found on animation object at col: {column}, row: {row}");
            return;
        }

        // Get the current symbol ID at this position
        if (column >= currentDisplayMatrix.Count || row >= currentDisplayMatrix[column].Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Invalid matrix position col: {column}, row: {row}");
            return;
        }

        int symbolId = currentDisplayMatrix[column][row];
        
        // Validate symbolId
        if (symbolId < 0 || symbolId >= animationSpriteArrays.Length)
        {
            Debug.LogError($"[AnimateWinSymbol] Invalid symbolId {symbolId} at col: {column}, row: {row}");
            return;
        }

        // Get the animation sprite array for this symbol
        List<Sprite> animSprites = animationSpriteArrays[symbolId];
        if (animSprites == null || animSprites.Count == 0)
        {
            Debug.LogWarning($"[AnimateWinSymbol] No animation sprites for symbolId {symbolId} at col: {column}, row: {row}");
            return;
        }

        // Set the sprite array on the ImageAnimation component
        imageAnim.textureArray = animSprites;
        imageAnim.useDynamicFramerate = true;
        imageAnim.dynamicLoopDuration = winSymbolLoopDuration;

        Color originalColor = new Color(symbolImage.color.r, symbolImage.color.g, symbolImage.color.b, 1f);

        Sequence seq = DOTween.Sequence();
        
        seq.AppendCallback(() => {
            animGO.SetActive(true);
            Image animRenderer = imageAnim.rendererDelegate;
            if (animRenderer != null)
            {
                animRenderer.DOKill();
                Color c = animRenderer.color;
                animRenderer.color = new Color(c.r, c.g, c.b, 0f);
                animRenderer.DOFade(1f, 0.2f);
            }
            symbolImage.DOKill();
            symbolImage.DOFade(0f, 0.2f);
        });

        if (winLineBoxToAnimationDelay > 0)
        {
            seq.AppendInterval(winLineBoxToAnimationDelay);
        }

        seq.AppendCallback(() => {
            imageAnim.StartAnimation();
        });

        int loopCount = (gameManager != null && (gameManager.isInFreeSpins || gameManager.isAutoPlaying)) ? 1 : winSymbolLoopCount;
        seq.AppendInterval(winSymbolLoopDuration * loopCount);

        seq.AppendCallback(() => {
            Image animRenderer = imageAnim != null ? imageAnim.rendererDelegate : null;

            if (animRenderer != null)
            {
                animRenderer.DOKill();
                animRenderer.DOFade(0f, 0.2f).OnComplete(() => {
                    if (imageAnim != null) imageAnim.StopAnimation();
                    if (animGO != null) animGO.SetActive(false);
                });
            }
            else
            {
                if (imageAnim != null) imageAnim.StopAnimation();
                if (animGO != null) animGO.SetActive(false);
            }

            if (symbolImage != null)
            {
                symbolImage.DOKill();
                symbolImage.DOFade(originalColor.a, 0.2f);
            }
        });

        winTweens.Add(seq);
    }

    private void KillWinTweens(bool stopCoroutine = true)
    {
        foreach (var tween in winTweens)
        {
            tween?.Kill();
        }
        winTweens.Clear();

        if (stopCoroutine && winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }
        AudioManager.Instance?.StopWinLine();

        // Stop all win animations and disable animation GameObjects
        if (winAnimationColumns != null)
        {
            foreach (var col in winAnimationColumns)
            {
                if (col?.rows != null)
                {
                    foreach (var animGO in col.rows)
                    {
                        if (animGO != null && animGO.activeSelf)
                        {
                            ImageAnimation imageAnim = animGO.GetComponent<ImageAnimation>();
                            if (imageAnim != null)
                            {
                                if (imageAnim.rendererDelegate != null) imageAnim.rendererDelegate.DOKill();
                                imageAnim.StopAnimation();
                            }
                            animGO.SetActive(false);
                        }
                    }
                }
            }
        }

        DisableColumns(winBoxColumns);

        // Restore all symbol image alphas to full opacity
        foreach (var reel in reelImagesList)
        {
            if (reel.images != null)
            {
                foreach (var image in reel.images)
                {
                    if (image != null)
                    {
                        image.DOKill();
                        image.transform.localScale = Vector3.one;
                        Color c = image.color;
                        image.color = new Color(c.r, c.g, c.b, 1f);
                    }
                }
            }
        }
    }

    #endregion

    #region Mask and Non-Display Icon Management

    private void EnableAllMasks()
    {
        if (reelMask == null) return;
        reelMask.enabled = true;
    }

    private void DisableAllMasks()
    {
        if (reelMask == null) return;
        reelMask.enabled = false;
    }

    

    private void EnableAllNonDisplayIcons()
    {
        if (reelNonDisplayIconsList == null) return;

        foreach (var reelIcons in reelNonDisplayIconsList)
        {
            if (reelIcons.topIcons != null)
            {
                foreach (var icon in reelIcons.topIcons)
                {
                    if (icon != null) icon.SetActive(true);
                }
            }

            if (reelIcons.bottomIcons != null)
            {
                foreach (var icon in reelIcons.bottomIcons)
                {
                    if (icon != null) icon.SetActive(true);
                }
            }
        }
    }

    private void DisableAllNonDisplayIcons()
    {
        if (reelNonDisplayIconsList == null) return;

        foreach (var reelIcons in reelNonDisplayIconsList)
        {
            if (reelIcons.topIcons != null)
            {
                foreach (var icon in reelIcons.topIcons)
                {
                    if (icon != null) icon.SetActive(false);
                }
            }

            if (reelIcons.bottomIcons != null)
            {
                foreach (var icon in reelIcons.bottomIcons)
                {
                    if (icon != null) icon.SetActive(false);
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

    /// <summary>
    /// Apply sticky wild overlays: enable the overlay GO and set its Image sprite
    /// to the correct wild multiplier variant.
    /// </summary>
    private void ApplyStickyWilds(Dictionary<string, int> stickyWilds)
    {
        if (stickyWilds == null) return;

        foreach (var kvp in stickyWilds)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int row) &&
                int.TryParse(parts[1], out int col))
            {
                var go = WinBox(stickyWildColumns, col, row);
                if (go)
                {
                    go.SetActive(true);

                    // Set the overlay image to the correct wild multiplier sprite
                    int multiplier = kvp.Value;
                    int wildSpriteId = GetWildSpriteIdForMultiplier(multiplier);
                    Image img = go.GetComponent<Image>();
                    if (img != null && wildSpriteId >= 0 && wildSpriteId < symbolSprites.Length)
                    {
                        img.sprite = symbolSprites[wildSpriteId];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Maps a wild multiplier value to the correct sprite ID.
    /// 1x → 11 (Wild), 2x → 13 (Wild2x), 3x → 14 (Wild3x), 5x → 15 (Wild5x)
    /// </summary>
    private int GetWildSpriteIdForMultiplier(int multiplier)
    {
        return multiplier switch
        {
            1 => 11,
            2 => 13,
            3 => 14,
            5 => 15,
            _ => 11
        };
    }

    /// <summary>
    /// Clear sticky wild state — call when free spins end.
    /// </summary>
    internal void ClearStickyWilds()
    {
        currentStickyWilds = null;
        DisableColumns(stickyWildColumns);
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


[System.Serializable]
public class ColumnOverlays
{
    [Tooltip("Row 0 = top, Row 1, Row 2, Row 3 = bottom")]
    public GameObject[] rows = new GameObject[4];
}

[System.Serializable]
public class ReelNonDisplayIcons
{
    [Tooltip("Top non-display icons (indices 0-5) that should be hidden when not spinning")]
    public List<GameObject> topIcons = new List<GameObject>();
    
    [Tooltip("Bottom non-display icons (indices 10-15) that should be hidden when not spinning")]
    public List<GameObject> bottomIcons = new List<GameObject>();
}