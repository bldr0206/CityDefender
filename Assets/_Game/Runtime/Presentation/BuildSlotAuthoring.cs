using System.Collections.Generic;
using ColorChargeTD.Data;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class BuildSlotAuthoring : MonoBehaviour
    {
        [SerializeField] private string slotId = "slot-01";
        [SerializeField] private float radius = 1.5f;
        [SerializeField] private BuildSlotKind kind = BuildSlotKind.Tower;
        [SerializeField] private List<AuxiliaryBuildingDefinition> allowedAuxiliaryBuildings = new List<AuxiliaryBuildingDefinition>();
        [SerializeField] private List<RoadTrapDefinition> allowedRoadTraps = new List<RoadTrapDefinition>();

        public BuildSlotRuntimeDefinition BuildDefinition()
        {
            float r = Mathf.Max(0.1f, radius);
            AuxiliaryBuildingDefinition[] aux = kind == BuildSlotKind.Auxiliary ? ToAuxiliaryArray(allowedAuxiliaryBuildings) : null;
            RoadTrapDefinition[] traps = kind == BuildSlotKind.RoadTrap ? ToRoadTrapArray(allowedRoadTraps) : null;
            return new BuildSlotRuntimeDefinition(slotId, transform.position, r, kind, aux, traps);
        }

        #region UnityAuthoring

        private void OnValidate()
        {
            if (kind == BuildSlotKind.Auxiliary && !HasAnyNonNull(allowedAuxiliaryBuildings))
            {
                Debug.LogWarning("BuildSlotAuthoring '" + name + "': Auxiliary slot has no allowed buildings assigned.", this);
            }

            if (kind == BuildSlotKind.RoadTrap && !HasAnyNonNull(allowedRoadTraps))
            {
                Debug.LogWarning("BuildSlotAuthoring '" + name + "': Road trap slot has no allowed traps assigned.", this);
            }
        }

        private void OnDrawGizmos()
        {
            Color color = BuildSlotVisualPalette.GizmoCircleColor(kind);
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

        #endregion

        #region Helpers

        private static AuxiliaryBuildingDefinition[] ToAuxiliaryArray(List<AuxiliaryBuildingDefinition> list)
        {
            if (list == null || list.Count == 0)
            {
                return System.Array.Empty<AuxiliaryBuildingDefinition>();
            }

            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return System.Array.Empty<AuxiliaryBuildingDefinition>();
            }

            AuxiliaryBuildingDefinition[] result = new AuxiliaryBuildingDefinition[count];
            int index = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    result[index++] = list[i];
                }
            }

            return result;
        }

        private static RoadTrapDefinition[] ToRoadTrapArray(List<RoadTrapDefinition> list)
        {
            if (list == null || list.Count == 0)
            {
                return System.Array.Empty<RoadTrapDefinition>();
            }

            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return System.Array.Empty<RoadTrapDefinition>();
            }

            RoadTrapDefinition[] result = new RoadTrapDefinition[count];
            int index = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    result[index++] = list[i];
                }
            }

            return result;
        }

        private static bool HasAnyNonNull<T>(List<T> list) where T : class
        {
            if (list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
