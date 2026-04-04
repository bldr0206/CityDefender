using ColorChargeTD.Data;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class BuildSlotWorldHandle : MonoBehaviour
    {
        private string slotId;
        private TextMesh plusMesh;
        private Collider slotCollider;

        public string SlotId => slotId;

        #region Setup

        public void Initialize(BuildSlotRuntimeDefinition slot)
        {
            slotId = slot.SlotId;
            transform.position = slot.Position;

            float r = Mathf.Max(0.35f, slot.Radius);
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(r * 2f, 0.35f, r * 2f);
            box.center = new Vector3(0f, 0.08f, 0f);
            slotCollider = box;

            GameObject plusGo = new GameObject("Plus");
            plusGo.transform.SetParent(transform, false);
            plusGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            plusMesh = plusGo.AddComponent<TextMesh>();
            plusMesh.text = "+";
            plusMesh.fontSize = 260;
            plusMesh.characterSize = 0.055f;
            plusMesh.anchor = TextAnchor.MiddleCenter;
            plusMesh.alignment = TextAlignment.Center;
            plusMesh.color = new Color(0.85f, 1f, 0.9f, 1f);
            plusMesh.fontStyle = FontStyle.Bold;
        }

        public void SetBuildableVisible(bool visible)
        {
            if (plusMesh != null)
            {
                plusMesh.gameObject.SetActive(visible);
            }

            if (slotCollider != null)
            {
                slotCollider.enabled = visible;
            }
        }

        #endregion

        #region UnityLifecycle

        private void LateUpdate()
        {
            if (plusMesh == null || !plusMesh.gameObject.activeSelf)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 toCam = plusMesh.transform.position - cam.transform.position;
            if (toCam.sqrMagnitude < 0.0001f)
            {
                return;
            }

            plusMesh.transform.rotation = Quaternion.LookRotation(toCam);
        }

        #endregion
    }
}
