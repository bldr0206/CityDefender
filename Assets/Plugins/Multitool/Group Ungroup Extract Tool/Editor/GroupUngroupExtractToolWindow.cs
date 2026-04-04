using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Multitool.GroupUngroupExtractTool
{
    public class GroupUngroupExtractToolWindow : EditorWindow
    {
        private SerializedObject _serializedSettings;
        private SerializedProperty _groupPositionModeProp;
        private SerializedProperty _enableRenameAfterGroupProp;

        private const string ShortcutGroupId = "Multitool/GroupUngroupExtractTool/Group";
        private const string ShortcutUngroupId = "Multitool/Group Ungroup Extract Tool/Ungroup";
        private const string ShortcutExtractId = "Multitool/Group Ungroup Extract Tool/Extract";

        [MenuItem("Window/Multitool/Group-Ungroup-Extract Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<GroupUngroupExtractToolWindow>(false, "Group/Ungroup/Extract", true);
            window.minSize = new Vector2(260, 120);
            window.Show();
        }

        private void OnEnable()
        {
            _serializedSettings = GroupUngroupExtractToolConfig.GetSerializedObject();
            _groupPositionModeProp = _serializedSettings.FindProperty("groupPositionMode");
            _enableRenameAfterGroupProp = _serializedSettings.FindProperty("enableRenameAfterGroup");
        }

        private static string GetShortcutDisplayString(string shortcutId)
        {
            try
            {
                var binding = ShortcutManager.instance.GetShortcutBinding(shortcutId);
                string bindingString = binding.ToString();
                // ToString() может вернуть пустую строку или "None" для не назначенных шорткатов
                if (!string.IsNullOrEmpty(bindingString) && bindingString != "None")
                {
                    // Форматируем для более читаемого отображения
                    bindingString = bindingString.Replace("+", " + ");
                    return bindingString;
                }
            }
            catch
            {
                // Если шорткат не найден или произошла ошибка, возвращаем пустую строку
            }
            return string.Empty;
        }

        private void OnGUI()
        {
            if (_serializedSettings == null)
            {
                OnEnable();
                if (_serializedSettings == null) return;
            }

            _serializedSettings.Update();

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(
                _groupPositionModeProp,
                new GUIContent("Pivot mode"));

            EditorGUILayout.PropertyField(
                _enableRenameAfterGroupProp,
                new GUIContent("Rename after group"));

            EditorGUILayout.Space(5);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                string groupShortcut = GetShortcutDisplayString(ShortcutGroupId);
                string groupButtonText = string.IsNullOrEmpty(groupShortcut)
                    ? "Group"
                    : $"Group ({groupShortcut})";

                if (GUILayout.Button(groupButtonText))
                {
                    // Имитируем хоткей: выполняем операцию после завершения текущего GUI-цикла.
                    EditorApplication.delayCall += () =>
                    {
                        if (GroupUngroupExtractToolOperations.ValidateGroupSelection())
                        {
                            GroupUngroupExtractToolOperations.GroupSelection();
                        }
                    };
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string ungroupShortcut = GetShortcutDisplayString(ShortcutUngroupId);
                string ungroupButtonText = string.IsNullOrEmpty(ungroupShortcut)
                    ? "Ungroup"
                    : $"Ungroup ({ungroupShortcut})";

                if (GUILayout.Button(ungroupButtonText))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (GroupUngroupExtractToolOperations.ValidateUngroupSelection())
                        {
                            GroupUngroupExtractToolOperations.UngroupSelection();
                        }
                    };
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string extractShortcut = GetShortcutDisplayString(ShortcutExtractId);
                string extractButtonText = string.IsNullOrEmpty(extractShortcut)
                    ? "Extract"
                    : $"Extract ({extractShortcut})";

                if (GUILayout.Button(extractButtonText))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (GroupUngroupExtractToolOperations.ValidateExtractSelection())
                        {
                            GroupUngroupExtractToolOperations.ExtractSelection();
                        }
                    };
                }
            }

            if (_serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_serializedSettings.targetObject);
            }
        }
    }
}

