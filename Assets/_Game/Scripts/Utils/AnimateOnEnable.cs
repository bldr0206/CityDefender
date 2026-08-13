using DG.Tweening;
using UnityEngine;

public class AnimateOnEnable : MonoBehaviour
{
    private enum AnimationPreset
    {
        ScaleUp,
        ScaleFromLarge,
        AttentionBounce,
        PunchScale,
        FloatUpAndSettle,
        RotateWiggle
    }

    [Header("Preset")]
    [SerializeField] private AnimationPreset preset = AnimationPreset.ScaleUp;
    [SerializeField, Min(0.05f)] private float duration = 0.35f;

    private Vector3 baseScale;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Tween activeTween;

    private void Awake()
    {
        CacheBaseTransform();
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        activeTween?.Kill();
    }

    private void CacheBaseTransform()
    {
        baseScale = transform.localScale;
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }

    private void Play()
    {
        activeTween?.Kill();
        CacheBaseTransform();
        // SetUpdate(true) — анимация появления играет и на паузе (timeScale = 0)
        activeTween = CreateTween().SetUpdate(true);
    }

    private Tween CreateTween()
    {
        switch (preset)
        {
            case AnimationPreset.ScaleUp:
                transform.localScale = baseScale * 0.85f;
                return transform
                    .DOScale(baseScale, duration)
                    .SetEase(Ease.OutBack);

            case AnimationPreset.ScaleFromLarge:
                transform.localScale = baseScale * 1.2f;
                return transform
                    .DOScale(baseScale, duration)
                    .SetEase(Ease.OutCubic);

            case AnimationPreset.AttentionBounce:
                {
                    transform.localScale = baseScale;
                    var sequence = DOTween.Sequence();
                    sequence
                        .Append(transform.DOScale(baseScale * 1.1f, duration * 0.45f).SetEase(Ease.OutQuad))
                        .Append(transform.DOScale(baseScale * 0.95f, duration * 0.25f).SetEase(Ease.InOutQuad))
                        .Append(transform.DOScale(baseScale * 1.05f, duration * 0.25f).SetEase(Ease.InOutQuad))
                        .Append(transform.DOScale(baseScale, duration * 0.3f).SetEase(Ease.OutQuad));
                    return sequence;
                }

            case AnimationPreset.PunchScale:
                transform.localScale = baseScale;
                return transform
                    .DOPunchScale(Vector3.one * 0.2f, duration, 8);

            case AnimationPreset.FloatUpAndSettle:
                {
                    transform.localPosition = baseLocalPosition + Vector3.up * 24f;
                    transform.localScale = baseScale * 0.9f;
                    var sequence = DOTween.Sequence();
                    sequence
                        .Join(transform.DOLocalMove(baseLocalPosition, duration).SetEase(Ease.OutCubic))
                        .Join(transform.DOScale(baseScale, duration).SetEase(Ease.OutBack));
                    return sequence;
                }

            case AnimationPreset.RotateWiggle:
                {
                    transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, -8f);
                    var sequence = DOTween.Sequence();
                    sequence
                        .Append(transform.DOLocalRotateQuaternion(baseLocalRotation * Quaternion.Euler(0f, 0f, 8f), duration * 0.45f).SetEase(Ease.OutQuad))
                        .Append(transform.DOLocalRotateQuaternion(baseLocalRotation, duration * 0.55f).SetEase(Ease.OutSine));
                    return sequence;
                }

            default:
                transform.localScale = baseScale * 0.85f;
                return transform
                    .DOScale(baseScale, duration)
                    .SetEase(Ease.OutBack);
        }
    }
}
