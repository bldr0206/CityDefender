using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Multitool.SelectVisible
{
    [InitializeOnLoad]
    public static class VisibleSelection
    {
        private const string MenuPathEnable = "Tools/Multitool/Select Visible/Enable";
        private const string MenuPathRespectAlpha = "Tools/Multitool/Select Visible/Respect Alpha";
        private const string MenuPathIgnoreAdditive = "Tools/Multitool/Select Visible/Ignore Additive";
        private const string MenuPathSelectionModeObject = "Tools/Multitool/Select Visible/Selection Mode/Object";
        private const string MenuPathSelectionModeNearestPrefab = "Tools/Multitool/Select Visible/Selection Mode/Nearest Prefab";
        private const string MenuPathSelectionModeTopPrefab = "Tools/Multitool/Select Visible/Selection Mode/Top Prefab";

        public const string PrefKeyEnable = "Multitool.SelectVisible.Enabled";
        public const string PrefKeyRespectAlpha = "Multitool.SelectVisible.RespectAlpha";
        public const string PrefKeyIgnoreAdditive = "Multitool.SelectVisible.IgnoreAdditive";
        public const string PrefKeySelectionMode = "Multitool.SelectVisible.SelectionMode";
        public const string PrefKeyShowBounds = "Multitool.SelectVisible.ShowBounds";
        public const string PrefKeyShowName = "Multitool.SelectVisible.ShowName";
        public const string PrefKeyBoundsOutlineAlpha = "Multitool.SelectVisible.BoundsOutlineAlpha";
        private const string PREF_OVERLAY_ENABLED = "SV_OverlayEnabled";
        private const string PREF_OVERLAY_EXPANDED = "SV_OverlayExpanded";


        private const float AlphaThreshold = 0.1f;
        private const float DefaultBoundsOutlineAlpha = 0.8f;
        private const float FlatBoundsRelativeThreshold = 0.025f;
        private const float FlatBoundsAbsoluteThreshold = 0.0005f;

        public static bool OverlayEnabled
        {
            get => EditorPrefs.GetBool(PREF_OVERLAY_ENABLED, true);
            set
            {
                EditorPrefs.SetBool(PREF_OVERLAY_ENABLED, value);
                SceneView.RepaintAll();
            }
        }

        public static bool OverlayExpanded
        {
            get => EditorPrefs.GetBool(PREF_OVERLAY_EXPANDED, true);
            set
            {
                EditorPrefs.SetBool(PREF_OVERLAY_EXPANDED, value);
                SceneView.RepaintAll();
            }
        }


        private const float ClickDragThreshold = 6f;


        private static bool _mouseDown;
        private static Vector2 _mouseDownPosition;
        private static bool _mouseDragged;

        private static Mesh _skinnedMeshBuffer;
        private static GameObject _hoveredObject;
        private static Vector2 _lastMousePosition;

        public enum SelectionMode
        {
            Object = 0,
            NearestPrefab = 1,
            TopPrefab = 2,
        }

        private struct PickCandidate
        {
            public GameObject GameObject;
            public RenderOrderInfo OrderInfo;
            public bool AlphaPassed;
            public int PickOrder;
        }

        private struct RenderOrderInfo
        {
            public int SortingLayerValue;
            public int SortingOrder;
            public int SortingGroupLayerValue;
            public int SortingGroupOrder;
            public float SurfaceDistance;
            public int SiblingIndex;
            public bool IsUIElement;
            public int UiRootCanvasInstanceId;
            public int[] UiHierarchyPath;
        }

        static VisibleSelection()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.delayCall += SyncMenuChecks;
        }

        [MenuItem(MenuPathEnable)]
        private static void ToggleEnable()
        {
            bool newValue = !IsEnabled();
            EditorPrefs.SetBool(PrefKeyEnable, newValue);
            SyncMenuChecks();
            SceneView.RepaintAll();
        }

        [MenuItem(MenuPathEnable, true)]
        private static bool ToggleEnableValidate()
        {
            Menu.SetChecked(MenuPathEnable, IsEnabled());
            return true;
        }

        [MenuItem(MenuPathRespectAlpha)]
        private static void ToggleRespectAlpha()
        {
            bool newValue = !RespectAlpha();
            EditorPrefs.SetBool(PrefKeyRespectAlpha, newValue);
            SyncMenuChecks();
        }

        [MenuItem(MenuPathRespectAlpha, true)]
        private static bool ToggleRespectAlphaValidate()
        {
            Menu.SetChecked(MenuPathRespectAlpha, RespectAlpha());
            return true;
        }

        [MenuItem(MenuPathIgnoreAdditive)]
        private static void ToggleIgnoreAdditive()
        {
            bool newValue = !IgnoreAdditiveEnabled();
            EditorPrefs.SetBool(PrefKeyIgnoreAdditive, newValue);
            SyncMenuChecks();
        }

        [MenuItem(MenuPathIgnoreAdditive, true)]
        private static bool ToggleIgnoreAdditiveValidate()
        {
            Menu.SetChecked(MenuPathIgnoreAdditive, IgnoreAdditiveEnabled());
            return true;
        }

        [MenuItem(MenuPathSelectionModeObject)]
        private static void SetSelectionModeObject()
        {
            SetSelectionMode(SelectionMode.Object);
        }

        [MenuItem(MenuPathSelectionModeObject, true)]
        private static bool SetSelectionModeObjectValidate()
        {
            Menu.SetChecked(MenuPathSelectionModeObject, GetSelectionMode() == SelectionMode.Object);
            return true;
        }

        [MenuItem(MenuPathSelectionModeNearestPrefab)]
        private static void SetSelectionModeNearestPrefab()
        {
            SetSelectionMode(SelectionMode.NearestPrefab);
        }

        [MenuItem(MenuPathSelectionModeNearestPrefab, true)]
        private static bool SetSelectionModeNearestPrefabValidate()
        {
            Menu.SetChecked(MenuPathSelectionModeNearestPrefab, GetSelectionMode() == SelectionMode.NearestPrefab);
            return true;
        }

        [MenuItem(MenuPathSelectionModeTopPrefab)]
        private static void SetSelectionModeTopPrefab()
        {
            SetSelectionMode(SelectionMode.TopPrefab);
        }

        [MenuItem(MenuPathSelectionModeTopPrefab, true)]
        private static bool SetSelectionModeTopPrefabValidate()
        {
            Menu.SetChecked(MenuPathSelectionModeTopPrefab, GetSelectionMode() == SelectionMode.TopPrefab);
            return true;
        }

        [MenuItem("Tools/Multitool/Select Visible/Show Bounds")]
        private static void ToggleShowBoundsMenu()
        {
            bool newValue = !ShowBounds();
            EditorPrefs.SetBool(PrefKeyShowBounds, newValue);
            if (newValue && BoundsOutlineAlpha() <= 0.0001f)
            {
                SetBoundsOutlineAlpha(DefaultBoundsOutlineAlpha);
            }
            SyncMenuChecks();
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/Multitool/Select Visible/Show Bounds", true)]
        private static bool ToggleShowBoundsMenuValidate()
        {
            Menu.SetChecked("Tools/Multitool/Select Visible/Show Bounds", ShowBounds());
            return true;
        }

        [MenuItem("Tools/Multitool/Select Visible/Show Name")]
        private static void ToggleShowNameMenu()
        {
            bool newValue = !ShowName();
            EditorPrefs.SetBool(PrefKeyShowName, newValue);
            SyncMenuChecks();
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/Multitool/Select Visible/Show Name", true)]
        private static bool ToggleShowNameMenuValidate()
        {
            Menu.SetChecked("Tools/Multitool/Select Visible/Show Name", ShowName());
            return true;
        }

        [MenuItem("Tools/Multitool/Select Visible/Show Overlay", false, 0)]
        private static void ToggleOverlayMenu()
        {
            OverlayEnabled = !OverlayEnabled;
        }

        [MenuItem("Tools/Multitool/Select Visible/Show Overlay", true)]
        private static bool ToggleOverlayMenuValidate()
        {
            Menu.SetChecked("Tools/Multitool/Select Visible/Show Overlay", OverlayEnabled);
            return true;
        }

        public static void SyncMenuChecks()
        {
            Menu.SetChecked(MenuPathEnable, IsEnabled());
            Menu.SetChecked(MenuPathRespectAlpha, RespectAlpha());
            Menu.SetChecked(MenuPathIgnoreAdditive, IgnoreAdditiveEnabled());
            Menu.SetChecked(MenuPathSelectionModeObject, GetSelectionMode() == SelectionMode.Object);
            Menu.SetChecked(MenuPathSelectionModeNearestPrefab, GetSelectionMode() == SelectionMode.NearestPrefab);
            Menu.SetChecked(MenuPathSelectionModeTopPrefab, GetSelectionMode() == SelectionMode.TopPrefab);
            Menu.SetChecked("Tools/Multitool/Select Visible/Show Bounds", ShowBounds());
            Menu.SetChecked("Tools/Multitool/Select Visible/Show Name", ShowName());
        }

        public static bool IsEnabled()
        {
            return EditorPrefs.GetBool(PrefKeyEnable, true);
        }

        public static bool RespectAlpha()
        {
            return EditorPrefs.GetBool(PrefKeyRespectAlpha, false);
        }

        public static bool IgnoreAdditiveEnabled()
        {
            return EditorPrefs.GetBool(PrefKeyIgnoreAdditive, false);
        }

        public static SelectionMode GetSelectionMode()
        {
            return (SelectionMode)EditorPrefs.GetInt(PrefKeySelectionMode, (int)SelectionMode.Object);
        }

        public static void SetSelectionMode(SelectionMode mode)
        {
            if (GetSelectionMode() == mode)
                return;

            EditorPrefs.SetInt(PrefKeySelectionMode, (int)mode);
            SyncMenuChecks();
            SceneView.RepaintAll();
        }

        public static bool ShowBounds()
        {
            return EditorPrefs.GetBool(PrefKeyShowBounds, false);
        }

        public static bool ShowName()
        {
            return EditorPrefs.GetBool(PrefKeyShowName, false);
        }

        public static float BoundsOutlineAlpha()
        {
            return EditorPrefs.GetFloat(PrefKeyBoundsOutlineAlpha, DefaultBoundsOutlineAlpha);
        }

        public static void SetBoundsOutlineAlpha(float alpha)
        {
            EditorPrefs.SetFloat(PrefKeyBoundsOutlineAlpha, Mathf.Clamp01(alpha));
        }

        private static bool ShouldDrawDebugOverlay()
        {
            return ShowBounds() || ShowName();
        }

        private static GameObject ApplySelectionMode(GameObject go)
        {
            if (go == null)
                return null;

            switch (GetSelectionMode())
            {
                case SelectionMode.NearestPrefab:
                    return GetNearestPrefabRoot(go);
                case SelectionMode.TopPrefab:
                    return GetTopPrefabRoot(go);
                default:
                    return go;
            }
        }

        private static GameObject GetNearestPrefabRoot(GameObject go)
        {
            if (go == null)
                return null;

            GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            return nearestRoot != null ? nearestRoot : go;
        }

        private static GameObject GetTopPrefabRoot(GameObject go)
        {
            if (go == null)
                return null;

            GameObject topRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            return topRoot != null ? topRoot : go;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!IsEnabled())
                return;

            Event e = Event.current;
            if (e == null)
                return;


            int hotControl = GUIUtility.hotControl;
            if (hotControl != 0)
            {

                if (_mouseDown)
                {

                    _mouseDragged = true;
                }

                if (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow)
                {
                    _mouseDown = false;
                    _mouseDragged = false;
                }

                return;
            }

            if (e.control || e.command)
                return;


            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        _mouseDown = true;
                        _mouseDragged = false;
                        _mouseDownPosition = e.mousePosition;
                    }
                    break;

                case EventType.MouseDrag:
                    if (_mouseDown && e.button == 0)
                    {
                        if (!_mouseDragged)
                        {
                            if ((e.mousePosition - _mouseDownPosition).magnitude > ClickDragThreshold)
                            {
                                _mouseDragged = true;
                            }
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (_mouseDown && e.button == 0)
                    {
                        _mouseDown = false;


                        if (_mouseDragged || e.alt)
                            break;

                        HandleClickSelection(sceneView, e.mousePosition, e);
                    }
                    break;

                case EventType.MouseMove:
                    if (ShouldDrawDebugOverlay())
                    {
                        UpdateHoveredObject(sceneView, e.mousePosition);
                        sceneView.Repaint();
                    }
                    break;

                case EventType.MouseLeaveWindow:
                    if (ShouldDrawDebugOverlay())
                    {
                        _hoveredObject = null;
                        sceneView.Repaint();
                    }
                    break;

                case EventType.Repaint:
                    if (ShouldDrawDebugOverlay() && _hoveredObject != null)
                    {
                        DrawDebugOutline(_hoveredObject, sceneView);
                    }
                    break;
            }
        }

        private static void HandleClickSelection(SceneView sceneView, Vector2 mousePos, Event e)
        {
            bool respectAlpha = RespectAlpha();
            GameObject picked = PickConsideringRenderOrder(sceneView, mousePos, respectAlpha);

            picked = ApplySelectionMode(picked);

            if (e.shift)
            {

                if (picked != null)
                {
                    var current = new List<Object>(Selection.objects);
                    if (!current.Contains(picked))
                    {
                        current.Add(picked);
                    }
                    Selection.objects = current.ToArray();
                }
            }
            else
            {
                if (picked != null)
                {
                    Selection.activeGameObject = picked;
                }
                else
                {
                    Selection.activeGameObject = null;
                }
            }

            e.Use();
            sceneView.Repaint();
        }

        private static GameObject PickConsideringRenderOrder(SceneView sceneView, Vector2 mousePos, bool respectAlpha)
        {
            var candidates = CollectPickCandidates(sceneView, mousePos, respectAlpha);
            if (candidates.Count == 0)
                return null;

            candidates.Sort(CompareCandidatesByRenderOrder);

            foreach (var candidate in candidates)
            {
                if (!respectAlpha || candidate.AlphaPassed)
                    return candidate.GameObject;
            }

            return null;
        }

        private static List<PickCandidate> CollectPickCandidates(SceneView sceneView, Vector2 mousePos, bool respectAlpha)
        {
            var candidates = new List<PickCandidate>(8);
            var ignore = new List<GameObject>(8);

            for (int safe = 0; safe < 64; safe++)
            {
                GameObject picked = ignore.Count == 0
                    ? HandleUtility.PickGameObject(mousePos, false)
                    : HandleUtility.PickGameObject(mousePos, false, ignore.ToArray());

                if (picked == null)
                    break;

                ignore.Add(picked);

                if (!IsPickable(picked, mousePos, sceneView))
                    continue;

                bool alphaPassed = !respectAlpha || PassesAlphaTest(picked, sceneView, mousePos, respectAlpha);
                candidates.Add(new PickCandidate
                {
                    GameObject = picked,
                    AlphaPassed = alphaPassed,
                    PickOrder = candidates.Count,
                    OrderInfo = BuildRenderOrderInfo(picked, sceneView, mousePos)
                });
            }

            return candidates;
        }

        private static int CompareCandidatesByRenderOrder(PickCandidate a, PickCandidate b)
        {
            int cmp = b.OrderInfo.SortingGroupLayerValue.CompareTo(a.OrderInfo.SortingGroupLayerValue);
            if (cmp != 0)
                return cmp;

            cmp = b.OrderInfo.SortingGroupOrder.CompareTo(a.OrderInfo.SortingGroupOrder);
            if (cmp != 0)
                return cmp;

            cmp = b.OrderInfo.SortingLayerValue.CompareTo(a.OrderInfo.SortingLayerValue);
            if (cmp != 0)
                return cmp;

            cmp = b.OrderInfo.SortingOrder.CompareTo(a.OrderInfo.SortingOrder);
            if (cmp != 0)
                return cmp;

            // Для UI элементов сортировка по иерархии приоритетнее дистанции
            if (a.OrderInfo.IsUIElement && b.OrderInfo.IsUIElement)
            {
                // Важный момент: plain SiblingIndex нельзя напрямую сравнивать между разными уровнями иерархии.
                // Для корректного порядка отрисовки UI сравниваем "путь" sibling-индексов от корня Canvas (preorder traversal).
                if (a.OrderInfo.UiRootCanvasInstanceId != 0 &&
                    a.OrderInfo.UiRootCanvasInstanceId == b.OrderInfo.UiRootCanvasInstanceId &&
                    a.OrderInfo.UiHierarchyPath != null && b.OrderInfo.UiHierarchyPath != null)
                {
                    cmp = CompareUiHierarchyPath(b.OrderInfo.UiHierarchyPath, a.OrderInfo.UiHierarchyPath);
                    if (cmp != 0)
                        return cmp;
                }
                else
                {
                    // Фолбэк: внутри одного уровня (одних родителей) sibling index действительно отражает порядок (больше = поверх).
                    cmp = b.OrderInfo.SiblingIndex.CompareTo(a.OrderInfo.SiblingIndex);
                    if (cmp != 0)
                        return cmp;
                }
            }

            if (!a.OrderInfo.IsUIElement && !b.OrderInfo.IsUIElement)
            {
                cmp = a.OrderInfo.SurfaceDistance.CompareTo(b.OrderInfo.SurfaceDistance);
                if (cmp != 0)
                    return cmp;
            }

            return a.PickOrder.CompareTo(b.PickOrder);
        }

        private static int CompareUiHierarchyPath(int[] pathA, int[] pathB)
        {
            if (pathA == null && pathB == null)
                return 0;
            if (pathA == null)
                return -1;
            if (pathB == null)
                return 1;

            int min = Mathf.Min(pathA.Length, pathB.Length);
            for (int i = 0; i < min; i++)
            {
                int a = pathA[i];
                int b = pathB[i];
                if (a != b)
                    return a.CompareTo(b);
            }

            // Если один путь — префикс другого, то более глубокий (длинный) будет отрисован позже => "поверх".
            return pathA.Length.CompareTo(pathB.Length);
        }

        private static RenderOrderInfo BuildRenderOrderInfo(GameObject go, SceneView sceneView, Vector2 guiMousePos)
        {
            var info = new RenderOrderInfo
            {
                SortingLayerValue = 0,
                SortingOrder = 0,
                SortingGroupLayerValue = 0,
                SortingGroupOrder = 0,
                SurfaceDistance = float.PositiveInfinity,
                SiblingIndex = 0,
                IsUIElement = false,
                UiRootCanvasInstanceId = 0,
                UiHierarchyPath = null
            };

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                info.SortingLayerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
                info.SortingOrder = renderer.sortingOrder;
                if (TryGetRendererSurfaceDistance(renderer, guiMousePos, out float surfaceDistance))
                    info.SurfaceDistance = surfaceDistance;

                SortingGroup sortingGroup = renderer.GetComponentInParent<SortingGroup>();
                if (sortingGroup != null)
                {
                    info.SortingGroupLayerValue = SortingLayer.GetLayerValueFromID(sortingGroup.sortingLayerID);
                    info.SortingGroupOrder = sortingGroup.sortingOrder;
                }

                return info;
            }

            Graphic graphic = go.GetComponent<Graphic>();
            if (graphic != null)
            {
                info.IsUIElement = true;
                info.SiblingIndex = go.transform.GetSiblingIndex();

                Canvas canvas = graphic.canvas;
                if (canvas != null)
                {
                    info.SortingLayerValue = SortingLayer.GetLayerValueFromID(canvas.sortingLayerID);
                    info.SortingOrder = canvas.sortingOrder;

                    Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                    if (rootCanvas != null)
                    {
                        info.UiRootCanvasInstanceId = rootCanvas.GetInstanceID();
                        info.UiHierarchyPath = BuildUiHierarchyPath(go.transform, rootCanvas.transform);
                    }
                }

                return info;
            }

            return info;
        }

        private static int[] BuildUiHierarchyPath(Transform element, Transform rootCanvasTransform)
        {
            if (element == null || rootCanvasTransform == null)
                return null;

            // Собираем sibling-индексы от элемента вверх до rootCanvasTransform (включая rootCanvasTransform).
            // Затем разворачиваем, чтобы получить путь "от корня к элементу".
            var list = new List<int>(16);
            Transform t = element;
            while (t != null)
            {
                list.Add(t.GetSiblingIndex());
                if (t == rootCanvasTransform)
                    break;
                t = t.parent;
            }

            // Если элемент не под этим rootCanvasTransform (например, canvas неожиданно другой), не используем путь.
            if (list.Count == 0 || (element != rootCanvasTransform && (list.Count == 1 && element.parent == null)))
                return null;

            list.Reverse();
            return list.ToArray();
        }

        private static bool TryGetRendererSurfaceDistance(Renderer renderer, Vector2 guiMousePos, out float surfaceDistance)
        {
            surfaceDistance = float.PositiveInfinity;
            Mesh mesh = GetRendererMesh(renderer, out Matrix4x4 matrix);
            if (mesh == null || !mesh.isReadable)
                return false;

            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            if (vertices == null || vertices.Length == 0 || indices == null || indices.Length == 0)
                return false;

            Ray ray = HandleUtility.GUIPointToWorldRay(guiMousePos);
            bool hitFound = false;

            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 v0 = matrix.MultiplyPoint3x4(vertices[indices[i]]);
                Vector3 v1 = matrix.MultiplyPoint3x4(vertices[indices[i + 1]]);
                Vector3 v2 = matrix.MultiplyPoint3x4(vertices[indices[i + 2]]);

                if (IntersectRayTriangle(ray, v0, v1, v2, out float distance, out _) && distance < surfaceDistance)
                {
                    surfaceDistance = distance;
                    hitFound = true;
                }
            }

            return hitFound;
        }


        private const float IconClickRadius = 17f;

        private static bool IsPickable(GameObject go)
        {
            return IsPickable(go, null, null);
        }

        private static bool IsClickNearIcon(GameObject go, Vector2 mousePos)
        {

            Vector3 worldPos = go.transform.position;
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            float distance = Vector2.Distance(mousePos, screenPos);
            return distance <= IconClickRadius;
        }

        private static bool IsPickable(GameObject go, Vector2? mousePos, SceneView sceneView)
        {
            if (!go.activeInHierarchy)
                return false;


            var renderer = go.GetComponent<Renderer>();
            var graphic = go.GetComponent<Graphic>();
            bool hasIcon = EditorGUIUtility.GetIconForObject(go) != null;


            if (renderer == null && graphic == null && !hasIcon)
                return false;


            if (renderer == null && graphic == null && hasIcon && mousePos.HasValue)
            {
                if (!IsClickNearIcon(go, mousePos.Value))
                    return false;
            }


            if ((Tools.visibleLayers & (1 << go.layer)) == 0)
                return false;


            if ((go.hideFlags & HideFlags.NotEditable) != 0)
                return false;
            if ((go.hideFlags & HideFlags.HideInHierarchy) != 0)
                return false;

            if (mousePos.HasValue && sceneView != null)
            {
                TMP_Text tmp = go.GetComponent<TMP_Text>();
                if (tmp != null && !TmpTextHitsVisibleCharacter(tmp, mousePos.Value, sceneView))
                    return false;
            }

            return true;
        }

        private static Camera GetTmpPickCamera(TMP_Text tmp, SceneView sceneView)
        {
            if (tmp is TextMeshProUGUI ui)
            {
                Canvas canvas = ui.canvas;
                if (canvas == null)
                    return sceneView != null ? sceneView.camera : null;

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return null;

                return canvas.worldCamera != null ? canvas.worldCamera : (sceneView != null ? sceneView.camera : null);
            }

            return sceneView != null ? sceneView.camera : Camera.main;
        }

        private static bool TmpTextHitsVisibleCharacter(TMP_Text tmp, Vector2 guiMousePos, SceneView sceneView)
        {
            if (tmp == null || string.IsNullOrEmpty(tmp.text))
                return false;

            Vector3 screenPoint = HandleUtility.GUIPointToScreenPixelCoordinate(guiMousePos);
            Camera cam = GetTmpPickCamera(tmp, sceneView);
            int index = TMP_TextUtilities.FindIntersectingCharacter(tmp, screenPoint, cam, true);
            return index >= 0;
        }

        private static bool PassesAlphaTest(GameObject go, SceneView sceneView, Vector2 guiMousePos, bool respectAlpha)
        {
            if (IgnoreAdditiveEnabled())
            {
                Graphic g0 = go.GetComponent<Graphic>();
                Material gm = g0 != null ? g0.materialForRendering : null;
                if (IsAdditiveMaterial(gm))
                    return false;

                Renderer r0 = go.GetComponent<Renderer>();
                Material rm = r0 != null ? r0.sharedMaterial : null;
                if (IsAdditiveMaterial(rm))
                    return false;
            }

            if (!respectAlpha)
                return true;


            Graphic g = go.GetComponent<Graphic>();
            if (g != null && g.enabled && g.gameObject.activeInHierarchy)
            {

                float effectiveAlpha = ComputeEffectiveUIAlpha(g);
                if (effectiveAlpha < AlphaThreshold)
                    return false;

                Vector2 screenPoint = HandleUtility.GUIPointToScreenPixelCoordinate(guiMousePos);
                Canvas canvas = g.canvas;
                Camera eventCamera = null;
                if (canvas != null)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        eventCamera = null;
                    }
                    else
                    {
                        eventCamera = canvas.worldCamera != null ? canvas.worldCamera : (sceneView != null ? sceneView.camera : Camera.main);
                    }
                }
                else
                {
                    eventCamera = sceneView != null ? sceneView.camera : Camera.main;
                }

                // Разрешаем оба варианта: стандартный Raycast (если включен) и простой тест прямоугольника.
                bool hitRaycast = g.Raycast(screenPoint, eventCamera);
                bool hitRect = RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform, screenPoint, eventCamera);
                if (!hitRaycast && !hitRect)
                    return false;

                if (g is TMP_Text)
                    return true;

                if (g is Image image)
                    return UiImagePassesAlphaAtPoint(image, screenPoint, eventCamera, effectiveAlpha);

                if (g is RawImage rawImage)
                    return UiRawImagePassesAlphaAtPoint(rawImage, screenPoint, eventCamera, effectiveAlpha);

                return true;
            }


            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                if (!RendererPassesAlpha(renderer, sceneView, guiMousePos))
                    return false;
            }


            return true;
        }

        private static float ComputeEffectiveUIAlpha(Graphic graphic)
        {
            float alpha = graphic.color.a;
            Transform t = graphic.transform;
            while (t != null)
            {
                var group = t.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    alpha *= group.alpha;
                    if (group.ignoreParentGroups)
                        break;
                }
                t = t.parent;
            }
            return alpha;
        }

        private static bool UiImagePassesAlphaAtPoint(Image image, Vector2 screenPoint, Camera eventCamera, float effectiveAlpha)
        {
            if (image == null || image.sprite == null)
                return true;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, screenPoint, eventCamera, out Vector2 local))
                return false;

            if (TrySampleUiImageMeshAlpha(image, local, effectiveAlpha, out bool meshAlphaPassed))
                return meshAlphaPassed;

            if (image.type != Image.Type.Simple && image.type != Image.Type.Filled)
                return true;

            Sprite spr = image.sprite;
            Texture2D tex = spr.texture;
            if (tex == null)
                return true;

            Rect r = image.rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f)
                return false;

            float nx = (local.x - r.xMin) / r.width;
            float ny = (local.y - r.yMin) / r.height;
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
                return false;

            Rect tr = spr.textureRect;
            float u = (tr.x + nx * tr.width) / tex.width;
            float v = (tr.y + ny * tr.height) / tex.height;
            float a = SampleTextureAlpha(tex, u, v) * effectiveAlpha;
            return a >= AlphaThreshold;
        }

        private static bool TrySampleUiImageMeshAlpha(Image image, Vector2 local, float effectiveAlpha, out bool alphaPassed)
        {
            alphaPassed = true;

            Mesh mesh = image.canvasRenderer != null ? image.canvasRenderer.GetMesh() : null;
            if (mesh == null || mesh.vertexCount == 0)
                return false;

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;
            if (uvs == null || uvs.Length != vertices.Length || triangles == null || triangles.Length < 3)
                return false;

            Texture2D tex = image.sprite.texture;
            if (tex == null)
                return false;

            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];
                if (i0 < 0 || i0 >= vertices.Length || i1 < 0 || i1 >= vertices.Length || i2 < 0 || i2 >= vertices.Length)
                    continue;

                Vector2 a = vertices[i0];
                Vector2 b = vertices[i1];
                Vector2 c = vertices[i2];
                if (!TryGetBarycentric(local, a, b, c, out Vector3 barycentric))
                    continue;

                Vector2 uv = uvs[i0] * barycentric.x + uvs[i1] * barycentric.y + uvs[i2] * barycentric.z;
                alphaPassed = SampleTextureAlpha(tex, uv.x, uv.y) * effectiveAlpha >= AlphaThreshold;
                return true;
            }

            alphaPassed = false;
            return true;
        }

        private static bool TryGetBarycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
        {
            barycentric = Vector3.zero;

            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) <= Mathf.Epsilon)
                return false;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1f - v - w;
            const float epsilon = 0.0001f;
            if (u < -epsilon || v < -epsilon || w < -epsilon)
                return false;

            barycentric = new Vector3(u, v, w);
            return true;
        }

        private static float SampleTextureAlpha(Texture2D tex, float u, float v)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            if (tex.isReadable)
                return tex.GetPixelBilinear(u, v).a;

            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);

            try
            {
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;

                int x = Mathf.Clamp(Mathf.RoundToInt(u * (tex.width - 1)), 0, tex.width - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(v * (tex.height - 1)), 0, tex.height - 1);
                pixel.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
                pixel.Apply(false, false);
                return pixel.GetPixel(0, 0).a;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(pixel);
            }
        }

        private static bool UiRawImagePassesAlphaAtPoint(RawImage rawImage, Vector2 screenPoint, Camera eventCamera, float effectiveAlpha)
        {
            if (rawImage == null || rawImage.texture == null)
                return true;

            Texture2D tex = rawImage.texture as Texture2D;
            if (tex == null)
                return true;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, screenPoint, eventCamera, out Vector2 local))
                return false;

            Rect r = rawImage.rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f)
                return false;

            float nx = (local.x - r.xMin) / r.width;
            float ny = (local.y - r.yMin) / r.height;
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
                return false;

            Rect uvRect = rawImage.uvRect;
            float u = Mathf.Lerp(uvRect.xMin, uvRect.xMax, nx);
            float v = Mathf.Lerp(uvRect.yMin, uvRect.yMax, ny);
            float a = SampleTextureAlpha(tex, u, v) * effectiveAlpha;
            return a >= AlphaThreshold;
        }

        private static bool RendererPassesAlpha(Renderer renderer, SceneView sceneView, Vector2 guiMousePos)
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
                return true;

            if (IgnoreAdditiveEnabled() && IsAdditiveMaterial(material))
                return false;

            Mesh mesh = GetRendererMesh(renderer, out Matrix4x4 matrix);
            if (mesh == null)
            {
                return GetMaterialAlpha(material) >= AlphaThreshold;
            }

            if (TrySampleMeshUV(guiMousePos, mesh, matrix, out Vector2 uv))
            {
                float sampledAlpha = SampleMaterialAlpha(material, uv);
                return sampledAlpha >= AlphaThreshold;
            }

            return GetMaterialAlpha(material) >= AlphaThreshold;
        }

        private static Mesh GetRendererMesh(Renderer renderer, out Matrix4x4 matrix)
        {
            matrix = renderer.localToWorldMatrix;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                return filter.sharedMesh;

            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                if (_skinnedMeshBuffer == null)
                    _skinnedMeshBuffer = new Mesh();
                else
                    _skinnedMeshBuffer.Clear();

                skinned.BakeMesh(_skinnedMeshBuffer);
                matrix = Matrix4x4.identity;
                return _skinnedMeshBuffer;
            }

            return null;
        }

        private static bool TrySampleMeshUV(Vector2 guiMousePos, Mesh mesh, Matrix4x4 matrix, out Vector2 uv)
        {
            uv = Vector2.zero;
            if (mesh == null || !mesh.isReadable)
                return false;

            var vertices = mesh.vertices;
            var uvs = mesh.uv;
            var indices = mesh.triangles;

            if (vertices == null || vertices.Length == 0 || uvs == null || uvs.Length == 0 || indices == null || indices.Length == 0)
                return false;

            Ray ray = HandleUtility.GUIPointToWorldRay(guiMousePos);
            bool hitFound = false;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < indices.Length; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                Vector3 v0 = matrix.MultiplyPoint3x4(vertices[i0]);
                Vector3 v1 = matrix.MultiplyPoint3x4(vertices[i1]);
                Vector3 v2 = matrix.MultiplyPoint3x4(vertices[i2]);

                if (IntersectRayTriangle(ray, v0, v1, v2, out float distance, out Vector3 barycentric))
                {
                    if (distance < bestDistance)
                    {
                        Vector2 uv0 = uvs.Length > i0 ? uvs[i0] : Vector2.zero;
                        Vector2 uv1 = uvs.Length > i1 ? uvs[i1] : Vector2.zero;
                        Vector2 uv2 = uvs.Length > i2 ? uvs[i2] : Vector2.zero;

                        uv = barycentric.x * uv0 + barycentric.y * uv1 + barycentric.z * uv2;
                        bestDistance = distance;
                        hitFound = true;
                    }
                }
            }

            return hitFound;
        }

        private static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float distance, out Vector3 barycentric)
        {
            distance = 0f;
            barycentric = Vector3.zero;

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            Vector3 pVec = Vector3.Cross(ray.direction, edge2);
            float det = Vector3.Dot(edge1, pVec);
            if (Mathf.Abs(det) < 1e-6f)
                return false;

            float invDet = 1f / det;
            Vector3 tVec = ray.origin - v0;
            float u = Vector3.Dot(tVec, pVec) * invDet;
            if (u < 0f || u > 1f)
                return false;

            Vector3 qVec = Vector3.Cross(tVec, edge1);
            float v = Vector3.Dot(ray.direction, qVec) * invDet;
            if (v < 0f || (u + v) > 1f)
                return false;

            float t = Vector3.Dot(edge2, qVec) * invDet;
            if (t < 0f)
                return false;

            distance = t;
            barycentric = new Vector3(1f - u - v, u, v);
            return true;
        }

        private static bool IsAdditiveMaterial(Material material)
        {
            if (material == null)
                return false;

            if (material.HasProperty("_DstBlend"))
            {
                int dst = material.GetInt("_DstBlend");
                if (dst == (int)BlendMode.One || dst == (int)BlendMode.OneMinusSrcColor)
                    return true;
            }

            string n = material.shader != null ? material.shader.name : null;
            return !string.IsNullOrEmpty(n) && n.IndexOf("Additive", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float SampleMaterialAlpha(Material material, Vector2 uv)
        {
            float alpha = GetMaterialAlpha(material);
            Texture2D tex = TryGetAlphaTexture(material);
            if (tex != null && tex.isReadable)
            {
                Color px = tex.GetPixelBilinear(uv.x, uv.y);
                alpha *= px.a;
            }

            return alpha;
        }

        private static float GetMaterialAlpha(Material material)
        {
            if (material == null)
                return 1f;

            Color baseColor;
            if (material.HasProperty("_BaseColor"))
            {
                baseColor = material.GetColor("_BaseColor");
            }
            else if (material.HasProperty("_Color"))
            {
                baseColor = material.color;
            }
            else
            {
                baseColor = Color.white;
            }

            return baseColor.a;
        }

        private static Texture2D TryGetAlphaTexture(Material material)
        {
            if (material == null)
                return null;

            Texture mainTex = null;
            if (material.HasProperty("_BaseMap"))
                mainTex = material.GetTexture("_BaseMap");
            if (mainTex == null && material.HasProperty("_MainTex"))
                mainTex = material.GetTexture("_MainTex");

            return mainTex as Texture2D;
        }

        private static void UpdateHoveredObject(SceneView sceneView, Vector2 mousePos)
        {
            if (Vector2.Distance(mousePos, _lastMousePosition) < 1f)
                return;

            _lastMousePosition = mousePos;

            bool respectAlpha = RespectAlpha();
            GameObject picked = PickConsideringRenderOrder(sceneView, mousePos, respectAlpha);

            picked = ApplySelectionMode(picked);

            _hoveredObject = picked;
        }

        private static void DrawDebugOutline(GameObject go, SceneView sceneView)
        {
            if (go == null || !go.activeInHierarchy)
                return;

            go = ApplySelectionMode(go);
            if (go == null || !go.activeInHierarchy)
                return;

            bool showBounds = ShowBounds();
            bool showName = ShowName();
            if (!showBounds && !showName)
                return;

            float outlineAlpha = BoundsOutlineAlpha();
            Handles.color = new Color(0.82f, 0.82f, 0.82f, outlineAlpha);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            Renderer renderer = go.GetComponent<Renderer>();
            RectTransform rectTransform = go.transform as RectTransform;

            Rect? screenRect = null;
            Color textColor = new Color(0.82f, 0.82f, 0.82f, 1f);

            if (showBounds)
            {
                if (renderer != null)
                {
                    screenRect = DrawRendererOutline(renderer, sceneView);
                }
                else if (rectTransform != null)
                {
                    screenRect = DrawRectTransformSquare(rectTransform, sceneView);
                }
                else
                {
                    Vector3 pos = go.transform.position;
                    Handles.DrawWireCube(pos, Vector3.one * 0.5f);
                }
            }

            // В prefab-режимах рисуем рамку и текст по bounds всего префаба голубым цветом
            if (showBounds && GetSelectionMode() != SelectionMode.Object)
            {
                // Используем голубой цвет только если объект реально является частью инстанса префаба
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    Bounds? prefabBounds = CalculatePrefabBounds(go);
                    if (prefabBounds.HasValue)
                    {
                        Rect? prefabRect = DrawPrefabBoundsOutline(prefabBounds.Value, sceneView);
                        if (prefabRect.HasValue)
                        {
                            screenRect = prefabRect;
                            textColor = new Color(0.2f, 0.95f, 1f, 1f); // яркий голубой текст в цвет рамки
                        }
                    }
                }
            }

            if (showName)
            {
                // Рисуем текст справа от курсора (если координаты курсора известны)
                Vector2 cursorGuiPos = _lastMousePosition;
                bool hasCursorPos = cursorGuiPos.sqrMagnitude > 0.01f;

                if (hasCursorPos)
                {
                    Rect textRect = BuildCursorLabelRect(cursorGuiPos);
                    DrawTextInScreenSpace(go.name, textRect, textColor);
                }
                else
                {
                    // Фолбэк: если нет позиции курсора, используем центр объекта
                    Vector3 textWorldPos = go.transform.position;
                    if (renderer != null)
                    {
                        textWorldPos = renderer.bounds.center;
                    }
                    else if (rectTransform != null)
                    {
                        Vector3[] corners = new Vector3[4];
                        rectTransform.GetWorldCorners(corners);
                        textWorldPos = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
                    }

                    if (sceneView != null && sceneView.camera != null)
                    {
                        Vector2 guiPos = HandleUtility.WorldToGUIPoint(textWorldPos);
                        Rect fallbackRect = BuildCursorLabelRect(guiPos);
                        DrawTextInScreenSpace(go.name, fallbackRect, textColor);
                    }
                    else
                    {
                        // Для объектов без Renderer и RectTransform используем стандартный способ
                        if (GUI.skin == null)
                            return;

                        Handles.Label(textWorldPos, go.name, new GUIStyle(GUI.skin.label)
                        {
                            normal = { textColor = Color.yellow },
                            fontSize = 12,
                            fontStyle = FontStyle.Bold
                        });
                    }
                }
            }
        }

        #region Outline Geometry

        private static Rect? DrawRendererOutline(Renderer renderer, SceneView sceneView)
        {
            Mesh mesh = GetRendererMesh(renderer, out Matrix4x4 matrix);
            if (mesh == null)
                return DrawBoundsSquare(renderer.bounds, sceneView);

            Bounds meshBounds = mesh.bounds;
            if (meshBounds.size.sqrMagnitude <= Mathf.Epsilon)
                return DrawBoundsSquare(renderer.bounds, sceneView);

            if (TryGetFlatFrameWorldCorners(meshBounds, matrix, out Vector3[] flatFrameCorners))
            {
                DrawWireQuad(flatFrameCorners);
                return CalculateScreenRect(flatFrameCorners, sceneView);
            }

            Vector3[] worldCorners = GetWorldBoundsCorners(meshBounds, matrix);
            DrawWireBounds(worldCorners);
            return CalculateScreenRect(worldCorners, sceneView);
        }

        private static Vector3[] GetWorldBoundsCorners(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            return new Vector3[8]
            {
                matrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z)),
                matrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z)),
                matrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z)),
                matrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z)),
                matrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z)),
                matrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z)),
                matrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z)),
                matrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z))
            };
        }

        private static bool TryGetFlatFrameWorldCorners(Bounds bounds, Matrix4x4 matrix, out Vector3[] worldCorners)
        {
            worldCorners = null;

            int flatAxis = GetFlatAxisIndex(bounds.size);
            if (flatAxis < 0)
                return false;

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 center = bounds.center;
            Vector3[] localCorners;

            switch (flatAxis)
            {
                case 0:
                    localCorners = new Vector3[4]
                    {
                        new Vector3(center.x, min.y, min.z),
                        new Vector3(center.x, max.y, min.z),
                        new Vector3(center.x, max.y, max.z),
                        new Vector3(center.x, min.y, max.z)
                    };
                    break;

                case 1:
                    localCorners = new Vector3[4]
                    {
                        new Vector3(min.x, center.y, min.z),
                        new Vector3(max.x, center.y, min.z),
                        new Vector3(max.x, center.y, max.z),
                        new Vector3(min.x, center.y, max.z)
                    };
                    break;

                default:
                    localCorners = new Vector3[4]
                    {
                        new Vector3(min.x, min.y, center.z),
                        new Vector3(max.x, min.y, center.z),
                        new Vector3(max.x, max.y, center.z),
                        new Vector3(min.x, max.y, center.z)
                    };
                    break;
            }

            worldCorners = new Vector3[4];
            for (int i = 0; i < localCorners.Length; i++)
            {
                worldCorners[i] = matrix.MultiplyPoint3x4(localCorners[i]);
            }

            return true;
        }

        private static int GetFlatAxisIndex(Vector3 size)
        {
            float x = Mathf.Abs(size.x);
            float y = Mathf.Abs(size.y);
            float z = Mathf.Abs(size.z);
            float maxSize = Mathf.Max(x, Mathf.Max(y, z));
            if (maxSize <= FlatBoundsAbsoluteThreshold)
                return -1;

            float minSize = x;
            int axis = 0;

            if (y < minSize)
            {
                minSize = y;
                axis = 1;
            }

            if (z < minSize)
            {
                minSize = z;
                axis = 2;
            }

            float flatThreshold = Mathf.Max(maxSize * FlatBoundsRelativeThreshold, FlatBoundsAbsoluteThreshold);
            return minSize <= flatThreshold ? axis : -1;
        }

        private static void DrawWireBounds(Vector3[] worldCorners)
        {
            if (worldCorners == null || worldCorners.Length != 8)
                return;

            DrawLinePair(worldCorners, 0, 1);
            DrawLinePair(worldCorners, 1, 2);
            DrawLinePair(worldCorners, 2, 3);
            DrawLinePair(worldCorners, 3, 0);

            DrawLinePair(worldCorners, 4, 5);
            DrawLinePair(worldCorners, 5, 6);
            DrawLinePair(worldCorners, 6, 7);
            DrawLinePair(worldCorners, 7, 4);

            DrawLinePair(worldCorners, 0, 4);
            DrawLinePair(worldCorners, 1, 5);
            DrawLinePair(worldCorners, 2, 6);
            DrawLinePair(worldCorners, 3, 7);
        }

        private static void DrawWireQuad(Vector3[] worldCorners)
        {
            if (worldCorners == null || worldCorners.Length != 4)
                return;

            DrawLinePair(worldCorners, 0, 1);
            DrawLinePair(worldCorners, 1, 2);
            DrawLinePair(worldCorners, 2, 3);
            DrawLinePair(worldCorners, 3, 0);
        }

        private static void DrawLinePair(Vector3[] worldCorners, int startIndex, int endIndex)
        {
            Handles.DrawLine(worldCorners[startIndex], worldCorners[endIndex]);
        }

        private static Rect? CalculateScreenRect(Vector3[] worldCorners, SceneView sceneView)
        {
            if (sceneView == null || sceneView.camera == null || worldCorners == null || worldCorners.Length == 0)
                return null;

            Camera cam = sceneView.camera;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool hasVisiblePoints = false;

            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector3 worldCorner = worldCorners[i];
                Vector3 screenPos = cam.WorldToScreenPoint(worldCorner);
                if (screenPos.z < 0f)
                    continue;

                Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldCorner);
                minX = Mathf.Min(minX, guiPos.x);
                minY = Mathf.Min(minY, guiPos.y);
                maxX = Mathf.Max(maxX, guiPos.x);
                maxY = Mathf.Max(maxY, guiPos.y);
                hasVisiblePoints = true;
            }

            if (!hasVisiblePoints)
                return null;

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion

        private static Rect? DrawBoundsSquare(Bounds bounds, SceneView sceneView)
        {
            if (sceneView == null || sceneView.camera == null)
            {
                Handles.DrawWireCube(bounds.center, bounds.size);
                return null;
            }

            Camera cam = sceneView.camera;

            // Получаем все 8 углов bounds
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            Vector3[] worldCorners = new Vector3[8]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z)
            };

            // Проецируем все углы в экранные координаты
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            bool hasVisiblePoints = false;

            foreach (Vector3 worldCorner in worldCorners)
            {
                Vector3 screenPos = cam.WorldToScreenPoint(worldCorner);

                // Пропускаем точки за камерой
                if (screenPos.z < 0)
                    continue;

                // Конвертируем в GUI координаты SceneView
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldCorner);

                if (guiPos.x < minX) minX = guiPos.x;
                if (guiPos.x > maxX) maxX = guiPos.x;
                if (guiPos.y < minY) minY = guiPos.y;
                if (guiPos.y > maxY) maxY = guiPos.y;
                hasVisiblePoints = true;
            }

            if (!hasVisiblePoints)
                return null;

            // Рисуем прямоугольник в экранном пространстве без перспективных искажений
            Handles.BeginGUI();

            Rect rect = new Rect(minX, minY, maxX - minX, maxY - minY);

            float a = BoundsOutlineAlpha();
            Color outlineColor = new Color(0.8f, 0.8f, 0.8f, a);
            Color shadowColor = new Color(0f, 0f, 0f, 0.25f * a);
            const float thickness = 1f;
            const float shadowOffset = 1f;

            Rect shadowRect = new Rect(rect.xMin + shadowOffset, rect.yMin + shadowOffset, rect.width, rect.height);
            EditorGUI.DrawRect(new Rect(shadowRect.xMin, shadowRect.yMin, shadowRect.width, thickness), shadowColor); // верх
            EditorGUI.DrawRect(new Rect(shadowRect.xMin, shadowRect.yMax - thickness, shadowRect.width, thickness), shadowColor); // низ
            EditorGUI.DrawRect(new Rect(shadowRect.xMin, shadowRect.yMin, thickness, shadowRect.height), shadowColor); // лево
            EditorGUI.DrawRect(new Rect(shadowRect.xMax - thickness, shadowRect.yMin, thickness, shadowRect.height), shadowColor); // право

            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), outlineColor); // верх
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), outlineColor); // низ
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), outlineColor); // лево
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), outlineColor); // право

            Handles.EndGUI();

            return rect;
        }

        private static Rect? DrawRectTransformSquare(RectTransform rectTransform, SceneView sceneView)
        {
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);
            DrawWireQuad(worldCorners);
            return CalculateScreenRect(worldCorners, sceneView);
        }

        private static Bounds? CalculatePrefabBounds(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return null;

            bool hasBounds = false;
            Bounds bounds = new Bounds();

            // Получаем все Renderers в префабе
            Renderer[] renderers = prefabRoot.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r == null || !r.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            // Получаем все RectTransforms в префабе
            RectTransform[] rectTransforms = prefabRoot.GetComponentsInChildren<RectTransform>();
            foreach (RectTransform rt in rectTransforms)
            {
                if (rt == null || !rt.gameObject.activeInHierarchy)
                    continue;

                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);

                if (!hasBounds)
                {
                    bounds = new Bounds(corners[0], Vector3.zero);
                    for (int i = 1; i < 4; i++)
                        bounds.Encapsulate(corners[i]);
                    hasBounds = true;
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                        bounds.Encapsulate(corners[i]);
                }
            }

            // Если нет ни Renderer, ни RectTransform, используем позицию корня
            if (!hasBounds)
            {
                bounds = new Bounds(prefabRoot.transform.position, Vector3.zero);
                hasBounds = true;
            }

            return hasBounds ? bounds : (Bounds?)null;
        }

        private static Rect? DrawPrefabBoundsOutline(Bounds bounds, SceneView sceneView)
        {
            float a = BoundsOutlineAlpha();
            Color prevColor = Handles.color;
            Handles.color = new Color(0.2f, 0.85f, 1f, a);

            try
            {
                if (TryGetFlatFrameWorldCorners(bounds, Matrix4x4.identity, out Vector3[] flatFrameCorners))
                {
                    DrawWireQuad(flatFrameCorners);
                    return CalculateScreenRect(flatFrameCorners, sceneView);
                }

                Vector3[] worldCorners = GetWorldBoundsCorners(bounds, Matrix4x4.identity);
                DrawWireBounds(worldCorners);
                return CalculateScreenRect(worldCorners, sceneView);
            }
            finally
            {
                Handles.color = prevColor;
            }
        }

        private static void DrawTextInScreenSpace(string text, Rect screenRect, Color textColor)
        {
            Handles.BeginGUI();
            int prevDepth = GUI.depth;
            try
            {
                if (GUI.skin == null)
                    return;

                const int GuiTopDepth = int.MinValue + 10; // максимум приоритета над гизмо и хэндлами
                GUI.depth = GuiTopDepth;

                const float paddingX = 6f;
                const float paddingY = 4f;
                Color bgColor = new Color(0f, 0f, 0f, 0.65f);

                GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = textColor },
                    hover = { textColor = textColor },
                    active = { textColor = textColor },
                    focused = { textColor = textColor },
                    // Чуть меньше размер шрифта (было 12)
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };

                Vector2 textSize = labelStyle.CalcSize(new GUIContent(text));

                Rect bgRect = new Rect(
                    screenRect.xMin,
                    screenRect.yMin,
                    textSize.x + paddingX * 2f,
                    textSize.y + paddingY * 2f
                );

                Rect labelRect = new Rect(
                    bgRect.xMin + paddingX,
                    bgRect.yMin + paddingY,
                    textSize.x,
                    textSize.y
                );

                EditorGUI.DrawRect(bgRect, bgColor);
                GUI.Label(labelRect, text, labelStyle);

            }
            finally
            {
                GUI.depth = prevDepth;
                Handles.EndGUI();
            }
        }

        private static Rect BuildCursorLabelRect(Vector2 cursorGuiPos)
        {
            const float width = 200f;
            const float height = 20f;
            const float offsetX = 24f;  // чуть правее курсора
            const float offsetY = 16f;
            return new Rect(cursorGuiPos.x + offsetX, cursorGuiPos.y + offsetY, width, height);
        }

    }

    [Overlay(typeof(SceneView), "\u200B", true)]
    public class VisibleSelectionOverlay : IMGUIOverlay, ITransientOverlay
    {
        private const float Spacing = 1f;
        private const float PanelWidth = 120f;
        private const float SliderWidth = 120f;
        private static GUIStyle _labelStyle;
        private static GUIStyle _toggleClippedStyle;
        private static readonly string[] SelectionModeOptions = { "Object", "Nearest", "Top" };

        private static bool ToggleLeftClipped(string label, bool value, float height)
        {
            if (_toggleClippedStyle == null)
            {
                _toggleClippedStyle = new GUIStyle(GUI.skin.toggle)
                {
                    wordWrap = false,
                    clipping = TextClipping.Clip
                };
            }

            return GUILayout.Toggle(value, label, _toggleClippedStyle, GUILayout.Width(PanelWidth), GUILayout.MaxWidth(PanelWidth), GUILayout.Height(height));
        }

        bool ITransientOverlay.visible => VisibleSelection.OverlayEnabled;

        public override void OnGUI()
        {
            if (!VisibleSelection.OverlayEnabled)
                return;

            if (GUI.skin == null)
                return;

            bool expanded = VisibleSelection.OverlayExpanded;

            // Let the overlay auto-size vertically to avoid empty space at the bottom.
            GUILayout.BeginVertical(GUILayout.Width(PanelWidth), GUILayout.MaxWidth(PanelWidth));

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
            }
            GUILayout.Label("Select Visible", _labelStyle, GUILayout.Width(PanelWidth), GUILayout.MaxWidth(PanelWidth), GUILayout.Height(11f));
            GUILayout.Space(Spacing);

            bool enabled = VisibleSelection.IsEnabled();
            bool newEnabled = EditorGUILayout.ToggleLeft("Enable", enabled, GUILayout.Width(PanelWidth), GUILayout.MaxWidth(PanelWidth), GUILayout.Height(16f));
            if (newEnabled != enabled)
            {
                EditorPrefs.SetBool(VisibleSelection.PrefKeyEnable, newEnabled);
                VisibleSelection.SyncMenuChecks();
                SceneView.RepaintAll();
            }

            GUILayout.Space(Spacing);
            GUILayout.Box(GUIContent.none, GUILayout.Width(PanelWidth), GUILayout.MaxWidth(PanelWidth), GUILayout.Height(1f));
            GUILayout.Space(Spacing * 0.5f);

            Rect foldoutRect = GUILayoutUtility.GetRect(PanelWidth, 16f, GUILayout.Width(PanelWidth), GUILayout.MaxWidth(PanelWidth));
            bool newExpanded = EditorGUI.Foldout(foldoutRect, expanded, expanded ? "Hide settings" : "Show settings", true);
            if (newExpanded != expanded)
            {
                VisibleSelection.OverlayExpanded = newExpanded;
            }

            if (VisibleSelection.OverlayExpanded)
            {
                GUILayout.Space(Spacing);

                float outlineAlpha = VisibleSelection.BoundsOutlineAlpha();
                EditorGUI.BeginChangeCheck();
                // Slider without the numeric value field (more compact).
                GUILayout.BeginHorizontal(GUILayout.Width(PanelWidth), GUILayout.MaxWidth(PanelWidth));
                GUILayout.FlexibleSpace();
                Rect sliderRect = GUILayoutUtility.GetRect(SliderWidth, 14f, GUILayout.Width(SliderWidth));
                float newOutlineAlpha = GUI.HorizontalSlider(sliderRect, outlineAlpha, 0f, 1f);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    bool shouldShowBounds = newOutlineAlpha > 0.0001f;
                    bool wasShowingBounds = VisibleSelection.ShowBounds();

                    VisibleSelection.SetBoundsOutlineAlpha(newOutlineAlpha);
                    if (shouldShowBounds != wasShowingBounds)
                    {
                        EditorPrefs.SetBool(VisibleSelection.PrefKeyShowBounds, shouldShowBounds);
                        VisibleSelection.SyncMenuChecks();
                    }
                    SceneView.RepaintAll();
                }

                GUILayout.Space(Spacing);

                bool respectAlpha = VisibleSelection.RespectAlpha();
                bool newRespectAlpha = ToggleLeftClipped("Respect Alpha", respectAlpha, 14f);
                if (newRespectAlpha != respectAlpha)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeyRespectAlpha, newRespectAlpha);
                    VisibleSelection.SyncMenuChecks();
                }

                GUILayout.Space(Spacing);

                bool ignoreAdditive = VisibleSelection.IgnoreAdditiveEnabled();
                bool newIgnoreAdditive = ToggleLeftClipped("Ignore Additive", ignoreAdditive, 14f);
                if (newIgnoreAdditive != ignoreAdditive)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeyIgnoreAdditive, newIgnoreAdditive);
                    VisibleSelection.SyncMenuChecks();
                }

                GUILayout.Space(Spacing);

                var selectionMode = VisibleSelection.GetSelectionMode();
                int selectionModeIndex = Mathf.Clamp((int)selectionMode, 0, 2);
                int newSelectionModeIndex = EditorGUILayout.Popup(
                    selectionModeIndex,
                    SelectionModeOptions,
                    GUILayout.Width(PanelWidth),
                    GUILayout.MaxWidth(PanelWidth),
                    GUILayout.Height(16f)
                );
                var newSelectionMode = (VisibleSelection.SelectionMode)newSelectionModeIndex;
                if (newSelectionMode != selectionMode)
                {
                    VisibleSelection.SetSelectionMode(newSelectionMode);
                }

                GUILayout.Space(Spacing);

                bool showName = VisibleSelection.ShowName();
                bool newShowName = ToggleLeftClipped("Show Name", showName, 14f);
                if (newShowName != showName)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeyShowName, newShowName);
                    VisibleSelection.SyncMenuChecks();
                    SceneView.RepaintAll();
                }

                GUILayout.Space(Spacing);
            }

            GUILayout.EndVertical();
        }
    }
}

