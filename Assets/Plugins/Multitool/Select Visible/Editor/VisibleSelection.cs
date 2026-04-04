using System.Collections.Generic;
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
        private const string MenuPathRespectRenderQueue = "Tools/Multitool/Select Visible/Respect Render Queue";
        private const string MenuPathSelectPrefabRoot = "Tools/Multitool/Select Visible/Select Nearest Prefab";

        public const string PrefKeyEnable = "Multitool.SelectVisible.Enabled";
        public const string PrefKeyRespectAlpha = "Multitool.SelectVisible.RespectAlpha";
        public const string PrefKeyRespectRenderQueue = "Multitool.SelectVisible.RespectRenderQueue";
        public const string PrefKeySelectPrefabRoot = "Multitool.SelectVisible.SelectPrefabRoot";
        // Legacy ключ, используется только для миграции в новые настройки
        public const string PrefKeyShowDebugOutline = "Multitool.SelectVisible.ShowDebugOutline";
        public const string PrefKeyShowBounds = "Multitool.SelectVisible.ShowBounds";
        public const string PrefKeyShowName = "Multitool.SelectVisible.ShowName";
        public const string PrefKeyOverlayClipBypass = "Multitool.SelectVisible.OverlayClipBypass";
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

        private struct PickCandidate
        {
            public GameObject GameObject;
            public RenderOrderInfo OrderInfo;
            public bool AlphaPassed;
            public int PickOrder;
        }

        private struct RenderOrderInfo
        {
            public int RenderQueue;
            public int SortingLayerValue;
            public int SortingOrder;
            public int SortingGroupLayerValue;
            public int SortingGroupOrder;
            public float DepthAlongView;
            public int SiblingIndex;
            public bool IsUIElement;
            public int UiRootCanvasInstanceId;
            public int[] UiHierarchyPath;
        }

        static VisibleSelection()
        {
            MigrateLegacyShowDebugOutlinePref();
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

        [MenuItem(MenuPathRespectRenderQueue)]
        private static void ToggleRespectRenderQueue()
        {
            bool newValue = !RespectRenderQueue();
            EditorPrefs.SetBool(PrefKeyRespectRenderQueue, newValue);
            SyncMenuChecks();
            SceneView.RepaintAll();
        }

        [MenuItem(MenuPathRespectRenderQueue, true)]
        private static bool ToggleRespectRenderQueueValidate()
        {
            Menu.SetChecked(MenuPathRespectRenderQueue, RespectRenderQueue());
            return true;
        }

        [MenuItem(MenuPathSelectPrefabRoot)]
        private static void ToggleSelectPrefabRoot()
        {
            bool newValue = !SelectPrefabRoot();
            EditorPrefs.SetBool(PrefKeySelectPrefabRoot, newValue);
            SyncMenuChecks();
        }

        [MenuItem(MenuPathSelectPrefabRoot, true)]
        private static bool ToggleSelectPrefabRootValidate()
        {
            Menu.SetChecked(MenuPathSelectPrefabRoot, SelectPrefabRoot());
            return true;
        }

        [MenuItem("Tools/Multitool/Select Visible/Show Bounds")]
        private static void ToggleShowBoundsMenu()
        {
            bool newValue = !ShowBounds();
            EditorPrefs.SetBool(PrefKeyShowBounds, newValue);
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
            Menu.SetChecked(MenuPathRespectRenderQueue, RespectRenderQueue());
            Menu.SetChecked(MenuPathSelectPrefabRoot, SelectPrefabRoot());
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

        public static bool RespectRenderQueue()
        {
            return EditorPrefs.GetBool(PrefKeyRespectRenderQueue, true);
        }

        public static bool SelectPrefabRoot()
        {
            return EditorPrefs.GetBool(PrefKeySelectPrefabRoot, false);
        }

        public static bool ShowBounds()
        {
            return EditorPrefs.GetBool(PrefKeyShowBounds, false);
        }

        public static bool ShowName()
        {
            return EditorPrefs.GetBool(PrefKeyShowName, false);
        }

        public static bool OverlayClipBypassEnabled()
        {
            return EditorPrefs.GetBool(PrefKeyOverlayClipBypass, true);
        }

        public static float BoundsOutlineAlpha()
        {
            return EditorPrefs.GetFloat(PrefKeyBoundsOutlineAlpha, DefaultBoundsOutlineAlpha);
        }

        public static void SetBoundsOutlineAlpha(float alpha)
        {
            EditorPrefs.SetFloat(PrefKeyBoundsOutlineAlpha, Mathf.Clamp01(alpha));
        }

        // Для обратной совместимости возвращает true, если включены границы или имя
        public static bool ShowDebugOutline()
        {
            return ShowBounds() || ShowName();
        }

        private static bool ShouldDrawDebugOverlay()
        {
            return ShowBounds() || ShowName();
        }

        private static void MigrateLegacyShowDebugOutlinePref()
        {
            // Если пользователь ранее включал старую галку, переносим значение в новые настройки, если они ещё не заданы.
            if (!EditorPrefs.HasKey(PrefKeyShowDebugOutline))
                return;

            bool legacy = EditorPrefs.GetBool(PrefKeyShowDebugOutline, false);

            if (!EditorPrefs.HasKey(PrefKeyShowBounds))
                EditorPrefs.SetBool(PrefKeyShowBounds, legacy);

            if (!EditorPrefs.HasKey(PrefKeyShowName))
                EditorPrefs.SetBool(PrefKeyShowName, legacy);
        }

        private static GameObject GetNearestPrefabRoot(GameObject go)
        {
            if (go == null)
                return null;

            GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            return nearestRoot != null ? nearestRoot : go;
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
            bool respectRenderQueue = RespectRenderQueue();
            GameObject picked = respectRenderQueue
                ? PickConsideringRenderOrder(sceneView, mousePos, respectAlpha)
                : PickSimple(sceneView, mousePos, respectAlpha);

            if (picked != null && SelectPrefabRoot())
            {
                picked = GetNearestPrefabRoot(picked);
            }

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

        private static GameObject PickSimple(SceneView sceneView, Vector2 mousePos, bool respectAlpha)
        {
            List<GameObject> ignoreList = respectAlpha ? new List<GameObject>(8) : null;
            GameObject picked = null;

            for (int safe = 0; safe < 64; safe++)
            {
                picked = ignoreList == null
                    ? HandleUtility.PickGameObject(mousePos, false)
                    : HandleUtility.PickGameObject(mousePos, false, ignoreList.ToArray());

                if (picked == null)
                    break;

                if (!IsPickable(picked, mousePos))
                {
                    ignoreList?.Add(picked);
                    continue;
                }

                if (!respectAlpha || PassesAlphaTest(picked, sceneView, mousePos, respectAlpha))
                    break;

                ignoreList?.Add(picked);
            }

            if (respectAlpha && picked != null && !PassesAlphaTest(picked, sceneView, mousePos, respectAlpha))
                picked = null;

            return picked;
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

                if (!IsPickable(picked, mousePos))
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

            cmp = a.OrderInfo.DepthAlongView.CompareTo(b.OrderInfo.DepthAlongView);
            if (cmp != 0)
                return cmp;

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
                RenderQueue = 2000,
                SortingLayerValue = 0,
                SortingOrder = 0,
                SortingGroupLayerValue = 0,
                SortingGroupOrder = 0,
                DepthAlongView = ComputeDepthAlongView(go, sceneView, guiMousePos),
                SiblingIndex = 0,
                IsUIElement = false,
                UiRootCanvasInstanceId = 0,
                UiHierarchyPath = null
            };

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                info.RenderQueue = CalculateRendererQueue(renderer);
                info.SortingLayerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
                info.SortingOrder = renderer.sortingOrder;

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

                Material material = graphic.materialForRendering;
                if (material != null)
                {
                    info.RenderQueue = material.renderQueue;
                }

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

        private static int CalculateRendererQueue(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return renderer.sharedMaterial != null ? renderer.sharedMaterial.renderQueue : 2000;
            }

            int queue = int.MinValue;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                queue = Mathf.Max(queue, material.renderQueue);
            }

            return queue == int.MinValue ? 2000 : queue;
        }

        private static float ComputeDepthAlongView(GameObject go, SceneView sceneView, Vector2 guiMousePos)
        {
            if (sceneView == null || sceneView.camera == null)
                return 0f;

            Ray viewRay = HandleUtility.GUIPointToWorldRay(guiMousePos);
            Vector3 referenceOrigin = viewRay.origin;
            Vector3 referenceDir = viewRay.direction.normalized;

            Vector3 samplePoint = go.transform.position;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Vector3 farPoint = referenceOrigin + referenceDir * 10000f;
                samplePoint = renderer.bounds.ClosestPoint(farPoint);
            }
            else
            {
                RectTransform rectTransform = go.transform as RectTransform;
                if (rectTransform != null)
                {
                    samplePoint = rectTransform.TransformPoint(rectTransform.rect.center);
                }
            }

            return Vector3.Dot(samplePoint - referenceOrigin, referenceDir);
        }


        private const float IconClickRadius = 17f;

        private static bool IsPickable(GameObject go)
        {
            return IsPickable(go, null);
        }

        private static bool IsClickNearIcon(GameObject go, Vector2 mousePos)
        {

            Vector3 worldPos = go.transform.position;
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            float distance = Vector2.Distance(mousePos, screenPos);
            return distance <= IconClickRadius;
        }

        private static bool IsPickable(GameObject go, Vector2? mousePos)
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

            return true;
        }

        private static bool PassesAlphaTest(GameObject go, SceneView sceneView, Vector2 guiMousePos, bool respectAlpha)
        {
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
                return hitRaycast || hitRect;
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

        private static bool RendererPassesAlpha(Renderer renderer, SceneView sceneView, Vector2 guiMousePos)
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
                return true;

            bool overlayClipEnabled = OverlayClipBypassEnabled();
            Texture2D overlayTex = null;
            bool hasOverlayClip = overlayClipEnabled && HasOverlayClipMaterial(material, out overlayTex);
            if (hasOverlayClip && (overlayTex == null || !overlayTex.isReadable))
            {
                // Если не можем прочитать паттерн клипа — считаем прозрачным, чтобы не блокировать клик
                return false;
            }

            Mesh mesh = GetRendererMesh(renderer, out Matrix4x4 matrix);
            if (mesh == null)
            {
                // fallback для объектов без читаемого меша
                if (hasOverlayClip && TryEvaluateOverlayClip(material, overlayTex, sceneView, renderer.bounds.center, Color.white))
                    return false;

                return GetMaterialAlpha(material) >= AlphaThreshold;
            }

            if (TrySampleMeshUV(guiMousePos, mesh, matrix, out Vector2 uv, out Vector3 hitWorldPos, out Color hitColor))
            {
                // Спецкейс: шейдеры, которые клипуют по экранным UV (как ftue_highlight),
                // но стоят в Opaque queue. Пытаемся вычислить реальный clip по OverlayTexture.
                if (hasOverlayClip && TryEvaluateOverlayClip(material, overlayTex, sceneView, hitWorldPos, hitColor))
                    return false;

                float sampledAlpha = SampleMaterialAlpha(material, uv);
                return sampledAlpha >= AlphaThreshold;
            }

            if (hasOverlayClip && TryEvaluateOverlayClip(material, overlayTex, sceneView, renderer.bounds.center, Color.white))
                return false;

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

        private static bool TrySampleMeshUV(Vector2 guiMousePos, Mesh mesh, Matrix4x4 matrix, out Vector2 uv, out Vector3 worldPos, out Color hitColor)
        {
            uv = Vector2.zero;
            worldPos = Vector3.zero;
            hitColor = Color.white;
            if (mesh == null || !mesh.isReadable)
                return false;

            var vertices = mesh.vertices;
            var uvs = mesh.uv;
            var colors = mesh.colors;
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
                        worldPos = barycentric.x * v0 + barycentric.y * v1 + barycentric.z * v2;
                        if (colors != null && colors.Length > 0)
                        {
                            Color c0 = colors.Length > i0 ? colors[i0] : Color.white;
                            Color c1 = colors.Length > i1 ? colors[i1] : Color.white;
                            Color c2 = colors.Length > i2 ? colors[i2] : Color.white;
                            hitColor = barycentric.x * c0 + barycentric.y * c1 + barycentric.z * c2;
                        }

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

        private static bool IsTransparentMaterial(Material material)
        {
            if (material == null)
                return false;

            if (material.renderQueue >= (int)RenderQueue.Transparent)
                return true;

            string renderType = material.GetTag("RenderType", false);
            return renderType == "Transparent" || renderType == "Fade";
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

        private static bool TryEvaluateOverlayClip(Material material, Texture2D overlayTex, SceneView sceneView, Vector3 hitWorldPos, Color hitColor)
        {
            // Поддержка шейдеров с клипом по экранным UV (например, shader_ftue_highlight),
            // у которых RenderQueue/RenderType не говорят о прозрачности.
            if (material == null || sceneView == null || sceneView.camera == null)
                return false;

            if (overlayTex == null || !overlayTex.isReadable)
                return false;

            Camera cam = sceneView.camera;
            Vector3 viewport = cam.WorldToViewportPoint(hitWorldPos);
            if (viewport.z <= 0f)
                return false;

            float aspect = (float)cam.pixelWidth / cam.pixelHeight;
            Vector2 screenUV = new Vector2(viewport.x * aspect, viewport.y);

            float scale = material.GetFloat("_OverlayScale");
            Vector2 tiledUV = screenUV * scale;

            float alphaPat = overlayTex.GetPixelBilinear(Mathf.Repeat(tiledUV.x, 1f), Mathf.Repeat(tiledUV.y, 1f)).r;
            float lowCut = material.GetFloat("_LowCut");
            float highCut = material.GetFloat("_HighCut");
            float opac = Mathf.InverseLerp(lowCut, highCut, hitColor.r);
            alphaPat = alphaPat >= opac ? 1f : 0f;

            // В шейдере происходит clip, когда alphaPat > 0.5
            return (1f - alphaPat) < 0.5f;
        }

        private static bool HasOverlayClipMaterial(Material material, out Texture2D overlayTex)
        {
            overlayTex = null;
            if (material == null)
                return false;

            if (!material.HasProperty("_OverlayTexture") ||
                !material.HasProperty("_OverlayScale") ||
                !material.HasProperty("_LowCut") ||
                !material.HasProperty("_HighCut"))
            {
                return false;
            }

            overlayTex = material.GetTexture("_OverlayTexture") as Texture2D;
            return true;
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
            bool respectRenderQueue = RespectRenderQueue();
            GameObject picked = respectRenderQueue
                ? PickConsideringRenderOrder(sceneView, mousePos, respectAlpha)
                : PickSimple(sceneView, mousePos, respectAlpha);

            if (picked != null && SelectPrefabRoot())
            {
                picked = GetNearestPrefabRoot(picked);
            }

            _hoveredObject = picked;
        }

        private static void DrawDebugOutline(GameObject go, SceneView sceneView)
        {
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

            // Если включён SelectPrefabRoot — рисуем рамку и текст по bounds всего префаба голубым цветом
            if (showBounds && SelectPrefabRoot())
            {
                GameObject prefabRoot = GetNearestPrefabRoot(go);
                // Используем голубой цвет только если объект реально является частью инстанса префаба
                if (prefabRoot != null && PrefabUtility.IsPartOfPrefabInstance(prefabRoot))
                {
                    Bounds? prefabBounds = CalculatePrefabBounds(prefabRoot);
                    if (prefabBounds.HasValue)
                    {
                        Rect? prefabRect = DrawPrefabBoundsSquare(prefabBounds.Value, sceneView);
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
                        Handles.Label(textWorldPos, go.name, new GUIStyle(EditorStyles.label)
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

        private static Rect? DrawPrefabBoundsSquare(Bounds bounds, SceneView sceneView)
        {
            if (sceneView == null || sceneView.camera == null)
                return null;

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
            Color outlineColor = new Color(0.2f, 0.85f, 1f, a);
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

        private static void DrawTextInScreenSpace(string text, Rect screenRect, Color textColor)
        {
            Handles.BeginGUI();
            const int GuiTopDepth = int.MinValue + 10; // максимум приоритета над гизмо и хэндлами
            int prevDepth = GUI.depth;
            GUI.depth = GuiTopDepth;

            const float paddingX = 6f;
            const float paddingY = 4f;
            Color bgColor = new Color(0f, 0f, 0f, 0.65f);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
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

            GUI.depth = prevDepth;
            Handles.EndGUI();
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

    [Overlay(typeof(SceneView), "", true)]
    public class VisibleSelectionOverlay : IMGUIOverlay, ITransientOverlay
    {
        private const float Padding = 1f;
        private const float Spacing = 1f;
        private const float PanelWidth = 150f;
        private const float PanelHeightExpanded = 200f;
        private const float PanelHeightCollapsed = 60f;
        private static GUIStyle _labelStyle;

        bool ITransientOverlay.visible => VisibleSelection.OverlayEnabled;

        public override void OnGUI()
        {
            if (!VisibleSelection.OverlayEnabled)
                return;

            bool expanded = VisibleSelection.OverlayExpanded;
            float panelHeight = expanded ? PanelHeightExpanded : PanelHeightCollapsed;

            Rect panelRect = GUILayoutUtility.GetRect(PanelWidth, panelHeight, GUILayout.Width(PanelWidth), GUILayout.Height(panelHeight));
            EditorGUI.DrawRect(panelRect, new Color(0f, 0f, 0f, 0.85f));

            GUILayout.BeginArea(new Rect(panelRect.x + Padding, panelRect.y + Padding, panelRect.width - Padding * 2, panelRect.height - Padding * 2));

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
            }
            GUILayout.Label("Select Visible", _labelStyle, GUILayout.Height(11f));
            GUILayout.Space(Spacing);

            bool enabled = VisibleSelection.IsEnabled();
            bool newEnabled = EditorGUILayout.ToggleLeft("Enable", enabled, GUILayout.Height(16f));
            if (newEnabled != enabled)
            {
                EditorPrefs.SetBool(VisibleSelection.PrefKeyEnable, newEnabled);
                VisibleSelection.SyncMenuChecks();
                SceneView.RepaintAll();
            }

            GUILayout.Space(Spacing);
            GUILayout.Box(GUIContent.none, GUILayout.Height(1f), GUILayout.ExpandWidth(true));
            GUILayout.Space(Spacing * 0.5f);

            bool newExpanded = EditorGUILayout.Foldout(expanded, expanded ? "Hide settings" : "Show settings", true);
            if (newExpanded != expanded)
            {
                VisibleSelection.OverlayExpanded = newExpanded;
            }

            if (VisibleSelection.OverlayExpanded)
            {
                GUILayout.Space(Spacing);

                bool respectAlpha = VisibleSelection.RespectAlpha();
                bool newRespectAlpha = EditorGUILayout.ToggleLeft("Respect Alpha", respectAlpha, GUILayout.Height(14f));
                if (newRespectAlpha != respectAlpha)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeyRespectAlpha, newRespectAlpha);
                    VisibleSelection.SyncMenuChecks();
                }

                GUILayout.Space(Spacing);

                bool selectPrefabRoot = VisibleSelection.SelectPrefabRoot();
                bool newSelectPrefabRoot = EditorGUILayout.ToggleLeft("Select Nearest Prefab", selectPrefabRoot, GUILayout.Height(14f));
                if (newSelectPrefabRoot != selectPrefabRoot)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeySelectPrefabRoot, newSelectPrefabRoot);
                    VisibleSelection.SyncMenuChecks();
                }

                GUILayout.Space(Spacing);

                bool showBounds = VisibleSelection.ShowBounds();
                bool newShowBounds = EditorGUILayout.ToggleLeft("Show Bounds", showBounds, GUILayout.Height(14f));
                if (newShowBounds != showBounds)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeyShowBounds, newShowBounds);
                    VisibleSelection.SyncMenuChecks();
                    SceneView.RepaintAll();
                }

                float outlineAlpha = VisibleSelection.BoundsOutlineAlpha();
                EditorGUI.BeginChangeCheck();
                float newOutlineAlpha = EditorGUILayout.Slider(outlineAlpha, 0f, 1f, GUILayout.Height(14f));
                if (EditorGUI.EndChangeCheck())
                {
                    VisibleSelection.SetBoundsOutlineAlpha(newOutlineAlpha);
                    SceneView.RepaintAll();
                }

                GUILayout.Space(Spacing);

                bool showName = VisibleSelection.ShowName();
                bool newShowName = EditorGUILayout.ToggleLeft("Show Name", showName, GUILayout.Height(14f));
                if (newShowName != showName)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeyShowName, newShowName);
                    VisibleSelection.SyncMenuChecks();
                    SceneView.RepaintAll();
                }

                GUILayout.Space(Spacing);

                bool overlayClipBypass = VisibleSelection.OverlayClipBypassEnabled();
                bool newOverlayClipBypass = EditorGUILayout.ToggleLeft("Bypass Overlay Clip", overlayClipBypass, GUILayout.Height(14f));
                if (newOverlayClipBypass != overlayClipBypass)
                {
                    EditorPrefs.SetBool(VisibleSelection.PrefKeyOverlayClipBypass, newOverlayClipBypass);
                }
            }

            GUILayout.EndArea();
        }
    }
}

