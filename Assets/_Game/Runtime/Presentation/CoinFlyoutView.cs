using System;
using System.Collections;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class CoinFlyoutView : MonoBehaviour
    {
        #region Serialized

        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve scaleCurve;

        #endregion

        #region Fly

        public void BeginFly(RectTransform parent, Vector2 localStart, Vector2 localEnd, Action onComplete)
        {
            RectTransform rt = rectTransform != null ? rectTransform : (RectTransform)transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = localStart;
            Vector3 baseScale = Vector3.one;
            rt.localScale = baseScale;

            StartCoroutine(FlyRoutine(rt, localStart, localEnd, baseScale, onComplete));
        }

        private IEnumerator FlyRoutine(RectTransform rt, Vector2 start, Vector2 end, Vector3 baseScale, Action onComplete)
        {
            float t = 0f;
            float dur = Mathf.Max(0.05f, duration);

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float moveT = moveCurve != null && moveCurve.length > 0 ? moveCurve.Evaluate(u) : u;
                float scaleT = EvaluateScale(u);

                rt.anchoredPosition = Vector2.LerpUnclamped(start, end, moveT);
                float s = Mathf.Lerp(0.65f, 1.05f, scaleT);
                rt.localScale = baseScale * s;

                yield return null;
            }

            rt.anchoredPosition = end;
            rt.localScale = baseScale;
            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private float EvaluateScale(float u)
        {
            if (scaleCurve != null && scaleCurve.length > 0)
            {
                return Mathf.Clamp01(scaleCurve.Evaluate(u));
            }

            if (u < 0.15f)
            {
                return u / 0.15f;
            }

            return 1f;
        }

        #endregion
    }
}
