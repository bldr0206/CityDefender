using ColorChargeTD.Data;
using TMPro;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class BuildSlotWorldHandle : MonoBehaviour
    {
        private string slotId;
        private BuildSlotKind slotKind;
        private Transform plusVisualRoot;
        private Collider slotCollider;
        private TMP_Text tmpPlus;
        private TextMesh legacyPlusMesh;

        public string SlotId => slotId;

        #region Setup

        public void Initialize(BuildSlotRuntimeDefinition slot, GameObject plusVisualPrefab)
        {
            slotId = slot.SlotId;
            slotKind = slot.Kind;
            transform.position = slot.Position;

            float r = Mathf.Max(0.35f, slot.Radius);
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(r * 2f, 0.35f, r * 2f);
            box.center = new Vector3(0f, 0.08f, 0f);
            slotCollider = box;

            Vector3 plusLocalPos = new Vector3(0f, 0.45f, 0f);

            if (plusVisualPrefab != null)
            {
                GameObject instance = Instantiate(plusVisualPrefab, transform);
                instance.name = "PlusVisual";
                Transform t = instance.transform;
                t.localPosition = plusLocalPos;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                plusVisualRoot = t;
                tmpPlus = instance.GetComponentInChildren<TMP_Text>(true);
            }
            else
            {
                GameObject plusGo = new GameObject("Plus");
                plusGo.transform.SetParent(transform, false);
                plusGo.transform.localPosition = plusLocalPos;
                TextMesh plusMesh = plusGo.AddComponent<TextMesh>();
                plusMesh.text = "+";
                plusMesh.fontSize = 260;
                plusMesh.characterSize = 0.055f;
                plusMesh.anchor = TextAnchor.MiddleCenter;
                plusMesh.alignment = TextAlignment.Center;
                plusMesh.fontStyle = FontStyle.Bold;
                legacyPlusMesh = plusMesh;
                plusVisualRoot = plusGo.transform;
            }

            SetAffordableVisual(false);
        }

        public void SetPlusVisible(bool visible)
        {
            if (plusVisualRoot != null)
            {
                plusVisualRoot.gameObject.SetActive(visible);
            }
        }

        public void SetSlotRaycastEnabled(bool enabled)
        {
            if (slotCollider != null)
            {
                slotCollider.enabled = enabled;
            }
        }

        public void SetAffordableVisual(bool affordable)
        {
            Color color = BuildSlotVisualPalette.PlusAffordableTint(slotKind, affordable);
            if (tmpPlus != null)
            {
                tmpPlus.color = color;
            }

            if (legacyPlusMesh != null)
            {
                legacyPlusMesh.color = color;
            }
        }

        #endregion

        #region UnityLifecycle

        private void LateUpdate()
        {
            if (plusVisualRoot == null || !plusVisualRoot.gameObject.activeSelf)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 toCam = plusVisualRoot.position - cam.transform.position;
            if (toCam.sqrMagnitude < 0.0001f)
            {
                return;
            }

            plusVisualRoot.rotation = Quaternion.LookRotation(toCam);
        }

        #endregion
    }
}
