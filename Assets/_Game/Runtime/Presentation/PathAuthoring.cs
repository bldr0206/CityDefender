using System;
using System.Collections.Generic;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class PathAuthoring : MonoBehaviour
    {
        [SerializeField] private string pathId = "path-a";
        [SerializeField] private EnemyMovePattern movePattern = EnemyMovePattern.SingleLane;
        [SerializeField] private Transform[] waypoints = Array.Empty<Transform>();
        [SerializeField] private bool autoCollectChildren = true;

        private void OnValidate()
        {
            if (!autoCollectChildren)
            {
                return;
            }

            List<Transform> collectedWaypoints = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                collectedWaypoints.Add(transform.GetChild(i));
            }

            waypoints = collectedWaypoints.ToArray();
        }

        public bool TryBuildDefinition(out LevelPathRuntimeDefinition definition, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(pathId))
            {
                definition = default;
                error = "Path id is required.";
                return false;
            }

            if (waypoints == null || waypoints.Length < 2)
            {
                definition = default;
                error = "Each path needs at least two waypoints.";
                return false;
            }

            Vector3[] runtimeWaypoints = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                runtimeWaypoints[i] = waypoints[i] != null ? waypoints[i].position : Vector3.zero;
            }

            definition = new LevelPathRuntimeDefinition(pathId, movePattern, runtimeWaypoints);
            return true;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            Gizmos.color = Color.yellow;

            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform waypoint = waypoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(waypoint.position, 0.18f);

                if (i >= waypoints.Length - 1 || waypoints[i + 1] == null)
                {
                    continue;
                }

                Gizmos.DrawLine(waypoint.position, waypoints[i + 1].position);
            }
        }
    }
}
