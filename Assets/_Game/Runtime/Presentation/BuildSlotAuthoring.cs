using ColorChargeTD.Data;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class BuildSlotAuthoring : MonoBehaviour
    {
        [SerializeField] private string slotId = "slot-01";
        [SerializeField] private float radius = 1.5f;

        public BuildSlotRuntimeDefinition BuildDefinition()
        {
            return new BuildSlotRuntimeDefinition(slotId, transform.position, Mathf.Max(0.1f, radius));
        }

        private void OnDrawGizmos()
        {
            Color color = new Color(0.2f, 0.8f, 0.4f, 0.85f);
            float r = Mathf.Max(0.1f, radius);
            Vector3 center = transform.position;
            Vector3 normal = transform.up;
            Vector3 tangent = Vector3.Cross(normal, Vector3.forward);
            if (tangent.sqrMagnitude < 1e-6f)
            {
                tangent = Vector3.Cross(normal, Vector3.right);
            }

            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            const int segments = 48;
            Gizmos.color = color;
            Vector3 prev = center + tangent * r;
            for (int i = 1; i <= segments + 1; i++)
            {
                float t = (float)i / segments * Mathf.PI * 2f;
                Vector3 next = center + (tangent * Mathf.Cos(t) + bitangent * Mathf.Sin(t)) * r;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            float mark = Mathf.Min(0.14f, r * 0.12f);
            Gizmos.DrawLine(center - tangent * mark, center + tangent * mark);
            Gizmos.DrawLine(center - bitangent * mark, center + bitangent * mark);
        }
    }
}
