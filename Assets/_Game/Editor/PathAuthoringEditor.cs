using ColorChargeTD.Presentation;
using UnityEditor;
using UnityEngine;

namespace ColorChargeTD.Editor
{
    [CustomEditor(typeof(PathAuthoring))]
    public sealed class PathAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty pathId;
        private SerializedProperty movePattern;
        private SerializedProperty waypoints;
        private SerializedProperty autoCollectChildren;

        #region UnityCallbacks
        private void OnEnable()
        {
            pathId = serializedObject.FindProperty("pathId");
            movePattern = serializedObject.FindProperty("movePattern");
            waypoints = serializedObject.FindProperty("waypoints");
            autoCollectChildren = serializedObject.FindProperty("autoCollectChildren");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(pathId);
            EditorGUILayout.PropertyField(movePattern);
            EditorGUILayout.PropertyField(autoCollectChildren);

            if (autoCollectChildren.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Route = direct child order. Add or duplicate waypoint objects under this path; reorder them in the Hierarchy to change the polyline. You do not assign the Waypoints list manually.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(waypoints, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}
