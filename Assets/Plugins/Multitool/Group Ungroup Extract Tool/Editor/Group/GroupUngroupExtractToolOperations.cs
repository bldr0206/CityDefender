using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using GroupPositionMode = Multitool.GroupUngroupExtractTool.GroupUngroupExtractToolConfig.GroupPositionMode;

namespace Multitool.GroupUngroupExtractTool
{
    public enum GroupContext
    {
        Hierarchy,
        Project,
        Timeline
    }

    public static class GroupUngroupExtractToolOperations
    {
        [MenuItem("Tools/Multitool/Group Ungroup Extract Tool/Group")]
        public static void GroupSelection()
        {
            GroupContext context = DetermineContext();



            switch (context)
            {
                case GroupContext.Hierarchy:
                    GroupHierarchySelection();
                    break;
                case GroupContext.Project:
                    GroupProjectSelection();
                    break;
                case GroupContext.Timeline:
                    GroupTimelineSelection();
                    break;
            }
        }

        // Shortcut for Group (configurable in Edit > Shortcuts)
        // Default: Ctrl/Cmd + G
        [Shortcut("Multitool/GroupUngroupExtractTool/Group",
            KeyCode.G, ShortcutModifiers.Action)]
        private static void ShortcutGroup()
        {
            var e = Event.current;
            if (ValidateGroupSelection())
            {
                GroupSelection();
            }
            e?.Use();
        }

        private static void GroupHierarchySelection()
        {
            var settings = GroupUngroupExtractToolConfig.Instance;
            var actualSelection = Selection.gameObjects;
            var selection = GetTopLevelSelection();
            if (selection.Count == 0) return;

            // Check if parent-child pair was selected (Unity filters it to less objects)
            if (actualSelection != null && actualSelection.Length > selection.Count)
            {
                ShowToast($"Can't group a parent with its child.\n Select objects at the same hierarchy level.", isWarning: true);
                return;
            }

            Transform commonParent = GetCommonDirectParent(selection);

            // Check if objects have different parents (excluding scene root case)
            if (selection.Count >= 2 && !HasSameDirectParent(selection))
            {
                ShowToast($"Can't group objects from different hierarchies.\n Select objects with the same direct parent.", isWarning: true);
                return;
            }
            // Check if we need RectTransform or regular Transform
            bool hasAnyRect = AnyHasRectTransform(selection);
            bool allHaveRect = AllHaveRectTransform(selection);

            Vector3 targetWorldPos = ComputeTargetWorldPosition(selection, settings.groupPositionMode);

            // Remember the sibling index of the last selected object
            Transform lastSelected = selection[selection.Count - 1];
            int targetSiblingIndex = lastSelected.GetSiblingIndex();

            // Create group with appropriate Transform type and name
            string groupName = hasAnyRect ? "UI Group" : "Group";
            GameObject groupGo = hasAnyRect
                ? new GameObject(groupName, typeof(RectTransform))
                : new GameObject(groupName); // Transform is added automatically
            Undo.RegisterCreatedObjectUndo(groupGo, "Group Selection");

            Transform groupTr = groupGo.transform;
            if (commonParent != null)
            {
                Undo.SetTransformParent(groupTr, commonParent, "Group Selection");
            }

            // Set the group at the same sibling index as the last selected object
            Undo.RecordObject(groupTr, "Group Selection");
            groupTr.SetSiblingIndex(targetSiblingIndex);
            // Ensure new group scale is always 1 to avoid unexpected scaling
            groupTr.localScale = Vector3.one;

            if (hasAnyRect && allHaveRect)
            {
                // All objects are RectTransform - calculate bounding box and set up parent accordingly
                SetupRectTransformGroup(groupTr as RectTransform, selection, commonParent);
            }
            else
            {
                // Regular transforms or mixed objects - simple positioning
                groupTr.position = targetWorldPos;

                // Reparent selection to group
                foreach (var tr in selection)
                {
                    Undo.SetTransformParent(tr, groupTr, "Group Selection");
                }
            }

            Selection.activeTransform = groupTr;
            Selection.objects = new Object[] { groupGo };

            // Start rename for the newly created group object in Hierarchy
            // (similar behaviour to grouping files into a folder in the Project window)
            if (settings.enableRenameAfterGroup)
            {
                EditorApplication.delayCall += () =>
                {
                    // Focus Hierarchy window if available to ensure rename works
                    FocusHierarchyWindowIfAvailable();

                    // Extra delay to ensure selection is fully applied before rename
                    EditorApplication.delayCall += () =>
                    {
                        if (Selection.activeGameObject == groupGo)
                        {
                            // Use Unity's built-in rename command (works for Hierarchy selection)
                            EditorApplication.ExecuteMenuItem("Edit/Rename");
                        }
                    };
                };
            }

            // Notify user
            NotifyGroupOperation(selection.Count);
        }

        [MenuItem("Tools/Multitool/Group Ungroup Extract Tool/Group", true)]
        public static bool ValidateGroupSelection()
        {
            GroupContext context = DetermineContext();

            switch (context)
            {
                case GroupContext.Hierarchy:
                    if (Selection.transforms == null) return false;
                    var top = GetTopLevelSelection();
                    return top.Count >= 1;
                case GroupContext.Project:
                    return IsProjectContext() && GetProjectSelection().Count >= 1;
                case GroupContext.Timeline:
                    return IsTimelineContext() && GetTimelineSelection().Count >= 1;
                default:
                    return false;
            }
        }

        [MenuItem("Tools/Multitool/Group Ungroup Extract Tool/Ungroup")]
        public static void UngroupSelection()
        {
            GroupContext context = DetermineContext();

            switch (context)
            {
                case GroupContext.Hierarchy:
                    UngroupHierarchySelection();
                    break;
                case GroupContext.Project:
                    UngroupProjectSelection();
                    break;
                case GroupContext.Timeline:
                    UngroupTimelineSelection();
                    break;
            }
        }

        // Shortcut for Ungroup (configurable in Edit > Shortcuts)
        // Default: Alt + G
        [Shortcut("Multitool/Group Ungroup Extract Tool/Ungroup",
            KeyCode.G, ShortcutModifiers.Alt)]
        private static void ShortcutUngroup()
        {
            var e = Event.current;
            if (ValidateUngroupSelection())
            {
                UngroupSelection();
            }
            e?.Use();
        }

        private enum UngroupHierarchyDecision
        {
            Proceed,
            ExtractOnly,
            Cancel
        }

        private static void UngroupHierarchySelection()
        {
            var selected = Selection.transforms;
            if (selected == null || selected.Length == 0) return;

            // Check if any selected object has children
            bool hasObjectsWithChildren = false;
            foreach (var tr in selected)
            {
                if (tr.childCount > 0)
                {
                    hasObjectsWithChildren = true;
                    break;
                }
            }

            // If no objects have children, show toast and return
            if (!hasObjectsWithChildren)
            {
                ShowToast("Nothing to ungroup.\nSelect objects with children.", isWarning: true);
                return;
            }

            // Local flag for deleting parent for this operation (setting always treated as enabled)
            bool deleteParent = true;

            // Check for prefab instances and components (if deleting parent)
            var decision = ConfirmUngroupOperation(selected, deleteParent);
            if (decision == UngroupHierarchyDecision.Cancel)
            {
                return; // User cancelled the operation
            }

            if (decision == UngroupHierarchyDecision.ExtractOnly)
            {
                // User selected only to extract children, do not delete parent
                deleteParent = false;
            }

            List<Object> newSelection = new List<Object>();
            List<GameObject> parentsToDelete = new List<GameObject>();
            int totalChildren = 0;

            foreach (var tr in selected)
            {
                if (tr.childCount == 0) continue;

                // Unpack prefab if needed
                if (PrefabUtility.IsPartOfPrefabInstance(tr.gameObject))
                {
                    GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(tr.gameObject);
                    if (prefabRoot == tr.gameObject)
                    {
                        // This is the root of the prefab instance, unpack it completely
                        PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                    }
                }

                Transform parent = tr.parent; // can be null for root

                // Move children up one level (unparent)
                List<Transform> children = new List<Transform>();
                for (int i = 0; i < tr.childCount; i++) children.Add(tr.GetChild(i));

                totalChildren += children.Count;

                foreach (var child in children)
                {
                    Undo.SetTransformParent(child, parent, "Ungroup Selection");
                    newSelection.Add(child.gameObject);
                }

                // Mark parent for deletion if enabled for this operation
                if (deleteParent)
                {
                    parentsToDelete.Add(tr.gameObject);
                }
            }

            // Delete parents if needed
            foreach (var parent in parentsToDelete)
            {
                Undo.DestroyObjectImmediate(parent);
            }

            if (newSelection.Count > 0)
            {
                Selection.objects = newSelection.ToArray();
            }

            // Notify user
            NotifyUngroupOperation(totalChildren, parentsToDelete.Count);
        }

        [MenuItem("Tools/Multitool/Group Ungroup Extract Tool/Ungroup", true)]
        public static bool ValidateUngroupSelection()
        {
            GroupContext context = DetermineContext();

            switch (context)
            {
                case GroupContext.Hierarchy:
                    return Selection.transforms != null && Selection.transforms.Length > 0;
                case GroupContext.Project:
                    if (!IsProjectContext()) return false;
                    var projectSelection = GetProjectSelection();
                    return projectSelection.Any(obj => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)));
                case GroupContext.Timeline:
                    if (!IsTimelineContext()) return false;
                    var timelineSelection = GetTimelineSelection();
                    return timelineSelection.Any(track => IsGroupTrack(track));
                default:
                    return false;
            }
        }

        // ========== Extract (Unparent without deleting parent) ==========

        // Shortcut for Extract (configurable in Edit > Shortcuts)
        // Default: Shift + Alt + G
        [Shortcut("Multitool/Group Ungroup Extract Tool/Extract",
            KeyCode.G, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void ShortcutExtract()
        {
            var e = Event.current;
            if (ValidateExtractSelection())
            {
                ExtractSelection();
            }
            e?.Use();
        }

        [MenuItem("Tools/Multitool/Group Ungroup Extract Tool/Extract")]
        public static void ExtractSelection()
        {
            GroupContext context = DetermineContext();

            switch (context)
            {
                case GroupContext.Hierarchy:
                    ExtractHierarchySelection();
                    break;
                case GroupContext.Project:
                    ExtractProjectSelection();
                    break;
                case GroupContext.Timeline:
                    ExtractTimelineSelection();
                    break;
            }
        }

        [MenuItem("Tools/Multitool/Group Ungroup Extract Tool/Extract", true)]
        public static bool ValidateExtractSelection()
        {
            GroupContext context = DetermineContext();

            switch (context)
            {
                case GroupContext.Hierarchy:
                    return Selection.transforms != null && Selection.transforms.Length > 0;
                case GroupContext.Project:
                    if (!IsProjectContext()) return false;
                    var projectSelection = GetProjectSelection();
                    return projectSelection.Any(obj => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)));
                case GroupContext.Timeline:
                    if (!IsTimelineContext()) return false;
                    var timelineSelection = GetTimelineSelection();
                    return timelineSelection.Any(track => IsGroupTrack(track));
                default:
                    return false;
            }
        }

        private static void ExtractHierarchySelection()
        {
            var selected = Selection.transforms;
            if (selected == null || selected.Length == 0) return;

            // Check if any selected object has children
            bool hasObjectsWithChildren = false;
            foreach (var tr in selected)
            {
                if (tr.childCount > 0)
                {
                    hasObjectsWithChildren = true;
                    break;
                }
            }

            // If no objects have children, show toast and return
            if (!hasObjectsWithChildren)
            {
                ShowToast("Nothing to extract.\nSelect objects with children.", isWarning: true);
                return;
            }

            List<Object> newSelection = new List<Object>();
            int totalChildren = 0;

            foreach (var tr in selected)
            {
                if (tr.childCount == 0) continue;

                // Unpack prefab if needed
                if (PrefabUtility.IsPartOfPrefabInstance(tr.gameObject))
                {
                    GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(tr.gameObject);
                    if (prefabRoot == tr.gameObject)
                    {
                        // This is the root of the prefab instance, unpack it completely
                        PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                    }
                }

                Transform parent = tr.parent; // can be null for root

                // Move children up one level (unparent)
                List<Transform> children = new List<Transform>();
                for (int i = 0; i < tr.childCount; i++) children.Add(tr.GetChild(i));

                totalChildren += children.Count;

                foreach (var child in children)
                {
                    Undo.SetTransformParent(child, parent, "Extract Children");
                    newSelection.Add(child.gameObject);
                }
            }

            if (newSelection.Count > 0)
            {
                Selection.objects = newSelection.ToArray();
            }

            // Notify user
            string message = totalChildren == 1
                ? "Extracted 1 object"
                : $"Extracted {totalChildren} objects";

            var settings = GroupUngroupExtractToolConfig.Instance;
            if (settings.showToastNotification)
            {
                ShowToast(message, isWarning: false);
            }
            if (settings.showConsoleLog)
            {
                Debug.Log($"[Group Ungroup Extract Tool] {message}");
            }
        }

        private static List<Transform> GetTopLevelSelection()
        {
            var selected = Selection.transforms;
            List<Transform> result = new List<Transform>();
            if (selected == null || selected.Length == 0) return result;

            var set = new HashSet<Transform>(selected);
            foreach (var tr in selected)
            {
                Transform p = tr.parent;
                bool parentInSelection = false;
                while (p != null)
                {
                    if (set.Contains(p)) { parentInSelection = true; break; }
                    p = p.parent;
                }
                if (!parentInSelection) result.Add(tr);
            }
            return result;
        }

        private static Transform GetCommonDirectParent(List<Transform> transforms)
        {
            if (transforms.Count == 0) return null;
            Transform parent = transforms[0].parent;
            for (int i = 1; i < transforms.Count; i++)
            {
                if (transforms[i].parent != parent) return null;
            }
            return parent;
        }

        private static bool HasSameDirectParent(List<Transform> transforms)
        {
            if (transforms.Count == 0) return true;
            Transform parent = transforms[0].parent;
            for (int i = 1; i < transforms.Count; i++)
            {
                // Compare parents (works correctly even if parent is null for scene root objects)
                if (transforms[i].parent != parent) return false;
            }
            return true;
        }

        private static void ShowToast(string message, bool isWarning = false)
        {
            // Try SceneView first (for Hierarchy context)
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.ShowNotification(new GUIContent(message), 1.8);
                return;
            }

            // Fallback: show in console for non-scene contexts (Project, Timeline)
            if (isWarning)
            {
                Debug.LogWarning($"[Group Ungroup Extract Tool] {message}");
            }
            else
            {
                Debug.Log($"[Group Ungroup Extract Tool] {message}");
            }
        }

        /// <summary>
        /// Фокусирует окно Hierarchy, если оно открыто.
        /// Использует рефлексию, т.к. SceneHierarchyWindow — internal класс.
        /// </summary>
        private static void FocusHierarchyWindowIfAvailable()
        {
            var hierarchyType = System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
            if (hierarchyType == null) return;

            var instances = Resources.FindObjectsOfTypeAll(hierarchyType);
            if (instances == null || instances.Length == 0) return;

            var window = instances[0] as EditorWindow;
            if (window != null)
            {
                window.Focus();
            }
        }

        /// <summary>
        /// Фокусирует окно Project, если оно открыто.
        /// Нужен для корректной работы команды Assets/Rename, когда группировка запускается не из Project.
        /// </summary>
        private static void FocusProjectWindowIfAvailable()
        {
            var projectBrowserType = System.Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
            if (projectBrowserType == null) return;

            var instances = Resources.FindObjectsOfTypeAll(projectBrowserType);
            if (instances == null || instances.Length == 0) return;

            var window = instances[0] as EditorWindow;
            if (window != null)
            {
                window.Focus();
            }
        }

        /// <summary>
        /// Фокусирует окно Timeline, если оно открыто.
        /// Нужен для корректной работы команд меню (Edit/Delete) при вызове из других окон.
        /// </summary>
        private static void FocusTimelineWindowIfAvailable()
        {
            // Тип окна Timeline: UnityEditor.Timeline.TimelineWindow из сборки Unity.Timeline.Editor
            var timelineWindowType = System.Type.GetType("UnityEditor.Timeline.TimelineWindow, Unity.Timeline.Editor");
            if (timelineWindowType == null) return;

            // У окна есть статическое свойство instance
            var instanceProp = timelineWindowType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProp == null) return;

            var instance = instanceProp.GetValue(null) as EditorWindow;
            if (instance != null)
            {
                instance.Focus();
            }
        }

        private static bool AnyHasRectTransform(List<Transform> transforms)
        {
            foreach (var t in transforms)
            {
                if (t.GetComponent<RectTransform>() != null) return true;
            }
            return false;
        }

        private static bool AllHaveRectTransform(List<Transform> transforms)
        {
            foreach (var t in transforms)
            {
                if (t.GetComponent<RectTransform>() == null) return false;
            }
            return transforms.Count > 0;
        }

        private static void SetupRectTransformGroup(RectTransform groupRect, List<Transform> children, Transform commonParent)
        {
            if (groupRect == null || children.Count == 0) return;

            Transform parent = groupRect.parent;

            // Calculate bounding box in parent local space (to derive correct sizeDelta and anchoredPosition)
            Vector3 minLocal = new Vector3(float.MaxValue, float.MaxValue, 0f);
            Vector3 maxLocal = new Vector3(float.MinValue, float.MinValue, 0f);

            foreach (var child in children)
            {
                RectTransform childRect = child.GetComponent<RectTransform>();
                if (childRect == null) continue;

                Vector3[] corners = new Vector3[4];
                childRect.GetWorldCorners(corners);

                for (int i = 0; i < 4; i++)
                {
                    Vector3 localCorner = parent != null ? parent.InverseTransformPoint(corners[i]) : corners[i];
                    if (localCorner.x < minLocal.x) minLocal.x = localCorner.x;
                    if (localCorner.y < minLocal.y) minLocal.y = localCorner.y;
                    if (localCorner.x > maxLocal.x) maxLocal.x = localCorner.x;
                    if (localCorner.y > maxLocal.y) maxLocal.y = localCorner.y;
                }
            }

            Vector2 sizeLocal = new Vector2(maxLocal.x - minLocal.x, maxLocal.y - minLocal.y);
            Vector2 centerLocal = new Vector2((minLocal.x + maxLocal.x) * 0.5f, (minLocal.y + maxLocal.y) * 0.5f);

            // Configure group RectTransform in parent space
            Undo.RecordObject(groupRect, "Setup Group RectTransform");
            groupRect.anchorMin = new Vector2(0.5f, 0.5f);
            groupRect.anchorMax = new Vector2(0.5f, 0.5f);
            groupRect.pivot = new Vector2(0.5f, 0.5f);
            groupRect.localScale = Vector3.one;

            if (parent != null)
            {
                groupRect.anchoredPosition = centerLocal;
                groupRect.sizeDelta = sizeLocal;
                // Ensure local Z is 0 to avoid depth offsets
                var lp = groupRect.localPosition;
                groupRect.localPosition = new Vector3(lp.x, lp.y, 0f);
            }
            else
            {
                // No parent → treat calculated values as world space
                groupRect.position = new Vector3(centerLocal.x, centerLocal.y, 0f);
                groupRect.sizeDelta = sizeLocal;
            }

            // Reparent children - they will keep their world positions
            foreach (var child in children)
            {
                Undo.SetTransformParent(child, groupRect, "Group Selection");
            }
        }

        private static Vector3 ComputeTargetWorldPosition(List<Transform> transforms, GroupPositionMode mode)
        {
            switch (mode)
            {
                case GroupPositionMode.FirstSelected:
                    return transforms[0].position;
                case GroupPositionMode.LastSelected:
                    return transforms[transforms.Count - 1].position;
                case GroupPositionMode.Zero:
                    return Vector3.zero;
                case GroupPositionMode.BoundsCenter:
                default:
                    return ComputeBoundsCenter(transforms);
            }
        }

        private static UngroupHierarchyDecision ConfirmUngroupOperation(Transform[] transforms, bool willDeleteParent)
        {
            List<string> prefabNames = new List<string>();
            List<string> objectsWithComponents = new List<string>();

            foreach (var tr in transforms)
            {
                if (tr.childCount == 0) continue;

                // Check for prefab instances
                bool isPrefab = false;
                if (PrefabUtility.IsPartOfPrefabInstance(tr.gameObject))
                {
                    GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(tr.gameObject);
                    if (prefabRoot == tr.gameObject)
                    {
                        prefabNames.Add(tr.gameObject.name);
                        isPrefab = true;
                    }
                }

                // Check for extra components (only if we're deleting the parent)
                if (willDeleteParent && !isPrefab)
                {
                    var extraComponents = GetExtraComponents(tr);
                    if (extraComponents.Count > 0)
                    {
                        string componentsList = string.Join(", ", extraComponents.ToArray());
                        objectsWithComponents.Add($"{tr.gameObject.name} ({componentsList})");
                    }
                }
            }

            // Show confirmation dialog if there are prefabs or objects with components
            if (prefabNames.Count > 0 || objectsWithComponents.Count > 0)
            {
                string message = "";
                string title = "";

                // Prefab warning
                if (prefabNames.Count > 0)
                {
                    title = willDeleteParent ? "Ungroup prefab?" : "Warning: Prefab Unpacking";
                    message = "You are about to ungroup ";
                    if (prefabNames.Count > 1)
                    {
                        message += "prefab instances";
                    }
                    else
                    {
                        message += "a prefab instance";
                    }
                    message += ":\n\n";
                    foreach (var name in prefabNames)
                    {
                        message += $"• {name}\n";
                    }
                    message += "\nThis will completely unpack the prefab";
                    if (prefabNames.Count > 1)
                    {
                        message += "s";
                    }
                    message += " and break the connection to the original prefab.";

                    if (willDeleteParent)
                    {
                        message += "\n\nThe parent object";
                        if (prefabNames.Count > 1)
                        {
                            message += "s";
                        }
                        message += " will also be DELETED.";
                    }
                }
                // Component warning (only if deleting parent and not a prefab)
                else if (objectsWithComponents.Count > 0)
                {
                    title = "Warning: Component Loss";
                    message = "The following objects contain components that will be lost when the parent is deleted:\n\n";
                    foreach (var obj in objectsWithComponents)
                    {
                        message += $"• {obj}\n";
                    }
                }

                // For prefabs, we leave a simple confirmation (Yes/Cancel)
                if (prefabNames.Count > 0)
                {
                    message += "\n\nContinue?";

                    bool proceed = EditorUtility.DisplayDialog(
                        title,
                        message,
                        "Yes, Ungroup",
                        "Cancel"
                    );

                    return proceed ? UngroupHierarchyDecision.Proceed : UngroupHierarchyDecision.Cancel;
                }

                // For objects with components, we show 3 buttons: Extract / Delete / Cancel
                if (objectsWithComponents.Count > 0 && willDeleteParent)
                {
                    message += "\n\nChoose what to do with the parent objects:";

                    int result = EditorUtility.DisplayDialogComplex(
                        title,
                        message,
                        "Extract (keep parent)", // 0
                        "Delete parent",         // 1
                        "Cancel"                 // 2
                    );

                    switch (result)
                    {
                        case 0:
                            return UngroupHierarchyDecision.ExtractOnly;
                        case 1:
                            return UngroupHierarchyDecision.Proceed;
                        default:
                            return UngroupHierarchyDecision.Cancel;
                    }
                }
            }

            return UngroupHierarchyDecision.Proceed; // No warnings, continue without confirmation
        }

        private static List<string> GetExtraComponents(Transform transform)
        {
            List<string> extraComponents = new List<string>();
            var allComponents = transform.GetComponents<Component>();

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                System.Type componentType = component.GetType();

                // Skip Transform and RectTransform
                if (componentType == typeof(Transform) || componentType == typeof(RectTransform))
                    continue;

                extraComponents.Add(componentType.Name);
            }

            return extraComponents;
        }

        private static Vector3 ComputeBoundsCenter(List<Transform> transforms)
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var t in transforms)
            {
                bool added = false;

                // 1) Try all child Renderers (including self)
                var renderers = t.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    foreach (var r in renderers)
                    {
                        if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
                        else { bounds.Encapsulate(r.bounds); }
                    }
                    added = true;
                }

                // 2) Try RectTransform hierarchy world corners
                if (!added)
                {
                    var rects = t.GetComponentsInChildren<RectTransform>();
                    if (rects != null && rects.Length > 0)
                    {
                        foreach (var rect in rects)
                        {
                            Vector3[] corners = new Vector3[4];
                            rect.GetWorldCorners(corners);
                            if (!hasBounds)
                            {
                                bounds = new Bounds(corners[0], Vector3.zero);
                                for (int i = 1; i < 4; i++) bounds.Encapsulate(corners[i]);
                                hasBounds = true;
                            }
                            else
                            {
                                for (int i = 0; i < 4; i++) bounds.Encapsulate(corners[i]);
                            }
                        }
                        added = true;
                    }
                }

                // 3) Fallback: use transform position
                if (!added)
                {
                    if (!hasBounds)
                    {
                        bounds = new Bounds(t.position, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(t.position);
                    }
                }
            }

            return bounds.center;
        }

        private static void NotifyGroupOperation(int objectCount)
        {
            var settings = GroupUngroupExtractToolConfig.Instance;

            string message = objectCount == 1
                ? "Grouped 1 object"
                : $"Grouped {objectCount} objects";

            // Show toast notification in Scene View with green success icon
            if (settings.showToastNotification)
            {
                ShowToast(message, isWarning: false);
            }

            // Show console log
            if (settings.showConsoleLog)
            {
                Debug.Log($"[Group Ungroup Extract Tool] {message}");
            }
        }

        private static void NotifyUngroupOperation(int childrenCount, int parentsDeleted)
        {
            var settings = GroupUngroupExtractToolConfig.Instance;

            string message = childrenCount == 1
                ? "Ungrouped 1 object"
                : $"Ungrouped {childrenCount} objects";

            if (parentsDeleted > 0)
            {
                message += parentsDeleted == 1
                    ? " (deleted 1 parent)"
                    : $" (deleted {parentsDeleted} parents)";
            }

            // Show toast notification in Scene View with green success icon
            if (settings.showToastNotification)
            {
                ShowToast(message, isWarning: false);
            }

            // Show console log
            if (settings.showConsoleLog)
            {
                Debug.Log($"[Group Ungroup Extract Tool] {message}");
            }
        }

        // ========== Context Detection ==========

        private static GroupContext DetermineContext()
        {
            // Check Timeline first (most specific)
            // If Timeline window is focused, treat hotkey as Timeline context.
            if (IsTimelineContext())
            {
                return GroupContext.Timeline;
            }

            // Check Project window
            if (IsProjectContext() && GetProjectSelection().Count > 0)
            {
                return GroupContext.Project;
            }

            // Default to Hierarchy
            return GroupContext.Hierarchy;
        }

        private static bool IsProjectContext()
        {
            // Check if Timeline is active first (Timeline has priority)
            if (IsTimelinePackageAvailable())
            {
                try
                {
                    var timelineWindowType = System.Type.GetType("UnityEditor.Timeline.TimelineWindow,Unity.Timeline.Editor");
                    if (timelineWindowType != null)
                    {
                        var instanceProperty = timelineWindowType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (instanceProperty != null)
                        {
                            var window = instanceProperty.GetValue(null);
                            if (window != null)
                            {
                                // Timeline window is open, check if it has selection
                                if (GetTimelineSelection().Count > 0)
                                {
                                    return false; // Timeline has priority
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // Check if we have selected objects but no transforms (likely Project window)
            if (Selection.objects == null || Selection.objects.Length == 0)
                return false;

            // If we have objects but no transforms, it's likely Project window
            if (Selection.transforms == null || Selection.transforms.Length == 0)
            {
                // Verify these are actual assets (not scene objects)
                foreach (var obj in Selection.objects)
                {
                    if (obj == null) continue;
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsTimelineContext()
        {
            // Если установлен пакет Timeline и в текущем Selection есть треки,
            // считаем, что работаем в контексте Timeline — независимо от того,
            // какое окно сейчас в фокусе (это важно для кнопок в нашем окне).
            if (IsTimelinePackageAvailable())
            {
                try
                {
                    if (GetTimelineSelection().Count > 0)
                    {
                        return true;
                    }
                }
                catch { }
            }

            // Fallback: старая логика по фокусу окна, чтобы ничего не сломать
            // в других сценариях (например, когда хоткеи жмутся прямо из Timeline).
            if (!IsTimelinePackageAvailable())
                return false;

            var focused = EditorWindow.focusedWindow;
            if (focused == null)
                return false;

            // Timeline window type name is 'UnityEditor.Timeline.TimelineWindow'
            var typeFullName = focused.GetType().FullName;
            return typeFullName == "UnityEditor.Timeline.TimelineWindow";
        }

        private static List<Object> GetProjectSelection()
        {
            List<Object> result = new List<Object>();
            if (Selection.objects == null) return result;

            foreach (var obj in Selection.objects)
            {
                if (obj == null) continue;
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                {
                    result.Add(obj);
                }
            }

            return result;
        }

        // ========== Project Grouping ==========

        private static void GroupProjectSelection()
        {
            var selection = GetProjectSelection();
            if (selection.Count == 0)
            {
                ShowToast("No valid assets selected in Project window.", isWarning: true);
                return;
            }

            // Accept both files and folders for grouping
            List<Object> assetsToGroup = selection;

            if (assetsToGroup.Count == 0)
            {
                ShowToast("No valid assets selected.", isWarning: true);
                return;
            }

            // Determine common parent folder
            string commonParent = GetCommonParentFolder(assetsToGroup);
            if (string.IsNullOrEmpty(commonParent))
            {
                ShowToast("Unable to determine common parent folder.", isWarning: true);
                return;
            }

            // Generate unique folder name
            string groupFolderName = "Folder";
            string groupFolderPath = commonParent + "/" + groupFolderName;
            int counter = 1;
            while (AssetDatabase.IsValidFolder(groupFolderPath) || File.Exists(groupFolderPath + ".meta"))
            {
                groupFolderPath = commonParent + "/" + groupFolderName + " " + counter;
                counter++;
            }

            // Create folder
            string guid = AssetDatabase.CreateFolder(commonParent, Path.GetFileName(groupFolderPath));
            if (string.IsNullOrEmpty(guid))
            {
                ShowToast("Failed to create group folder.", isWarning: true);
                return;
            }

            // Move assets (files and folders) into the new folder
            List<string> movedPaths = new List<string>();
            foreach (var obj in assetsToGroup)
            {
                string sourcePath = AssetDatabase.GetAssetPath(obj);
                string assetName = Path.GetFileName(sourcePath);
                string destPath = groupFolderPath + "/" + assetName;

                string moveResult = AssetDatabase.MoveAsset(sourcePath, destPath);
                if (string.IsNullOrEmpty(moveResult))
                {
                    movedPaths.Add(destPath);
                }
                else
                {
                    Debug.LogWarning($"[Group Ungroup Extract Tool] Failed to move {sourcePath}: {moveResult}");
                }
            }

            AssetDatabase.Refresh();

            // Select the new folder and start rename
            Object folderObject = AssetDatabase.LoadAssetAtPath<Object>(groupFolderPath);
            if (folderObject != null)
            {
                Selection.activeObject = folderObject;

                // Start rename using Unity's standard rename command
                var settings = GroupUngroupExtractToolConfig.Instance;
                if (settings.enableRenameAfterGroup)
                {
                    EditorApplication.delayCall += () =>
                    {
                        // Гарантируем, что сфокусировано окно Project, чтобы Rename сработал
                        FocusProjectWindowIfAvailable();
                        EditorApplication.delayCall += () =>
                        {
                            // Execute Unity's built-in Rename command
                            EditorApplication.ExecuteMenuItem("Assets/Rename");
                        };
                    };
                }
            }

            NotifyGroupOperation(assetsToGroup.Count);
        }

        private static void UngroupProjectSelection()
        {
            var selection = GetProjectSelection();
            if (selection.Count == 0)
            {
                ShowToast("No valid assets selected in Project window.", isWarning: true);
                return;
            }

            List<string> foldersToUngroup = new List<string>();
            foreach (var obj in selection)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    foldersToUngroup.Add(path);
                }
            }

            if (foldersToUngroup.Count == 0)
            {
                ShowToast("Select folders to ungroup.", isWarning: true);
                return;
            }

            List<Object> newSelection = new List<Object>();
            int totalFilesMoved = 0;
            List<string> foldersToDelete = new List<string>();

            foreach (string folderPath in foldersToUngroup)
            {
                string parentFolder = Path.GetDirectoryName(folderPath).Replace('\\', '/');
                if (string.IsNullOrEmpty(parentFolder))
                    parentFolder = "Assets";

                // Get direct children only (not recursive)
                List<string> directChildren = new List<string>();

                // Get all direct child folders
                string[] subFolders = Directory.GetDirectories(folderPath);
                foreach (string subFolder in subFolders)
                {
                    string relativePath = subFolder.Replace('\\', '/');
                    directChildren.Add(relativePath);
                }

                // Get all direct child files
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    // Skip .meta files
                    if (file.EndsWith(".meta")) continue;

                    string relativePath = file.Replace('\\', '/');
                    directChildren.Add(relativePath);
                }

                if (directChildren.Count == 0) continue;

                // Move direct children (both files and folders) out of the folder
                foreach (string childPath in directChildren)
                {
                    string assetName = Path.GetFileName(childPath);
                    string destPath = parentFolder + "/" + assetName;

                    // Handle name conflicts
                    int counter = 1;
                    string originalDestPath = destPath;
                    while (Directory.Exists(destPath) || File.Exists(destPath) || File.Exists(destPath + ".meta"))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(originalDestPath);
                        string ext = Path.GetExtension(originalDestPath);
                        destPath = parentFolder + "/" + nameWithoutExt + " " + counter + ext;
                        counter++;
                    }

                    string moveResult = AssetDatabase.MoveAsset(childPath, destPath);
                    if (string.IsNullOrEmpty(moveResult))
                    {
                        totalFilesMoved++;
                        Object movedObj = AssetDatabase.LoadAssetAtPath<Object>(destPath);
                        if (movedObj != null)
                            newSelection.Add(movedObj);
                    }
                    else
                    {
                        Debug.LogWarning($"[Group Ungroup Extract Tool] Failed to move {childPath}: {moveResult}");
                    }
                }

                // Always mark folder for deletion in Ungroup
                foldersToDelete.Add(folderPath);
            }

            // Delete folders (always for Ungroup in Project)
            foreach (string folderPath in foldersToDelete)
            {
                bool deleteSuccess = AssetDatabase.DeleteAsset(folderPath);
                if (!deleteSuccess)
                {
                    Debug.LogWarning($"[Group Ungroup Extract Tool] Failed to delete folder {folderPath}");
                }
            }

            AssetDatabase.Refresh();

            if (newSelection.Count > 0)
            {
                Selection.objects = newSelection.ToArray();
            }

            NotifyUngroupOperation(totalFilesMoved, foldersToDelete.Count);
        }

        private static string GetCommonParentFolder(List<Object> objects)
        {
            if (objects.Count == 0) return null;

            string firstPath = AssetDatabase.GetAssetPath(objects[0]);
            string commonParent = Path.GetDirectoryName(firstPath).Replace('\\', '/');

            for (int i = 1; i < objects.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(objects[i]);
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');

                // Find common parent
                while (!string.IsNullOrEmpty(commonParent) && !parent.StartsWith(commonParent))
                {
                    commonParent = Path.GetDirectoryName(commonParent).Replace('\\', '/');
                }
            }

            return commonParent;
        }

        private static void ExtractProjectSelection()
        {
            var selection = GetProjectSelection();
            if (selection.Count == 0)
            {
                ShowToast("No valid assets selected in Project window.", isWarning: true);
                return;
            }

            List<string> foldersToExtract = new List<string>();
            foreach (var obj in selection)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    foldersToExtract.Add(path);
                }
            }

            if (foldersToExtract.Count == 0)
            {
                ShowToast("Select folders to extract.", isWarning: true);
                return;
            }

            List<Object> newSelection = new List<Object>();
            int totalFilesMoved = 0;

            foreach (string folderPath in foldersToExtract)
            {
                // Get all files in the folder
                string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
                if (guids.Length == 0) continue;

                string parentFolder = Path.GetDirectoryName(folderPath).Replace('\\', '/');
                if (string.IsNullOrEmpty(parentFolder))
                    parentFolder = "Assets";

                // Move files out of the folder (but keep the folder)
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(assetPath))
                        continue; // Skip subfolders for now

                    string fileName = Path.GetFileName(assetPath);
                    string destPath = parentFolder + "/" + fileName;

                    // Handle name conflicts
                    int counter = 1;
                    string originalDestPath = destPath;
                    while (File.Exists(destPath) || File.Exists(destPath + ".meta"))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(originalDestPath);
                        string ext = Path.GetExtension(originalDestPath);
                        destPath = parentFolder + "/" + nameWithoutExt + " " + counter + ext;
                        counter++;
                    }

                    string moveResult = AssetDatabase.MoveAsset(assetPath, destPath);
                    if (string.IsNullOrEmpty(moveResult))
                    {
                        totalFilesMoved++;
                        Object movedObj = AssetDatabase.LoadAssetAtPath<Object>(destPath);
                        if (movedObj != null)
                            newSelection.Add(movedObj);
                    }
                }
            }

            AssetDatabase.Refresh();

            if (newSelection.Count > 0)
            {
                Selection.objects = newSelection.ToArray();
            }

            // Notify user
            string message = totalFilesMoved == 1
                ? "Extracted 1 file"
                : $"Extracted {totalFilesMoved} files";

            var settings = GroupUngroupExtractToolConfig.Instance;
            if (settings.showToastNotification)
            {
                ShowToast(message, isWarning: false);
            }
            if (settings.showConsoleLog)
            {
                Debug.Log($"[Group Ungroup Extract Tool] {message}");
            }
        }

        // ========== Timeline Grouping ==========

        private static bool IsTimelinePackageAvailable()
        {
            // Check if Timeline assembly is available
            return System.Type.GetType("UnityEngine.Timeline.TimelineAsset,Unity.Timeline") != null;
        }

        private static List<object> GetTimelineSelection()
        {
            // New implementation for Timeline selection:
            // Unity's internal SelectionManager.SelectedTracks() simply filters
            // UnityEditor.Selection.objects by TrackAsset type.
            // We mirror this behaviour via reflection without taking a hard dependency
            // on UnityEngine.Timeline at compile time.

            List<object> tracks = new List<object>();

            if (!IsTimelinePackageAvailable())
            {
                return tracks;
            }

            try
            {
                // Resolve TrackAsset type from the Timeline runtime assembly
                // Full name: UnityEngine.Timeline.TrackAsset, Unity.Timeline
                var trackAssetType = System.Type.GetType("UnityEngine.Timeline.TrackAsset, Unity.Timeline");
                if (trackAssetType == null)
                {
                    return tracks;
                }

                var selectionObjects = Selection.objects;
                if (selectionObjects == null || selectionObjects.Length == 0)
                {
                    return tracks;
                }

                foreach (var obj in selectionObjects)
                {
                    if (obj == null) continue;

                    var objType = obj.GetType();
                    if (trackAssetType.IsAssignableFrom(objType))
                    {
                        tracks.Add(obj);
                    }
                }

            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Group Ungroup Extract Tool] Failed to get Timeline selection: {ex.Message}\n{ex.StackTrace}");
            }

            return tracks;
        }

        private static bool IsGroupTrack(object track)
        {
            if (track == null || !IsTimelinePackageAvailable())
                return false;

            try
            {
                var groupTrackType = System.Type.GetType("UnityEngine.Timeline.GroupTrack,Unity.Timeline");
                if (groupTrackType == null)
                    return false;

                return groupTrackType.IsAssignableFrom(track.GetType());
            }
            catch
            {
                return false;
            }
        }

        private static void GroupTimelineSelection()
        {
            // Упрощённая и надёжная логика для Timeline:
            // - если выделены только обычные треки -> группируем их в новую группу;
            // - если выделены только группы (2 и более) -> группируем группы в новую группу;
            // - смешанное выделение (треки + группы) не поддерживаем, чтобы не ломать состояние Timeline.

            if (!IsTimelinePackageAvailable())
            {
                ShowToast("Timeline package is not available.", isWarning: true);
                return;
            }

            var selection = GetTimelineSelection();
            if (selection.Count == 0)
            {
                ShowToast("No tracks selected in Timeline.", isWarning: true);
                return;
            }

            var tracksToGroup = new List<object>();
            var groupsToGroup = new List<object>();
            foreach (var t in selection)
            {
                if (IsGroupTrack(t))
                    groupsToGroup.Add(t);
                else
                    tracksToGroup.Add(t);
            }

            bool hasTracks = tracksToGroup.Count > 0;
            bool hasGroups = groupsToGroup.Count > 0;

            if (hasTracks && hasGroups)
            {
                ShowToast("Select either only tracks or only group tracks to group in Timeline.", isWarning: true);
                return;
            }

            // Если только группы – нужно минимум две для новой группы
            if (!hasTracks && groupsToGroup.Count < 2)
            {
                ShowToast("Select at least two group tracks to group them in Timeline.", isWarning: true);
                return;
            }

            // Разрешаем типы и получаем TimelineAsset
            var trackAssetType = System.Type.GetType("UnityEngine.Timeline.TrackAsset, Unity.Timeline");
            var groupTrackType = System.Type.GetType("UnityEngine.Timeline.GroupTrack, Unity.Timeline");
            var timelineEditorType = System.Type.GetType("UnityEditor.Timeline.TimelineEditor, Unity.Timeline.Editor");
            if (trackAssetType == null || groupTrackType == null || timelineEditorType == null)
            {
                ShowToast("Timeline types are not available.", isWarning: true);
                return;
            }

            var inspectedAssetProperty = timelineEditorType.GetProperty("inspectedAsset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var timeline = inspectedAssetProperty?.GetValue(null);
            if (timeline == null)
            {
                ShowToast("No Timeline asset is open.", isWarning: true);
                return;
            }

            var timelineType = timeline.GetType();
            var createTrackMethod = timelineType.GetMethod(
                "CreateTrack",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(System.Type), trackAssetType, typeof(string) },
                null);
            if (createTrackMethod == null)
            {
                ShowToast("Failed to find CreateTrack method.", isWarning: true);
                return;
            }

            var trackExtensionsType = System.Type.GetType("UnityEditor.Timeline.TrackExtensions, Unity.Timeline.Editor");
            if (trackExtensionsType == null)
            {
                ShowToast("Timeline TrackExtensions type not found.", isWarning: true);
                return;
            }

            var reparentMethod = trackExtensionsType.GetMethod(
                "ReparentTracks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (reparentMethod == null)
            {
                ShowToast("Failed to access Timeline reparent method.", isWarning: true);
                return;
            }

            // Выбираем, что именно группируем
            var sourceList = hasTracks ? tracksToGroup : groupsToGroup;

            // Находим общего родителя
            object commonParent = GetCommonTimelineParentForTracks(sourceList, trackAssetType, timeline);

            // Создаём новую группу
            string groupName = "Group";
            object newGroupTrack;
            if (createTrackMethod.GetParameters().Length == 3)
            {
                newGroupTrack = createTrackMethod.Invoke(timeline, new object[] { groupTrackType, commonParent, groupName });
            }
            else
            {
                newGroupTrack = createTrackMethod.Invoke(timeline, new object[] { groupTrackType, commonParent });
                if (newGroupTrack != null)
                {
                    var nameProp = newGroupTrack.GetType().GetProperty("name");
                    nameProp?.SetValue(newGroupTrack, groupName);
                }
            }

            if (newGroupTrack == null)
            {
                ShowToast("Failed to create group track.", isWarning: true);
                return;
            }

            // Готовим список TrackAsset для ReparentTracks
            var trackListType = typeof(List<>).MakeGenericType(trackAssetType);
            var trackList = System.Activator.CreateInstance(trackListType);
            var addMethod = trackListType.GetMethod("Add");
            foreach (var t in sourceList)
            {
                addMethod.Invoke(trackList, new object[] { t });
            }

            // ReparentTracks(List<TrackAsset>, PlayableAsset targetParent, TrackAsset insertMarker, bool insertBefore)
            reparentMethod.Invoke(null, new object[] { trackList, newGroupTrack, null, false });

            // Обновляем Timeline
            EditorUtility.SetDirty(timeline as UnityEngine.Object);
            var refresh = timelineEditorType.GetMethod("Refresh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (refresh != null)
            {
                var refreshReasonType = System.Type.GetType("UnityEditor.Timeline.RefreshReason, Unity.Timeline.Editor");
                if (refreshReasonType != null)
                {
                    var contentsModified = System.Enum.Parse(refreshReasonType, "ContentsAddedOrRemoved");
                    refresh.Invoke(null, new object[] { contentsModified });
                }
            }

            var timelineWindowType = System.Type.GetType("UnityEditor.Timeline.TimelineWindow, Unity.Timeline.Editor");
            var instanceProp = timelineWindowType?.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var window = instanceProp?.GetValue(null) as EditorWindow;
            window?.Repaint();

            Selection.activeObject = newGroupTrack as UnityEngine.Object;
            NotifyGroupOperation(sourceList.Count);
        }

        // Находит общего родителя для треков в Timeline: TrackAsset или null (корень).
        private static object GetCommonTimelineParentForTracks(List<object> tracks, System.Type trackAssetType, object timelineAsset)
        {
            if (tracks == null || tracks.Count == 0)
                return null;

            object commonParent = null;

            foreach (var t in tracks)
            {
                if (t == null) continue;

                var parentProp = t.GetType().GetProperty("parent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                object parent = parentProp?.GetValue(t);

                // Корневой уровень (TimelineAsset) трактуем как null
                if (parent == null || parent == timelineAsset)
                {
                    parent = null;
                }
                else if (!trackAssetType.IsAssignableFrom(parent.GetType()))
                {
                    // Неизвестный тип родителя – считаем, что общего родителя нет
                    return null;
                }

                if (commonParent == null)
                {
                    commonParent = parent;
                }
                else if (!ReferenceEquals(commonParent, parent))
                {
                    // Родители различаются – создаём группу на корне
                    return null;
                }
            }

            return commonParent;
        }

        private static void UngroupTimelineSelection()
        {
            if (!IsTimelinePackageAvailable())
            {
                ShowToast("Timeline package is not available.", isWarning: true);
                return;
            }

            var tracks = GetTimelineSelection();

            if (tracks.Count == 0)
            {
                ShowToast("No tracks selected in Timeline.", isWarning: true);
                return;
            }

            // Filter only group tracks
            List<object> groupTracks = new List<object>();
            foreach (var track in tracks)
            {
                if (IsGroupTrack(track))
                {
                    groupTracks.Add(track);
                }
            }

            if (groupTracks.Count == 0)
            {
                ShowToast("Select group tracks to ungroup.", isWarning: true);
                return;
            }

            // Готовим типы и TimelineAsset
            var trackAssetType = System.Type.GetType("UnityEngine.Timeline.TrackAsset, Unity.Timeline");
            var timelineEditorType = System.Type.GetType("UnityEditor.Timeline.TimelineEditor, Unity.Timeline.Editor");
            if (trackAssetType == null || timelineEditorType == null)
            {
                ShowToast("Timeline types are not available.", isWarning: true);
                return;
            }

            var inspectedAssetProperty = timelineEditorType.GetProperty("inspectedAsset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var timeline = inspectedAssetProperty?.GetValue(null);
            if (timeline == null)
            {
                ShowToast("Cannot access Timeline asset.", isWarning: true);
                return;
            }

            var trackExtensionsType = System.Type.GetType("UnityEditor.Timeline.TrackExtensions, Unity.Timeline.Editor");
            if (trackExtensionsType == null)
            {
                ShowToast("Timeline TrackExtensions type not found.", isWarning: true);
                return;
            }

            var reparentMethod = trackExtensionsType.GetMethod(
                "ReparentTracks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (reparentMethod == null)
            {
                ShowToast("Failed to access Timeline reparent method.", isWarning: true);
                return;
            }

            var trackListType = typeof(List<>).MakeGenericType(trackAssetType);
            int totalTracksMoved = 0;

            // Для каждой группы переносим её детей на уровень родителя
            foreach (var groupTrack in groupTracks)
            {
                if (groupTrack == null) continue;

                // Достаём детей через публичный GetChildTracks()
                var getChildTracks = groupTrack.GetType().GetMethod("GetChildTracks", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var enumerable = getChildTracks?.Invoke(groupTrack, null) as System.Collections.IEnumerable;
                if (enumerable == null)
                    continue;

                var childList = System.Activator.CreateInstance(trackListType);
                var addMethod = trackListType.GetMethod("Add");

                foreach (var child in enumerable)
                {
                    if (child == null) continue;
                    addMethod.Invoke(childList, new object[] { child });
                    totalTracksMoved++;
                }

                // Определяем целевого родителя: либо TrackAsset, либо сам TimelineAsset
                var parentProp = groupTrack.GetType().GetProperty("parent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var parent = parentProp?.GetValue(groupTrack);
                object targetParentPlayable = timeline; // по умолчанию корень
                if (parent != null && trackAssetType.IsAssignableFrom(parent.GetType()))
                {
                    targetParentPlayable = parent;
                }

                // Переносим детей вверх
                reparentMethod.Invoke(null, new object[] { childList, targetParentPlayable, null, false });
            }

            // После переноса детей удаляем сами группы так же, как делал Unity при Delete:
            // выделяем группы, фокусируем Timeline и вызываем Edit/Delete.
            var groupUnityObjects = new List<UnityEngine.Object>();
            foreach (var g in groupTracks)
            {
                if (g is UnityEngine.Object uo)
                    groupUnityObjects.Add(uo);
            }

            if (groupUnityObjects.Count > 0)
            {
                Selection.objects = groupUnityObjects.ToArray();

                // Гарантируем, что команда Delete применяется к трекам Timeline
                FocusTimelineWindowIfAvailable();
                EditorApplication.ExecuteMenuItem("Edit/Delete");
            }

            // Обновляем Timeline
            EditorUtility.SetDirty(timeline as UnityEngine.Object);
            var refreshMethod = timelineEditorType.GetMethod("Refresh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (refreshMethod != null)
            {
                var refreshReasonType = System.Type.GetType("UnityEditor.Timeline.RefreshReason, Unity.Timeline.Editor");
                if (refreshReasonType != null)
                {
                    var contentsModified = System.Enum.Parse(refreshReasonType, "ContentsAddedOrRemoved");
                    refreshMethod.Invoke(null, new object[] { contentsModified });
                }
            }

            var timelineWindowType2 = System.Type.GetType("UnityEditor.Timeline.TimelineWindow,Unity.Timeline.Editor");
            var instanceProperty2 = timelineWindowType2?.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var window2 = instanceProperty2?.GetValue(null) as EditorWindow;
            window2?.Repaint();

            NotifyUngroupOperation(totalTracksMoved, groupTracks.Count);
        }

        private static object GetCommonParentTrack(List<object> tracks, object rootTrack)
        {
            if (tracks.Count == 0 || rootTrack == null)
                return rootTrack;

            object commonParent = null;

            foreach (var track in tracks)
            {
                var parentProperty = track.GetType().GetProperty("parent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (parentProperty == null)
                    continue;

                object parent = parentProperty.GetValue(track);
                if (parent == null)
                    parent = rootTrack;

                if (commonParent == null)
                {
                    commonParent = parent;
                }
                else if (commonParent != parent)
                {
                    // Tracks have different parents, return root
                    return rootTrack;
                }
            }

            return commonParent ?? rootTrack;
        }

        private static void ExtractTimelineSelection()
        {
            if (!IsTimelinePackageAvailable())
            {
                ShowToast("Timeline package is not available.", isWarning: true);
                return;
            }

            var tracks = GetTimelineSelection();
            if (tracks.Count == 0)
            {
                ShowToast("No tracks selected in Timeline.", isWarning: true);
                return;
            }

            // Filter only group tracks
            List<object> groupTracks = new List<object>();
            foreach (var track in tracks)
            {
                if (IsGroupTrack(track))
                {
                    groupTracks.Add(track);
                }
            }

            if (groupTracks.Count == 0)
            {
                ShowToast("Select group tracks to extract.", isWarning: true);
                return;
            }

            // Готовим типы и TimelineAsset
            var trackAssetType = System.Type.GetType("UnityEngine.Timeline.TrackAsset, Unity.Timeline");
            var timelineEditorType = System.Type.GetType("UnityEditor.Timeline.TimelineEditor, Unity.Timeline.Editor");
            if (trackAssetType == null || timelineEditorType == null)
            {
                ShowToast("Timeline types are not available.", isWarning: true);
                return;
            }

            var inspectedAssetProperty = timelineEditorType.GetProperty("inspectedAsset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var timeline = inspectedAssetProperty?.GetValue(null);
            if (timeline == null)
            {
                ShowToast("Cannot access Timeline asset.", isWarning: true);
                return;
            }

            var trackExtensionsType = System.Type.GetType("UnityEditor.Timeline.TrackExtensions, Unity.Timeline.Editor");
            if (trackExtensionsType == null)
            {
                ShowToast("Timeline TrackExtensions type not found.", isWarning: true);
                return;
            }

            var reparentMethod = trackExtensionsType.GetMethod(
                "ReparentTracks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (reparentMethod == null)
            {
                ShowToast("Failed to access Timeline reparent method.", isWarning: true);
                return;
            }

            var trackListType = typeof(List<>).MakeGenericType(trackAssetType);
            int totalTracksMoved = 0;

            // Для каждой группы переносим её детей на уровень родителя (но НЕ удаляем саму группу)
            foreach (var groupTrack in groupTracks)
            {
                if (groupTrack == null) continue;

                // Достаём детей через публичный GetChildTracks()
                var getChildTracks = groupTrack.GetType().GetMethod("GetChildTracks", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var enumerable = getChildTracks?.Invoke(groupTrack, null) as System.Collections.IEnumerable;
                if (enumerable == null)
                    continue;

                var childList = System.Activator.CreateInstance(trackListType);
                var addMethod = trackListType.GetMethod("Add");

                foreach (var child in enumerable)
                {
                    if (child == null) continue;
                    addMethod.Invoke(childList, new object[] { child });
                    totalTracksMoved++;
                }

                // Определяем целевого родителя: либо TrackAsset, либо сам TimelineAsset
                var parentProp = groupTrack.GetType().GetProperty("parent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var parent = parentProp?.GetValue(groupTrack);
                object targetParentPlayable = timeline; // по умолчанию корень
                if (parent != null && trackAssetType.IsAssignableFrom(parent.GetType()))
                {
                    targetParentPlayable = parent;
                }

                // Переносим детей вверх (но группу НЕ удаляем)
                reparentMethod.Invoke(null, new object[] { childList, targetParentPlayable, null, false });
            }

            // Обновляем Timeline
            EditorUtility.SetDirty(timeline as UnityEngine.Object);
            var refreshMethod = timelineEditorType.GetMethod("Refresh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (refreshMethod != null)
            {
                var refreshReasonType = System.Type.GetType("UnityEditor.Timeline.RefreshReason, Unity.Timeline.Editor");
                if (refreshReasonType != null)
                {
                    var contentsModified = System.Enum.Parse(refreshReasonType, "ContentsAddedOrRemoved");
                    refreshMethod.Invoke(null, new object[] { contentsModified });
                }
            }

            var timelineWindowType = System.Type.GetType("UnityEditor.Timeline.TimelineWindow,Unity.Timeline.Editor");
            var instanceProperty = timelineWindowType?.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var window = instanceProperty?.GetValue(null) as EditorWindow;
            window?.Repaint();

            // Notify user
            string message = totalTracksMoved == 1
                ? "Extracted 1 track"
                : $"Extracted {totalTracksMoved} tracks";

            var settings = GroupUngroupExtractToolConfig.Instance;
            if (settings.showToastNotification)
            {
                ShowToast(message, isWarning: false);
            }
            if (settings.showConsoleLog)
            {
                Debug.Log($"[Group Ungroup Extract Tool] {message}");
            }
        }
    }
}

