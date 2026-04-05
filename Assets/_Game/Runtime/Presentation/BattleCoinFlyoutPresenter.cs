using System.Collections;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class BattleCoinFlyoutPresenter : MonoBehaviour
    {
        #region Serialized

        [SerializeField] private RectTransform flyLayer;
        [SerializeField] private RectTransform flyTarget;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private BattleHudView hudView;
        [SerializeField] private float staggerSeconds = 0.04f;
        [SerializeField] private int maxVisualCoins = 8;
        [SerializeField] private float spawnSpreadPixels = 18f;

        #endregion

        #region PublicAPI

        public void OnEnemyKilled(Vector3 worldPosition, int reward)
        {
            if (reward <= 0 || flyLayer == null || flyTarget == null || coinPrefab == null)
            {
                return;
            }

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            int visual = Mathf.Clamp(reward, 1, maxVisualCoins);
            StartCoroutine(SpawnStaggered(worldPosition, visual, cam));
        }

        public void OnPeriodicIncomePayout(Vector3 worldPosition, int amount)
        {
            if (amount <= 0 || flyLayer == null || flyTarget == null || coinPrefab == null)
            {
                return;
            }

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            int visual = Mathf.Clamp(amount, 1, maxVisualCoins);
            StartCoroutine(SpawnStaggered(worldPosition, visual, cam));
        }

        #endregion

        #region Spawn

        private IEnumerator SpawnStaggered(Vector3 worldPosition, int count, Camera cam)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnOne(worldPosition, cam);
                if (i < count - 1)
                {
                    yield return new WaitForSecondsRealtime(staggerSeconds);
                }
            }
        }

        private void SpawnOne(Vector3 worldPosition, Camera cam)
        {
            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f)
            {
                return;
            }

            Vector2 offset = Random.insideUnitCircle * spawnSpreadPixels;
            screen.x += offset.x;
            screen.y += offset.y;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    flyLayer,
                    screen,
                    null,
                    out Vector2 localStart))
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    flyLayer,
                    RectTransformUtility.WorldToScreenPoint(null, flyTarget.position),
                    null,
                    out Vector2 localEnd))
            {
                localEnd = localStart;
            }

            GameObject instance = Instantiate(coinPrefab, flyLayer);
            CoinFlyoutView view = instance.GetComponent<CoinFlyoutView>();
            if (view == null)
            {
                Destroy(instance);
                return;
            }

            view.BeginFly(flyLayer, localStart, localEnd, () => hudView?.PlayResourceEarnPulse());
        }

        #endregion
    }
}
