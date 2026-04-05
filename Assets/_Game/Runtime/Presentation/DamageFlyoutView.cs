using System.Collections;
using TMPro;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class DamageFlyoutView : MonoBehaviour
    {
        #region Serialized

        [SerializeField] private TMP_Text label;
        [SerializeField] private float riseDuration = 0.32f;
        [SerializeField] private float holdDuration = 0.14f;
        [SerializeField] private float fadeDuration = 0.22f;
        [SerializeField] private float risePixels = 64f;
        [SerializeField] private float popDuration = 0.08f;
        [SerializeField] private float popFromScale = 0.55f;

        #endregion

        #region Play

        public void Play(int damage)
        {
            TMP_Text text = label != null ? label : GetComponent<TMP_Text>();
            if (text == null)
            {
                Destroy(gameObject);
                return;
            }

            text.text = damage.ToString();
            RectTransform rt = text.rectTransform;
            Vector2 start = rt.anchoredPosition;
            Vector2 end = start + new Vector2(0f, risePixels);
            StartCoroutine(PlayRoutine(rt, text, start, end));
        }

        private IEnumerator PlayRoutine(RectTransform rt, TMP_Text text, Vector2 start, Vector2 end)
        {
            float riseDur = Mathf.Max(0.02f, riseDuration);
            float holdDur = Mathf.Max(0f, holdDuration);
            float fadeDur = Mathf.Max(0.02f, fadeDuration);
            float popDur = Mathf.Max(0.01f, popDuration);

            Color c = text.color;
            c.a = 1f;
            text.color = c;

            float popT = 0f;
            while (popT < popDur)
            {
                popT += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(popT / popDur);
                float s = Mathf.Lerp(popFromScale, 1f, EaseOutCubic(u));
                rt.localScale = Vector3.one * s;
                yield return null;
            }

            rt.localScale = Vector3.one;

            float t = 0f;
            while (t < riseDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / riseDur);
                float eased = EaseOutQuart(u);
                rt.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                yield return null;
            }

            rt.anchoredPosition = end;

            if (holdDur > 0f)
            {
                yield return new WaitForSecondsRealtime(holdDur);
            }

            t = 0f;
            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / fadeDur);
                c.a = 1f - u;
                text.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }

        #endregion

        #region Easing

        private static float EaseOutCubic(float u)
        {
            float v = 1f - u;
            return 1f - v * v * v;
        }

        private static float EaseOutQuart(float u)
        {
            float v = 1f - u;
            return 1f - v * v * v * v;
        }

        #endregion
    }
}
