using System.Collections;
using UnityEngine;

namespace ColorChargeTD.Battle
{
    public sealed class TowerProjectileView : MonoBehaviour
    {
        private Coroutine flightRoutine;

        private void OnDestroy()
        {
            if (flightRoutine != null)
            {
                StopCoroutine(flightRoutine);
                flightRoutine = null;
            }
        }

        public void BeginFlight(Vector3 worldStart, Transform targetTransform, float duration)
        {
            transform.position = worldStart;
            if (flightRoutine != null)
            {
                StopCoroutine(flightRoutine);
                flightRoutine = null;
            }

            if (targetTransform == null || duration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            flightRoutine = StartCoroutine(FlightRoutine(worldStart, targetTransform, duration));
        }

        private IEnumerator FlightRoutine(Vector3 start, Transform target, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                {
                    flightRoutine = null;
                    Destroy(gameObject);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, target.position, t);
                yield return null;
            }

            flightRoutine = null;
            Destroy(gameObject);
        }
    }
}
