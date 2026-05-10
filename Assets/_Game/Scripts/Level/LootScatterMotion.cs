using System;
using DG.Tweening;
using UnityEngine;

public sealed class LootScatterMotion : MonoBehaviour
{
    Tween _motionTween;

    public void FlyToWorldPosition(Vector3 worldStart, Vector3 worldEnd, float durationSeconds, float arcHeight, Action onComplete)
    {
        _motionTween?.Kill(false);
        transform.position = worldStart;

        Vector3 mid = Vector3.Lerp(worldStart, worldEnd, 0.5f) + Vector3.up * arcHeight;
        Vector3[] path = { mid, worldEnd };

        _motionTween = transform
            .DOPath(path, durationSeconds, PathType.Linear)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _motionTween = null;
                onComplete?.Invoke();
            });
    }

    void OnDestroy()
    {
        _motionTween?.Kill(false);
        _motionTween = null;
    }
}
