using System;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class PathAuthoring : MonoBehaviour
    {
        #region SerializedFields
        [SerializeField] private string pathId = "path-a";
        [SerializeField] private EnemyMovePattern movePattern = EnemyMovePattern.SingleLane;
        [SerializeField] private Transform[] waypoints = Array.Empty<Transform>();
        [SerializeField] private bool autoCollectChildren = true;
        #endregion

        #region PublicAPI
        public bool AutoCollectChildren => autoCollectChildren;

        public void SyncWaypointsFromChildren()
        {
            if (!autoCollectChildren)
            {
                return;
            }

            Transform[] collected = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                collected[i] = transform.GetChild(i);
            }

            waypoints = collected;
        }

        public bool WaypointsDifferFromChildOrder()
        {
            if (!autoCollectChildren)
            {
                return false;
            }

            if (waypoints == null || transform.childCount != waypoints.Length)
            {
                return true;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (waypoints[i] != transform.GetChild(i))
                {
                    return true;
                }
            }

            return false;
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

            Transform[] sourceTransforms = ResolveWaypointTransforms();
            if (sourceTransforms == null || sourceTransforms.Length < 2)
            {
                definition = default;
                error = "Each path needs at least two waypoints.";
                return false;
            }

            Vector3[] runtimeWaypoints = new Vector3[sourceTransforms.Length];
            for (int i = 0; i < sourceTransforms.Length; i++)
            {
                Transform t = sourceTransforms[i];
                runtimeWaypoints[i] = t != null ? t.position : Vector3.zero;
            }

            definition = new LevelPathRuntimeDefinition(pathId, movePattern, runtimeWaypoints);
            return true;
        }
        #endregion

        #region UnityCallbacks
        private void OnValidate()
        {
            SyncWaypointsFromChildren();
        }

        private void OnDrawGizmos()
        {
            Transform[] drawPoints = ResolveWaypointTransforms();
            if (drawPoints == null || drawPoints.Length == 0)
            {
                return;
            }

            Gizmos.color = Color.yellow;

            for (int i = 0; i < drawPoints.Length; i++)
            {
                Transform waypoint = drawPoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(waypoint.position, 0.18f);

                if (i >= drawPoints.Length - 1 || drawPoints[i + 1] == null)
                {
                    continue;
                }

                Gizmos.DrawLine(waypoint.position, drawPoints[i + 1].position);
            }
        }
        #endregion

        #region Internals
        private Transform[] ResolveWaypointTransforms()
        {
            if (autoCollectChildren)
            {
                if (transform.childCount == 0)
                {
                    return Array.Empty<Transform>();
                }

                Transform[] fromChildren = new Transform[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    fromChildren[i] = transform.GetChild(i);
                }

                return fromChildren;
            }

            return waypoints;
        }
        #endregion
    }
}
