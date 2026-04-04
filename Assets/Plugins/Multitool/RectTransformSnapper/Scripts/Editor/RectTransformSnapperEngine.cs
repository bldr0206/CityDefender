using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Multitool.RectTransformSnapper
{
    [InitializeOnLoad]
    public static class RectTransformSnapperEngine
    {
        private const string PREF_CANVAS_ID = "RTS_AssignedCanvasID";
        private const string PREF_CANVAS_GLOBAL_ID = "RTS_AssignedCanvasGlobalId";
        private static Canvas _assignedCanvas;

        private static Transform _activeReferenceImage;
        public static Transform ActiveReferenceImage
        {
            get => _activeReferenceImage;
            set => _activeReferenceImage = value;
        }

        public static Canvas AssignedCanvas
        {
            get
            {
                if (_assignedCanvas == null)
                {

                    string gid = EditorPrefs.GetString(PREF_CANVAS_GLOBAL_ID, string.Empty);
                    if (!string.IsNullOrEmpty(gid))
                    {
                        if (GlobalObjectId.TryParse(gid, out var goid))
                        {
                            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(goid) as Canvas;
                            if (obj != null) _assignedCanvas = obj; else EditorPrefs.DeleteKey(PREF_CANVAS_GLOBAL_ID);
                        }
                        else
                        {
                            EditorPrefs.DeleteKey(PREF_CANVAS_GLOBAL_ID);
                        }
                    }


                    if (_assignedCanvas == null)
                    {
                        int id = EditorPrefs.GetInt(PREF_CANVAS_ID, 0);
                        if (id != 0)
                        {
                            var obj = EditorUtility.InstanceIDToObject(id) as Canvas;
                            if (obj != null) _assignedCanvas = obj; else EditorPrefs.DeleteKey(PREF_CANVAS_ID);
                        }
                    }
                }
                return _assignedCanvas;
            }
            set
            {
                _assignedCanvas = value;
                if (value != null)
                {
                    EditorPrefs.SetInt(PREF_CANVAS_ID, value.GetInstanceID());
                    string gid = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
                    EditorPrefs.SetString(PREF_CANVAS_GLOBAL_ID, gid);
                }
                else
                {
                    EditorPrefs.DeleteKey(PREF_CANVAS_ID);
                    EditorPrefs.DeleteKey(PREF_CANVAS_GLOBAL_ID);
                }
                SceneView.RepaintAll();
            }
        }


        private const string PREF_CANVAS_ORIGIN = "RTS_CanvasOriginIndex";
        public static int CanvasOriginIndex
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(PREF_CANVAS_ORIGIN, 4), 0, 8);
            set { EditorPrefs.SetInt(PREF_CANVAS_ORIGIN, Mathf.Clamp(value, 0, 8)); SceneView.RepaintAll(); }
        }

        public static float SnapStep
        {
            get => EditorPrefs.GetFloat("RTS_SnapStep", 64f);
            set
            {
                EditorPrefs.SetFloat("RTS_SnapStep", Mathf.Max(1f, value));
                SceneView.RepaintAll();
            }
        }
        public static int SnapDivisor
        {
            get => Mathf.Max(1, EditorPrefs.GetInt("RTS_SnapDivisor", 1));
            set { EditorPrefs.SetInt("RTS_SnapDivisor", Mathf.Max(1, value)); SceneView.RepaintAll(); }
        }
        public static float SnapOffsetPercentX
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat("RTS_SnapOffsetPercentX", 0f), -1f, 1f);
            set
            {
                EditorPrefs.SetFloat("RTS_SnapOffsetPercentX", Mathf.Clamp(value, -1f, 1f));
                SceneView.RepaintAll();
            }
        }
        public static float SnapOffsetPercentY
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat("RTS_SnapOffsetPercentY", 0f), -1f, 1f);
            set
            {
                EditorPrefs.SetFloat("RTS_SnapOffsetPercentY", Mathf.Clamp(value, -1f, 1f));
                SceneView.RepaintAll();
            }
        }
        public static int MaxDots
        {
            get => Mathf.Clamp(EditorPrefs.GetInt("RTS_MaxDots", 8192 * 2), 1000, 200000); set => EditorPrefs.SetInt("RTS_MaxDots", Mathf.Clamp(value, 1000, 200000));
        }
        public static bool Enabled { get => EditorPrefs.GetBool("RTS_Enabled", false); set => EditorPrefs.SetBool("RTS_Enabled", value); }
        public static bool ShowGrid { get => EditorPrefs.GetBool("RTS_ShowGrid", true); set => EditorPrefs.SetBool("RTS_ShowGrid", value); }
        public static int GridCount { get => EditorPrefs.GetInt("RTS_GridCount", 20); set => EditorPrefs.SetInt("RTS_GridCount", Mathf.Clamp(value, 6, 64)); }
        public static float DotSize
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat("RTS_DotSize", 1f), 1f, 4f);
            set => EditorPrefs.SetFloat("RTS_DotSize", Mathf.Clamp(value, 1f, 4f));
        }

        public static float DotOpacity
        {
            get => 1f;
            set { }
        }
        public static Color DotColor
        {
            get => new Color(
                EditorPrefs.GetFloat("RTS_DotColorR", 0.6784314f),
                EditorPrefs.GetFloat("RTS_DotColorG", 0.6784314f),
                EditorPrefs.GetFloat("RTS_DotColorB", 0.6784314f),
                EditorPrefs.GetFloat("RTS_DotColorA", 1f));
            set
            {
                EditorPrefs.SetFloat("RTS_DotColorR", value.r);
                EditorPrefs.SetFloat("RTS_DotColorG", value.g);
                EditorPrefs.SetFloat("RTS_DotColorB", value.b);
                EditorPrefs.SetFloat("RTS_DotColorA", value.a);
            }
        }

        public static Color ReferenceColor
        {
            get => new Color(
                EditorPrefs.GetFloat("RTS_RefColorR", 1f),
                EditorPrefs.GetFloat("RTS_RefColorG", 1f),
                EditorPrefs.GetFloat("RTS_RefColorB", 1f),
                1f);
            set
            {
                EditorPrefs.SetFloat("RTS_RefColorR", value.r);
                EditorPrefs.SetFloat("RTS_RefColorG", value.g);
                EditorPrefs.SetFloat("RTS_RefColorB", value.b);
                SceneView.RepaintAll();
            }
        }

        public static float ReferenceAlpha
        {
            get => Mathf.Clamp01(EditorPrefs.GetFloat("RTS_RefAlpha", 0.5f));
            set
            {
                EditorPrefs.SetFloat("RTS_RefAlpha", Mathf.Clamp01(value));
                SceneView.RepaintAll();
            }
        }

        public static bool ReferenceAlwaysOnTop
        {
            get => EditorPrefs.GetBool("RTS_RefAlwaysOnTop", true);
            set
            {
                EditorPrefs.SetBool("RTS_RefAlwaysOnTop", value);
                SceneView.RepaintAll();
            }
        }

        public static bool HasSavedDefaults
        {
            get => RectTransformSnapperDefaultsAsset.Load() != null;
        }
        public static void SaveDefaultsFromCurrent()
        {
            var asset = RectTransformSnapperDefaultsAsset.Load();
            if (asset == null)
            {
                var dir = System.IO.Path.GetDirectoryName(RectTransformSnapperDefaultsAsset.AssetPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                asset = ScriptableObject.CreateInstance<RectTransformSnapperDefaultsAsset>();
                AssetDatabase.CreateAsset(asset, RectTransformSnapperDefaultsAsset.AssetPath);
            }
            asset.enabled = Enabled;
            asset.proportionalChildrenEnabled = ProportionalChildrenEnabled;
            asset.snapToRectEdges = SnapToRectEdges;
            asset.canvasOriginIndex = CanvasOriginIndex;
            asset.alignToCanvas = AlignToCanvas;
            asset.snapStep = SnapStep;
            asset.snapDivisor = SnapDivisor;
            asset.snapOffsetPercentX = SnapOffsetPercentX;
            asset.snapOffsetPercentY = SnapOffsetPercentY;
            asset.showGrid = ShowGrid;
            asset.dotSize = DotSize;
            asset.dotColor = DotColor;
            asset.referenceColor = ReferenceColor;
            asset.referenceAlpha = ReferenceAlpha;
            asset.referenceAlwaysOnTop = ReferenceAlwaysOnTop;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
        public static void ClearSavedDefaults()
        {
            var asset = RectTransformSnapperDefaultsAsset.Load();
            if (asset != null)
            {
                AssetDatabase.DeleteAsset(RectTransformSnapperDefaultsAsset.AssetPath);
                AssetDatabase.SaveAssets();
            }
        }
        public static bool ApplySavedDefaults()
        {
            var asset = RectTransformSnapperDefaultsAsset.Load();
            if (asset == null) return false;
            Enabled = asset.enabled;
            ProportionalChildrenEnabled = asset.proportionalChildrenEnabled;
            SnapToRectEdges = asset.snapToRectEdges;
            CanvasOriginIndex = Mathf.Clamp(asset.canvasOriginIndex, 0, 8);
            AlignToCanvas = asset.alignToCanvas;
            SnapStep = Mathf.Max(1f, asset.snapStep);
            SnapDivisor = Mathf.Max(1, asset.snapDivisor);
            SnapOffsetPercentX = Mathf.Clamp(asset.snapOffsetPercentX, -1f, 1f);
            SnapOffsetPercentY = Mathf.Clamp(asset.snapOffsetPercentY, -1f, 1f);
            ShowGrid = asset.showGrid;
            DotSize = Mathf.Clamp(asset.dotSize, 1f, 4f);
            DotColor = asset.dotColor;
            ReferenceColor = asset.referenceColor;
            ReferenceAlpha = Mathf.Clamp01(asset.referenceAlpha);
            ReferenceAlwaysOnTop = asset.referenceAlwaysOnTop;
            SceneView.RepaintAll();
            RepaintSnapperWindow();
            return true;
        }

        public static bool SnapToCanvasBoundaries { get => true; set { } }
        public static float CanvasSnapThreshold { get => EditorPrefs.GetFloat("RTS_CanvasSnapThreshold", 4f); set => EditorPrefs.SetFloat("RTS_CanvasSnapThreshold", Mathf.Max(1f, value)); }

        public static bool SnapToRectEdges { get => EditorPrefs.GetBool("RTS_SnapToRectEdges", true); set => EditorPrefs.SetBool("RTS_SnapToRectEdges", value); }
        public static float RectEdgesSnapThreshold { get => EditorPrefs.GetFloat("RTS_RectEdgesSnapThreshold", 4f); set => EditorPrefs.SetFloat("RTS_RectEdgesSnapThreshold", Mathf.Max(1f, value)); }

        public static bool ProportionalChildrenEnabled { get => EditorPrefs.GetBool("RTS_ProportionalChildren", true); set => EditorPrefs.SetBool("RTS_ProportionalChildren", value); }

        public static bool AlignToCanvas { get => EditorPrefs.GetBool("RTS_AlignToCanvas", false); set => EditorPrefs.SetBool("RTS_AlignToCanvas", value); }

        #region PublicActions_ForShortcuts
        public static void ToggleEnabledAndGrid()
        {
            Enabled = !Enabled;
            ShowGrid = Enabled;
            SceneView.RepaintAll();
            RepaintSnapperWindow();
        }

        public static void ToggleResizeChildren()
        {
            ProportionalChildrenEnabled = !ProportionalChildrenEnabled;
            SceneView.RepaintAll();
            RepaintSnapperWindow();
        }

        public static void ToggleSnapToRectEdges()
        {
            SnapToRectEdges = !SnapToRectEdges;
            SceneView.RepaintAll();
            RepaintSnapperWindow();
        }

        public static void MultiplyGridStep(float factor)
        {
            factor = Mathf.Abs(factor) < 0.0001f ? 1f : factor;
            float newStep = Mathf.Max(1f, SnapStep * factor);
            var st = RectTransformSnapperSettingsUndoState.Instance;
            Undo.RecordObject(st, "Change Grid Step");
            st.snapStep = newStep;
            EditorUtility.SetDirty(st);
            SnapStep = newStep;
        }

        public static void MultiplySubdivisions(float factor)
        {
            int div = Mathf.Max(1, SnapDivisor);
            int newDiv = Mathf.Max(1, Mathf.RoundToInt(div * factor));
            var st = RectTransformSnapperSettingsUndoState.Instance;
            Undo.RecordObject(st, "Change Subdivisions");
            st.snapDivisor = newDiv;
            EditorUtility.SetDirty(st);
            SnapDivisor = newDiv;
        }

        public static void CreateNewCanvasAndAssign()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
            }

            AssignedCanvas = canvas;
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            SceneView.RepaintAll();
            RepaintSnapperWindow();
        }

        private const string REF_PICKER_CONTROL_NAME = "RTS_REF_PICKER_SHORTCUT";
        private static bool _waitingForRefPicker;

        public static void AddReferenceViaPicker()
        {
            if (AssignedCanvas == null)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Assign a Canvas first.", "OK");
                return;
            }
            _waitingForRefPicker = true;
            EditorGUIUtility.ShowObjectPicker<Sprite>(null, false, string.Empty, REF_PICKER_CONTROL_NAME.GetHashCode());
        }
        #endregion

        private enum DragKind { None, Body, Edge, Corner }
        private enum Edge { None, Left, Right, Bottom, Top }
        private enum AxisLock { None, X, Y }

        private static bool isDragging;
        private static bool pendingDrag;
        private static Vector2 mouseDownGUIPos;
        private static RectTransform pendingRT;
        private static DragKind pendingKind;
        private static Edge pendingEH, pendingEV;
        private static AxisLock pendingAxisLock = AxisLock.None;
        private static Vector3 lastRTWorldPosition;
        private static Vector2 lastGroupCenterInParent;

        private const float DRAG_THRESHOLD = 0f;
        private static DragKind dragKind = DragKind.None;
        private static Edge dragEdgeH = Edge.None;
        private static Edge dragEdgeV = Edge.None;
        private static AxisLock axisLock = AxisLock.None;
        private static bool dragSymmetric;
        private static bool dragKeepAspect;
        private static SnapAxis dragEdgeSnapAxis = SnapAxis.Auto;
        private const float EDGE_SNAP_AXIS_HYSTERESIS = 0.15f;

        private static Vector2 dragStartMouseParent;
        private static float startL, startR, startB, startT;
        private static Vector2 startCenter;
        private static float dragOriginParentX;
        private static float dragOriginParentY;
        private const float MIN_THICKNESS = 1f;

        #region Drag_LocalSpaceState

        private static Vector2 startAnchoredPosition;
        private static Vector2 startSizeDelta;
        private static Vector2 startPivot01;
        private static float startLocalWidth;
        private static float startLocalHeight;
        private static float startLocalAspectRatio;
        private static bool startFlipX;
        private static bool startFlipY;

        private static Vector2 startBasisUParent;
        private static Vector2 startBasisVParent;

        private static readonly Vector2[] startCornersParent = new Vector2[4];
        #endregion

        #region GroupSelection_New

        private sealed class GroupSelectionState
        {
            public RectTransform parent;
            public RectTransform active;
            public readonly List<RectTransform> members = new List<RectTransform>(16);
            public readonly HashSet<RectTransform> memberSet = new HashSet<RectTransform>();

            public Vector2 uAxisParent;
            public Vector2 vAxisParent;
            public Vector2 uNorm;
            public Vector2 vNorm;
            public float uLen;
            public float vLen;

            public float minU, maxU, minV, maxV;

            public readonly Vector2[] cornersParent = new Vector2[4];

            public float aabbL, aabbR, aabbB, aabbT;
            public Vector2 aabbCenter => new Vector2((aabbL + aabbR) * 0.5f, (aabbB + aabbT) * 0.5f);
        }

        private struct GroupMemberStartState
        {
            public ProportionalChildLocalState local;
            public Vector2 pivotParent;
            public Vector2 basisUParent;
            public Vector2 basisVParent;
        }

        private static bool isGroupDragging;
        private static GroupSelectionState groupDragStart;
        private static readonly Dictionary<RectTransform, GroupMemberStartState> groupMemberStart = new Dictionary<RectTransform, GroupMemberStartState>();
        private static GroupSelectionState pendingGroupSelection;
        private static bool pendingIsGroup;
        #endregion

        private static readonly List<RectTransform> proportionalChildren = new List<RectTransform>();
        private struct ProportionalChildLocalState
        {
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
        }
        private static readonly Dictionary<RectTransform, ProportionalChildLocalState> proportionalChildrenStartLocal = new Dictionary<RectTransform, ProportionalChildLocalState>();
        private static Rect proportionalParentStartRect;

        private static bool _hasStoredUnitySettings;
        private static bool _storedGridVisible;
        private static bool _storedSnapEnabled;
        private static bool _isCustomGridActive;


        static RectTransformSnapperEngine()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.quitting += OnEditorQuitting;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Selection.selectionChanged += OnSelectionChangedAssignCanvas;
            Undo.undoRedoPerformed += OnUndoRedoPerformedAssignCanvas;
        }

        private static void OnEditorQuitting()
        {
            if (_isCustomGridActive)
            {
                RestoreUnitySettings();
                _isCustomGridActive = false;
            }
        }

        private static void OnHierarchyChanged()
        {
            if (!ReferenceAlwaysOnTop) return;


            if (_activeReferenceImage != null && _activeReferenceImage.gameObject != null)
            {
                var canvas = AssignedCanvas;
                if (canvas != null)
                {
                    var canvasRT = canvas.GetComponent<RectTransform>();
                    if (canvasRT != null && _activeReferenceImage.parent == canvasRT)
                    {

                        if (_activeReferenceImage.GetSiblingIndex() != canvasRT.childCount - 1)
                        {
                            _activeReferenceImage.SetAsLastSibling();
                        }
                    }
                }
            }
        }

        private static void OnSelectionChangedAssignCanvas()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            if (AssignedCanvas == canvas) return;
            AssignedCanvas = canvas;
        }

        private static void OnUndoRedoPerformedAssignCanvas()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            if (AssignedCanvas == canvas) return;
            AssignedCanvas = canvas;
        }

        private static void DrawAssignedCanvasBounds()
        {
            var canvas = AssignedCanvas;
            if (canvas == null) return;
            if (SceneVisibilityManager.instance.IsHidden(canvas.gameObject)) return;

            var rt = canvas.GetComponent<RectTransform>();
            if (rt == null) return;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var verts = new Vector3[] { corners[0], corners[1], corners[2], corners[3] };
            var prevColor = Handles.color;
            var prevZTest = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Color fill = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(1f, 0.8156863f, 0.454902f, 0.9f);
            Handles.DrawSolidRectangleWithOutline(verts, fill, outline);
            Handles.zTest = prevZTest;
            Handles.color = prevColor;
        }

        private static void StoreUnitySettings()
        {
            if (_hasStoredUnitySettings) return;

            _storedGridVisible = SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.showGrid;
            _storedSnapEnabled = EditorSnapSettings.gridSnapEnabled;
            _hasStoredUnitySettings = true;
        }

        private static void DisableUnityGridAndSnap()
        {
            var allSceneViews = SceneView.sceneViews;
            for (int i = 0; i < allSceneViews.Count; i++)
            {
                var sv = allSceneViews[i] as SceneView;
                if (sv != null)
                {
                    sv.showGrid = false;
                }
            }
            EditorSnapSettings.gridSnapEnabled = false;
        }

        private static void RestoreUnitySettings()
        {
            if (!_hasStoredUnitySettings) return;

            var allSceneViews = SceneView.sceneViews;
            for (int i = 0; i < allSceneViews.Count; i++)
            {
                var sv = allSceneViews[i] as SceneView;
                if (sv != null)
                {
                    sv.showGrid = _storedGridVisible;
                }
            }
            EditorSnapSettings.gridSnapEnabled = _storedSnapEnabled;
            _hasStoredUnitySettings = false;
        }

        private static bool ShouldShowCustomGrid()
        {
            return Enabled && ShowGrid && AssignedCanvas != null;
        }

        private static void UpdateUnityGridState()
        {
            bool shouldShow = ShouldShowCustomGrid();

            if (shouldShow && !_isCustomGridActive)
            {
                StoreUnitySettings();
                DisableUnityGridAndSnap();
                _isCustomGridActive = true;
            }
            else if (!shouldShow && _isCustomGridActive)
            {
                RestoreUnitySettings();
                _isCustomGridActive = false;
            }
        }

        private static void HandleHotkeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

        }

        private static void RepaintSnapperWindow()
        {
            var windows = Resources.FindObjectsOfTypeAll<RectTransformSnapperWindow>();
            foreach (var window in windows)
            {
                if (window != null) window.Repaint();
            }
        }

        private static void OnSceneGUI(SceneView sv)
        {
            UpdateUnityGridState();
            int controlId = GUIUtility.GetControlID(FocusType.Keyboard);

            if (_waitingForRefPicker && Event.current != null && Event.current.commandName == "ObjectSelectorClosed")
            {
                _waitingForRefPicker = false;
                var obj = EditorGUIUtility.GetObjectPickerObject();
                if (obj is Sprite sp) ApplyPickedReferenceSprite(sp);
            }

            if (!Enabled) return;


            HandleHotkeys();

            if (AssignedCanvas == null)
            {
                Handles.BeginGUI();
                var messageWidth = 380;
                var x = (sv.position.width - messageWidth) / 2;
                var rect = new Rect(x, 10, messageWidth, 48);
                EditorGUI.HelpBox(rect, "RectTransform Snapper: Select a Canvas or one of its children to enable the tool.", MessageType.Warning);
                Handles.EndGUI();
                return;
            }

            var e = Event.current;

            if (Tools.current != Tool.Rect && Tools.current != Tool.Move) { DrawGridIfNeeded(sv, e); return; }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                pendingDrag = false;
                if (TryGetSelectedRT(out var rt) && rt.parent is RectTransform)
                {
                    pendingRT = rt;
                    lastRTWorldPosition = rt.transform.position;
                    mouseDownGUIPos = e.mousePosition;
                    pendingAxisLock = AxisLock.None;
                    if (Tools.current == Tool.Move && rt.parent is RectTransform p0)
                    {
                        bool dummyHit;
                        pendingAxisLock = DetectAxisLockOnMove(rt, p0, e.mousePosition, out dummyHit);
                    }

                    pendingIsGroup = false;
                    pendingGroupSelection = null;

                    if (TryBuildGroupSelection(rt, out var gSel))
                    {
                        pendingIsGroup = true;
                        pendingGroupSelection = gSel;

                        if (Tools.current == Tool.Move)
                        {
                            Vector2 groupPivotForDetection = GetGroupHandlePositionInParent(gSel.parent);
                            bool dummyHit;
                            pendingAxisLock = DetectAxisLockOnMoveGroup(gSel.parent, groupPivotForDetection, e.mousePosition, out dummyHit);
                            pendingDrag = false;
                            return;
                        }

                        if (Tools.current == Tool.Rect)
                        {
                            if (TryPickGroupHandle(gSel, e.mousePosition, out var gKind, out var gEH, out var gEV))
                            {
                                pendingKind = gKind; pendingEH = gEH; pendingEV = gEV;
                            }
                            else
                            {
                                pendingDrag = false;
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (Tools.current == Tool.Rect)
                        {
                            if (TryPickHandle(rt, e.mousePosition, out var kind0, out var eh0, out var ev0))
                            {
                                pendingKind = kind0; pendingEH = eh0; pendingEV = ev0;
                            }
                            else
                            {
                                pendingDrag = false;
                                return;
                            }
                        }
                        else if (Tools.current == Tool.Move)
                        {
                            pendingDrag = false;
                            return;
                        }
                    }

                    if (e.alt && (Tools.current == Tool.Move || pendingKind == DragKind.Body))
                    {
                        pendingDrag = false;
                        return;
                    }

                    pendingDrag = true;
                }
            }

            if (Tools.current == Tool.Move && !isDragging && !pendingDrag && e.type == EventType.MouseDrag)
            {
                if (TryGetSelectedRT(out var moveRT) && moveRT.parent is RectTransform moveParent)
                {
                    Vector3 currentWorldPos = moveRT.transform.position;
                    if (e.button == 0 && Vector3.Distance(currentWorldPos, lastRTWorldPosition) > 0.001f)
                    {
                        pendingRT = moveRT;
                        mouseDownGUIPos = e.mousePosition;
                        pendingKind = DragKind.Body;
                        pendingEH = Edge.None;
                        pendingEV = Edge.None;
                        bool dummyHit;
                        pendingAxisLock = DetectAxisLockOnMove(moveRT, moveParent, e.mousePosition, out dummyHit);

                        pendingIsGroup = false;
                        pendingGroupSelection = null;
                        if (TryBuildGroupSelection(moveRT, out var moveGroupSel))
                        {
                            pendingIsGroup = true;
                            pendingGroupSelection = moveGroupSel;
                            Vector2 groupPivotForDetection = GetGroupHandlePositionInParent(moveGroupSel.parent);
                            pendingAxisLock = DetectAxisLockOnMoveGroup(moveGroupSel.parent, groupPivotForDetection, e.mousePosition, out dummyHit);
                        }

                        if (TryMouseParent(moveParent, e.mousePosition, out var mParent))
                        {
                            if (pendingIsGroup && pendingGroupSelection != null)
                                BeginGroupDrag(pendingGroupSelection, mParent, pendingKind, pendingEH, pendingEV);
                            else
                                PrepareDrag(moveRT, moveParent, mParent, pendingKind, pendingEH, pendingEV);
                            GUIUtility.hotControl = controlId;
                            if (e.type == EventType.MouseDrag)
                            {
                                e.Use();
                            }
                            SceneView.currentDrawingSceneView?.Repaint();
                        }
                    }
                }
            }

            if (!isDragging && pendingDrag && e.type == EventType.MouseDrag)
            {
                if ((e.mousePosition - mouseDownGUIPos).sqrMagnitude >= DRAG_THRESHOLD * DRAG_THRESHOLD)
                {
                    if (Tools.current == Tool.Rect && e.alt)
                    {
                        pendingDrag = false;
                        return;
                    }
                    dragSymmetric = e != null && e.alt;
                    dragKeepAspect = e != null && e.shift;
                    RectTransform currentSel = null;
                    if (Selection.activeGameObject != null)
                        currentSel = Selection.activeGameObject.GetComponent<RectTransform>();
                    var rt = currentSel != null ? currentSel : pendingRT;
                    if (rt != null && rt.parent is RectTransform parent && TryMouseParent(parent, mouseDownGUIPos, out var mParent))
                    {
                        if (pendingIsGroup && pendingGroupSelection != null && pendingGroupSelection.parent == parent)
                        {
                            BeginGroupDrag(pendingGroupSelection, mParent, pendingKind, pendingEH, pendingEV);
                            GUIUtility.hotControl = controlId;
                        }
                        else
                        {
                            if (rt != pendingRT && Tools.current == Tool.Rect && TryPickHandle(rt, mouseDownGUIPos, out var k2, out var eh2, out var ev2))
                            {
                                pendingKind = k2; pendingEH = eh2; pendingEV = ev2;
                            }
                            PrepareDrag(rt, parent, mParent, pendingKind, pendingEH, pendingEV);
                            GUIUtility.hotControl = controlId;
                        }
                        e.Use();
                    }
                    else
                    {
                        pendingDrag = false;
                    }
                }
            }

            if (isDragging && e.type == EventType.MouseDrag)
            {
                dragSymmetric = e != null && e.alt;
                dragKeepAspect = e != null && e.shift;
                if (isGroupDragging && groupDragStart != null && groupDragStart.parent != null &&
                    TryMouseParent(groupDragStart.parent, e.mousePosition, out var mParent))
                {
                    LiveGroupDrag(mParent);
                    e.Use();
                    SceneView.currentDrawingSceneView?.Repaint();
                }
                else if (TryGetSelectedRT(out var rt) && rt.parent is RectTransform parent
                    && TryMouseParent(parent, e.mousePosition, out var mParentSingle))
                {
                    switch (dragKind)
                    {
                        case DragKind.Body: LiveSnapBody(rt, parent, mParentSingle); break;
                        case DragKind.Edge: LiveSnapEdge(rt, parent, mParentSingle); break;
                        case DragKind.Corner: LiveSnapCorner(rt, parent, mParentSingle); break;
                    }
                    e.Use();
                    SceneView.currentDrawingSceneView?.Repaint();
                }

                if (Tools.current == Tool.Rect)
                {
                    Edge effectiveEdgeH = dragEdgeH;
                    Edge effectiveEdgeV = dragEdgeV;
                    if (TryGetSelectedRT(out var rtDrag))
                    {
                        if (startFlipX)
                        {
                            if (dragEdgeH == Edge.Left) effectiveEdgeH = Edge.Right;
                            else if (dragEdgeH == Edge.Right) effectiveEdgeH = Edge.Left;
                        }
                        if (startFlipY)
                        {
                            if (dragEdgeV == Edge.Bottom) effectiveEdgeV = Edge.Top;
                            else if (dragEdgeV == Edge.Top) effectiveEdgeV = Edge.Bottom;
                        }
                    }
                    var cursor = GetCursorForHandle(rtDrag, dragKind, effectiveEdgeH, effectiveEdgeV);
                    Handles.BeginGUI();
                    EditorGUIUtility.AddCursorRect(new Rect(0, 0, sv.position.width, sv.position.height), cursor);
                    Handles.EndGUI();
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
                pendingDrag = false;
                dragKind = DragKind.None;
                dragEdgeH = Edge.None;
                dragEdgeV = Edge.None;
                axisLock = AxisLock.None;
                pendingAxisLock = AxisLock.None;
                if (isGroupDragging) EndGroupDrag();
                pendingIsGroup = false;
                pendingGroupSelection = null;
                proportionalChildren.Clear();
                proportionalChildrenStartLocal.Clear();
                if (TryGetSelectedRT(out var resetRT))
                {
                    lastRTWorldPosition = resetRT.transform.position;
                }
                if (TryGetSelectedRT(out var resetRT2) && TryBuildGroupSelection(resetRT2, out var resetSel))
                {
                    lastGroupCenterInParent = resetSel.aabbCenter;
                }
                else
                {
                    lastGroupCenterInParent = Vector2.zero;
                }
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }

                SceneView.RepaintAll();
            }

            if (TryGetSelectedRT(out var anyRt) && TryBuildGroupSelection(anyRt, out var groupSel))
            {
                DrawGroupSelection(groupSel);
            }

            DrawGridIfNeeded(sv, e);
            DrawAssignedCanvasBounds();

            if (Tools.current == Tool.Move && !isDragging && !pendingDrag && e.type == EventType.Layout)
            {
                if (TryGetSelectedRT(out var trackRT))
                {
                    if (lastRTWorldPosition == Vector3.zero)
                    {
                        lastRTWorldPosition = trackRT.transform.position;
                    }
                    else if (Vector3.Distance(trackRT.transform.position, lastRTWorldPosition) < 0.001f)
                    {
                        lastRTWorldPosition = trackRT.transform.position;
                    }
                }
                if (TryGetSelectedRT(out var trackRT2) && TryBuildGroupSelection(trackRT2, out var trackSel))
                {
                    Vector2 currentGroupCenter = trackSel.aabbCenter;
                    if (lastGroupCenterInParent == Vector2.zero)
                    {
                        lastGroupCenterInParent = currentGroupCenter;
                    }
                    else if (Vector2.Distance(currentGroupCenter, lastGroupCenterInParent) < 0.001f)
                    {
                        lastGroupCenterInParent = currentGroupCenter;
                    }
                }
            }

            UpdateResizeCursorIcon(sv, e);
        }

        private static void ApplyPickedReferenceSprite(Sprite sp)
        {
            if (sp == null) return;
            var canvas = AssignedCanvas;
            if (canvas == null) return;
            var canvasRT = canvas.GetComponent<RectTransform>();
            if (canvasRT == null) return;

            UnityEngine.UI.Image img = null;
            if (ActiveReferenceImage != null)
            {
                img = ActiveReferenceImage.GetComponent<UnityEngine.UI.Image>();
                if (img == null) img = null;
            }

            if (img != null)
            {
                Undo.RecordObject(img, "Update Reference Sprite");
                img.sprite = sp;
                var c = ReferenceColor;
                img.color = new Color(c.r, c.g, c.b, ReferenceAlpha);
                EditorUtility.SetDirty(img);
                var rtRef = img.GetComponent<RectTransform>();
                if (rtRef != null)
                {
                    Undo.RecordObject(rtRef, "Update Reference Size");
                    var r = sp.rect;
                    rtRef.sizeDelta = new Vector2(r.width, r.height);
                    EditorUtility.SetDirty(rtRef);
                }
                ActiveReferenceImage = img.transform;
            }
            else
            {
                GameObject go = new GameObject("Reference_" + sp.name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                Undo.RegisterCreatedObjectUndo(go, "Add Ref Overlay");
                go.transform.SetParent(canvasRT, false);
                go.transform.SetAsLastSibling();

                var newImg = go.GetComponent<UnityEngine.UI.Image>();
                newImg.sprite = sp;
                var c = ReferenceColor;
                newImg.color = new Color(c.r, c.g, c.b, ReferenceAlpha);
                newImg.raycastTarget = false;

                var rtNew = go.GetComponent<RectTransform>();
                rtNew.anchorMin = rtNew.anchorMax = new Vector2(0.5f, 0.5f);
                rtNew.pivot = new Vector2(0.5f, 0.5f);
                rtNew.anchoredPosition = Vector2.zero;
                var r = sp.rect;
                rtNew.sizeDelta = new Vector2(r.width, r.height);

                ActiveReferenceImage = go.transform;
            }

            SceneView.RepaintAll();
            RepaintSnapperWindow();
        }

        private static void PrepareDrag(RectTransform rt, RectTransform parent, Vector2 mouseParent, DragKind kind, Edge eh, Edge ev)
        {
            isDragging = true;
            dragKind = kind;
            dragEdgeH = eh;
            dragEdgeV = ev;
            dragEdgeSnapAxis = SnapAxis.Auto;

            axisLock = (Tools.current == Tool.Move) ? pendingAxisLock : AxisLock.None;
            dragStartMouseParent = mouseParent;

            GetEdgesInParent(rt, parent, out startL, out startR, out startB, out startT);
            startCenter = new Vector2((startL + startR) * 0.5f, (startB + startT) * 0.5f);

            startAnchoredPosition = rt.anchoredPosition;
            startSizeDelta = rt.sizeDelta;
            startPivot01 = rt.pivot;
            startLocalWidth = Mathf.Max(MIN_THICKNESS, rt.rect.width);
            startLocalHeight = Mathf.Max(MIN_THICKNESS, rt.rect.height);
            startLocalAspectRatio = startLocalWidth / startLocalHeight;

            {
                var wc = new Vector3[4];
                rt.GetWorldCorners(wc);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = parent.InverseTransformPoint(wc[i]);
                    startCornersParent[i] = new Vector2(p.x, p.y);
                }

                Vector3 uWorld = rt.TransformVector(Vector3.right);
                Vector3 vWorld = rt.TransformVector(Vector3.up);
                startFlipX = Vector3.Dot(uWorld, rt.transform.right) < 0f;
                startFlipY = Vector3.Dot(vWorld, rt.transform.up) < 0f;
                if (startFlipX) uWorld = -uWorld;
                if (startFlipY) vWorld = -vWorld;
                Vector3 uParent3 = parent.InverseTransformVector(uWorld);
                Vector3 vParent3 = parent.InverseTransformVector(vWorld);
                startBasisUParent = new Vector2(uParent3.x, uParent3.y);
                startBasisVParent = new Vector2(vParent3.x, vParent3.y);
            }

            var origin = ComputeGlobalOriginInParent(parent, rt);
            dragOriginParentX = origin.x;
            dragOriginParentY = origin.y;

            Undo.RegisterCompleteObjectUndo(rt, "Rect Snap Begin");

            if (ProportionalChildrenEnabled && dragKind != DragKind.None)
            {
                PrepareProportionalChildren(rt);
            }
        }

        private static bool TryGetSelectedRT(out RectTransform rt)
        {
            rt = null;
            if (Selection.activeGameObject == null) return false;
            rt = Selection.activeGameObject.GetComponent<RectTransform>();
            return rt != null;
        }

        private static bool TryMouseParent(RectTransform parent, Vector2 gui, out Vector2 local)
        {
            var ray = HandleUtility.GUIPointToWorldRay(gui);
            var plane = new Plane(parent.transform.forward, parent.transform.position);
            if (plane.Raycast(ray, out float hit)
            )
            {
                var world = ray.GetPoint(hit);
                local = parent.InverseTransformPoint(world);
                return true;
            }
            local = default;
            return false;
        }

        private static bool IsMouseInsideRect(RectTransform rt, Vector2 guiPoint)
        {
            var ray = HandleUtility.GUIPointToWorldRay(guiPoint);
            var plane = new Plane(rt.transform.forward, rt.transform.position);
            if (!plane.Raycast(ray, out float hit)) return false;
            Vector3 world = ray.GetPoint(hit);
            Vector2 local = rt.InverseTransformPoint(world);
            return rt.rect.Contains(local);
        }

        private static float RoundToGrid(float value, float step, float origin) =>
            Mathf.Round((value - origin) / step) * step + origin;

        private static float RoundToPrecision(float value) =>
            Mathf.Round(value * 1000f) / 1000f;

        #region Drag_LocalMathAndSnapping
        private enum SnapAxis { Auto, X, Y }

        private static bool TryDecomposeDeltaParentToLocal(Vector2 deltaParent, Vector2 uParent, Vector2 vParent, out float dxLocal, out float dyLocal)
        {
            float det = uParent.x * vParent.y - uParent.y * vParent.x;
            if (Mathf.Abs(det) < 1e-6f)
            {
                dxLocal = dyLocal = 0f;
                return false;
            }

            dxLocal = (deltaParent.x * vParent.y - deltaParent.y * vParent.x) / det;
            dyLocal = (uParent.x * deltaParent.y - uParent.y * deltaParent.x) / det;
            return !(float.IsNaN(dxLocal) || float.IsInfinity(dxLocal) || float.IsNaN(dyLocal) || float.IsInfinity(dyLocal));
        }

        private static Vector2 GetStartEdgeMidpointInParent(Edge edgeH, Edge edgeV)
        {
            if (edgeH == Edge.Left) return (startCornersParent[0] + startCornersParent[1]) * 0.5f;
            if (edgeH == Edge.Right) return (startCornersParent[3] + startCornersParent[2]) * 0.5f;
            if (edgeV == Edge.Bottom) return (startCornersParent[0] + startCornersParent[3]) * 0.5f;
            if (edgeV == Edge.Top) return (startCornersParent[1] + startCornersParent[2]) * 0.5f;
            return (startCornersParent[0] + startCornersParent[2]) * 0.5f;
        }

        private static void GetStartEdgeCornerPairInParent(Edge edgeH, Edge edgeV, out Vector2 a, out Vector2 b)
        {
            if (edgeH == Edge.Left) { a = startCornersParent[0]; b = startCornersParent[1]; return; }
            if (edgeH == Edge.Right) { a = startCornersParent[3]; b = startCornersParent[2]; return; }
            if (edgeV == Edge.Bottom) { a = startCornersParent[0]; b = startCornersParent[3]; return; }
            if (edgeV == Edge.Top) { a = startCornersParent[1]; b = startCornersParent[2]; return; }
            a = startCornersParent[0]; b = startCornersParent[2];
        }

        private static Vector2 GetStartCornerInParent(Edge edgeH, Edge edgeV)
        {
            if (edgeH == Edge.Left && edgeV == Edge.Bottom) return startCornersParent[0];
            if (edgeH == Edge.Left && edgeV == Edge.Top) return startCornersParent[1];
            if (edgeH == Edge.Right && edgeV == Edge.Top) return startCornersParent[2];
            if (edgeH == Edge.Right && edgeV == Edge.Bottom) return startCornersParent[3];
            return startCornersParent[2];
        }

        private static float SnapPointCoord(
            float coord,
            bool horizontal,
            float step,
            float originCoord,
            float offsetBase,
            RectTransform parent,
            RectTransform excludeRT)
        {
            float target = RoundToGrid(coord, step, originCoord + offsetBase);

            if (SnapToCanvasBoundaries && CanvasSnapThreshold > 0f && parent != null)
            {
                bool ok;
                float canvas = NearestCanvasAxis(coord, parent, horizontal, out ok);
                if (ok && Mathf.Abs(coord - canvas) <= CanvasSnapThreshold)
                    target = ChooseClosest(coord, target, canvas);
            }

            if (SnapToRectEdges && RectEdgesSnapThreshold > 0f && parent != null)
            {
                bool ok;
                float rect = NearestRectEdgeAxis(coord, parent, excludeRT, horizontal, RectEdgesSnapThreshold, out ok);
                if (ok && Mathf.Abs(coord - rect) <= RectEdgesSnapThreshold)
                    target = ChooseClosest(coord, target, rect);
            }

            return target;
        }

        private static float SnapEdgeDeltaLocalFromTwoCorners(
            float deltaLocal,
            Edge edgeH,
            Edge edgeV,
            Vector2 axisDirParent,
            float step,
            Vector2 gridOriginParent,
            Vector2 offBase,
            RectTransform parent,
            RectTransform excludeRT)
        {
            GetStartEdgeCornerPairInParent(edgeH, edgeV, out var c0, out var c1);
            return SnapEdgeDeltaLocalFromTwoCorners(
                deltaLocal,
                c0,
                c1,
                axisDirParent,
                step,
                gridOriginParent,
                offBase,
                parent,
                excludeRT);
        }

        private static float SnapEdgeDeltaLocalFromTwoCorners(
            float deltaLocal,
            Vector2 corner0StartParent,
            Vector2 corner1StartParent,
            Vector2 axisDirParent,
            float step,
            Vector2 gridOriginParent,
            Vector2 offBase,
            RectTransform parent,
            RectTransform excludeRT)
        {

            const float EPS = 1e-6f;

            float bestDeltaX = deltaLocal;
            float bestDeltaY = deltaLocal;
            float bestCostX = float.PositiveInfinity;
            float bestCostY = float.PositiveInfinity;

            void ConsiderCorner(Vector2 cornerStart)
            {
                Vector2 p0 = cornerStart;
                Vector2 pUn = p0 + axisDirParent * deltaLocal;

                if (Mathf.Abs(axisDirParent.x) > EPS)
                {
                    float targetX = SnapPointCoord(pUn.x, true, step, gridOriginParent.x, offBase.x, parent, excludeRT);
                    float cand = (targetX - p0.x) / axisDirParent.x;
                    float cost = Mathf.Abs(targetX - pUn.x) / Mathf.Max(EPS, Mathf.Abs(axisDirParent.x));
                    if (cost < bestCostX)
                    {
                        bestCostX = cost;
                        bestDeltaX = cand;
                    }
                }

                if (Mathf.Abs(axisDirParent.y) > EPS)
                {
                    float targetY = SnapPointCoord(pUn.y, false, step, gridOriginParent.y, offBase.y, parent, excludeRT);
                    float cand = (targetY - p0.y) / axisDirParent.y;
                    float cost = Mathf.Abs(targetY - pUn.y) / Mathf.Max(EPS, Mathf.Abs(axisDirParent.y));
                    if (cost < bestCostY)
                    {
                        bestCostY = cost;
                        bestDeltaY = cand;
                    }
                }
            }

            ConsiderCorner(corner0StartParent);
            ConsiderCorner(corner1StartParent);

            SnapAxis chosen;
            if (bestCostX < bestCostY)
                chosen = SnapAxis.X;
            else if (bestCostY < bestCostX)
                chosen = SnapAxis.Y;
            else
                chosen = SnapAxis.Auto;

            if (dragEdgeSnapAxis == SnapAxis.X)
            {
                if (bestCostY < bestCostX * (1f - EDGE_SNAP_AXIS_HYSTERESIS))
                    dragEdgeSnapAxis = SnapAxis.Y;
            }
            else if (dragEdgeSnapAxis == SnapAxis.Y)
            {
                if (bestCostX < bestCostY * (1f - EDGE_SNAP_AXIS_HYSTERESIS))
                    dragEdgeSnapAxis = SnapAxis.X;
            }
            else
            {
                dragEdgeSnapAxis = chosen == SnapAxis.Auto ? (bestCostX <= bestCostY ? SnapAxis.X : SnapAxis.Y) : chosen;
            }

            float bestDelta = (dragEdgeSnapAxis == SnapAxis.Y) ? bestDeltaY : bestDeltaX;
            if (float.IsNaN(bestDelta) || float.IsInfinity(bestDelta)) return deltaLocal;
            return bestDelta;
        }

        private static int GetEdgeSign(Edge e)
        {
            switch (e)
            {
                case Edge.Left:
                case Edge.Bottom:
                    return -1;
                case Edge.Right:
                case Edge.Top:
                    return 1;
                default:
                    return 0;
            }
        }

        private static float SnapEdgeDeltaLocal(
            float deltaLocal,
            Vector2 edgeMidStartParent,
            Vector2 axisDirParent,
            float step,
            Vector2 gridOriginParent,
            Vector2 offBase,
            RectTransform parent,
            RectTransform excludeRT,
            SnapAxis snapAxis = SnapAxis.Auto)
        {
            Vector2 p0 = edgeMidStartParent;
            Vector2 pUn = p0 + axisDirParent * deltaLocal;

            float bestDelta = deltaLocal;
            float bestCost = float.PositiveInfinity;
            const float EPS = 1e-6f;

            float targetX = SnapPointCoord(pUn.x, true, step, gridOriginParent.x, offBase.x, parent, excludeRT);
            float targetY = SnapPointCoord(pUn.y, false, step, gridOriginParent.y, offBase.y, parent, excludeRT);

            if (snapAxis != SnapAxis.Y && Mathf.Abs(axisDirParent.x) > EPS)
            {
                float cand = (targetX - p0.x) / axisDirParent.x;
                float cost = Mathf.Abs(cand - deltaLocal) * axisDirParent.magnitude;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestDelta = cand;
                }
            }

            if (snapAxis != SnapAxis.X && Mathf.Abs(axisDirParent.y) > EPS)
            {
                float cand = (targetY - p0.y) / axisDirParent.y;
                float cost = Mathf.Abs(cand - deltaLocal) * axisDirParent.magnitude;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestDelta = cand;
                }
            }

            if (float.IsNaN(bestDelta) || float.IsInfinity(bestDelta)) return deltaLocal;
            return bestDelta;
        }

        private static Vector2 SnapCornerDeltaLocal(
            Vector2 deltaLocal,
            Vector2 cornerStartParent,
            Vector2 uParent,
            Vector2 vParent,
            float step,
            Vector2 gridOriginParent,
            Vector2 offBase,
            RectTransform parent,
            RectTransform excludeRT,
            SnapAxis snapAxis = SnapAxis.Auto)
        {
            Vector2 p0 = cornerStartParent;
            Vector2 pUn = p0 + uParent * deltaLocal.x + vParent * deltaLocal.y;

            float targetX = SnapPointCoord(pUn.x, true, step, gridOriginParent.x, offBase.x, parent, excludeRT);
            float targetY = SnapPointCoord(pUn.y, false, step, gridOriginParent.y, offBase.y, parent, excludeRT);

            Vector2 pTarget = new Vector2(targetX, targetY);
            Vector2 deltaParentTarget = pTarget - p0;
            if (!TryDecomposeDeltaParentToLocal(deltaParentTarget, uParent, vParent, out float dx, out float dy))
                return deltaLocal;
            return new Vector2(dx, dy);
        }
        #endregion

        private struct AxisSnapCandidate
        {
            public float center;
            public int priority;
        }

        private static readonly List<AxisSnapCandidate> axisSnapCandidates = new List<AxisSnapCandidate>(6);

        private static void AddAxisSnapCandidate(float center, int priority)
        {
            if (float.IsNaN(center) || float.IsInfinity(center)) return;
            for (int i = 0; i < axisSnapCandidates.Count; i++)
            {
                if (Mathf.Abs(axisSnapCandidates[i].center - center) <= 0.0001f)
                {
                    if (priority < axisSnapCandidates[i].priority)
                    {
                        axisSnapCandidates[i] = new AxisSnapCandidate { center = center, priority = priority };
                    }
                    return;
                }
            }
            axisSnapCandidates.Add(new AxisSnapCandidate { center = center, priority = priority });
        }

        private static float ComputeSnappedCenter(
            float contCenter,
            float startCenter,
            float startMin,
            float startMax,
            float halfExtent,
            float smallStep,
            float originCoord,
            float offsetBase,
            RectTransform parent,
            bool horizontal,
            float canvasThreshold,
            RectTransform excludeRT = null)
        {
            axisSnapCandidates.Clear();

            float baseOrigin = originCoord + offsetBase;

            float gridCenter = RoundToGrid(contCenter, smallStep, baseOrigin);
            AddAxisSnapCandidate(gridCenter, 0);

            float gridMin = RoundToGrid(contCenter - halfExtent, smallStep, baseOrigin) + halfExtent;
            AddAxisSnapCandidate(gridMin, 0);

            float gridMax = RoundToGrid(contCenter + halfExtent, smallStep, baseOrigin) - halfExtent;
            AddAxisSnapCandidate(gridMax, 0);

            if (SnapToCanvasBoundaries && parent != null && canvasThreshold > 0f &&
                TryGetCanvasLocalBounds(parent, out float minX, out float maxX, out float minY, out float maxY))
            {
                float canvasMin = horizontal ? minX : minY;
                float canvasMax = horizontal ? maxX : maxY;
                float canvasCenter = (canvasMin + canvasMax) * 0.5f;

                float contMin = contCenter - halfExtent;
                float contMax = contCenter + halfExtent;

                if (Mathf.Abs(contMin - canvasMin) <= canvasThreshold)
                    AddAxisSnapCandidate(canvasMin + halfExtent, 1);
                if (Mathf.Abs(contMax - canvasMax) <= canvasThreshold)
                    AddAxisSnapCandidate(canvasMax - halfExtent, 1);
                if (Mathf.Abs(contCenter - canvasCenter) <= canvasThreshold)
                    AddAxisSnapCandidate(canvasCenter, 1);
            }

            if (SnapToRectEdges && parent != null)
            {
                float threshold = RectEdgesSnapThreshold;
                float contMin = contCenter - halfExtent;
                float contMax = contCenter + halfExtent;

                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    var childRT = child.GetComponent<RectTransform>();
                    if (ShouldSkipRectEdgeCandidate(childRT, parent, excludeRT)) continue;

                    GetEdgesInParent(childRT, parent, out float childL, out float childR, out float childB, out float childT);

                    if (horizontal)
                    {
                        float childCenter = (childL + childR) * 0.5f;
                        if (Mathf.Abs(contCenter - childCenter) <= threshold)
                            AddAxisSnapCandidate(childCenter, 2);
                        if (Mathf.Abs(contCenter - childL) <= threshold)
                            AddAxisSnapCandidate(childL, 2);
                        if (Mathf.Abs(contCenter - childR) <= threshold)
                            AddAxisSnapCandidate(childR, 2);
                        if (Mathf.Abs(contMin - childL) <= threshold)
                            AddAxisSnapCandidate(childL + halfExtent, 2);
                        if (Mathf.Abs(contMax - childR) <= threshold)
                            AddAxisSnapCandidate(childR - halfExtent, 2);
                        if (Mathf.Abs(contMin - childR) <= threshold)
                            AddAxisSnapCandidate(childR + halfExtent, 2);
                        if (Mathf.Abs(contMax - childL) <= threshold)
                            AddAxisSnapCandidate(childL - halfExtent, 2);
                    }
                    else
                    {
                        float childCenter = (childB + childT) * 0.5f;
                        if (Mathf.Abs(contCenter - childCenter) <= threshold)
                            AddAxisSnapCandidate(childCenter, 2);
                        if (Mathf.Abs(contCenter - childB) <= threshold)
                            AddAxisSnapCandidate(childB, 2);
                        if (Mathf.Abs(contCenter - childT) <= threshold)
                            AddAxisSnapCandidate(childT, 2);
                        if (Mathf.Abs(contMin - childB) <= threshold)
                            AddAxisSnapCandidate(childB + halfExtent, 2);
                        if (Mathf.Abs(contMax - childT) <= threshold)
                            AddAxisSnapCandidate(childT - halfExtent, 2);
                        if (Mathf.Abs(contMin - childT) <= threshold)
                            AddAxisSnapCandidate(childT + halfExtent, 2);
                        if (Mathf.Abs(contMax - childB) <= threshold)
                            AddAxisSnapCandidate(childB - halfExtent, 2);
                    }
                }
            }

            if (axisSnapCandidates.Count == 0)
            {
                axisSnapCandidates.Clear();
                return contCenter;
            }

            AxisSnapCandidate best = axisSnapCandidates[0];
            float bestDiff = Mathf.Abs(best.center - contCenter);
            const float EPS = 0.0001f;

            for (int i = 1; i < axisSnapCandidates.Count; i++)
            {
                var cand = axisSnapCandidates[i];
                float diff = Mathf.Abs(cand.center - contCenter);
                if (diff + EPS < bestDiff)
                {
                    best = cand;
                    bestDiff = diff;
                }
                else if (Mathf.Abs(diff - bestDiff) <= EPS && cand.priority < best.priority)
                {
                    best = cand;
                }
            }

            float result = best.center;
            axisSnapCandidates.Clear();
            return result;
        }

        private static bool TryGetCanvasLocalBounds(RectTransform parent, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (parent == null) return false;
            var canvas = AssignedCanvas;
            if (canvas == null) return false;
            var canvasRT = canvas.GetComponent<RectTransform>();
            if (canvasRT == null) return false;


            var wc = new Vector3[4];
            canvasRT.GetWorldCorners(wc);
            minX = float.PositiveInfinity;
            minY = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            maxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p3 = parent.InverseTransformPoint(wc[i]);
                minX = Mathf.Min(minX, p3.x);
                maxX = Mathf.Max(maxX, p3.x);
                minY = Mathf.Min(minY, p3.y);
                maxY = Mathf.Max(maxY, p3.y);
            }

            return true;
        }

        private static float NearestCanvasAxis(float value, RectTransform parent, bool horizontal, out bool ok)
        {
            ok = TryGetCanvasLocalBounds(parent, out float minX, out float maxX, out float minY, out float maxY);
            if (!ok) return value;
            if (horizontal)
            {
                float c = (minX + maxX) * 0.5f;
                float dxMin = Mathf.Abs(value - minX);
                float dxC = Mathf.Abs(value - c);
                float dxMax = Mathf.Abs(value - maxX);
                return (dxMin <= dxC && dxMin <= dxMax) ? minX : ((dxMax < dxC && dxMax < dxMin) ? maxX : c);
            }
            else
            {
                float c = (minY + maxY) * 0.5f;
                float dyMin = Mathf.Abs(value - minY);
                float dyC = Mathf.Abs(value - c);
                float dyMax = Mathf.Abs(value - maxY);
                return (dyMin <= dyC && dyMin <= dyMax) ? minY : ((dyMax < dyC && dyMax < dyMin) ? maxY : c);
            }
        }

        private static float ChooseClosest(float continuous, float a, float b)
        {
            return (Mathf.Abs(continuous - a) <= Mathf.Abs(continuous - b)) ? a : b;
        }

        #region RectEdgesSnap_Exclusions

        private static bool ShouldSkipRectEdgeCandidate(RectTransform candidate, RectTransform parent, RectTransform excludeRT)
        {
            if (candidate == null) return true;
            if (excludeRT != null && candidate == excludeRT) return true;

            // While dragging a group selection, exclude ALL members of the group from snapping candidates.
            // This prevents self-snapping (group bounds snapping to inner members).
            if (isGroupDragging && groupDragStart != null && groupDragStart.parent == parent)
            {
                return groupDragStart.memberSet.Contains(candidate);
            }

            return false;
        }

        #endregion

        private static float NearestRectEdgeAxis(float value, RectTransform parent, RectTransform excludeRT, bool horizontal, float threshold, out bool ok)
        {
            ok = false;
            if (parent == null) return value;

            float bestValue = value;
            float bestDistance = threshold;
            ok = false;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childRT = child.GetComponent<RectTransform>();
                if (ShouldSkipRectEdgeCandidate(childRT, parent, excludeRT)) continue;

                GetEdgesInParent(childRT, parent, out float childL, out float childR, out float childB, out float childT);

                if (horizontal)
                {
                    float childCenter = (childL + childR) * 0.5f;
                    float distL = Mathf.Abs(value - childL);
                    float distR = Mathf.Abs(value - childR);
                    float distC = Mathf.Abs(value - childCenter);

                    if (distL <= threshold && distL < bestDistance)
                    {
                        bestValue = childL;
                        bestDistance = distL;
                        ok = true;
                    }
                    if (distR <= threshold && distR < bestDistance)
                    {
                        bestValue = childR;
                        bestDistance = distR;
                        ok = true;
                    }
                    if (distC <= threshold && distC < bestDistance)
                    {
                        bestValue = childCenter;
                        bestDistance = distC;
                        ok = true;
                    }
                }
                else
                {
                    float childCenter = (childB + childT) * 0.5f;
                    float distB = Mathf.Abs(value - childB);
                    float distT = Mathf.Abs(value - childT);
                    float distC = Mathf.Abs(value - childCenter);

                    if (distB <= threshold && distB < bestDistance)
                    {
                        bestValue = childB;
                        bestDistance = distB;
                        ok = true;
                    }
                    if (distT <= threshold && distT < bestDistance)
                    {
                        bestValue = childT;
                        bestDistance = distT;
                        ok = true;
                    }
                    if (distC <= threshold && distC < bestDistance)
                    {
                        bestValue = childCenter;
                        bestDistance = distC;
                        ok = true;
                    }
                }
            }

            return ok ? bestValue : value;
        }

        private static Vector2 GetSnapOffsetInParent(float step)
        {

            float baseStep = SnapStep;
            return new Vector2(baseStep * SnapOffsetPercentX, baseStep * SnapOffsetPercentY);
        }

        private static void GetEdgesInParent(RectTransform rt, RectTransform parent,
         out float L, out float R, out float B, out float T)
        {
            Rect r = rt.rect;
            Vector3 wBL = rt.TransformPoint(new Vector3(r.xMin, r.yMin, 0f));
            Vector3 wTL = rt.TransformPoint(new Vector3(r.xMin, r.yMax, 0f));
            Vector3 wBR = rt.TransformPoint(new Vector3(r.xMax, r.yMin, 0f));
            Vector3 wTR = rt.TransformPoint(new Vector3(r.xMax, r.yMax, 0f));
            Vector3 pBL = parent.InverseTransformPoint(wBL);
            Vector3 pTL = parent.InverseTransformPoint(wTL);
            Vector3 pBR = parent.InverseTransformPoint(wBR);
            Vector3 pTR = parent.InverseTransformPoint(wTR);
            L = Mathf.Min(pBL.x, pTL.x, pBR.x, pTR.x);
            R = Mathf.Max(pBL.x, pTL.x, pBR.x, pTR.x);
            B = Mathf.Min(pBL.y, pTL.y, pBR.y, pTR.y);
            T = Mathf.Max(pBL.y, pTL.y, pBR.y, pTR.y);
        }

        private static void SetEdgesInParent(RectTransform rt, RectTransform parent,
            float L, float R, float B, float T)
        {
            L = RoundToPrecision(L);
            R = RoundToPrecision(R);
            B = RoundToPrecision(B);
            T = RoundToPrecision(T);

            Vector3 scale = rt.localScale;
            bool hasScale = Mathf.Abs(scale.x - 1f) > 0.0001f || Mathf.Abs(scale.y - 1f) > 0.0001f;

            Vector2 p = parent.rect.size;
            Vector2 pivotOff = new Vector2(p.x * parent.pivot.x, p.y * parent.pivot.y);
            Vector2 offMin = rt.offsetMin;
            Vector2 offMax = rt.offsetMax;

            if (!hasScale)
            {
                float L_lb_simple = L + pivotOff.x;
                float R_lb_simple = R + pivotOff.x;
                float B_lb_simple = B + pivotOff.y;
                float T_lb_simple = T + pivotOff.y;

                offMin.x = L_lb_simple - p.x * rt.anchorMin.x;
                offMax.x = R_lb_simple - p.x * rt.anchorMax.x;
                offMin.y = B_lb_simple - p.y * rt.anchorMin.y;
                offMax.y = T_lb_simple - p.y * rt.anchorMax.y;

                offMin.x = RoundToPrecision(offMin.x);
                offMin.y = RoundToPrecision(offMin.y);
                offMax.x = RoundToPrecision(offMax.x);
                offMax.y = RoundToPrecision(offMax.y);

                rt.offsetMin = offMin;
                rt.offsetMax = offMax;
                return;
            }

            float desiredVisualWidth = R - L;
            float desiredVisualHeight = T - B;
            float neededRectWidth = Mathf.Abs(scale.x) > 0.0001f ? desiredVisualWidth / Mathf.Abs(scale.x) : desiredVisualWidth;
            float neededRectHeight = Mathf.Abs(scale.y) > 0.0001f ? desiredVisualHeight / Mathf.Abs(scale.y) : desiredVisualHeight;
            float centerX = (L + R) * 0.5f;
            float centerY = (B + T) * 0.5f;
            float approxL = centerX - neededRectWidth * 0.5f;
            float approxR = centerX + neededRectWidth * 0.5f;
            float approxB = centerY - neededRectHeight * 0.5f;
            float approxT = centerY + neededRectHeight * 0.5f;
            float L_lb = approxL + pivotOff.x;
            float R_lb = approxR + pivotOff.x;
            float B_lb = approxB + pivotOff.y;
            float T_lb = approxT + pivotOff.y;

            offMin.x = L_lb - p.x * rt.anchorMin.x;
            offMax.x = R_lb - p.x * rt.anchorMax.x;
            offMin.y = B_lb - p.y * rt.anchorMin.y;
            offMax.y = T_lb - p.y * rt.anchorMax.y;

            for (int iter = 0; iter < 3; iter++)
            {
                rt.offsetMin = offMin;
                rt.offsetMax = offMax;
                GetEdgesInParent(rt, parent, out float actualL, out float actualR, out float actualB, out float actualT);
                float errorX = (actualL - L) + (actualR - R);
                float errorY = (actualB - B) + (actualT - T);
                offMin.x -= errorX * 0.5f;
                offMax.x -= errorX * 0.5f;
                offMin.y -= errorY * 0.5f;
                offMax.y -= errorY * 0.5f;
            }

            offMin.x = RoundToPrecision(offMin.x);
            offMin.y = RoundToPrecision(offMin.y);
            offMax.x = RoundToPrecision(offMax.x);
            offMax.y = RoundToPrecision(offMax.y);

            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        #region GroupSelection_New_Impl
        private static bool TryBuildGroupSelection(RectTransform active, out GroupSelectionState state)
        {
            state = null;
            if (active == null || active.parent == null) return false;
            var parent = active.parent as RectTransform;
            if (parent == null) return false;

            var transforms = Selection.transforms;
            if (transforms == null || transforms.Length <= 1) return false;

            var st = new GroupSelectionState
            {
                parent = parent,
                active = active
            };

            for (int i = 0; i < transforms.Length; i++)
            {
                var rt = transforms[i].GetComponent<RectTransform>();
                if (rt == null) continue;
                if (rt.parent != parent) return false;
                if (st.memberSet.Add(rt))
                    st.members.Add(rt);
            }

            if (st.members.Count <= 1) return false;

            Vector3 uWorld = active.TransformVector(Vector3.right);
            Vector3 vWorld = active.TransformVector(Vector3.up);

            if (Vector3.Dot(uWorld, active.transform.right) < 0f) uWorld = -uWorld;
            if (Vector3.Dot(vWorld, active.transform.up) < 0f) vWorld = -vWorld;

            Vector3 uParent3 = parent.InverseTransformVector(uWorld);
            Vector3 vParent3 = parent.InverseTransformVector(vWorld);
            st.uAxisParent = new Vector2(uParent3.x, uParent3.y);
            st.vAxisParent = new Vector2(vParent3.x, vParent3.y);
            st.uLen = st.uAxisParent.magnitude;
            st.vLen = st.vAxisParent.magnitude;
            if (st.uLen < 1e-6f || st.vLen < 1e-6f) return false;
            st.uNorm = st.uAxisParent / st.uLen;
            st.vNorm = st.vAxisParent / st.vLen;

            float minU = float.PositiveInfinity, maxU = float.NegativeInfinity;
            float minV = float.PositiveInfinity, maxV = float.NegativeInfinity;
            var wc = new Vector3[4];

            for (int i = 0; i < st.members.Count; i++)
            {
                var m = st.members[i];
                if (m == null) continue;
                m.GetWorldCorners(wc);
                for (int k = 0; k < 4; k++)
                {
                    Vector3 p3 = parent.InverseTransformPoint(wc[k]);
                    Vector2 p = new Vector2(p3.x, p3.y);
                    float u = Vector2.Dot(p, st.uNorm);
                    float v = Vector2.Dot(p, st.vNorm);
                    if (u < minU) minU = u;
                    if (u > maxU) maxU = u;
                    if (v < minV) minV = v;
                    if (v > maxV) maxV = v;
                }
            }

            st.minU = minU; st.maxU = maxU;
            st.minV = minV; st.maxV = maxV;

            st.cornersParent[0] = st.uNorm * minU + st.vNorm * minV;
            st.cornersParent[1] = st.uNorm * minU + st.vNorm * maxV;
            st.cornersParent[2] = st.uNorm * maxU + st.vNorm * maxV;
            st.cornersParent[3] = st.uNorm * maxU + st.vNorm * minV;

            float L = float.PositiveInfinity, R = float.NegativeInfinity;
            float B = float.PositiveInfinity, T = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                var c = st.cornersParent[i];
                if (c.x < L) L = c.x;
                if (c.x > R) R = c.x;
                if (c.y < B) B = c.y;
                if (c.y > T) T = c.y;
            }
            st.aabbL = L; st.aabbR = R; st.aabbB = B; st.aabbT = T;

            state = st;
            return true;
        }

        #endregion

        private static Vector2 GetGroupHandlePositionInParent(RectTransform parent)
        {
            if (parent == null) return Vector2.zero;
            Vector3 handleWorld = Tools.handlePosition;
            Vector3 handleLocal = parent.InverseTransformPoint(handleWorld);
            return new Vector2(handleLocal.x, handleLocal.y);
        }

        private static bool TryPickGroupHandle(
            GroupSelectionState sel,
            Vector2 guiPoint,
            out DragKind kind,
            out Edge edgeH,
            out Edge edgeV)
        {
            kind = DragKind.Body; edgeH = Edge.None; edgeV = Edge.None;
            if (sel == null || sel.parent == null) return false;

            Vector2 gBL = HandleUtility.WorldToGUIPoint(sel.parent.TransformPoint(new Vector3(sel.cornersParent[0].x, sel.cornersParent[0].y, 0f)));
            Vector2 gTL = HandleUtility.WorldToGUIPoint(sel.parent.TransformPoint(new Vector3(sel.cornersParent[1].x, sel.cornersParent[1].y, 0f)));
            Vector2 gTR = HandleUtility.WorldToGUIPoint(sel.parent.TransformPoint(new Vector3(sel.cornersParent[2].x, sel.cornersParent[2].y, 0f)));
            Vector2 gBR = HandleUtility.WorldToGUIPoint(sel.parent.TransformPoint(new Vector3(sel.cornersParent[3].x, sel.cornersParent[3].y, 0f)));

            float pickPx = 5f * EditorGUIUtility.pixelsPerPoint;
            float dLeft = DistancePointToSegment(guiPoint, gBL, gTL);
            float dRight = DistancePointToSegment(guiPoint, gBR, gTR);
            float dBottom = DistancePointToSegment(guiPoint, gBL, gBR);
            float dTop = DistancePointToSegment(guiPoint, gTL, gTR);
            bool nearL = dLeft <= pickPx;
            bool nearR = dRight <= pickPx;
            bool nearB = dBottom <= pickPx;
            bool nearT = dTop <= pickPx;

            if ((nearL || nearR) && (nearB || nearT))
            {
                kind = DragKind.Corner;
                edgeH = nearL ? Edge.Left : Edge.Right;
                edgeV = nearB ? Edge.Bottom : Edge.Top;
                return true;
            }

            if ((nearL || nearR) ^ (nearB || nearT))
            {
                kind = DragKind.Edge;
                edgeH = nearL ? Edge.Left : (nearR ? Edge.Right : Edge.None);
                edgeV = nearB ? Edge.Bottom : (nearT ? Edge.Top : Edge.None);
                return true;
            }

            if (IsPointInConvexQuad(guiPoint, gBL, gBR, gTR, gTL))
            {
                kind = DragKind.Body;
                return true;
            }

            return false;
        }

        private static bool IsPointInConvexQuad(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
            bool s1 = Cross(b - a, p - a) >= 0f;
            bool s2 = Cross(c - b, p - b) >= 0f;
            bool s3 = Cross(d - c, p - c) >= 0f;
            bool s4 = Cross(a - d, p - d) >= 0f;
            return (s1 == s2) && (s2 == s3) && (s3 == s4);
        }

        private static void BeginGroupDrag(GroupSelectionState sel, Vector2 mouseParent, DragKind kind, Edge eh, Edge ev)
        {
            if (sel == null || sel.parent == null || sel.members == null || sel.members.Count <= 1)
                return;

            isDragging = true;
            isGroupDragging = true;
            dragKind = kind; dragEdgeH = eh; dragEdgeV = ev;
            axisLock = (Tools.current == Tool.Move) ? pendingAxisLock : AxisLock.None;
            dragStartMouseParent = mouseParent;
            groupDragStart = sel;

            var origin = ComputeGlobalOriginInParent(sel.parent, sel.active != null ? sel.active : sel.members[0]);
            dragOriginParentX = origin.x;
            dragOriginParentY = origin.y;

            groupMemberStart.Clear();
            for (int i = 0; i < sel.members.Count; i++)
            {
                var m = sel.members[i];
                if (m == null) continue;

                var st = new GroupMemberStartState
                {
                    local = new ProportionalChildLocalState
                    {
                        anchoredPosition = m.anchoredPosition,
                        sizeDelta = m.sizeDelta,
                        offsetMin = m.offsetMin,
                        offsetMax = m.offsetMax,
                        anchorMin = m.anchorMin,
                        anchorMax = m.anchorMax
                    },
                    pivotParent = sel.parent.InverseTransformPoint(m.TransformPoint(Vector3.zero)),
                    basisUParent = sel.parent.InverseTransformVector(m.TransformVector(Vector3.right)),
                    basisVParent = sel.parent.InverseTransformVector(m.TransformVector(Vector3.up))
                };

                groupMemberStart[m] = st;
                Undo.RegisterCompleteObjectUndo(m, "Rect Snap Begin (Group)");
            }
        }

        private static void EndGroupDrag()
        {
            isGroupDragging = false;
            groupDragStart = null;
            groupMemberStart.Clear();
        }

        private static void LiveGroupDrag(Vector2 mouseParent)
        {
            if (groupDragStart == null || groupDragStart.parent == null) return;

            switch (dragKind)
            {
                case DragKind.Body:
                    LiveGroupMove(mouseParent);
                    break;
                case DragKind.Edge:
                    LiveGroupEdge(mouseParent);
                    break;
                case DragKind.Corner:
                    LiveGroupCorner(mouseParent);
                    break;
            }
        }

        private static void LiveGroupMove(Vector2 mouseParent)
        {
            var sel = groupDragStart;
            Vector2 delta = mouseParent - dragStartMouseParent;

            Vector2 contCenter = sel.aabbCenter + delta;
            if (axisLock == AxisLock.X) contCenter.y = sel.aabbCenter.y;
            else if (axisLock == AxisLock.Y) contCenter.x = sel.aabbCenter.x;

            Vector2 origin = new Vector2(dragOriginParentX, dragOriginParentY);
            float smallStep = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 offBase = GetSnapOffsetInParent(smallStep);

            float halfW = (sel.aabbR - sel.aabbL) * 0.5f;
            float halfH = (sel.aabbT - sel.aabbB) * 0.5f;

            float targetX = sel.aabbCenter.x;
            if (axisLock != AxisLock.Y)
            {
                targetX = ComputeSnappedCenter(
                    contCenter.x,
                    sel.aabbCenter.x,
                    sel.aabbL,
                    sel.aabbR,
                    halfW,
                    smallStep,
                    origin.x,
                    offBase.x,
                    sel.parent,
                    true,
                    CanvasSnapThreshold,
                    null);
            }

            float targetY = sel.aabbCenter.y;
            if (axisLock != AxisLock.X)
            {
                targetY = ComputeSnappedCenter(
                    contCenter.y,
                    sel.aabbCenter.y,
                    sel.aabbB,
                    sel.aabbT,
                    halfH,
                    smallStep,
                    origin.y,
                    offBase.y,
                    sel.parent,
                    false,
                    CanvasSnapThreshold,
                    null);
            }

            Vector2 t = new Vector2(targetX - sel.aabbCenter.x, targetY - sel.aabbCenter.y);
            ApplyGroupTranslationToMembers(t);
        }

        private static void LiveGroupEdge(Vector2 mouseParent)
        {
            var sel = groupDragStart;
            Vector2 delta = mouseParent - dragStartMouseParent;

            float du = Vector2.Dot(delta, sel.uNorm);
            float dv = Vector2.Dot(delta, sel.vNorm);

            float smallStep = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 offBase = GetSnapOffsetInParent(smallStep);
            Vector2 gridOrigin = new Vector2(dragOriginParentX, dragOriginParentY);

            float newMinU = sel.minU, newMaxU = sel.maxU;
            float newMinV = sel.minV, newMaxV = sel.maxV;

            if (dragEdgeH == Edge.Left || dragEdgeH == Edge.Right)
            {
                Vector2 c0 = (dragEdgeH == Edge.Left) ? sel.cornersParent[0] : sel.cornersParent[3];
                Vector2 c1 = (dragEdgeH == Edge.Left) ? sel.cornersParent[1] : sel.cornersParent[2];

                float duSnapped = SnapEdgeDeltaLocalFromTwoCorners(
                    du,
                    c0,
                    c1,
                    sel.uNorm,
                    smallStep,
                    gridOrigin,
                    offBase,
                    sel.parent,
                    null);

                if (dragEdgeH == Edge.Left)
                {
                    newMinU = sel.minU + duSnapped;
                    if (dragSymmetric) newMaxU = sel.maxU - duSnapped;
                }
                else
                {
                    newMaxU = sel.maxU + duSnapped;
                    if (dragSymmetric) newMinU = sel.minU - duSnapped;
                }
            }
            else if (dragEdgeV == Edge.Bottom || dragEdgeV == Edge.Top)
            {
                Vector2 c0 = (dragEdgeV == Edge.Bottom) ? sel.cornersParent[0] : sel.cornersParent[1];
                Vector2 c1 = (dragEdgeV == Edge.Bottom) ? sel.cornersParent[3] : sel.cornersParent[2];

                float dvSnapped = SnapEdgeDeltaLocalFromTwoCorners(
                    dv,
                    c0,
                    c1,
                    sel.vNorm,
                    smallStep,
                    gridOrigin,
                    offBase,
                    sel.parent,
                    null);

                if (dragEdgeV == Edge.Bottom)
                {
                    newMinV = sel.minV + dvSnapped;
                    if (dragSymmetric) newMaxV = sel.maxV - dvSnapped;
                }
                else
                {
                    newMaxV = sel.maxV + dvSnapped;
                    if (dragSymmetric) newMinV = sel.minV - dvSnapped;
                }
            }

            if (newMaxU - newMinU < MIN_THICKNESS) newMaxU = newMinU + MIN_THICKNESS;
            if (newMaxV - newMinV < MIN_THICKNESS) newMaxV = newMinV + MIN_THICKNESS;

            ApplyGroupUvRemapToMembers(sel, newMinU, newMaxU, newMinV, newMaxV);
        }

        private static void LiveGroupCorner(Vector2 mouseParent)
        {
            var sel = groupDragStart;
            Vector2 delta = mouseParent - dragStartMouseParent;
            float du = Vector2.Dot(delta, sel.uNorm);
            float dv = Vector2.Dot(delta, sel.vNorm);

            float smallStep = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 offBase = GetSnapOffsetInParent(smallStep);
            Vector2 gridOrigin = new Vector2(dragOriginParentX, dragOriginParentY);

            Vector2 cornerStart =
                (dragEdgeH == Edge.Left && dragEdgeV == Edge.Bottom) ? sel.cornersParent[0] :
                (dragEdgeH == Edge.Left && dragEdgeV == Edge.Top) ? sel.cornersParent[1] :
                (dragEdgeH == Edge.Right && dragEdgeV == Edge.Top) ? sel.cornersParent[2] :
                sel.cornersParent[3];

            Vector2 snapped = SnapCornerDeltaLocal(
                new Vector2(du, dv),
                cornerStart,
                sel.uNorm,
                sel.vNorm,
                smallStep,
                gridOrigin,
                offBase,
                sel.parent,
                null,
                SnapAxis.Auto);

            du = snapped.x;
            dv = snapped.y;

            float newMinU = sel.minU, newMaxU = sel.maxU;
            float newMinV = sel.minV, newMaxV = sel.maxV;

            if (dragEdgeH == Edge.Left)
            {
                newMinU = sel.minU + du;
                if (dragSymmetric) newMaxU = sel.maxU - du;
            }
            else if (dragEdgeH == Edge.Right)
            {
                newMaxU = sel.maxU + du;
                if (dragSymmetric) newMinU = sel.minU - du;
            }

            if (dragEdgeV == Edge.Bottom)
            {
                newMinV = sel.minV + dv;
                if (dragSymmetric) newMaxV = sel.maxV - dv;
            }
            else if (dragEdgeV == Edge.Top)
            {
                newMaxV = sel.maxV + dv;
                if (dragSymmetric) newMinV = sel.minV - dv;
            }

            if (dragKeepAspect)
            {
                float startW = Mathf.Max(1e-6f, sel.maxU - sel.minU);
                float startH = Mathf.Max(1e-6f, sel.maxV - sel.minV);
                float aspect = startW / startH;
                float w = Mathf.Max(MIN_THICKNESS, newMaxU - newMinU);
                float h = Mathf.Max(MIN_THICKNESS, newMaxV - newMinV);

                float relW = Mathf.Abs((w - startW) / startW);
                float relH = Mathf.Abs((h - startH) / startH);

                if (relW >= relH)
                {
                    h = Mathf.Max(MIN_THICKNESS, w / aspect);
                    if (dragEdgeV == Edge.Bottom) newMinV = newMaxV - h;
                    else newMaxV = newMinV + h;
                }
                else
                {
                    w = Mathf.Max(MIN_THICKNESS, h * aspect);
                    if (dragEdgeH == Edge.Left) newMinU = newMaxU - w;
                    else newMaxU = newMinU + w;
                }
            }

            if (newMaxU - newMinU < MIN_THICKNESS) newMaxU = newMinU + MIN_THICKNESS;
            if (newMaxV - newMinV < MIN_THICKNESS) newMaxV = newMinV + MIN_THICKNESS;

            ApplyGroupUvRemapToMembers(sel, newMinU, newMaxU, newMinV, newMaxV);
        }

        private static void ApplyGroupTranslationToMembers(Vector2 t)
        {
            if (groupDragStart == null) return;

            foreach (var kv in groupMemberStart)
            {
                var m = kv.Key;
                if (m == null) continue;
                var st = kv.Value.local;

                bool stretchX = Mathf.Abs(st.anchorMin.x - st.anchorMax.x) > 0.000001f;
                bool stretchY = Mathf.Abs(st.anchorMin.y - st.anchorMax.y) > 0.000001f;

                if (!stretchX || !stretchY)
                {
                    Vector2 ap = m.anchoredPosition;
                    if (!stretchX) ap.x = RoundToPrecision(st.anchoredPosition.x + t.x);
                    if (!stretchY) ap.y = RoundToPrecision(st.anchoredPosition.y + t.y);
                    m.anchoredPosition = ap;
                }

                if (stretchX || stretchY)
                {
                    Vector2 om = m.offsetMin;
                    Vector2 ox = m.offsetMax;
                    if (stretchX)
                    {
                        om.x = RoundToPrecision(st.offsetMin.x + t.x);
                        ox.x = RoundToPrecision(st.offsetMax.x + t.x);
                    }
                    if (stretchY)
                    {
                        om.y = RoundToPrecision(st.offsetMin.y + t.y);
                        ox.y = RoundToPrecision(st.offsetMax.y + t.y);
                    }
                    m.offsetMin = om;
                    m.offsetMax = ox;
                }
            }
        }

        private static void ApplyGroupUvRemapToMembers(GroupSelectionState startSel, float newMinU, float newMaxU, float newMinV, float newMaxV)
        {
            if (startSel == null || startSel.parent == null) return;

            float startW = Mathf.Max(1e-6f, startSel.maxU - startSel.minU);
            float startH = Mathf.Max(1e-6f, startSel.maxV - startSel.minV);
            float su = Mathf.Max(0.0001f, (newMaxU - newMinU) / startW);
            float sv = Mathf.Max(0.0001f, (newMaxV - newMinV) / startH);

            Vector2 uNorm = startSel.uNorm;
            Vector2 vNorm = startSel.vNorm;

            foreach (var kv in groupMemberStart)
            {
                var m = kv.Key;
                if (m == null) continue;
                var ms = kv.Value;

                Vector2 p0 = ms.pivotParent;
                float u0 = Vector2.Dot(p0, uNorm);
                float v0 = Vector2.Dot(p0, vNorm);
                float u1 = newMinU + (u0 - startSel.minU) * su;
                float v1 = newMinV + (v0 - startSel.minV) * sv;
                Vector2 p1 = uNorm * u1 + vNorm * v1;
                Vector2 dp = p1 - p0;

                float scaleX = 1f;
                float scaleY = 1f;
                if (ms.basisUParent.sqrMagnitude > 1e-8f)
                {
                    Vector2 v = ms.basisUParent;
                    float vu = Vector2.Dot(v, uNorm);
                    float vv = Vector2.Dot(v, vNorm);
                    Vector2 vNew = uNorm * (vu * su) + vNorm * (vv * sv);
                    scaleX = vNew.magnitude / v.magnitude;
                }
                if (ms.basisVParent.sqrMagnitude > 1e-8f)
                {
                    Vector2 v = ms.basisVParent;
                    float vu = Vector2.Dot(v, uNorm);
                    float vv = Vector2.Dot(v, vNorm);
                    Vector2 vNew = uNorm * (vu * su) + vNorm * (vv * sv);
                    scaleY = vNew.magnitude / v.magnitude;
                }

                var st = ms.local;
                bool stretchX = Mathf.Abs(st.anchorMin.x - st.anchorMax.x) > 0.000001f;
                bool stretchY = Mathf.Abs(st.anchorMin.y - st.anchorMax.y) > 0.000001f;

                if (!stretchX || !stretchY)
                {
                    Vector2 ap = m.anchoredPosition;
                    if (!stretchX) ap.x = RoundToPrecision(st.anchoredPosition.x + dp.x);
                    if (!stretchY) ap.y = RoundToPrecision(st.anchoredPosition.y + dp.y);
                    m.anchoredPosition = ap;
                }

                if (!stretchX)
                {
                    Vector2 sd = m.sizeDelta;
                    sd.x = RoundToPrecision(st.sizeDelta.x * scaleX);
                    m.sizeDelta = sd;
                }
                if (!stretchY)
                {
                    Vector2 sd = m.sizeDelta;
                    sd.y = RoundToPrecision(st.sizeDelta.y * scaleY);
                    m.sizeDelta = sd;
                }

                if (stretchX || stretchY)
                {
                    Vector2 om = m.offsetMin;
                    Vector2 ox = m.offsetMax;
                    if (stretchX)
                    {
                        float center = ((st.offsetMin.x + st.offsetMax.x) * 0.5f) + dp.x;
                        float width = (st.offsetMax.x - st.offsetMin.x) * scaleX;
                        om.x = RoundToPrecision(center - width * 0.5f);
                        ox.x = RoundToPrecision(center + width * 0.5f);
                    }
                    if (stretchY)
                    {
                        float center = ((st.offsetMin.y + st.offsetMax.y) * 0.5f) + dp.y;
                        float height = (st.offsetMax.y - st.offsetMin.y) * scaleY;
                        om.y = RoundToPrecision(center - height * 0.5f);
                        ox.y = RoundToPrecision(center + height * 0.5f);
                    }
                    m.offsetMin = om;
                    m.offsetMax = ox;
                }
            }
        }

        private static bool TryPickHandle(RectTransform rt, Vector2 guiPoint, out DragKind kind, out Edge edgeH, out Edge edgeV)
        {
            kind = DragKind.Body; edgeH = Edge.None; edgeV = Edge.None;

            var ray = HandleUtility.GUIPointToWorldRay(guiPoint);
            var plane = new Plane(rt.transform.forward, rt.transform.position);
            if (!plane.Raycast(ray, out float hit)) return false;
            Vector3 world = ray.GetPoint(hit);
            Vector2 local = rt.InverseTransformPoint(world);

            Rect r = rt.rect;

            Vector3 wBL = rt.TransformPoint(new Vector3(r.xMin, r.yMin, 0f));
            Vector3 wTL = rt.TransformPoint(new Vector3(r.xMin, r.yMax, 0f));
            Vector3 wBR = rt.TransformPoint(new Vector3(r.xMax, r.yMin, 0f));
            Vector3 wTR = rt.TransformPoint(new Vector3(r.xMax, r.yMax, 0f));

            Vector2 gBL = HandleUtility.WorldToGUIPoint(wBL);
            Vector2 gTL = HandleUtility.WorldToGUIPoint(wTL);
            Vector2 gBR = HandleUtility.WorldToGUIPoint(wBR);
            Vector2 gTR = HandleUtility.WorldToGUIPoint(wTR);

            float pickPx = 5f * EditorGUIUtility.pixelsPerPoint;

            float dLeft = DistancePointToSegment(guiPoint, gBL, gTL);
            float dRight = DistancePointToSegment(guiPoint, gBR, gTR);
            float dBottom = DistancePointToSegment(guiPoint, gBL, gBR);
            float dTop = DistancePointToSegment(guiPoint, gTL, gTR);

            bool nearL = dLeft <= pickPx;
            bool nearR = dRight <= pickPx;
            bool nearB = dBottom <= pickPx;
            bool nearT = dTop <= pickPx;

            if ((nearL || nearR) && (nearB || nearT))
            {
                kind = DragKind.Corner;
                edgeH = nearL ? Edge.Left : Edge.Right;
                edgeV = nearB ? Edge.Bottom : Edge.Top;
                return true;
            }

            if ((nearL || nearR) ^ (nearB || nearT))
            {
                kind = DragKind.Edge;
                edgeH = nearL ? Edge.Left : (nearR ? Edge.Right : Edge.None);
                edgeV = nearB ? Edge.Bottom : (nearT ? Edge.Top : Edge.None);
                return true;
            }

            if (r.Contains(local))
            {
                kind = DragKind.Body;
                return true;
            }

            return false;
        }

        private static AxisLock DetectAxisLockOnMove(RectTransform rt, RectTransform parent, Vector2 mouseGUI, out bool hitAnyMoveZone)
        {
            hitAnyMoveZone = false;
            Vector3 worldPivot = rt.transform.position;
            Vector2 guiPivot = HandleUtility.WorldToGUIPoint(worldPivot);
            Vector3 xDirWorld = Tools.pivotRotation == PivotRotation.Global ? Vector3.right : rt.transform.right;
            Vector3 yDirWorld = Tools.pivotRotation == PivotRotation.Global ? Vector3.up : rt.transform.up;
            Vector2 xDirGui = HandleUtility.WorldToGUIPoint(worldPivot + xDirWorld) - guiPivot;
            Vector2 yDirGui = HandleUtility.WorldToGUIPoint(worldPivot + yDirWorld) - guiPivot;
            if (xDirGui.sqrMagnitude < 0.0001f || yDirGui.sqrMagnitude < 0.0001f)
                return AxisLock.None;
            xDirGui.Normalize();
            yDirGui.Normalize();
            const float arrowLengthPx = 96f;
            const float pickThicknessPx = 6f;
            const float planeSquareSize = 20f;

            Vector2 corner1 = guiPivot;
            Vector2 corner2 = guiPivot + xDirGui * planeSquareSize;
            Vector2 corner3 = guiPivot + xDirGui * planeSquareSize + yDirGui * planeSquareSize;
            Vector2 corner4 = guiPivot + yDirGui * planeSquareSize;

            if (IsPointInQuad(mouseGUI, corner1, corner2, corner3, corner4))
            {
                hitAnyMoveZone = true;
                return AxisLock.None;
            }

            Vector2 xA = guiPivot;
            Vector2 xB = guiPivot + xDirGui * arrowLengthPx;
            Vector2 yA = guiPivot;
            Vector2 yB = guiPivot + yDirGui * arrowLengthPx;
            float dx = DistancePointToSegment(mouseGUI, xA, xB);
            float dy = DistancePointToSegment(mouseGUI, yA, yB);
            bool hitX = dx <= pickThicknessPx;
            bool hitY = dy <= pickThicknessPx;
            if (hitX && hitY)
            {
                hitAnyMoveZone = true;
                return dx <= dy ? AxisLock.X : AxisLock.Y;
            }
            if (hitX)
            {
                hitAnyMoveZone = true;
                return AxisLock.X;
            }
            if (hitY)
            {
                hitAnyMoveZone = true;
                return AxisLock.Y;
            }
            return AxisLock.None;
        }

        private static AxisLock DetectAxisLockOnMoveGroup(RectTransform groupParent, Vector2 groupCenterParent, Vector2 mouseGUI, out bool hitAnyMoveZone)
        {
            hitAnyMoveZone = false;
            Vector3 worldPivot = groupParent.TransformPoint(new Vector3(groupCenterParent.x, groupCenterParent.y, 0f));
            Vector2 guiPivot = HandleUtility.WorldToGUIPoint(worldPivot);
            Vector3 xDirWorld = Tools.pivotRotation == PivotRotation.Global ? Vector3.right : groupParent.transform.right;
            Vector3 yDirWorld = Tools.pivotRotation == PivotRotation.Global ? Vector3.up : groupParent.transform.up;
            Vector2 xDirGui = HandleUtility.WorldToGUIPoint(worldPivot + xDirWorld) - guiPivot;
            Vector2 yDirGui = HandleUtility.WorldToGUIPoint(worldPivot + yDirWorld) - guiPivot;
            if (xDirGui.sqrMagnitude < 0.0001f || yDirGui.sqrMagnitude < 0.0001f)
                return AxisLock.None;
            xDirGui.Normalize();
            yDirGui.Normalize();
            const float arrowLengthPx = 96f;
            const float pickThicknessPx = 6f;
            const float planeSquareSize = 20f;

            Vector2 corner1 = guiPivot;
            Vector2 corner2 = guiPivot + xDirGui * planeSquareSize;
            Vector2 corner3 = guiPivot + xDirGui * planeSquareSize + yDirGui * planeSquareSize;
            Vector2 corner4 = guiPivot + yDirGui * planeSquareSize;

            if (IsPointInQuad(mouseGUI, corner1, corner2, corner3, corner4))
            {
                hitAnyMoveZone = true;
                return AxisLock.None;
            }

            Vector2 xA = guiPivot;
            Vector2 xB = guiPivot + xDirGui * arrowLengthPx;
            Vector2 yA = guiPivot;
            Vector2 yB = guiPivot + yDirGui * arrowLengthPx;
            float dx = DistancePointToSegment(mouseGUI, xA, xB);
            float dy = DistancePointToSegment(mouseGUI, yA, yB);
            bool hitX = dx <= pickThicknessPx;
            bool hitY = dy <= pickThicknessPx;
            if (hitX && hitY)
            {
                hitAnyMoveZone = true;
                return dx <= dy ? AxisLock.X : AxisLock.Y;
            }
            if (hitX)
            {
                hitAnyMoveZone = true;
                return AxisLock.X;
            }
            if (hitY)
            {
                hitAnyMoveZone = true;
                return AxisLock.Y;
            }
            return AxisLock.None;
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude + 1e-6f;
            float t = Vector2.Dot(point - a, ab) / denom;
            t = Mathf.Clamp01(t);
            Vector2 closest = a + ab * t;
            return Vector2.Distance(point, closest);
        }

        private static bool IsPointInQuad(Vector2 point, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            Rect bounds = GetBounds(p1, p2, p3, p4);
            return bounds.Contains(point);
        }

        private static void LiveSnapBody(RectTransform rt, RectTransform parent, Vector2 mouseParent)
        {
            Vector2 delta = mouseParent - dragStartMouseParent;
            Vector2 contCenter = startCenter + delta;
            if (axisLock == AxisLock.X) contCenter.y = startCenter.y;
            else if (axisLock == AxisLock.Y) contCenter.x = startCenter.x;

            Vector2 origin = new Vector2(dragOriginParentX, dragOriginParentY);
            float smallStep = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 offBase = GetSnapOffsetInParent(smallStep);

            float halfW = (startR - startL) * 0.5f;
            float halfH = (startT - startB) * 0.5f;

            float targetX = startCenter.x;
            if (axisLock != AxisLock.Y)
            {
                targetX = ComputeSnappedCenter(
                    contCenter.x,
                    startCenter.x,
                    startL,
                    startR,
                    halfW,
                    smallStep,
                    origin.x,
                    offBase.x,
                    parent,
                    true,
                    CanvasSnapThreshold,
                    rt);
            }

            float targetY = startCenter.y;
            if (axisLock != AxisLock.X)
            {
                targetY = ComputeSnappedCenter(
                    contCenter.y,
                    startCenter.y,
                    startB,
                    startT,
                    halfH,
                    smallStep,
                    origin.y,
                    offBase.y,
                    parent,
                    false,
                    CanvasSnapThreshold,
                    rt);
            }

            Vector2 deltaFinal = new Vector2(targetX - startCenter.x, targetY - startCenter.y);
            Vector2 newPos = startAnchoredPosition + deltaFinal;
            rt.anchoredPosition = new Vector2(RoundToPrecision(newPos.x), RoundToPrecision(newPos.y));
        }

        private static void LiveSnapEdge(RectTransform rt, RectTransform parent, Vector2 mouseParent)
        {
            Vector2 deltaParent = mouseParent - dragStartMouseParent;

            Edge effectiveEdgeH = dragEdgeH;
            Edge effectiveEdgeV = dragEdgeV;
            if (startFlipX)
            {
                if (dragEdgeH == Edge.Left) effectiveEdgeH = Edge.Right;
                else if (dragEdgeH == Edge.Right) effectiveEdgeH = Edge.Left;
            }
            if (startFlipY)
            {
                if (dragEdgeV == Edge.Bottom) effectiveEdgeV = Edge.Top;
                else if (dragEdgeV == Edge.Top) effectiveEdgeV = Edge.Bottom;
            }

            if (!TryDecomposeDeltaParentToLocal(deltaParent, startBasisUParent, startBasisVParent, out float dxLocal, out float dyLocal))
                return;

            float smallStepNew = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 offBaseNew = GetSnapOffsetInParent(smallStepNew);
            Vector2 gridOrigin = new Vector2(dragOriginParentX, dragOriginParentY);

            Vector2 newSize = startSizeDelta;
            Vector2 newPos = startAnchoredPosition;

            if (effectiveEdgeH == Edge.Left || effectiveEdgeH == Edge.Right)
            {
                int signX = GetEdgeSign(effectiveEdgeH);
                float dxSnapped = SnapEdgeDeltaLocalFromTwoCorners(
                    dxLocal,
                    effectiveEdgeH,
                    Edge.None,
                    startBasisUParent,
                    smallStepNew,
                    gridOrigin,
                    offBaseNew,
                    parent,
                    rt);

                float dwWanted = dxSnapped * signX * (dragSymmetric ? 2f : 1f);
                float newW = Mathf.Max(MIN_THICKNESS, startLocalWidth + dwWanted);
                float dwActual = newW - startLocalWidth;

                newSize.x = RoundToPrecision(newSize.x + dwActual);

                float pivotShiftLocalX;
                if (dragSymmetric)
                    pivotShiftLocalX = (startPivot01.x - 0.5f) * dwActual;
                else
                    pivotShiftLocalX = (signX > 0) ? (startPivot01.x * dwActual) : (-(1f - startPivot01.x) * dwActual);

                Vector2 shiftParent = startBasisUParent * pivotShiftLocalX;
                newPos.x = RoundToPrecision(newPos.x + shiftParent.x);
                newPos.y = RoundToPrecision(newPos.y + shiftParent.y);
            }
            else if (effectiveEdgeV == Edge.Bottom || effectiveEdgeV == Edge.Top)
            {
                int signY = GetEdgeSign(effectiveEdgeV);
                float dySnapped = SnapEdgeDeltaLocalFromTwoCorners(
                    dyLocal,
                    Edge.None,
                    effectiveEdgeV,
                    startBasisVParent,
                    smallStepNew,
                    gridOrigin,
                    offBaseNew,
                    parent,
                    rt);

                float dhWanted = dySnapped * signY * (dragSymmetric ? 2f : 1f);
                float newH = Mathf.Max(MIN_THICKNESS, startLocalHeight + dhWanted);
                float dhActual = newH - startLocalHeight;

                newSize.y = RoundToPrecision(newSize.y + dhActual);

                float pivotShiftLocalY;
                if (dragSymmetric)
                    pivotShiftLocalY = (startPivot01.y - 0.5f) * dhActual;
                else
                    pivotShiftLocalY = (signY > 0) ? (startPivot01.y * dhActual) : (-(1f - startPivot01.y) * dhActual);

                Vector2 shiftParent = startBasisVParent * pivotShiftLocalY;
                newPos.x = RoundToPrecision(newPos.x + shiftParent.x);
                newPos.y = RoundToPrecision(newPos.y + shiftParent.y);
            }

            rt.anchoredPosition = newPos;
            rt.sizeDelta = newSize;

            if (ProportionalChildrenEnabled && proportionalChildren.Count > 0)
            {
                ApplyProportionalChildrenLocal(rt);
            }
        }

        private static void LiveSnapCorner(RectTransform rt, RectTransform parent, Vector2 mouseParent)
        {
            Vector2 deltaParent = mouseParent - dragStartMouseParent;

            Edge effectiveEdgeH = dragEdgeH;
            Edge effectiveEdgeV = dragEdgeV;
            if (startFlipX)
            {
                if (dragEdgeH == Edge.Left) effectiveEdgeH = Edge.Right;
                else if (dragEdgeH == Edge.Right) effectiveEdgeH = Edge.Left;
            }
            if (startFlipY)
            {
                if (dragEdgeV == Edge.Bottom) effectiveEdgeV = Edge.Top;
                else if (dragEdgeV == Edge.Top) effectiveEdgeV = Edge.Bottom;
            }

            if (!TryDecomposeDeltaParentToLocal(deltaParent, startBasisUParent, startBasisVParent, out float dxLocal, out float dyLocal))
                return;

            float smallStepNew = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 offBaseNew = GetSnapOffsetInParent(smallStepNew);
            Vector2 gridOrigin = new Vector2(dragOriginParentX, dragOriginParentY);

            Vector2 newSize = startSizeDelta;
            Vector2 newPos = startAnchoredPosition;

            int signX = GetEdgeSign(effectiveEdgeH);
            int signY = GetEdgeSign(effectiveEdgeV);

            Vector2 cornerStart = GetStartCornerInParent(effectiveEdgeH, effectiveEdgeV);
            Vector2 deltaLocal = new Vector2(dxLocal, dyLocal);
            Vector2 deltaLocalSnapped = SnapCornerDeltaLocal(deltaLocal, cornerStart, startBasisUParent, startBasisVParent, smallStepNew, gridOrigin, offBaseNew, parent, rt, SnapAxis.Auto);

            float dwWanted = deltaLocalSnapped.x * signX * (dragSymmetric ? 2f : 1f);
            float dhWanted = deltaLocalSnapped.y * signY * (dragSymmetric ? 2f : 1f);

            float newW = Mathf.Max(MIN_THICKNESS, startLocalWidth + dwWanted);
            float newH = Mathf.Max(MIN_THICKNESS, startLocalHeight + dhWanted);

            if (dragKeepAspect && startLocalAspectRatio > 0.0001f)
            {
                float relW = Mathf.Abs((newW - startLocalWidth) / Mathf.Max(0.0001f, startLocalWidth));
                float relH = Mathf.Abs((newH - startLocalHeight) / Mathf.Max(0.0001f, startLocalHeight));
                if (relW >= relH)
                {
                    newH = Mathf.Max(MIN_THICKNESS, newW / startLocalAspectRatio);
                }
                else
                {
                    newW = Mathf.Max(MIN_THICKNESS, newH * startLocalAspectRatio);
                }
            }

            float dwActual = newW - startLocalWidth;
            float dhActual = newH - startLocalHeight;

            newSize.x = RoundToPrecision(newSize.x + dwActual);
            newSize.y = RoundToPrecision(newSize.y + dhActual);

            float pivotShiftLocalX;
            float pivotShiftLocalY;
            if (dragSymmetric)
            {
                pivotShiftLocalX = (startPivot01.x - 0.5f) * dwActual;
                pivotShiftLocalY = (startPivot01.y - 0.5f) * dhActual;
            }
            else
            {
                pivotShiftLocalX = (signX > 0) ? (startPivot01.x * dwActual) : (-(1f - startPivot01.x) * dwActual);
                pivotShiftLocalY = (signY > 0) ? (startPivot01.y * dhActual) : (-(1f - startPivot01.y) * dhActual);
            }

            Vector2 shiftParent = startBasisUParent * pivotShiftLocalX + startBasisVParent * pivotShiftLocalY;
            newPos.x = RoundToPrecision(newPos.x + shiftParent.x);
            newPos.y = RoundToPrecision(newPos.y + shiftParent.y);

            rt.anchoredPosition = newPos;
            rt.sizeDelta = newSize;

            if (ProportionalChildrenEnabled && proportionalChildren.Count > 0)
            {
                ApplyProportionalChildrenLocal(rt);
            }
        }

        private static void DrawGridIfNeeded(SceneView sv, Event e)
        {
            if (!ShowGrid) return;

            var canvas = AssignedCanvas;
            if (canvas == null) return;

            if (!canvas.gameObject.activeInHierarchy) return;

            if (SceneVisibilityManager.instance.IsHidden(canvas.gameObject)) return;

            var canvasRT = canvas.GetComponent<RectTransform>();
            if (canvasRT == null) return;

            Rect canvasRect = canvasRT.rect;
            float minX = canvasRect.xMin;
            float maxX = canvasRect.xMax;
            float minY = canvasRect.yMin;
            float maxY = canvasRect.yMax;

            Vector2 origin = ComputeGlobalOriginInParent(canvasRT, canvasRT);
            float largeStep = SnapStep;
            int divisor = Mathf.Max(1, SnapDivisor);
            float step = Mathf.Max(1f, largeStep / divisor);
            Vector2 offBase = GetSnapOffsetInParent(step);

            int pxSmall = Mathf.Clamp(Mathf.RoundToInt(DotSize), 1, 4);
            int pxLarge = pxSmall;
            float halfSmall = pxSmall * 0.5f;
            float halfLarge = halfSmall;
            Color baseC = DotColor;
            Color colorLarge = baseC;
            Color colorSmall = new Color(baseC.r, baseC.g, baseC.b, baseC.a * 0.33f);


            float ox = origin.x + offBase.x;
            float oy = origin.y + offBase.y;
            int ixMin = Mathf.FloorToInt((minX - ox) / step) - 1;
            int ixMax = Mathf.CeilToInt((maxX - ox) / step) + 1;
            int iyMin = Mathf.FloorToInt((minY - oy) / step) - 1;
            int iyMax = Mathf.CeilToInt((maxY - oy) / step) + 1;

            {
                var plane = new Plane(canvasRT.transform.forward, canvasRT.transform.position);
                Vector2[] guiCorners = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(sv.position.width, 0f),
                    new Vector2(0f, sv.position.height),
                    new Vector2(sv.position.width, sv.position.height)
                };
                bool anyHit = false;
                Vector2 visMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 visMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (int i = 0; i < guiCorners.Length; i++)
                {
                    var ray = HandleUtility.GUIPointToWorldRay(guiCorners[i]);
                    if (plane.Raycast(ray, out float hit))
                    {
                        Vector3 w = ray.GetPoint(hit);
                        Vector3 local3 = canvasRT.InverseTransformPoint(w);
                        var local = new Vector2(local3.x, local3.y);
                        visMin = Vector2.Min(visMin, local);
                        visMax = Vector2.Max(visMax, local);
                        anyHit = true;
                    }
                }
                if (anyHit)
                {
                    float vMinX = Mathf.Clamp(visMin.x, minX, maxX);
                    float vMaxX = Mathf.Clamp(visMax.x, minX, maxX);
                    float vMinY = Mathf.Clamp(visMin.y, minY, maxY);
                    float vMaxY = Mathf.Clamp(visMax.y, minY, maxY);
                    ixMin = Mathf.FloorToInt((vMinX - ox) / step) - 1;
                    ixMax = Mathf.CeilToInt((vMaxX - ox) / step) + 1;
                    iyMin = Mathf.FloorToInt((vMinY - oy) / step) - 1;
                    iyMax = Mathf.CeilToInt((vMaxY - oy) / step) + 1;
                }
            }

            const int MAX_SPAN = 5000;
            int spanX = Mathf.Max(0, ixMax - ixMin + 1);
            int spanY = Mathf.Max(0, iyMax - iyMin + 1);
            int adaptFactorX = spanX > MAX_SPAN ? Mathf.CeilToInt((float)spanX / MAX_SPAN) : 1;
            int adaptFactorY = spanY > MAX_SPAN ? Mathf.CeilToInt((float)spanY / MAX_SPAN) : 1;


            float cxLocal = (minX + maxX) * 0.5f;
            float cyLocal = (minY + maxY) * 0.5f;
            Vector3 gui0 = HandleUtility.WorldToGUIPoint(canvasRT.TransformPoint(new Vector3(cxLocal, cyLocal, 0f)));
            Vector3 guiX = HandleUtility.WorldToGUIPoint(canvasRT.TransformPoint(new Vector3(cxLocal + step, cyLocal, 0f)));
            Vector3 guiY = HandleUtility.WorldToGUIPoint(canvasRT.TransformPoint(new Vector3(cxLocal, cyLocal + step, 0f)));
            float dxPx = Mathf.Abs(guiX.x - gui0.x);
            float dyPx = Mathf.Abs(guiY.y - gui0.y);
            float minPixelSpacing = Mathf.Max(2f, pxSmall + 2f);
            int strideX = Mathf.Max(1, dxPx > 0.001f ? Mathf.CeilToInt(minPixelSpacing / dxPx) : 1);
            int strideY = Mathf.Max(1, dyPx > 0.001f ? Mathf.CeilToInt(minPixelSpacing / dyPx) : 1);

            if (adaptFactorX > 1) strideX *= adaptFactorX;
            if (adaptFactorY > 1) strideY *= adaptFactorY;

            long visibleSpanX = Mathf.Max(0, ixMax - ixMin + 1);
            long visibleSpanY = Mathf.Max(0, iyMax - iyMin + 1);
            long visibleTotal = visibleSpanX * visibleSpanY;
            int strideLOD = 1;
            bool hasEnoughDotsForSubdivs = true;
            if (visibleTotal > MaxDots)
            {
                float ratio = Mathf.Sqrt((float)visibleTotal / Mathf.Max(1f, (float)MaxDots));
                strideLOD = Mathf.Max(1, Mathf.CeilToInt(ratio));
                hasEnoughDotsForSubdivs = false;
            }
            strideX = Mathf.Max(strideX, strideLOD);
            strideY = Mathf.Max(strideY, strideLOD);

            int strideLargeX = strideX;
            { int r = strideLargeX % divisor; if (r < 0) r += divisor; if (r != 0) strideLargeX += (divisor - r); strideLargeX = Mathf.Max(strideLargeX, divisor); }
            int strideLargeY = strideY;
            { int r = strideLargeY % divisor; if (r < 0) r += divisor; if (r != 0) strideLargeY += (divisor - r); strideLargeY = Mathf.Max(strideLargeY, divisor); }

            int ixStartLarge = ixMin;
            { int r = ixStartLarge % divisor; if (r < 0) r += divisor; if (r != 0) ixStartLarge += (divisor - r); }
            int iyStartLarge = iyMin;
            { int r = iyStartLarge % divisor; if (r < 0) r += divisor; if (r != 0) iyStartLarge += (divisor - r); }

            Handles.BeginGUI();

            for (int ix = ixStartLarge; ix <= ixMax; ix += strideLargeX)
            {
                float gx = ox + ix * step;
                for (int iy = iyStartLarge; iy <= iyMax; iy += strideLargeY)
                {
                    float gy = oy + iy * step;
                    if (gx < minX || gx > maxX || gy < minY || gy > maxY) continue;
                    Vector3 world = canvasRT.TransformPoint(new Vector3(gx, gy, 0f));
                    Vector3 gui3 = HandleUtility.WorldToGUIPointWithDepth(world);
                    if (gui3.z <= 0f) continue;
                    var rect = new Rect(gui3.x - halfLarge, gui3.y - halfLarge, pxLarge, pxLarge);
                    EditorGUI.DrawRect(rect, colorLarge);
                }
            }

            bool drawSubdivs = hasEnoughDotsForSubdivs && (dxPx >= minPixelSpacing) && (dyPx >= minPixelSpacing);
            if (drawSubdivs)
            {
                for (int ix = ixStartLarge; ix <= ixMax; ix += strideLargeX)
                {
                    float gx = ox + ix * step;
                    for (int iy = iyMin; iy <= iyMax; iy += strideY)
                    {
                        int modY = iy % divisor; if (modY < 0) modY += divisor;
                        if (modY == 0) continue;
                        float gy = oy + iy * step;
                        if (gx < minX || gx > maxX || gy < minY || gy > maxY) continue;
                        Vector3 world = canvasRT.TransformPoint(new Vector3(gx, gy, 0f));
                        Vector3 gui3 = HandleUtility.WorldToGUIPointWithDepth(world);
                        if (gui3.z <= 0f) continue;
                        var rect = new Rect(gui3.x - halfSmall, gui3.y - halfSmall, pxSmall, pxSmall);
                        EditorGUI.DrawRect(rect, colorSmall);
                    }
                }

                for (int iy = iyStartLarge; iy <= iyMax; iy += strideLargeY)
                {
                    float gy = oy + iy * step;
                    for (int ix = ixMin; ix <= ixMax; ix += strideX)
                    {
                        int modX = ix % divisor; if (modX < 0) modX += divisor;
                        if (modX == 0) continue;
                        float gx = ox + ix * step;
                        if (gx < minX || gx > maxX || gy < minY || gy > maxY) continue;
                        Vector3 world = canvasRT.TransformPoint(new Vector3(gx, gy, 0f));
                        Vector3 gui3 = HandleUtility.WorldToGUIPointWithDepth(world);
                        if (gui3.z <= 0f) continue;
                        var rect = new Rect(gui3.x - halfSmall, gui3.y - halfSmall, pxSmall, pxSmall);
                        EditorGUI.DrawRect(rect, colorSmall);
                    }
                }
            }

            Handles.EndGUI();
        }

        private static Rect GetBounds(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float minX = Mathf.Min(p1.x, p2.x, p3.x, p4.x);
            float maxX = Mathf.Max(p1.x, p2.x, p3.x, p4.x);
            float minY = Mathf.Min(p1.y, p2.y, p3.y, p4.y);
            float maxY = Mathf.Max(p1.y, p2.y, p3.y, p4.y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static void UpdateResizeCursorIcon(SceneView sv, Event e)
        {
            if (sv == null || e == null) return;
            if (Tools.current != Tool.Rect) return;

            var currentEvent = Event.current;
            if (currentEvent == null) return;

            if (currentEvent.type != EventType.Repaint &&
                currentEvent.type != EventType.Layout &&
                currentEvent.type != EventType.MouseMove &&
                currentEvent.type != EventType.MouseDrag) return;

            MouseCursor cursorToShow = MouseCursor.Arrow;
            bool shouldOverride = false;

            if (isDragging)
            {
                Edge effectiveEdgeH = dragEdgeH;
                Edge effectiveEdgeV = dragEdgeV;
                if (TryGetSelectedRT(out var rtDrag))
                {
                    if (startFlipX)
                    {
                        if (dragEdgeH == Edge.Left) effectiveEdgeH = Edge.Right;
                        else if (dragEdgeH == Edge.Right) effectiveEdgeH = Edge.Left;
                    }
                    if (startFlipY)
                    {
                        if (dragEdgeV == Edge.Bottom) effectiveEdgeV = Edge.Top;
                        else if (dragEdgeV == Edge.Top) effectiveEdgeV = Edge.Bottom;
                    }
                }
                cursorToShow = GetCursorForHandle(rtDrag, dragKind, effectiveEdgeH, effectiveEdgeV);
                shouldOverride = true;
            }
            else
            {
                if (TryGetSelectedRT(out var rt))
                {
                    if (TryPickHandle(rt, currentEvent.mousePosition, out var kind, out var edgeH, out var edgeV))
                    {
                        Edge effectiveEdgeH = edgeH;
                        Edge effectiveEdgeV = edgeV;
                        bool flipX = Vector3.Dot(rt.TransformVector(Vector3.right), rt.transform.right) < 0f;
                        bool flipY = Vector3.Dot(rt.TransformVector(Vector3.up), rt.transform.up) < 0f;
                        if (flipX)
                        {
                            if (edgeH == Edge.Left) effectiveEdgeH = Edge.Right;
                            else if (edgeH == Edge.Right) effectiveEdgeH = Edge.Left;
                        }
                        if (flipY)
                        {
                            if (edgeV == Edge.Bottom) effectiveEdgeV = Edge.Top;
                            else if (edgeV == Edge.Top) effectiveEdgeV = Edge.Bottom;
                        }
                        cursorToShow = GetCursorForHandle(rt, kind, effectiveEdgeH, effectiveEdgeV);
                        shouldOverride = true;
                    }
                }
            }

            if (shouldOverride)
            {
                var cursorRect = new Rect(0, 0, sv.position.width, sv.position.height);

                Handles.BeginGUI();
                EditorGUIUtility.AddCursorRect(cursorRect, cursorToShow);
                Handles.EndGUI();
            }
        }

        private static MouseCursor GetCursorForHandle(RectTransform rt, DragKind kind, Edge edgeH, Edge edgeV)
        {
            if (kind == DragKind.Edge)
            {
                if (rt == null)
                {
                    if (edgeH == Edge.Left || edgeH == Edge.Right) return MouseCursor.ResizeHorizontal;
                    if (edgeV == Edge.Bottom || edgeV == Edge.Top) return MouseCursor.ResizeVertical;
                    return MouseCursor.Arrow;
                }

                Vector3 normalWorld = Vector3.zero;
                if (edgeH == Edge.Left) normalWorld = -rt.transform.right;
                else if (edgeH == Edge.Right) normalWorld = rt.transform.right;
                else if (edgeV == Edge.Bottom) normalWorld = -rt.transform.up;
                else if (edgeV == Edge.Top) normalWorld = rt.transform.up;

                if (normalWorld == Vector3.zero) return MouseCursor.Arrow;

                Vector2 g0 = HandleUtility.WorldToGUIPoint(rt.transform.position);
                Vector2 g1 = HandleUtility.WorldToGUIPoint(rt.transform.position + normalWorld);
                Vector2 d = g1 - g0;
                if (d.sqrMagnitude < 0.000001f) return MouseCursor.Arrow;
                d.Normalize();
                return (Mathf.Abs(d.x) > Mathf.Abs(d.y)) ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical;
            }
            if (kind == DragKind.Corner)
            {
                if (rt == null)
                {
                    bool isUpLeft = (edgeH == Edge.Left && edgeV == Edge.Top) || (edgeH == Edge.Right && edgeV == Edge.Bottom);
                    return isUpLeft ? MouseCursor.ResizeUpLeft : MouseCursor.ResizeUpRight;
                }

                int sx = (edgeH == Edge.Left) ? -1 : 1;
                int sy = (edgeV == Edge.Bottom) ? -1 : 1;
                Vector3 dirWorld = (rt.transform.right * sx) + (rt.transform.up * sy);
                if (dirWorld.sqrMagnitude < 0.000001f)
                {
                    bool isUpLeft = (edgeH == Edge.Left && edgeV == Edge.Top) || (edgeH == Edge.Right && edgeV == Edge.Bottom);
                    return isUpLeft ? MouseCursor.ResizeUpLeft : MouseCursor.ResizeUpRight;
                }

                Vector2 g0 = HandleUtility.WorldToGUIPoint(rt.transform.position);
                Vector2 g1 = HandleUtility.WorldToGUIPoint(rt.transform.position + dirWorld);
                Vector2 d = g1 - g0;
                if (d.sqrMagnitude < 0.000001f)
                {
                    bool isUpLeft = (edgeH == Edge.Left && edgeV == Edge.Top) || (edgeH == Edge.Right && edgeV == Edge.Bottom);
                    return isUpLeft ? MouseCursor.ResizeUpLeft : MouseCursor.ResizeUpRight;
                }

                return (d.x * d.y > 0f) ? MouseCursor.ResizeUpLeft : MouseCursor.ResizeUpRight;
            }
            return MouseCursor.Arrow;
        }

        private static Vector2 ComputeGlobalOriginInParent(RectTransform parent, RectTransform rtForFallback)
        {
            if (parent == null) return Vector2.zero;

            var canvas = AssignedCanvas;
            if (canvas != null)
            {
                var canvasRT = canvas.GetComponent<RectTransform>();
                if (canvasRT != null)
                {
                    Rect cRect = canvasRT.rect;
                    Vector2 pivotPoint = GetCanvasOriginPoint(CanvasOriginIndex, cRect);
                    Vector3 localPoint3 = new Vector3(pivotPoint.x, pivotPoint.y, 0f);
                    Vector3 worldPoint = canvasRT.TransformPoint(localPoint3);
                    Vector3 parentLocal = parent.InverseTransformPoint(worldPoint);
                    return new Vector2(parentLocal.x, parentLocal.y);
                }
            }

            return Vector2.zero;
        }

        private static void LiveSnapGroupBody(RectTransform parent, Vector2 mouseParent)
        {
            LiveGroupMove(mouseParent);
        }

        private static void LiveSnapGroupEdge(RectTransform parent, Vector2 mouseParent)
        {
            LiveGroupEdge(mouseParent);
        }

        private static void LiveSnapGroupCorner(RectTransform parent, Vector2 mouseParent)
        {
#if false
            Vector2 deltaParent = mouseParent - dragStartMouseParent;

            Edge effectiveEdgeH = dragEdgeH;
            Edge effectiveEdgeV = dragEdgeV;
            if (groupStartFlipX)
            {
                if (dragEdgeH == Edge.Left) effectiveEdgeH = Edge.Right;
                else if (dragEdgeH == Edge.Right) effectiveEdgeH = Edge.Left;
            }
            if (groupStartFlipY)
            {
                if (dragEdgeV == Edge.Bottom) effectiveEdgeV = Edge.Top;
                else if (dragEdgeV == Edge.Top) effectiveEdgeV = Edge.Bottom;
            }

            if (!TryDecomposeDeltaParentToLocal(deltaParent, groupStartBasisUParent, groupStartBasisVParent, out float dxLocal, out float dyLocal))
                return;

            float smallStepNew = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 offBaseNew = GetSnapOffsetInParent(smallStepNew);
            Vector2 gridOrigin = new Vector2(dragOriginParentX, dragOriginParentY);

            int signX = GetEdgeSign(effectiveEdgeH);
            int signY = GetEdgeSign(effectiveEdgeV);

            Vector2 cornerStart = GetGroupCornerInParent(effectiveEdgeH, effectiveEdgeV);
            Vector2 deltaLocal = new Vector2(dxLocal, dyLocal);
            Vector2 deltaLocalSnapped = SnapCornerDeltaLocal(deltaLocal, cornerStart, groupStartBasisUParent, groupStartBasisVParent, smallStepNew, gridOrigin, offBaseNew, parent, null, SnapAxis.Auto);

            float dwWanted = deltaLocalSnapped.x * signX * (dragSymmetric ? 2f : 1f);
            float dhWanted = deltaLocalSnapped.y * signY * (dragSymmetric ? 2f : 1f);

            float newW = Mathf.Max(MIN_THICKNESS, groupStartLocalWidth + dwWanted);
            float newH = Mathf.Max(MIN_THICKNESS, groupStartLocalHeight + dhWanted);

            if (dragKeepAspect && groupStartAspectRatio > 0.0001f)
            {
                float relW = Mathf.Abs((newW - groupStartLocalWidth) / Mathf.Max(0.0001f, groupStartLocalWidth));
                float relH = Mathf.Abs((newH - groupStartLocalHeight) / Mathf.Max(0.0001f, groupStartLocalHeight));
                if (relW >= relH)
                {
                    newH = Mathf.Max(MIN_THICKNESS, newW / groupStartAspectRatio);
                }
                else
                {
                    newW = Mathf.Max(MIN_THICKNESS, newH * groupStartAspectRatio);
                }
            }

            float dwActual = newW - groupStartLocalWidth;
            float dhActual = newH - groupStartLocalHeight;

            float newLocalWidth = Mathf.Max(MIN_THICKNESS, groupStartLocalWidth + dwActual);
            float newLocalHeight = Mathf.Max(MIN_THICKNESS, groupStartLocalHeight + dhActual);

            Vector2 groupCenterParent = groupStartObbCenterParent;

            float halfW = groupStartLocalWidth * 0.5f;
            float halfH = groupStartLocalHeight * 0.5f;
            float newHalfW = newLocalWidth * 0.5f;
            float newHalfH = newLocalHeight * 0.5f;

            bool moveLeft = (effectiveEdgeH == Edge.Left);
            bool moveRight = (effectiveEdgeH == Edge.Right);
            bool moveBottom = (effectiveEdgeV == Edge.Bottom);
            bool moveTop = (effectiveEdgeV == Edge.Top);

            Vector2[] localCornerOffsets = new Vector2[4];
            localCornerOffsets[0] = new Vector2(-halfW, -halfH);
            localCornerOffsets[1] = new Vector2(-halfW, halfH);
            localCornerOffsets[2] = new Vector2(halfW, halfH);
            localCornerOffsets[3] = new Vector2(halfW, -halfH);

            if (moveLeft && moveBottom)
            {
                localCornerOffsets[0] = new Vector2(-newHalfW, -newHalfH);
                if (dragSymmetric)
                {
                    localCornerOffsets[2] = new Vector2(newHalfW, newHalfH);
                }
            }
            else if (moveLeft && moveTop)
            {
                localCornerOffsets[1] = new Vector2(-newHalfW, newHalfH);
                if (dragSymmetric)
                {
                    localCornerOffsets[3] = new Vector2(newHalfW, -newHalfH);
                }
            }
            else if (moveRight && moveTop)
            {
                localCornerOffsets[2] = new Vector2(newHalfW, newHalfH);
                if (dragSymmetric)
                {
                    localCornerOffsets[0] = new Vector2(-newHalfW, -newHalfH);
                }
            }
            else if (moveRight && moveBottom)
            {
                localCornerOffsets[3] = new Vector2(newHalfW, -newHalfH);
                if (dragSymmetric)
                {
                    localCornerOffsets[1] = new Vector2(-newHalfW, newHalfH);
                }
            }

            float newL = float.PositiveInfinity;
            float newR = float.NegativeInfinity;
            float newB = float.PositiveInfinity;
            float newT = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector2 cornerParent = groupCenterParent + groupStartBasisUParent * localCornerOffsets[i].x + groupStartBasisVParent * localCornerOffsets[i].y;
                if (cornerParent.x < newL) newL = cornerParent.x;
                if (cornerParent.x > newR) newR = cornerParent.x;
                if (cornerParent.y < newB) newB = cornerParent.y;
                if (cornerParent.y > newT) newT = cornerParent.y;
            }

            if (newR - newL < MIN_THICKNESS)
            {
                float centerX = (newL + newR) * 0.5f;
                newL = centerX - MIN_THICKNESS * 0.5f;
                newR = centerX + MIN_THICKNESS * 0.5f;
            }
            if (newT - newB < MIN_THICKNESS)
            {
                float centerY = (newB + newT) * 0.5f;
                newB = centerY - MIN_THICKNESS * 0.5f;
                newT = centerY + MIN_THICKNESS * 0.5f;
            }

            if (ProportionalChildrenEnabled)
            {
                ApplyGroupResizeChildren(parent, newL, newR, newB, newT);
            }
            else
            {
                ApplyGroupRemap(parent, newL, newR, newB, newT);
            }
#endif
            LiveGroupCorner(mouseParent);
        }

        private static void ApplyGroupRemap(RectTransform parent, float newL, float newR, float newB, float newT)
        {
        }

        private static void ApplyGroupResizeChildren(RectTransform parent, float newL, float newR, float newB, float newT)
        {
        }

        private static void PrepareProportionalChildren(RectTransform parent)
        {
            proportionalChildren.Clear();
            proportionalChildrenStartLocal.Clear();

            if (parent == null) return;

            proportionalParentStartRect = parent.rect;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i) as RectTransform;
                if (child == null) continue;

                proportionalChildren.Add(child);
                proportionalChildrenStartLocal[child] = new ProportionalChildLocalState
                {
                    anchoredPosition = child.anchoredPosition,
                    sizeDelta = child.sizeDelta,
                    offsetMin = child.offsetMin,
                    offsetMax = child.offsetMax,
                    anchorMin = child.anchorMin,
                    anchorMax = child.anchorMax
                };
                Undo.RegisterCompleteObjectUndo(child, "Rect Snap Begin (Proportional)");
            }
        }

        private static void ApplyProportionalChildrenLocal(RectTransform parent)
        {
            if (parent == null || proportionalChildren.Count == 0) return;

            float srcW = Mathf.Max(0.0001f, proportionalParentStartRect.width);
            float srcH = Mathf.Max(0.0001f, proportionalParentStartRect.height);
            Rect newRect = parent.rect;
            float dstW = Mathf.Max(0.0001f, newRect.width);
            float dstH = Mathf.Max(0.0001f, newRect.height);

            float scaleX = dstW / srcW;
            float scaleY = dstH / srcH;

            for (int i = 0; i < proportionalChildren.Count; i++)
            {
                var child = proportionalChildren[i];
                if (child == null) continue;
                if (!proportionalChildrenStartLocal.TryGetValue(child, out var st)) continue;

                bool stretchX = Mathf.Abs(st.anchorMin.x - st.anchorMax.x) > 0.0001f;
                bool stretchY = Mathf.Abs(st.anchorMin.y - st.anchorMax.y) > 0.0001f;

                if (stretchX)
                {
                    Vector2 om = st.offsetMin;
                    Vector2 ox = st.offsetMax;
                    om.x = RoundToPrecision(om.x * scaleX);
                    ox.x = RoundToPrecision(ox.x * scaleX);
                    child.offsetMin = new Vector2(om.x, child.offsetMin.y);
                    child.offsetMax = new Vector2(ox.x, child.offsetMax.y);
                }
                else
                {
                    Vector3 uP3 = parent.InverseTransformVector(child.TransformVector(Vector3.right));
                    Vector3 vP3 = parent.InverseTransformVector(child.TransformVector(Vector3.up));
                    Vector2 u = new Vector2(uP3.x, uP3.y);
                    Vector2 v = new Vector2(vP3.x, vP3.y);
                    if (u.sqrMagnitude > 1e-8f) u.Normalize(); else u = Vector2.right;
                    if (v.sqrMagnitude > 1e-8f) v.Normalize(); else v = Vector2.up;
                    float sxLocal = scaleX * (u.x * u.x) + scaleY * (u.y * u.y);
                    float syLocal = scaleX * (v.x * v.x) + scaleY * (v.y * v.y);

                    Vector2 ap = st.anchoredPosition;
                    Vector2 sd = st.sizeDelta;
                    ap.x = RoundToPrecision(ap.x * scaleX);
                    sd.x = RoundToPrecision(sd.x * sxLocal);
                    child.anchoredPosition = new Vector2(ap.x, child.anchoredPosition.y);
                    child.sizeDelta = new Vector2(sd.x, child.sizeDelta.y);
                }

                if (stretchY)
                {
                    Vector2 om = child.offsetMin;
                    Vector2 ox = child.offsetMax;
                    om.y = RoundToPrecision(st.offsetMin.y * scaleY);
                    ox.y = RoundToPrecision(st.offsetMax.y * scaleY);
                    child.offsetMin = new Vector2(child.offsetMin.x, om.y);
                    child.offsetMax = new Vector2(child.offsetMax.x, ox.y);
                }
                else
                {
                    Vector3 uP3 = parent.InverseTransformVector(child.TransformVector(Vector3.right));
                    Vector3 vP3 = parent.InverseTransformVector(child.TransformVector(Vector3.up));
                    Vector2 u = new Vector2(uP3.x, uP3.y);
                    Vector2 v = new Vector2(vP3.x, vP3.y);
                    if (u.sqrMagnitude > 1e-8f) u.Normalize(); else u = Vector2.right;
                    if (v.sqrMagnitude > 1e-8f) v.Normalize(); else v = Vector2.up;
                    float sxLocal = scaleX * (u.x * u.x) + scaleY * (u.y * u.y);
                    float syLocal = scaleX * (v.x * v.x) + scaleY * (v.y * v.y);

                    Vector2 ap = child.anchoredPosition;
                    Vector2 sd = child.sizeDelta;
                    ap.y = RoundToPrecision(st.anchoredPosition.y * scaleY);
                    sd.y = RoundToPrecision(st.sizeDelta.y * syLocal);
                    child.anchoredPosition = new Vector2(child.anchoredPosition.x, ap.y);
                    child.sizeDelta = new Vector2(child.sizeDelta.x, sd.y);
                }
            }
        }

        private static void DrawGroupSelection(GroupSelectionState sel)
        {
            if (sel == null || sel.parent == null) return;

            Color grayOutline = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            Color greenOutline = new Color(0.4f, 1f, 0.4f, 0.5f);
            Color noFill = new Color(0f, 0f, 0f, 0f);

            Vector3 wBL = sel.parent.TransformPoint(new Vector3(sel.cornersParent[0].x, sel.cornersParent[0].y, 0f));
            Vector3 wTL = sel.parent.TransformPoint(new Vector3(sel.cornersParent[1].x, sel.cornersParent[1].y, 0f));
            Vector3 wTR = sel.parent.TransformPoint(new Vector3(sel.cornersParent[2].x, sel.cornersParent[2].y, 0f));
            Vector3 wBR = sel.parent.TransformPoint(new Vector3(sel.cornersParent[3].x, sel.cornersParent[3].y, 0f));
            Handles.DrawSolidRectangleWithOutline(new Vector3[] { wBL, wTL, wTR, wBR }, noFill, grayOutline);

            var wc = new Vector3[4];
            for (int i = 0; i < sel.members.Count; i++)
            {
                var member = sel.members[i];
                if (member == null) continue;
                member.GetWorldCorners(wc);
                Handles.DrawSolidRectangleWithOutline(new Vector3[] { wc[0], wc[1], wc[2], wc[3] }, noFill, greenOutline);
            }
        }

#if false
        #region GroupSelection_OrientedBounds_Legacy
        private static bool TryComputeGroupOrientedBoundsInParent(
            RectTransform parent,
            List<RectTransform> members,
            RectTransform active,
            out Vector2 c0,
            out Vector2 c1,
            out Vector2 c2,
            out Vector2 c3)
        {
            c0 = c1 = c2 = c3 = default;
            if (parent == null || members == null || members.Count == 0) return false;
            if (active == null) return false;

            Vector3 uW = active.transform.right;
            Vector3 vW = active.transform.up;
            Vector3 uP3 = parent.InverseTransformVector(uW);
            Vector3 vP3 = parent.InverseTransformVector(vW);
            Vector2 u = new Vector2(uP3.x, uP3.y);
            Vector2 v = new Vector2(vP3.x, vP3.y);
            if (u.sqrMagnitude < 1e-8f || v.sqrMagnitude < 1e-8f) return false;
            u.Normalize();
            v.Normalize();

            float minU = float.PositiveInfinity, maxU = float.NegativeInfinity;
            float minV = float.PositiveInfinity, maxV = float.NegativeInfinity;

            var wc = new Vector3[4];
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                if (m == null) continue;
                m.GetWorldCorners(wc);
                for (int k = 0; k < 4; k++)
                {
                    Vector3 p3 = parent.InverseTransformPoint(wc[k]);
                    Vector2 p = new Vector2(p3.x, p3.y);
                    float pu = Vector2.Dot(p, u);
                    float pv = Vector2.Dot(p, v);
                    if (pu < minU) minU = pu;
                    if (pu > maxU) maxU = pu;
                    if (pv < minV) minV = pv;
                    if (pv > maxV) maxV = pv;
                }
            }

            if (float.IsInfinity(minU) || float.IsInfinity(minV)) return false;

            c0 = u * minU + v * minV;
            c1 = u * maxU + v * minV;
            c2 = u * maxU + v * maxV;
            c3 = u * minU + v * maxV;
            return true;
        }
        #endregion
#endif

        private static Vector2 GetCanvasOriginPoint(int index, Rect r)
        {
            int row = index / 3;
            int col = index % 3;
            float x = col == 0 ? r.xMin : (col == 1 ? (r.xMin + r.xMax) * 0.5f : r.xMax);
            float y = row == 0 ? r.yMax : (row == 1 ? (r.yMin + r.yMax) * 0.5f : r.yMin);
            return new Vector2(x, y);
        }

        public static void ResetToDefaults()
        {
            var undoState = RectTransformSnapperSettingsUndoState.Instance;
            Undo.RecordObject(undoState, "Reset To Defaults");

            if (!ApplySavedDefaults())
            {
                // Hard-coded defaults when no saved defaults exist
                undoState.snapStep = 64f;
                undoState.snapDivisor = 1;
                undoState.offsetX = 0f;
                undoState.offsetY = 0f;
                undoState.dotSize = 1f;
                undoState.dotColor = new Color(0.6784314f, 0.6784314f, 0.6784314f, 1f);
                undoState.proportionalChildrenEnabled = true;
                undoState.referenceColor = new Color(1f, 1f, 1f, 1f);
                undoState.referenceAlpha = 0.5f;

                Enabled = false;
                ProportionalChildrenEnabled = true;
                SnapToRectEdges = true;
                CanvasOriginIndex = 4;
                AlignToCanvas = false;
                SnapStep = 64f;
                SnapDivisor = 1;
                SnapOffsetPercentX = 0f;
                SnapOffsetPercentY = 0f;
                ShowGrid = true;
                DotSize = 1f;
                DotColor = new Color(0.6784314f, 0.6784314f, 0.6784314f, 1f);
                ReferenceColor = new Color(1f, 1f, 1f, 1f);
                ReferenceAlpha = 0.5f;
                ReferenceAlwaysOnTop = true;
            }
            else
            {
                // When saved defaults exist, they are already applied by ApplySavedDefaults()
                // Just sync undoState
                undoState.ReadFromEngine();
            }


            if (ActiveReferenceImage != null)
            {
                var img = ActiveReferenceImage.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    Undo.RecordObject(img, "Reset Reference Appearance");
                    var c = ReferenceColor;
                    img.color = new Color(c.r, c.g, c.b, ReferenceAlpha);
                    EditorUtility.SetDirty(img);
                }
            }

            EditorUtility.SetDirty(undoState);
            RepaintSnapperWindow();
            SceneView.RepaintAll();
        }

        #region FitToGrid
        public static void FitSelectedToGrid()
        {
            var transforms = Selection.transforms;
            if (transforms == null || transforms.Length == 0)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Выберите один или несколько объектов с RectTransform.", "OK");
                return;
            }

            var rts = new List<RectTransform>(transforms.Length);
            for (int i = 0; i < transforms.Length; i++)
            {
                var tr = transforms[i];
                if (tr == null) continue;
                var rt = tr.GetComponent<RectTransform>();
                if (rt != null) rts.Add(rt);
            }

            if (rts.Count == 0)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "В выделении нет объектов с RectTransform.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Fit To Grid");
            int group = Undo.GetCurrentGroup();

            for (int i = 0; i < rts.Count; i++)
            {
                var rt = rts[i];
                if (rt == null) continue;
                var parent = rt.parent as RectTransform;
                if (parent == null) continue;

                Undo.RegisterCompleteObjectUndo(rt, "Fit To Grid");
                FitToGridSingle(rt, parent);
                EditorUtility.SetDirty(rt);
            }

            Undo.CollapseUndoOperations(group);
            SceneView.RepaintAll();
        }

        private static void FitToGridSingle(RectTransform rt, RectTransform parent)
        {
            if (rt == null || parent == null) return;

            bool resizeChildren = ProportionalChildrenEnabled;
            if (resizeChildren)
            {
                PrepareProportionalChildren(rt);
            }

            float step = Mathf.Max(1f, SnapStep / Mathf.Max(1, SnapDivisor));
            Vector2 origin = ComputeGlobalOriginInParent(parent, rt) + GetSnapOffsetInParent(step);


            bool hasCanvasBounds = TryGetCanvasLocalBounds(parent, out float canvasMinX, out float canvasMaxX, out float canvasMinY, out float canvasMaxY);

            float SnapCoordToGridOrCanvasEdge(float coord, float gridStep, float gridOrigin, bool hasCanvas, float cMin, float cMax)
            {
                float grid = RoundToGrid(coord, gridStep, gridOrigin);
                if (!hasCanvas) return grid;

                float canvas = (Mathf.Abs(coord - cMin) <= Mathf.Abs(coord - cMax)) ? cMin : cMax;
                float dGrid = Mathf.Abs(coord - grid);
                float dCanvas = Mathf.Abs(coord - canvas);


                return dCanvas < dGrid ? canvas : grid;
            }

            var wc = new Vector3[4];
            rt.GetWorldCorners(wc);
            var snapped = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = parent.InverseTransformPoint(wc[i]);
                snapped[i] = new Vector2(
                    SnapCoordToGridOrCanvasEdge(p.x, step, origin.x, hasCanvasBounds, canvasMinX, canvasMaxX),
                    SnapCoordToGridOrCanvasEdge(p.y, step, origin.y, hasCanvasBounds, canvasMinY, canvasMaxY));
            }

            Vector3 uWorld = rt.TransformVector(Vector3.right);
            Vector3 vWorld = rt.TransformVector(Vector3.up);
            bool flipX = Vector3.Dot(uWorld, rt.transform.right) < 0f;
            bool flipY = Vector3.Dot(vWorld, rt.transform.up) < 0f;
            if (flipX) uWorld = -uWorld;
            if (flipY) vWorld = -vWorld;
            Vector3 uP3 = parent.InverseTransformVector(uWorld);
            Vector3 vP3 = parent.InverseTransformVector(vWorld);
            Vector2 uP = new Vector2(uP3.x, uP3.y);
            Vector2 vP = new Vector2(vP3.x, vP3.y);

            float lenU = uP.magnitude;
            float lenV = vP.magnitude;
            if (lenU < 1e-6f || lenV < 1e-6f) return;
            Vector2 uUnit = uP / lenU;
            Vector2 vUnit = vP / lenV;

            int idxBL = 0;
            int idxTR = 0;
            float bestBL = float.PositiveInfinity;
            float bestTR = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                float su = Vector2.Dot(snapped[i], uUnit);
                float sv = Vector2.Dot(snapped[i], vUnit);
                float score = su + sv;
                if (score < bestBL) { bestBL = score; idxBL = i; }
                if (score > bestTR) { bestTR = score; idxTR = i; }
            }

            Vector2 bl = snapped[idxBL];
            Vector2 tr = snapped[idxTR];
            Vector2 diag = tr - bl;

            float widthParent = Mathf.Abs(Vector2.Dot(diag, uUnit));
            float heightParent = Mathf.Abs(Vector2.Dot(diag, vUnit));

            widthParent = Mathf.Max(1e-4f, widthParent);
            heightParent = Mathf.Max(1e-4f, heightParent);

            float widthLocal = Mathf.Max(MIN_THICKNESS, widthParent / lenU);
            float heightLocal = Mathf.Max(MIN_THICKNESS, heightParent / lenV);

            Vector2 pivot01 = rt.pivot;
            float xMin = -pivot01.x * widthLocal;
            float yMin = -pivot01.y * heightLocal;
            Vector2 pivotParentNew = bl - (uP * xMin + vP * yMin);

            bool stretchXAnch = Mathf.Abs(rt.anchorMin.x - rt.anchorMax.x) > 0.0001f;
            bool stretchYAnch = Mathf.Abs(rt.anchorMin.y - rt.anchorMax.y) > 0.0001f;

            if (!stretchXAnch && !stretchYAnch)
            {
                Vector2 pivotParentOld = parent.InverseTransformPoint(rt.transform.position);
                Vector2 deltaPivot = pivotParentNew - pivotParentOld;

                rt.anchoredPosition = new Vector2(
                    RoundToPrecision(rt.anchoredPosition.x + deltaPivot.x),
                    RoundToPrecision(rt.anchoredPosition.y + deltaPivot.y));

                float curW = Mathf.Max(MIN_THICKNESS, rt.rect.width);
                float curH = Mathf.Max(MIN_THICKNESS, rt.rect.height);
                float dw = widthLocal - curW;
                float dh = heightLocal - curH;
                rt.sizeDelta = new Vector2(
                    RoundToPrecision(rt.sizeDelta.x + dw),
                    RoundToPrecision(rt.sizeDelta.y + dh));

                if (resizeChildren) ApplyProportionalChildrenLocal(rt);
                return;
            }

            float minX = Mathf.Min(snapped[0].x, snapped[1].x, snapped[2].x, snapped[3].x);
            float maxX = Mathf.Max(snapped[0].x, snapped[1].x, snapped[2].x, snapped[3].x);
            float minY = Mathf.Min(snapped[0].y, snapped[1].y, snapped[2].y, snapped[3].y);
            float maxY = Mathf.Max(snapped[0].y, snapped[1].y, snapped[2].y, snapped[3].y);
            SetEdgesInParent(rt, parent, minX, maxX, minY, maxY);

            if (resizeChildren) ApplyProportionalChildrenLocal(rt);
        }
        #endregion

        #region AlignmentDistribution
        public enum AlignMode
        {
            Left,
            CenterX,
            Right,
            Top,
            CenterY,
            Bottom
        }
        public enum DistributeMode { HorizontalCenters, VerticalCenters, HorizontalSpacing, VerticalSpacing }

        public static void AlignSelected(AlignMode mode)
        {
            if (!TryGetSelectedObjects(out var objects)) return;
            if (!TryGetReferenceSpaceForSelection(objects, out var space)) return;
            if (objects.Count < 2) return;

            float minX, maxX, minY, maxY;
            RectTransform canvasRT = space as RectTransform;
            bool useCanvas = AlignToCanvas && canvasRT != null;

            if (useCanvas)
            {
                Rect cr = canvasRT.rect;
                minX = cr.xMin; maxX = cr.xMax; minY = cr.yMin; maxY = cr.yMax;
            }
            else
            {
                ComputeSelectionAabbInSpace(objects, space, out minX, out maxX, out minY, out maxY);
            }

            float target = mode switch
            {
                AlignMode.Left => minX,
                AlignMode.Right => maxX,
                AlignMode.CenterX => (minX + maxX) * 0.5f,
                AlignMode.Bottom => minY,
                AlignMode.Top => maxY,
                AlignMode.CenterY => (minY + maxY) * 0.5f,
                _ => minX
            };

            Undo.SetCurrentGroupName($"Align {mode}");
            int group = Undo.GetCurrentGroup();

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj.isRectTransform && obj.rt == null) continue;
                if (!obj.isRectTransform && obj.tr == null) continue;

                if (obj.isRectTransform)
                {
                    Undo.RecordObject(obj.rt, $"Align {mode}");
                }
                else
                {
                    Undo.RecordObject(obj.tr, $"Align {mode}");
                }

                GetAabbInSpace(obj, space, out float l, out float r, out float b, out float t);
                Vector2 deltaSpace = Vector2.zero;
                switch (mode)
                {
                    case AlignMode.Left: deltaSpace.x = target - l; break;
                    case AlignMode.Right: deltaSpace.x = target - r; break;
                    case AlignMode.CenterX: deltaSpace.x = target - ((l + r) * 0.5f); break;
                    case AlignMode.Bottom: deltaSpace.y = target - b; break;
                    case AlignMode.Top: deltaSpace.y = target - t; break;
                    case AlignMode.CenterY: deltaSpace.y = target - ((b + t) * 0.5f); break;
                }
                ApplyTranslationInSpace(obj, space, deltaSpace);

                if (obj.isRectTransform)
                {
                    EditorUtility.SetDirty(obj.rt);
                }
                else
                {
                    EditorUtility.SetDirty(obj.tr);
                }
            }

            Undo.CollapseUndoOperations(group);
            SceneView.RepaintAll();
        }

        public static void DistributeSelected(DistributeMode mode)
        {
            if (!TryGetSelectedObjects(out var objects)) return;
            if (!TryGetReferenceSpaceForSelection(objects, out var space)) return;
            if (objects.Count < 3) return;

            Undo.SetCurrentGroupName($"Distribute {mode}");
            int group = Undo.GetCurrentGroup();

            if (mode == DistributeMode.HorizontalCenters || mode == DistributeMode.VerticalCenters)
            {
                bool horizontal = mode == DistributeMode.HorizontalCenters;
                var items = BuildItems(objects, space);
                items.Sort((a, b) => horizontal ? a.center.x.CompareTo(b.center.x) : a.center.y.CompareTo(b.center.y));

                RectTransform canvasRT = space as RectTransform;
                bool useCanvas = AlignToCanvas && canvasRT != null;

                float start;
                float end;

                if (useCanvas)
                {

                    Rect cr = canvasRT.rect;
                    float canvasMin = horizontal ? cr.xMin : cr.yMin;
                    float canvasMax = horizontal ? cr.xMax : cr.yMax;
                    float firstSize = horizontal ? (items[0].r - items[0].l) : (items[0].t - items[0].b);
                    float lastSize = horizontal ? (items[^1].r - items[^1].l) : (items[^1].t - items[^1].b);

                    start = canvasMin + firstSize * 0.5f;
                    end = canvasMax - lastSize * 0.5f;


                    if (end < start) end = start;
                }
                else
                {
                    start = horizontal ? items[0].center.x : items[0].center.y;
                    end = horizontal ? items[^1].center.x : items[^1].center.y;
                }

                float step = (end - start) / (items.Count - 1);

                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it.isRectTransform && it.rt == null) continue;
                    if (!it.isRectTransform && it.tr == null) continue;

                    float target = start + step * i;
                    Vector2 delta = Vector2.zero;
                    if (horizontal) delta.x = target - it.center.x;
                    else delta.y = target - it.center.y;

                    AlignableObject obj = new AlignableObject { rt = it.rt, tr = it.tr, isRectTransform = it.isRectTransform };
                    if (it.isRectTransform)
                    {
                        Undo.RecordObject(it.rt, $"Distribute {mode}");
                    }
                    else
                    {
                        Undo.RecordObject(it.tr, $"Distribute {mode}");
                    }
                    ApplyTranslationInSpace(obj, space, delta);

                    if (it.isRectTransform)
                    {
                        EditorUtility.SetDirty(it.rt);
                    }
                    else
                    {
                        EditorUtility.SetDirty(it.tr);
                    }
                }
            }
            else
            {
                bool horizontal = mode == DistributeMode.HorizontalSpacing;
                var items = BuildItems(objects, space);
                items.Sort((a, b) => horizontal ? a.l.CompareTo(b.l) : a.b.CompareTo(b.b));

                RectTransform canvasRT = space as RectTransform;
                bool useCanvas = AlignToCanvas && canvasRT != null;

                float firstEdge;
                float lastEdge;

                if (useCanvas)
                {

                    Rect cr = canvasRT.rect;
                    firstEdge = horizontal ? cr.xMin : cr.yMin;
                    lastEdge = horizontal ? cr.xMax : cr.yMax;
                }
                else
                {
                    firstEdge = horizontal ? items[0].l : items[0].b;
                    lastEdge = horizontal ? items[^1].r : items[^1].t;
                }

                float totalSpan = lastEdge - firstEdge;

                float totalSize = 0f;
                for (int i = 0; i < items.Count; i++)
                    totalSize += horizontal ? (items[i].r - items[i].l) : (items[i].t - items[i].b);

                float gaps = items.Count - 1;
                float gap = gaps > 0 ? (totalSpan - totalSize) / gaps : 0f;

                float cursor = firstEdge;
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it.isRectTransform && it.rt == null) continue;
                    if (!it.isRectTransform && it.tr == null) continue;

                    float targetEdge = cursor;
                    Vector2 delta = Vector2.zero;
                    if (horizontal) delta.x = targetEdge - it.l;
                    else delta.y = targetEdge - it.b;

                    AlignableObject obj = new AlignableObject { rt = it.rt, tr = it.tr, isRectTransform = it.isRectTransform };
                    if (it.isRectTransform)
                    {
                        Undo.RecordObject(it.rt, $"Distribute {mode}");
                    }
                    else
                    {
                        Undo.RecordObject(it.tr, $"Distribute {mode}");
                    }
                    ApplyTranslationInSpace(obj, space, delta);

                    if (it.isRectTransform)
                    {
                        EditorUtility.SetDirty(it.rt);
                    }
                    else
                    {
                        EditorUtility.SetDirty(it.tr);
                    }

                    float size = horizontal ? (it.r - it.l) : (it.t - it.b);
                    cursor = targetEdge + size + gap;
                }
            }

            Undo.CollapseUndoOperations(group);
            SceneView.RepaintAll();
        }

        private struct AlignableObject
        {
            public RectTransform rt;
            public Transform tr;
            public bool isRectTransform;
        }

        private struct AabbItem
        {
            public RectTransform rt;
            public Transform tr;
            public bool isRectTransform;
            public float l, r, b, t;
            public Vector2 center;
        }

        private static List<AabbItem> BuildItems(List<AlignableObject> objects, Transform space)
        {
            var items = new List<AabbItem>(objects.Count);
            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj.isRectTransform && obj.rt == null) continue;
                if (!obj.isRectTransform && obj.tr == null) continue;

                GetAabbInSpace(obj, space, out float l, out float r, out float b, out float t);
                items.Add(new AabbItem
                {
                    rt = obj.rt,
                    tr = obj.tr,
                    isRectTransform = obj.isRectTransform,
                    l = l,
                    r = r,
                    b = b,
                    t = t,
                    center = new Vector2((l + r) * 0.5f, (b + t) * 0.5f)
                });
            }
            return items;
        }

        private static bool TryGetSelectedObjects(out List<AlignableObject> objects)
        {
            objects = new List<AlignableObject>();
            var trs = Selection.transforms;
            if (trs == null || trs.Length == 0) return false;

            for (int i = 0; i < trs.Length; i++)
            {
                var tr = trs[i];
                if (tr == null) continue;

                var rt = tr.GetComponent<RectTransform>();
                if (rt != null)
                {
                    objects.Add(new AlignableObject { rt = rt, isRectTransform = true });
                }
                else
                {
                    if (HasRenderer(tr))
                    {
                        objects.Add(new AlignableObject { tr = tr, isRectTransform = false });
                    }
                }
            }
            return objects.Count > 0;
        }

        private static bool HasRenderer(Transform tr)
        {
            if (tr == null) return false;
            return tr.GetComponent<Renderer>() != null || tr.GetComponentInChildren<Renderer>() != null;
        }

        private static Bounds GetRendererBounds(Transform tr)
        {
            Bounds bounds = new Bounds(tr.position, Vector3.zero);
            bool hasBounds = false;

            var renderer = tr.GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }

            var renderers = tr.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
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

            return bounds;
        }

        private static bool TryGetReferenceSpaceForSelection(List<AlignableObject> objects, out Transform space)
        {
            space = null;

            bool hasRectTransform = false;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].isRectTransform)
                {
                    hasRectTransform = true;
                    break;
                }
            }

            if (hasRectTransform)
            {
                RectTransform rtSpace = null;
                if (AlignToCanvas && AssignedCanvas != null)
                {
                    rtSpace = AssignedCanvas.GetComponent<RectTransform>();
                    if (rtSpace != null)
                    {
                        space = rtSpace;
                        return true;
                    }
                }

                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i].isRectTransform && objects[i].rt != null)
                    {
                        var canvas = objects[i].rt.GetComponentInParent<Canvas>();
                        if (canvas != null)
                        {
                            rtSpace = canvas.GetComponent<RectTransform>();
                            if (rtSpace != null)
                            {
                                space = rtSpace;
                                return true;
                            }
                        }
                        break;
                    }
                }
            }

            if (objects.Count > 0)
            {
                Transform commonParent = objects[0].isRectTransform ? objects[0].rt?.parent : objects[0].tr?.parent;

                if (commonParent != null)
                {
                    bool allSameParent = true;
                    for (int i = 1; i < objects.Count; i++)
                    {
                        Transform parent = objects[i].isRectTransform ? objects[i].rt?.parent : objects[i].tr?.parent;
                        if (parent != commonParent)
                        {
                            allSameParent = false;
                            break;
                        }
                    }

                    if (allSameParent)
                    {
                        space = commonParent;
                        return true;
                    }
                }
            }

            space = null;
            return true;
        }

        private static void ComputeSelectionAabbInSpace(List<AlignableObject> objects, Transform space, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = float.PositiveInfinity;
            minY = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            maxY = float.NegativeInfinity;

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj.isRectTransform && obj.rt == null) continue;
                if (!obj.isRectTransform && obj.tr == null) continue;

                GetAabbInSpace(obj, space, out float l, out float r, out float b, out float t);
                minX = Mathf.Min(minX, l);
                maxX = Mathf.Max(maxX, r);
                minY = Mathf.Min(minY, b);
                maxY = Mathf.Max(maxY, t);
            }
        }

        private static void GetAabbInSpace(AlignableObject obj, Transform space, out float minX, out float maxX, out float minY, out float maxY)
        {
            if (obj.isRectTransform && obj.rt != null)
            {
                var wc = new Vector3[4];
                obj.rt.GetWorldCorners(wc);
                minX = float.PositiveInfinity;
                minY = float.PositiveInfinity;
                maxX = float.NegativeInfinity;
                maxY = float.NegativeInfinity;

                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = space != null ? space.InverseTransformPoint(wc[i]) : wc[i];
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                }
            }
            else if (!obj.isRectTransform && obj.tr != null)
            {
                Bounds bounds = GetRendererBounds(obj.tr);
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;

                Vector3[] corners = new Vector3[8];
                corners[0] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);
                corners[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);
                corners[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
                corners[3] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
                corners[4] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);
                corners[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);
                corners[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
                corners[7] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);

                minX = float.PositiveInfinity;
                minY = float.PositiveInfinity;
                maxX = float.NegativeInfinity;
                maxY = float.NegativeInfinity;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = space != null ? space.InverseTransformPoint(corners[i]) : corners[i];
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                }
            }
            else
            {
                minX = minY = maxX = maxY = 0f;
            }
        }

        private static void ApplyTranslationInSpace(AlignableObject obj, Transform space, Vector2 deltaSpace)
        {
            if (deltaSpace.sqrMagnitude < 1e-12f) return;

            if (obj.isRectTransform && obj.rt != null)
            {
                if (space == null) return;

                Vector3 deltaWorld = space.TransformVector(new Vector3(deltaSpace.x, deltaSpace.y, 0f));
                if (obj.rt.parent is RectTransform parent)
                {
                    Vector3 dp = parent.InverseTransformVector(deltaWorld);
                    obj.rt.anchoredPosition += new Vector2(dp.x, dp.y);
                }
                else
                {
                    obj.rt.position += deltaWorld;
                }
            }
            else if (!obj.isRectTransform && obj.tr != null)
            {
                Vector3 deltaWorld;
                if (space != null)
                {
                    deltaWorld = space.TransformVector(new Vector3(deltaSpace.x, deltaSpace.y, 0f));
                }
                else
                {
                    deltaWorld = new Vector3(deltaSpace.x, deltaSpace.y, 0f);
                }

                obj.tr.position += deltaWorld;
            }
        }
        #endregion

    }
}