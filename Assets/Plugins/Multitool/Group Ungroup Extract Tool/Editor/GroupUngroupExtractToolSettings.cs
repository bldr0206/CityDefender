using UnityEngine;
using UnityEditor;

namespace Multitool.GroupUngroupExtractTool
{
    /// <summary>
    /// ScriptableObject с настройками инструмента Group/Ungroup/Extract.
    /// Переименован в GroupUngroupExtractToolConfig, чтобы избежать конфликтов имён.
    /// </summary>
    public class GroupUngroupExtractToolConfig : ScriptableObject
    {
        public enum GroupPositionMode
        {
            BoundsCenter,
            FirstSelected,
            LastSelected,
            Zero
        }

        private const string AssetPath = "Assets/Plugins/Multitool/Group Ungroup Extract Tool/GroupUngroupExtractToolSettings.asset";

        public GroupPositionMode groupPositionMode = GroupPositionMode.BoundsCenter;

        public bool enableRenameAfterGroup = true;

        // Internal notification settings (always enabled, not shown in UI)
        public bool showToastNotification = true;
        public bool showConsoleLog = false;

        private static GroupUngroupExtractToolConfig _cached;

        public static GroupUngroupExtractToolConfig Instance
        {
            get
            {
                if (_cached != null) return _cached;
                _cached = AssetDatabase.LoadAssetAtPath<GroupUngroupExtractToolConfig>(AssetPath);
                if (_cached == null)
                {
                    _cached = CreateInstance<GroupUngroupExtractToolConfig>();
                    AssetDatabase.CreateAsset(_cached, AssetPath);
                    AssetDatabase.SaveAssets();
                }
                return _cached;
            }
        }

        public static SerializedObject GetSerializedObject()
        {
            return new SerializedObject(Instance);
        }
    }
}
