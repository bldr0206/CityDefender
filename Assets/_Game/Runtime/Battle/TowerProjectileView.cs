using DG.Tweening;
using UnityEngine;

namespace ColorChargeTD.Battle
{
    public sealed class TowerProjectileView : MonoBehaviour
    {
        private Tween flightTween;

        private void OnDestroy()
        {
            KillFlightTween();
        }

        public void BeginFlight(Vector3 worldStart, Transform targetTransform, float duration, float arcPeakHeight)
        {
            transform.position = worldStart;
            KillFlightTween();

            if (targetTransform == null || duration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 start = worldStart;
            Transform target = targetTransform;

            flightTween = DOTween.To(
                    () => 0f,
                    t =>
                    {
                        if (target == null)
                        {
                            KillFlightTween();
                            Destroy(gameObject);
                            return;
                        }

                        transform.position = SampleArcPoint(start, target.position, t, arcPeakHeight);
                    },
                    1f,
                    duration)
                .SetEase(Ease.Linear)
                .SetTarget(this)
                .SetLink(gameObject)
                .OnComplete(OnFlightTweenComplete);
        }

        #region Flight
        private void OnFlightTweenComplete()
        {
            flightTween = null;
            Destroy(gameObject);
        }

        private void KillFlightTween()
        {
            if (flightTween != null && flightTween.IsActive())
            {
                flightTween.Kill(false);
            }

            flightTween = null;
        }
        #endregion

        #region Arc sampling
        private static Vector3 SampleArcPoint(Vector3 start, Vector3 end, float u, float arcPeakHeight)
        {
            Vector3 basePos = Vector3.Lerp(start, end, u);
            float arc = arcPeakHeight * 4f * u * (1f - u);
            return basePos + Vector3.up * arc;
        }
        #endregion
    }
}
