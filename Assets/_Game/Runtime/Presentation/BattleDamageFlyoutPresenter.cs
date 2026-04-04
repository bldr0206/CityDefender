using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class BattleDamageFlyoutPresenter : MonoBehaviour
    {
        #region Serialized

        [SerializeField] private RectTransform flyLayer;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject damageFlyoutPrefab;
        [SerializeField] private float worldYOffset = 0.45f;
        [SerializeField] private float spawnSpreadPixels = 12f;

        #endregion

        #region PublicAPI

        public void ShowDamage(Vector3 worldPosition, int damage)
        {
            if (damage <= 0 || flyLayer == null || damageFlyoutPrefab == null)
            {
                return;
            }

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 origin = worldPosition + Vector3.up * worldYOffset;
            Vector3 screen = cam.WorldToScreenPoint(origin);
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

            GameObject instance = Instantiate(damageFlyoutPrefab, flyLayer);
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt == null)
            {
                Destroy(instance);
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = localStart;
            rt.localScale = Vector3.one;

            DamageFlyoutView view = instance.GetComponent<DamageFlyoutView>();
            if (view == null)
            {
                Destroy(instance);
                return;
            }

            view.Play(damage);
        }

        #endregion
    }
}
