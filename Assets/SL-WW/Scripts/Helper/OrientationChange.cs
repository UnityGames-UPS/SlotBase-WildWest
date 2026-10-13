using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class OrientationChange : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform UIWrapper;
    [SerializeField] private CanvasScaler CanvasScaler;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private float waitForRotation = 0.2f;
    private Vector2 referenceResolution;
    private Tween matchTween;
    private Tween rotationTween;
    private Coroutine rotationRoutine;
    private bool isLandscape;

    private void Awake()
    {
        referenceResolution = CanvasScaler.referenceResolution; 

        ApplyMatch(Screen.width, Screen.height, instant: true);
    }

    void SwitchDisplay(string dimensions)
    {
        if (rotationRoutine != null) StopCoroutine(rotationRoutine);
        rotationRoutine = StartCoroutine(RotationCoroutine(dimensions));
    }

    IEnumerator RotationCoroutine(string dimensions)
    {
        yield return new WaitForSecondsRealtime(waitForRotation);

        string[] parts = dimensions.Split(',');
        if (parts.Length == 2
            && int.TryParse(parts[0], out int w)
            && int.TryParse(parts[1], out int h)
            && w > 0 && h > 0)
        {
            ApplyMatch(w, h, instant: false);
        }
        else
        {
            Debug.LogWarning("[OrientationChange] Invalid dimensions: " + dimensions);
        }
    }

    private void ApplyMatch(int screenW, int screenH, bool instant)
    {
        isLandscape = screenW > screenH;

        Quaternion targetRotation = isLandscape ? Quaternion.identity : Quaternion.Euler(0, 0, -90);
        if (UIWrapper != null)
        {
            if (rotationTween != null && rotationTween.IsActive()) rotationTween.Kill();
            if (instant)
                UIWrapper.localRotation = targetRotation;
            else
                rotationTween = UIWrapper.DOLocalRotateQuaternion(targetRotation, transitionDuration).SetEase(Ease.OutCubic);
        }

        float refW = referenceResolution.x;
        float refH = referenceResolution.y;

        float widthScale = screenW / refW;
        float heightScale = screenH / refH;

        float targetMatch;
        if (Mathf.Abs(heightScale - widthScale) < 0.0001f)
        {
            targetMatch = 0.5f;
        }
        else
        {
            float targetScale = isLandscape
                ? Mathf.Min(widthScale, heightScale)
                : Mathf.Min((float)screenH / refW, (float)screenW / refH);
            float logRatio = Mathf.Log(heightScale / widthScale);
            targetMatch = Mathf.Clamp01(Mathf.Log(targetScale / widthScale) / logRatio);
        }

        if (instant)
        {
            CanvasScaler.matchWidthOrHeight = targetMatch;
            return;
        }

        if (matchTween != null && matchTween.IsActive()) matchTween.Kill();
        matchTween = DOTween
          .To(
            () => CanvasScaler.matchWidthOrHeight,
            x => CanvasScaler.matchWidthOrHeight = x,
            targetMatch,
            transitionDuration)
          .SetEase(Ease.InOutQuad);
    }
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SwitchDisplay($"{Screen.width},{Screen.height}");
    }
#endif
}