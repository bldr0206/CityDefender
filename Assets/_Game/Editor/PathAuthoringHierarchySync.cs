using ColorChargeTD.Presentation;
using UnityEditor;
using UnityEngine;

namespace ColorChargeTD.Editor
{
    [InitializeOnLoad]
    internal static class PathAuthoringHierarchySync
    {
        private static bool flushScheduled;

        static PathAuthoringHierarchySync()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        #region Hierarchy
        private static void OnHierarchyChanged()
        {
            if (flushScheduled)
            {
                return;
            }

            flushScheduled = true;
            EditorApplication.delayCall += FlushPendingPathSync;
        }

        private static void FlushPendingPathSync()
        {
            flushScheduled = false;

            PathAuthoring[] paths = Object.FindObjectsByType<PathAuthoring>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < paths.Length; i++)
            {
                PathAuthoring path = paths[i];
                if (path == null || !path.AutoCollectChildren || !path.WaypointsDifferFromChildOrder())
                {
                    continue;
                }

                Undo.RecordObject(path, "Sync path waypoints from children");
                path.SyncWaypointsFromChildren();
                EditorUtility.SetDirty(path);
            }
        }
        #endregion
    }
}
