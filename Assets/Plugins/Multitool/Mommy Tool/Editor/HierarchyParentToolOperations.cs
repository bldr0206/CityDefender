using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Multitool.MommyTool
{
    internal static class HierarchyParentToolOperations
    {
        private const string ParentUndoLabel = "Mommy Tool - Make Mommy";
        private const string UnparentUndoLabel = "Mommy Tool - You're Not My Mommy";
        private static Object _lastHierarchyHighlight;
        private static readonly Type SceneHierarchyWindowType;
        private static readonly MethodInfo SetExpandedRecursiveMethod;

        static HierarchyParentToolOperations()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;

            SceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (SceneHierarchyWindowType != null)
            {
                SetExpandedRecursiveMethod = SceneHierarchyWindowType.GetMethod(
                    "SetExpandedRecursive",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(int), typeof(bool) },
                    null);
            }
        }

        [MenuItem("Tools/Multitool/Mommy Tool/Make Mommy")]
        public static void MakeMommy()
        {
            ParentSelection();
        }

        [MenuItem("Tools/Multitool/Mommy Tool/Make Mommy", true)]
        public static bool ValidateMakeMommy()
        {
            return ValidateParentSelection();
        }

        internal static bool ValidateParentSelection()
        {
            if (Selection.activeTransform == null) return false;

            Transform[] selection = Selection.transforms;
            if (selection == null || selection.Length < 2) return false;

            return selection.Any(t => t != Selection.activeTransform);
        }

        internal static void ParentSelection()
        {
            Transform parent = Selection.activeTransform;
            if (parent == null) return;

            Transform[] selection = Selection.transforms;
            if (selection == null) return;

            List<Object> newSelection = new List<Object>();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(ParentUndoLabel);

            foreach (Transform child in selection)
            {
                if (child == null || child == parent) continue;
                // Don't allow parent to become a child of its own child.
                if (parent.IsChildOf(child)) continue;

                Undo.SetTransformParent(child, parent, ParentUndoLabel);
                newSelection.Add(child.gameObject);
            }

            // If some objects got a mommy, select them instead of the mommy.
            if (newSelection.Count > 0)
            {
                Object[] childrenArray = newSelection.ToArray();
                List<Object> combinedSelection = new List<Object>(childrenArray.Length + 1);
                combinedSelection.AddRange(childrenArray);
                combinedSelection.Add(parent.gameObject);
                SetSelectionWithActive(combinedSelection.ToArray(), parent.gameObject);
                PingInHierarchy(parent.gameObject);
                ExpandHierarchyNode(parent.gameObject);

                ShowNotification("Mommy!");
            }
            else
            {
                // Fallback: keep focus on the mommy if no children were reparented.
                Selection.activeObject = parent.gameObject;
                _lastHierarchyHighlight = parent.gameObject;
                PingInHierarchy(parent.gameObject);
                ExpandHierarchyNode(parent.gameObject);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        [MenuItem("Tools/Multitool/Mommy Tool/You're Not My Mommy")]
        public static void YoureNotMyMommy()
        {
            UnparentSelection();
        }

        [MenuItem("Tools/Multitool/Mommy Tool/You're Not My Mommy", true)]
        public static bool ValidateYoureNotMyMommy()
        {
            return ValidateUnparentSelection();
        }

        internal static bool ValidateUnparentSelection()
        {
            Transform[] selection = Selection.transforms;
            if (selection == null || selection.Length == 0) return false;

            return selection.Any(t => t != null && t.parent != null);
        }

        internal static void UnparentSelection()
        {
            Transform[] selection = Selection.transforms;
            if (selection == null || selection.Length == 0) return;

            // Detect prefab instances in selection and ask user whether to unpack them.
            // We do this before changing hierarchy so user has full control.
            HashSet<GameObject> prefabRoots = new HashSet<GameObject>();
            foreach (var tr in selection)
            {
                if (tr == null || tr.parent == null) continue;

                if (PrefabUtility.IsPartOfPrefabInstance(tr.gameObject))
                {
                    GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(tr.gameObject);
                    if (root != null)
                    {
                        prefabRoots.Add(root);
                    }
                }
            }

            if (prefabRoots.Count > 0)
            {
                bool unpack = EditorUtility.DisplayDialog(
                    "Mommy Tool - You're Not My Mommy",
                    "Some selected objects belong to a prefab instance.\n\n" +
                    "To detach them from their current mommy, the prefab instance needs to be unpacked.\n\n" +
                    "Do you want to unpack the prefab instance(s) and unparent the selected objects?",
                    "Unpack and Unparent",
                    "Cancel");

                if (!unpack)
                {
                    return;
                }
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UnparentUndoLabel);

            GameObject focusTarget = null;
            bool anyUnparented = false;
            List<GameObject> unparentedObjects = new List<GameObject>();

            // If user agreed, unpack all involved prefab roots inside this undo group.
            foreach (var root in prefabRoots)
            {
                if (root == null) continue;
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            }

            var valid = selection
                .Where(t => t != null && t.parent != null)
                .GroupBy(t => t.parent);

            foreach (var group in valid)
            {
                Transform parent = group.Key;
                if (parent == null) continue;

                Transform newParent = parent.parent;
                int insertIndex = parent.GetSiblingIndex() + 1;
                var orderedChildren = group.OrderBy(t => t.GetSiblingIndex()).ToList();

                foreach (Transform child in orderedChildren)
                {
                    Undo.SetTransformParent(child, newParent, UnparentUndoLabel);
                    anyUnparented = true;
                    unparentedObjects.Add(child.gameObject);

                    if (focusTarget == null)
                    {
                        focusTarget = child.gameObject;
                    }

                    // Place right under the former parent.
                    if (newParent != null)
                    {
                        child.SetSiblingIndex(Mathf.Min(insertIndex, newParent.childCount - 1));
                    }
                    else
                    {
                        Scene scene = child.gameObject.scene;
                        int rootCount = scene.rootCount;
                        child.SetSiblingIndex(Mathf.Min(insertIndex, rootCount - 1));
                    }

                    insertIndex++;
                }
            }

            if (focusTarget != null)
            {
                SetSelectionWithActive(unparentedObjects.Cast<Object>().ToArray(), focusTarget);
                PingInHierarchy(focusTarget);
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (anyUnparented)
            {
                ShowNotification("You're not my Mommy");
            }
        }

        private static void PingInHierarchy(Object target)
        {
            if (target == null) return;
            EditorApplication.delayCall += () => EditorGUIUtility.PingObject(target);
        }

        private static void SetSelectionWithActive(Object[] selection, Object activeObject)
        {
            List<Object> ordered = new List<Object>();
            if (selection != null)
            {
                ordered.Capacity = selection.Length;
                foreach (Object obj in selection)
                {
                    if (obj == null) continue;
                    if (activeObject != null && obj == activeObject) continue;
                    ordered.Add(obj);
                }
            }

            if (activeObject != null)
            {
                ordered.Add(activeObject);
            }

            if (ordered.Count > 0)
            {
                Object[] orderedArray = ordered.ToArray();
                Selection.objects = orderedArray;
                if (activeObject != null)
                {
                    Selection.activeObject = activeObject;
                    _lastHierarchyHighlight = activeObject;
                }
                else
                {
                    Selection.activeObject = orderedArray[orderedArray.Length - 1];
                    _lastHierarchyHighlight = Selection.activeObject;
                }
            }
            else if (activeObject != null)
            {
                Selection.objects = new[] { activeObject };
                Selection.activeObject = activeObject;
                _lastHierarchyHighlight = activeObject;
            }
        }

        private static void ShowNotification(string message)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.ShowNotification(new GUIContent(message), 1.8f);
            }
        }

        private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            TrackHierarchyClick(instanceID, selectionRect);

            if (Selection.objects == null || Selection.objects.Length < 2) return;

            Object highlightTarget = null;
            if (_lastHierarchyHighlight != null && Selection.Contains(_lastHierarchyHighlight))
            {
                highlightTarget = _lastHierarchyHighlight;
            }
            else
            {
                highlightTarget = Selection.activeObject;
            }

            if (highlightTarget == null || highlightTarget.GetInstanceID() != instanceID) return;

            Color overlay = new Color(1f, 1f, 1f, 0.2f);
            float fullWidth = EditorGUIUtility.currentViewWidth;
            Rect rect = new Rect(0f, selectionRect.y, fullWidth, selectionRect.height);
            EditorGUI.DrawRect(rect, overlay);
        }

        private static void TrackHierarchyClick(int instanceID, Rect rowRect)
        {
            Event evt = Event.current;
            if (evt == null) return;

            if (evt.type == EventType.MouseDown && evt.button == 0 && rowRect.Contains(evt.mousePosition))
            {
                Object clicked = EditorUtility.InstanceIDToObject(instanceID);
                if (clicked != null)
                {
                    _lastHierarchyHighlight = clicked;
                }
            }
        }

        private static void ExpandHierarchyNode(GameObject target)
        {
            if (target == null || SceneHierarchyWindowType == null || SetExpandedRecursiveMethod == null) return;

            int instanceId = target.GetInstanceID();
            EditorApplication.delayCall += () =>
            {
                EditorWindow hierarchyWindow = GetHierarchyWindow();
                if (hierarchyWindow == null) return;

                SetExpandedRecursiveMethod.Invoke(hierarchyWindow, new object[] { instanceId, true });
            };
        }

        private static EditorWindow GetHierarchyWindow()
        {
            if (SceneHierarchyWindowType == null) return null;

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(SceneHierarchyWindowType);
            if (windows != null && windows.Length > 0)
            {
                return windows[0] as EditorWindow;
            }

            return EditorWindow.GetWindow(SceneHierarchyWindowType);
        }

        // Shortcut for Parent (configurable in Edit > Shortcuts)
        // Default: M
        [Shortcut("Multitool/Mommy Tool/Make Mommy (M)", KeyCode.M)]
        private static void ShortcutParent()
        {
            if (ValidateParentSelection())
            {
                ParentSelection();
            }
        }

        // Shortcut for Unparent (configurable in Edit > Shortcuts)
        // Default: Alt + M
        [Shortcut("Multitool/Mommy Tool/You're Not My Mommy (Alt+M)", KeyCode.M, ShortcutModifiers.Alt)]
        private static void ShortcutUnparent()
        {
            if (ValidateUnparentSelection())
            {
                UnparentSelection();
            }
        }
    }
}


