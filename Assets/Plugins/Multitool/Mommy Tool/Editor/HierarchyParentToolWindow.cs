using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Multitool.MommyTool
{
    public class HierarchyParentToolWindow : EditorWindow
    {
        private const string ShortcutParentId = "Multitool/Mommy Tool/Make Mommy (M)";
        private const string ShortcutUnparentId = "Multitool/Mommy Tool/You're Not My Mommy (Alt+M)";

        [MenuItem("Window/Multitool/Mommy Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<HierarchyParentToolWindow>(false, "Mommy Tool", true);
            window.minSize = new Vector2(260, 100);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += RepaintOnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= RepaintOnSelectionChanged;
        }

        private void RepaintOnSelectionChanged()
        {
            if (this != null)
            {
                Repaint();
            }
        }

        private static string GetShortcutDisplayString(string shortcutId)
        {
            try
            {
                var binding = ShortcutManager.instance.GetShortcutBinding(shortcutId);
                string bindingString = binding.ToString();
                if (!string.IsNullOrEmpty(bindingString) && bindingString != "None")
                {
                    return bindingString;
                }
            }
            catch
            {
                // Shortcut may not exist, return empty string.
            }

            return string.Empty;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Select several objects. The last selected becomes mommy for the rest. " +
                "Use 'You're Not My Mommy' to detach objects from their current mommy.",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawParentButton();
            DrawUnparentButton();
        }

        private void DrawParentButton()
        {
            string shortcut = GetShortcutDisplayString(ShortcutParentId);
            string label = string.IsNullOrEmpty(shortcut) ? "Make Mommy (M)" : $"Make Mommy ({shortcut})";

            using (new EditorGUI.DisabledScope(!HierarchyParentToolOperations.ValidateParentSelection()))
            {
                if (GUILayout.Button(label, GUILayout.Height(20)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (HierarchyParentToolOperations.ValidateParentSelection())
                        {
                            HierarchyParentToolOperations.ParentSelection();
                        }
                    };
                }
            }
        }

        private void DrawUnparentButton()
        {
            string shortcut = GetShortcutDisplayString(ShortcutUnparentId);
            string label = string.IsNullOrEmpty(shortcut) ? "You're Not My Mommy (Alt+M)" : $"You're Not My Mommy ({shortcut})";

            using (new EditorGUI.DisabledScope(!HierarchyParentToolOperations.ValidateUnparentSelection()))
            {
                if (GUILayout.Button(label, GUILayout.Height(20)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (HierarchyParentToolOperations.ValidateUnparentSelection())
                        {
                            HierarchyParentToolOperations.UnparentSelection();
                        }
                    };
                }
            }
        }

    }
}

