using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace Multitool.RectTransformSnapper
{
    public class RectTransformSnapperWindow : EditorWindow
    {
        public static void OpenFromWindow() => GetWindow<RectTransformSnapperWindow>(false, "Rect Transform Snapper", true);
        private Vector2 _scrollPos;
        private const string PICKER_CONTROL_NAME = "RTS_SPRITE_PICKER";
        private bool _waitingForPicker;
        private bool _foldoutOverview = true;
        private bool _foldoutGrid = true;
        private bool _foldoutCanvas = true;
        private bool _foldoutAlignment = true;
        private bool _foldoutReference = true;
        private const string MANUAL_ASSET_PATH = "Assets/Plugins/Multitool/RectTransformSnapper/Docs/RectTransformSnapper_Manual_EN.txt";
        private const string ICONS_BASE_PATH = "Assets/Plugins/Multitool/RectTransformSnapper/Icons/";
        private const string LOGO_FILE_NAME = "Logo.png";
        private const string VERSION_TEXT = "v1.4.0";
        private static readonly GUIContent OverviewFoldoutContent = new GUIContent("Overview", "Show or hide the Rect Transform Snapper logo and hotkey reference.");
        private static readonly GUIContent EnabledToggleContent = new GUIContent("Enabled", "Toggle Rect Transform Snapper on or off, including the snapping grid overlay.");
        private static readonly GUIContent FitToGridButtonContent = new GUIContent("Fit To Grid", "Fit selected RectTransforms to the nearest grid points (each object individually, rotation-aware).");
        private static readonly GUIContent ManualButtonContent = new GUIContent("?", "Open the Rect Transform Snapper manual in your editor.");
        private static readonly GUIContent CanvasFieldContent = new GUIContent("Selected Canvas", "Assign the Canvas that Rect Transform Snapper should target.");
        private static readonly GUIContent NewCanvasButtonContent = new GUIContent("New Canvas", "Create a new Canvas configured for Rect Transform Snapper.");
        private static readonly GUIContent GridOriginLabelContent = new GUIContent("Grid Origin", "Choose the anchor point used as the grid's origin.");
        private static readonly GUIContent GridStepFieldContent = new GUIContent("Grid Step", "Base size of the snapping grid in canvas units.");
        private static readonly GUIContent GridStepHalveButtonContent = new GUIContent("÷2", "Halve the current grid step.");
        private static readonly GUIContent GridStepDoubleButtonContent = new GUIContent("×2", "Double the current grid step.");
        private static readonly GUIContent SubdivisionsFieldContent = new GUIContent("Subdivisions", "Number of subdivisions inside each grid cell.");
        private static readonly GUIContent SubdivisionsHalveButtonContent = new GUIContent("÷2", "Halve the number of subdivisions.");
        private static readonly GUIContent SubdivisionsDoubleButtonContent = new GUIContent("×2", "Double the number of subdivisions.");
        private static readonly GUIContent GridOffsetLabelContent = new GUIContent("Grid Offset", "Shift the grid horizontally (X) and vertically (Y) in percentage of the grid size.");
        private static readonly GUIContent DotSizeSliderContent = new GUIContent("Dot Size", "Adjust the size of grid dots drawn in the Scene view.");
        private static readonly GUIContent DotColorFieldContent = new GUIContent("Dot Color", "Set the color for grid dots in the Scene view.");
        private static readonly GUIContent ResizeChildrenToggleContent = new GUIContent("Resize Children", "Scale child RectTransforms proportionally when resizing their parent.");
        private static readonly GUIContent SnapToRectEdgesToggleContent = new GUIContent("Snap to Rect Edges", "Snap edges to edges of other RectTransform objects.");
        private static readonly GUIContent AddReferenceButtonContent = new GUIContent("Add Ref", "Pick a Sprite or Texture2D from the object selector.");
        private static readonly GUIContent FitToReferenceButtonContent = new GUIContent("Fit Canvas to Ref", "Resize the Canvas scaler to match the active reference overlay.");
        private static readonly GUIContent RemoveReferenceButtonContent = new GUIContent("Remove Ref", "Delete the active reference overlay from the Canvas.");
        private static readonly GUIContent ReferenceColorFieldContent = new GUIContent("Reference Color", "Tint color applied to the reference overlay image.");
        private static readonly GUIContent ReferenceOpacitySliderContent = new GUIContent("Opacity", "Transparency of the reference overlay image.");
        private static readonly GUIContent ReferenceAlwaysOnTopToggleContent = new GUIContent("Always On Top", "Keep the reference overlay as the top-most sibling inside the Canvas.");
        private static readonly GUIContent AlignmentFoldoutContent = new GUIContent("Alignment", "Align and distribute multiple selected RectTransforms. Includes center alignment for both axes (rotation/scale-safe).");
        private static readonly GUIContent OverlayModeContent = new GUIContent("overlay mode", "Show this panel in SceneView as an overlay.");
        private static readonly GUIContent AlignToCanvasToggleContent = new GUIContent("Align to Canvas", "Align to Canvas boundaries instead of the selection bounding box.");
        private Texture2D _logoTex;
        private RectTransformSnapperSettingsUndoState UndoState => RectTransformSnapperSettingsUndoState.Instance;
        #region UI Constants
        private const float SPACING_SMALL = 4f;
        private const float SPACING_MEDIUM = 6f;
        private const float SPACING_LARGE = 8f;
        private const float PADDING_WINDOW = 8f;
        private const float INDENT_PER_LEVEL = 15f;
        private const float INDENT_EXTRA = 16f;
        private const float BUTTON_HEIGHT_STANDARD = 22f;
        private const float BUTTON_HEIGHT_LARGE = 24f;
        private const float BUTTON_WIDTH_COMPACT = 22f;
        private const float BUTTON_WIDTH_SMALL = 28f;
        private const float BUTTON_WIDTH_MEDIUM = 80f;
        private const float BUTTON_WIDTH_LARGE = 110f;
        private const float BUTTON_WIDTH_XLARGE = 140f;
        private const float BUTTON_WIDTH_XXLARGE = 150f;
        private const float BUTTON_WIDTH_ICON = 22f;
        private const float BUTTON_SIZE_ALIGNMENT = 28f;
        private const float BUTTON_SPACING_ALIGNMENT = 0f;
        private const float GROUP_SPACING_ALIGNMENT = 4f;
        private const float FIELD_WIDTH_LABEL = 12f;
        private const float FIELD_WIDTH_OFFSET_SMALL = 48f;
        private const float FIELD_WIDTH_OFFSET_LARGE = 56f;
        private const float FIELD_WIDTH_REFERENCE_SMALL = 120f;
        private const float FIELD_WIDTH_REFERENCE_LARGE = 160f;
        private const float FIELD_WIDTH_MIN = 40f;
        private const float FIELD_WIDTH_MAX = 90f;
        private const float FIELD_WIDTH_ICON_GRID = 26f;
        private const float FIELD_HEIGHT_ICON_GRID = 18f;
        private const float LABEL_WIDTH_PERCENT = 0.42f;
        private const float FIELD_WIDTH_PERCENT = 0.24f;
        private const float LABEL_WIDTH_MIN = 80f;
        private const float LABEL_WIDTH_MAX = 120f;
        private const float WINDOW_WIDTH_COMPACT_THRESHOLD_1 = 360f;
        private const float WINDOW_WIDTH_COMPACT_THRESHOLD_2 = 380f;
        private const float COLOR_ALPHA_SEPARATOR = 0.35f;
        private const float COLOR_ALPHA_VERSION = 0.65f;
        private const float COLOR_ALPHA_GRID_ORIGIN_SELECTED = 0.85f;
        private const float COLOR_ALPHA_GRID_ORIGIN_UNSELECTED = 0.55f;
        private static readonly Color COLOR_GRID_ORIGIN_SELECTED = new Color(0.25f, 0.55f, 0.9f, COLOR_ALPHA_GRID_ORIGIN_SELECTED);
        private static readonly Color COLOR_GRID_ORIGIN_UNSELECTED = new Color(0.16f, 0.16f, 0.16f, COLOR_ALPHA_GRID_ORIGIN_UNSELECTED);
        #endregion
        private void OnEnable()
        {
            titleContent = new GUIContent("Rect Transform Snapper");
            TryLoadLogo();
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }
        private void OnSelectionChange()
        {
            Repaint();
        }
        private Texture2D GetOriginIcon(int index)
        {
            const string basePath = "Assets/Plugins/Multitool/RectTransformSnapper/Icons/";
            string name = index switch
            {
                0 => "Left Top.png",
                1 => "Top.png",
                2 => "Right Top.png",
                3 => "Left.png",
                4 => "Center.png",
                5 => "Right.png",
                6 => "Left Bottom.png",
                7 => "Bottom.png",
                8 => "Right Bottom.png",
                _ => null
            };
            if (string.IsNullOrEmpty(name)) return null;
            var path = System.IO.Path.Combine(basePath, name).Replace("\\", "/");
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return tex;
        }
        private void CreateNewCanvas()
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
            RectTransformSnapperEngine.AssignedCanvas = canvas;
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            SceneView.RepaintAll();
        }
        private bool TryGetReferenceImage(out UnityEngine.UI.Image image)
        {
            image = null;
            var canvas = RectTransformSnapperEngine.AssignedCanvas;
            if (canvas == null) return false;
            if (Selection.activeGameObject != null)
            {
                var selImg = Selection.activeGameObject.GetComponent<UnityEngine.UI.Image>();
                if (selImg != null && selImg.sprite != null && Selection.activeGameObject.name.StartsWith("Reference_"))
                {
                    if (selImg.canvas == canvas)
                    {
                        image = selImg;
                        RectTransformSnapperEngine.ActiveReferenceImage = selImg.transform;
                        return true;
                    }
                }
            }
            var rt = canvas.GetComponent<RectTransform>();
            if (rt == null) return false;
            UnityEngine.UI.Image found = null;
            for (int i = rt.childCount - 1; i >= 0; i--)
            {
                var child = rt.GetChild(i) as RectTransform;
                if (child == null) continue;
                if (!child.gameObject.name.StartsWith("Reference_")) continue;
                var img = child.GetComponent<UnityEngine.UI.Image>();
                if (img != null && img.sprite != null)
                {
                    found = img;
                    break;
                }
            }
            if (found != null)
            {
                image = found;
                RectTransformSnapperEngine.ActiveReferenceImage = found.transform;
                return true;
            }
            return false;
        }
        private Vector2 GetRealTextureSize(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return Vector2.zero;
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(assetPath))
                return Vector2.zero;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                return new Vector2(width, height);
            }
            return new Vector2(sprite.texture.width, sprite.texture.height);
        }
        private void SetMaxTextureSize(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return;
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(assetPath))
                return;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                int maxSize = Mathf.Max(width, height);
                int targetMaxSize = 32;
                while (targetMaxSize < maxSize && targetMaxSize < 8192)
                {
                    targetMaxSize *= 2;
                }
                if (importer.maxTextureSize < targetMaxSize)
                {
                    importer.maxTextureSize = targetMaxSize;
                    importer.SaveAndReimport();
                }
            }
        }
        private void FitCanvasToReference()
        {
            var canvas = RectTransformSnapperEngine.AssignedCanvas;
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Assign a Canvas first.", "OK");
                return;
            }
            if (!TryGetReferenceImage(out var refImage))
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "No reference image found under assigned Canvas. Create one via Reference Overlay.", "OK");
                return;
            }
            var sprite = refImage.sprite;
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Selected reference has no sprite.", "OK");
                return;
            }
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = Undo.AddComponent<UnityEngine.UI.CanvasScaler>(canvas.gameObject);
            }
            Undo.RecordObject(scaler, "Fit Canvas to Ref");
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            Vector2 realSize = GetRealTextureSize(sprite);
            if (realSize.x > 0 && realSize.y > 0)
            {
                scaler.referenceResolution = realSize;
            }
            else
            {
                var r = sprite.rect;
                scaler.referenceResolution = new Vector2(r.width, r.height);
            }
            EditorUtility.SetDirty(scaler);
            SceneView.RepaintAll();
        }
        private void FitReferenceToCanvas()
        {
            var canvas = RectTransformSnapperEngine.AssignedCanvas;
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Assign a Canvas first.", "OK");
                return;
            }
            if (!TryGetReferenceImage(out var refImage) || refImage == null)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "No reference image found under assigned Canvas. Create one via Reference Overlay.", "OK");
                return;
            }
            Vector2 targetSize = Vector2.zero;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                var rr = scaler.referenceResolution;
                if (rr.x > 0 && rr.y > 0) targetSize = rr;
            }
            if (targetSize == Vector2.zero)
            {
                var canvasRT = canvas.GetComponent<RectTransform>();
                if (canvasRT != null)
                    targetSize = canvasRT.rect.size;
            }
            if (targetSize.x <= 0f || targetSize.y <= 0f)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Canvas reference size is invalid.", "OK");
                return;
            }
            var rtRef = refImage.GetComponent<RectTransform>();
            if (rtRef == null) return;
            Undo.RecordObject(rtRef, "Fit Ref to Canvas");
            rtRef.anchorMin = new Vector2(0f, 0f);
            rtRef.anchorMax = new Vector2(1f, 1f);
            rtRef.offsetMin = Vector2.zero;
            rtRef.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(rtRef);
            SceneView.RepaintAll();
        }
        private void OpenSpritePicker()
        {
            if (RectTransformSnapperEngine.AssignedCanvas == null)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Assign a Canvas first.", "OK");
                return;
            }
            _waitingForPicker = true;
            EditorGUIUtility.ShowObjectPicker<Sprite>(null, false, string.Empty, PICKER_CONTROL_NAME.GetHashCode());
        }
        private void HandleHotkeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.control && !e.shift && !e.alt && e.keyCode == KeyCode.Quote)
            {
                RectTransformSnapperEngine.Enabled = !RectTransformSnapperEngine.Enabled;
                RectTransformSnapperEngine.ShowGrid = RectTransformSnapperEngine.Enabled;
                e.Use();
                Repaint();
                SceneView.RepaintAll();
                return;
            }
        }
        private void OnUndoRedoPerformed()
        {
            var st = UndoState;
            if (st == null) return;
            st.ApplyToEngine();
            UpdateExistingReferenceOverlayColorAndAlpha();
            Repaint();
            SceneView.RepaintAll();
        }
        private void OnGUI()
        {
            HandleHotkeys();
            var paddingStyle = new GUIStyle();
            paddingStyle.padding = new RectOffset((int)PADDING_WINDOW, (int)PADDING_WINDOW, (int)PADDING_WINDOW, (int)PADDING_WINDOW);
            EditorGUILayout.BeginVertical(paddingStyle);
            float windowW = position.width;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            float oldFieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(windowW * LABEL_WIDTH_PERCENT, LABEL_WIDTH_MIN, LABEL_WIDTH_MAX);
            EditorGUIUtility.fieldWidth = Mathf.Clamp(windowW * FIELD_WIDTH_PERCENT, FIELD_WIDTH_MIN, FIELD_WIDTH_MAX);
            int compactBtnW = windowW < WINDOW_WIDTH_COMPACT_THRESHOLD_1 ? (int)BUTTON_WIDTH_COMPACT : (int)BUTTON_WIDTH_SMALL;
            float offsetFieldW = windowW < WINDOW_WIDTH_COMPACT_THRESHOLD_1 ? FIELD_WIDTH_OFFSET_SMALL : FIELD_WIDTH_OFFSET_LARGE;
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            _foldoutOverview = EditorGUILayout.Foldout(_foldoutOverview, OverviewFoldoutContent, true);
            if (_foldoutOverview)
            {
                if (_logoTex != null)
                {
                    float targetHeight = _logoTex.height;
                    var rowRect = GUILayoutUtility.GetRect(0, targetHeight, GUILayout.ExpandWidth(true));
                    float maxWidth = Mathf.Max(0f, rowRect.width);
                    float drawW = Mathf.Min(_logoTex.width, maxWidth);
                    float drawH = (_logoTex.width > 0) ? drawW * (_logoTex.height / (float)_logoTex.width) : targetHeight;
                    float x = rowRect.x + (rowRect.width - drawW) * 0.5f;
                    float y = rowRect.y + (rowRect.height - drawH) * 0.5f;
                    var drawRect = new Rect(x, y, drawW, drawH);
                    GUI.DrawTexture(drawRect, _logoTex, ScaleMode.ScaleToFit);
                    var verStyle = new GUIStyle(EditorStyles.miniLabel);
                    verStyle.alignment = TextAnchor.LowerRight;
                    verStyle.normal.textColor = new Color(1f, 1f, 1f, COLOR_ALPHA_VERSION);
                    var verRect = new Rect(drawRect.x, drawRect.y, drawRect.width - SPACING_SMALL, drawRect.height - 2f);
                    GUI.Label(verRect, VERSION_TEXT, verStyle);
                    GUILayout.Space(SPACING_MEDIUM);
                }
            }
            EditorGUILayout.Space(SPACING_LARGE);
            DrawSeparator();
            EditorGUILayout.Space(SPACING_LARGE);
            EditorGUILayout.BeginHorizontal();
            bool en = EditorGUILayout.Toggle(EnabledToggleContent, RectTransformSnapperEngine.Enabled);
            if (GUILayout.Button(ManualButtonContent, GUILayout.Width(BUTTON_WIDTH_ICON))) OpenManual();
            EditorGUILayout.EndHorizontal();
            if (en != RectTransformSnapperEngine.Enabled)
            {
                RectTransformSnapperEngine.Enabled = en;
                RectTransformSnapperEngine.ShowGrid = en;
                SceneView.RepaintAll();
            }
            using (new EditorGUI.DisabledScope(Selection.transforms == null || Selection.transforms.Length == 0))
            {
                if (GUILayout.Button(FitToGridButtonContent, GUILayout.Height(BUTTON_HEIGHT_LARGE)))
                {
                    RectTransformSnapperEngine.FitSelectedToGrid();
                }
            }
            EditorGUILayout.Space(SPACING_MEDIUM);
            DrawSettingsPanel();
            EditorGUILayout.Space(SPACING_LARGE);
            DrawSeparator();
            EditorGUILayout.Space(SPACING_LARGE);
            _foldoutCanvas = EditorGUILayout.Foldout(_foldoutCanvas,
                new GUIContent("Canvas", "Canvas Settings let you pick or create the target Canvas so snapping and reference overlays know where to operate."), true);
            if (_foldoutCanvas)
            {
                EditorGUI.indentLevel++;
                float indentPixels = EditorGUI.indentLevel * INDENT_PER_LEVEL;
                var canvas = RectTransformSnapperEngine.AssignedCanvas;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(CanvasFieldContent, canvas, typeof(Canvas), true);
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indentPixels);
                if (GUILayout.Button(NewCanvasButtonContent, GUILayout.Height(BUTTON_HEIGHT_STANDARD), GUILayout.MinWidth(BUTTON_WIDTH_MEDIUM)))
                    CreateNewCanvas();
                EditorGUILayout.EndHorizontal();
                if (RectTransformSnapperEngine.AssignedCanvas == null)
                    EditorGUILayout.HelpBox("Assign a Canvas above to enable RectTransform snapping.", MessageType.Warning);
                EditorGUILayout.Space(SPACING_SMALL);
                EditorGUILayout.LabelField(GridOriginLabelContent, EditorStyles.boldLabel);
                for (int r = 0; r < 3; r++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(indentPixels);
                    for (int c = 0; c < 3; c++)
                    {
                        int idx = r * 3 + c;
                        var tex = GetOriginIcon(idx);
                        var isSel = idx == RectTransformSnapperEngine.CanvasOriginIndex;
                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = isSel ? COLOR_GRID_ORIGIN_SELECTED : COLOR_GRID_ORIGIN_UNSELECTED;
                        if (GUILayout.Button(tex != null ? new GUIContent(tex) : GUIContent.none, GUILayout.Width(FIELD_WIDTH_ICON_GRID), GUILayout.Height(FIELD_HEIGHT_ICON_GRID)))
                        {
                            var st = UndoState;
                            Undo.RecordObject(st, "Change Grid Origin");
                            st.canvasOriginIndex = idx;
                            EditorUtility.SetDirty(st);
                            RectTransformSnapperEngine.CanvasOriginIndex = idx;
                        }
                        GUI.backgroundColor = prev;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(SPACING_LARGE);
            DrawSeparator();
            EditorGUILayout.Space(SPACING_LARGE);
            EditorGUILayout.BeginHorizontal();
            _foldoutAlignment = EditorGUILayout.Foldout(_foldoutAlignment, AlignmentFoldoutContent, true);
            GUILayout.FlexibleSpace();
            bool alignOv = EditorPrefs.GetBool("RTS_AlignmentOverlayVisible", false);
            bool newAlignOv = GUILayout.Toggle(alignOv, OverlayModeContent, EditorStyles.miniButton, GUILayout.Width(BUTTON_WIDTH_LARGE));
            if (newAlignOv != alignOv)
            {
                EditorPrefs.SetBool("RTS_AlignmentOverlayVisible", newAlignOv);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
            if (_foldoutAlignment)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SPACING_SMALL);
                bool alignToCanvas = EditorGUILayout.Toggle(AlignToCanvasToggleContent, RectTransformSnapperEngine.AlignToCanvas);
                if (alignToCanvas != RectTransformSnapperEngine.AlignToCanvas)
                {
                    RectTransformSnapperEngine.AlignToCanvas = alignToCanvas;
                    SceneView.RepaintAll();
                }
                bool canAlign = Selection.transforms != null && Selection.transforms.Length >= 2;
                bool canDistribute = Selection.transforms != null && Selection.transforms.Length >= 3;
                using (new EditorGUI.DisabledScope(!canAlign))
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * INDENT_PER_LEVEL);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Left), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                        RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Left);
                    GUILayout.Space(BUTTON_SPACING_ALIGNMENT);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.CenterX), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                        RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.CenterX);
                    GUILayout.Space(BUTTON_SPACING_ALIGNMENT);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Right), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                        RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Right);
                    GUILayout.Space(GROUP_SPACING_ALIGNMENT);
                    using (new EditorGUI.DisabledScope(!canDistribute))
                    {
                        if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.HorizontalCenters), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                            RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.HorizontalCenters);
                        GUILayout.Space(BUTTON_SPACING_ALIGNMENT);
                        if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.HorizontalSpacing), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                            RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.HorizontalSpacing);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space(SPACING_SMALL);
                using (new EditorGUI.DisabledScope(!canAlign))
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * INDENT_PER_LEVEL);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Top), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                        RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Top);
                    GUILayout.Space(BUTTON_SPACING_ALIGNMENT);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.CenterY), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                        RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.CenterY);
                    GUILayout.Space(BUTTON_SPACING_ALIGNMENT);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Bottom), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                        RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Bottom);
                    GUILayout.Space(GROUP_SPACING_ALIGNMENT);
                    using (new EditorGUI.DisabledScope(!canDistribute))
                    {
                        if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.VerticalCenters), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                            RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.VerticalCenters);
                        GUILayout.Space(BUTTON_SPACING_ALIGNMENT);
                        if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.VerticalSpacing), GUILayout.Width(BUTTON_SIZE_ALIGNMENT), GUILayout.Height(BUTTON_SIZE_ALIGNMENT)))
                            RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.VerticalSpacing);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(SPACING_LARGE);
            DrawSeparator();
            EditorGUILayout.Space(SPACING_LARGE);
            EditorGUILayout.BeginHorizontal();
            _foldoutGrid = EditorGUILayout.Foldout(_foldoutGrid,
                new GUIContent("Grid", "Grid settings define the snapping step, subdivisions, and offsets that control how RectTransforms align while you work."), true);
            GUILayout.FlexibleSpace();
            bool gridOv = EditorPrefs.GetBool("RTS_GridOverlayVisible", false);
            bool newGridOv = GUILayout.Toggle(gridOv, new GUIContent("overlay mode", "Show grid overlay in SceneView"), EditorStyles.miniButton, GUILayout.Width(BUTTON_WIDTH_LARGE));
            if (newGridOv != gridOv)
            {
                EditorPrefs.SetBool("RTS_GridOverlayVisible", newGridOv);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
            if (_foldoutGrid)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SPACING_SMALL);
                EditorGUILayout.BeginHorizontal();
                float snap = EditorGUILayout.FloatField(GridStepFieldContent, RectTransformSnapperEngine.SnapStep);
                if (GUILayout.Button(GridStepHalveButtonContent, GUILayout.Width(compactBtnW))) snap = Mathf.Max(1f, RectTransformSnapperEngine.SnapStep / 2f);
                if (GUILayout.Button(GridStepDoubleButtonContent, GUILayout.Width(compactBtnW))) snap = RectTransformSnapperEngine.SnapStep * 2f;
                EditorGUILayout.EndHorizontal();
                if (!Mathf.Approximately(snap, RectTransformSnapperEngine.SnapStep))
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Change Grid Step");
                    st.snapStep = Mathf.Max(1f, snap);
                    EditorUtility.SetDirty(st);
                    RectTransformSnapperEngine.SnapStep = Mathf.Max(1f, snap);
                }
                EditorGUILayout.BeginHorizontal();
                int div = EditorGUILayout.IntField(SubdivisionsFieldContent, RectTransformSnapperEngine.SnapDivisor);
                if (GUILayout.Button(SubdivisionsHalveButtonContent, GUILayout.Width(compactBtnW))) div = Mathf.Max(1, RectTransformSnapperEngine.SnapDivisor / 2);
                if (GUILayout.Button(SubdivisionsDoubleButtonContent, GUILayout.Width(compactBtnW))) div = Mathf.Max(1, RectTransformSnapperEngine.SnapDivisor * 2);
                EditorGUILayout.EndHorizontal();
                div = Mathf.Max(1, div);
                if (div != RectTransformSnapperEngine.SnapDivisor)
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Change Subdivisions");
                    st.snapDivisor = div;
                    EditorUtility.SetDirty(st);
                    RectTransformSnapperEngine.SnapDivisor = div;
                }
                EditorGUILayout.Space(SPACING_LARGE);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(GridOffsetLabelContent);
                float offX = RectTransformSnapperEngine.SnapOffsetPercentX;
                float offY = RectTransformSnapperEngine.SnapOffsetPercentY;
                GUILayout.Label("X", GUILayout.Width(FIELD_WIDTH_LABEL));
                offX = EditorGUILayout.FloatField(offX, GUILayout.Width(offsetFieldW));
                GUILayout.Space(SPACING_MEDIUM);
                GUILayout.Label("Y", GUILayout.Width(FIELD_WIDTH_LABEL));
                offY = EditorGUILayout.FloatField(offY, GUILayout.Width(offsetFieldW));
                EditorGUILayout.EndHorizontal();
                offX = Mathf.Clamp(offX, -1f, 1f);
                offY = Mathf.Clamp(offY, -1f, 1f);
                bool changedOffX = !Mathf.Approximately(offX, RectTransformSnapperEngine.SnapOffsetPercentX);
                bool changedOffY = !Mathf.Approximately(offY, RectTransformSnapperEngine.SnapOffsetPercentY);
                if (changedOffX || changedOffY)
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Change Grid Offset");
                    st.offsetX = offX;
                    st.offsetY = offY;
                    EditorUtility.SetDirty(st);
                    if (changedOffX) RectTransformSnapperEngine.SnapOffsetPercentX = offX;
                    if (changedOffY) RectTransformSnapperEngine.SnapOffsetPercentY = offY;
                }
                EditorGUILayout.Space(SPACING_LARGE);
                int dot = EditorGUILayout.IntSlider(DotSizeSliderContent, Mathf.RoundToInt(RectTransformSnapperEngine.DotSize), 1, 4);
                if (dot != Mathf.RoundToInt(RectTransformSnapperEngine.DotSize))
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Change Dot Size");
                    st.dotSize = dot;
                    EditorUtility.SetDirty(st);
                    RectTransformSnapperEngine.DotSize = dot; SceneView.RepaintAll();
                }
                EditorGUILayout.Space(SPACING_SMALL);
                var col = EditorGUILayout.ColorField(DotColorFieldContent, RectTransformSnapperEngine.DotColor);
                if (col != RectTransformSnapperEngine.DotColor)
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Change Dot Color");
                    st.dotColor = col;
                    EditorUtility.SetDirty(st);
                    RectTransformSnapperEngine.DotColor = col;
                    SceneView.RepaintAll();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(SPACING_LARGE);
            DrawSeparator();
            EditorGUILayout.Space(SPACING_LARGE);
            EditorGUILayout.BeginHorizontal();
            _foldoutReference = EditorGUILayout.Foldout(_foldoutReference,
                new GUIContent("Refernce", "Reference overlay helps you use a semi-transparent sprite as a guide to align your UI with mockups or screenshots."), true);
            GUILayout.FlexibleSpace();
            bool refOv = EditorPrefs.GetBool("RTS_RefOverlayVisible", false);
            bool newRefOv = GUILayout.Toggle(refOv, new GUIContent("overlay mode", "Show reference overlay panel in SceneView"), EditorStyles.miniButton, GUILayout.Width(BUTTON_WIDTH_LARGE));
            if (newRefOv != refOv)
            {
                EditorPrefs.SetBool("RTS_RefOverlayVisible", newRefOv);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
            if (_foldoutReference)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(SPACING_SMALL);
                float indentPixels = EditorGUI.indentLevel * INDENT_PER_LEVEL;
                bool hasCanvas = RectTransformSnapperEngine.AssignedCanvas != null;
                bool hasReference = hasCanvas && TryGetReferenceImage(out _);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indentPixels);
                using (new EditorGUI.DisabledScope(!hasCanvas))
                {
                    if (GUILayout.Button(AddReferenceButtonContent, GUILayout.ExpandWidth(true))) OpenSpritePicker();
                }
                using (new EditorGUI.DisabledScope(!hasReference))
                {
                    if (GUILayout.Button(RemoveReferenceButtonContent, GUILayout.ExpandWidth(true)))
                    {
                        RemoveActiveReferenceOverlay();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(INDENT_EXTRA);
                using (new EditorGUI.DisabledScope(!hasReference))
                {
                    if (GUILayout.Button(FitToReferenceButtonContent, GUILayout.MinWidth(BUTTON_WIDTH_XLARGE))) FitCanvasToReference();
                    if (GUILayout.Button(new GUIContent("Fit Ref to Canvas", "Resize the Reference Overlay to match the Canvas reference size."), GUILayout.MinWidth(BUTTON_WIDTH_XLARGE))) FitReferenceToCanvas();
                }
                EditorGUILayout.EndHorizontal();
                if (!hasCanvas)
                {
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.HelpBox("Select a Canvas or one of its children to add a Reference Overlay.", MessageType.Info);
                }
                EditorGUILayout.Space(SPACING_SMALL);
                Color refColor = EditorGUILayout.ColorField(ReferenceColorFieldContent, RectTransformSnapperEngine.ReferenceColor);
                if (refColor != RectTransformSnapperEngine.ReferenceColor)
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Change Reference Color");
                    st.referenceColor = refColor;
                    EditorUtility.SetDirty(st);
                    RectTransformSnapperEngine.ReferenceColor = refColor;
                    UpdateExistingReferenceOverlayColorAndAlpha();
                }
                float refAlpha = EditorGUILayout.Slider(ReferenceOpacitySliderContent, RectTransformSnapperEngine.ReferenceAlpha, 0f, 1f);
                if (!Mathf.Approximately(refAlpha, RectTransformSnapperEngine.ReferenceAlpha))
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Change Reference Opacity");
                    st.referenceAlpha = refAlpha;
                    EditorUtility.SetDirty(st);
                    RectTransformSnapperEngine.ReferenceAlpha = refAlpha;
                    UpdateExistingReferenceOverlayColorAndAlpha();
                }
                bool alwaysOnTop = EditorGUILayout.Toggle(ReferenceAlwaysOnTopToggleContent, RectTransformSnapperEngine.ReferenceAlwaysOnTop);
                if (alwaysOnTop != RectTransformSnapperEngine.ReferenceAlwaysOnTop)
                {
                    var st = UndoState;
                    Undo.RecordObject(st, "Toggle Reference Always On Top");
                    EditorUtility.SetDirty(st);
                    RectTransformSnapperEngine.ReferenceAlwaysOnTop = alwaysOnTop;
                    if (RectTransformSnapperEngine.AssignedCanvas != null && TryGetReferenceImage(out var refImg))
                    {
                        Undo.RecordObject(refImg.transform, "Move Reference On Top");
                        refImg.transform.SetAsLastSibling();
                        EditorUtility.SetDirty(refImg.transform);
                        RectTransformSnapperEngine.ActiveReferenceImage = refImg.transform;
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUIUtility.fieldWidth = oldFieldWidth;
            if (_waitingForPicker)
            {
                string cmd = Event.current.commandName;
                if (cmd == "ObjectSelectorClosed")
                {
                    var obj = EditorGUIUtility.GetObjectPickerObject();
                    _waitingForPicker = false;
                    if (obj is Sprite sp)
                    {
                        var canvas3 = RectTransformSnapperEngine.AssignedCanvas;
                        if (canvas3 != null)
                        {
                            var canvasRT = canvas3.GetComponent<RectTransform>();
                            if (canvasRT != null)
                            {
                                if (TryGetReferenceImage(out var existing))
                                {
                                    Undo.RecordObject(existing, "Update Reference Sprite");
                                    existing.sprite = sp;
                                    var c = RectTransformSnapperEngine.ReferenceColor;
                                    existing.color = new Color(c.r, c.g, c.b, RectTransformSnapperEngine.ReferenceAlpha);
                                    EditorUtility.SetDirty(existing);
                                    var rtRef = existing.GetComponent<RectTransform>();
                                    Vector2 realSize = GetRealTextureSize(sp);
                                    rtRef.sizeDelta = (realSize.x > 0 && realSize.y > 0) ? realSize : new Vector2(sp.rect.width, sp.rect.height);
                                    Selection.activeGameObject = existing.gameObject;
                                    RectTransformSnapperEngine.ActiveReferenceImage = existing.transform;
                                }
                                else
                                {
                                    CreateReferenceOverlay(sp, canvasRT);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void DrawSettingsPanel()
        {
            bool proportionalChildren = EditorGUILayout.Toggle(ResizeChildrenToggleContent, RectTransformSnapperEngine.ProportionalChildrenEnabled);
            if (proportionalChildren != RectTransformSnapperEngine.ProportionalChildrenEnabled)
            {
                var st = UndoState;
                Undo.RecordObject(st, "Toggle Resize Children");
                st.proportionalChildrenEnabled = proportionalChildren;
                EditorUtility.SetDirty(st);
                RectTransformSnapperEngine.ProportionalChildrenEnabled = proportionalChildren;
                SceneView.RepaintAll();
            }
            bool snapToRectEdges = EditorGUILayout.Toggle(SnapToRectEdgesToggleContent, RectTransformSnapperEngine.SnapToRectEdges);
            if (snapToRectEdges != RectTransformSnapperEngine.SnapToRectEdges)
            {
                RectTransformSnapperEngine.SnapToRectEdges = snapToRectEdges;
                SceneView.RepaintAll();
            }
        }
        private void DrawSeparator()
        {
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0, 0, 0, COLOR_ALPHA_SEPARATOR));
        }
        private void RemoveActiveReferenceOverlay()
        {
            var activeRef = RectTransformSnapperEngine.ActiveReferenceImage;
            if (activeRef != null && activeRef.gameObject != null)
            {
                Undo.DestroyObjectImmediate(activeRef.gameObject);
                RectTransformSnapperEngine.ActiveReferenceImage = null;
            }
        }
        private void CreateReferenceOverlay(Sprite sprite, RectTransform canvasRT)
        {
            SetMaxTextureSize(sprite);
            GameObject go = new GameObject("Reference_" + sprite.name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            Undo.RegisterCreatedObjectUndo(go, "Add Ref Overlay");
            go.transform.SetParent(canvasRT, false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            var c = RectTransformSnapperEngine.ReferenceColor;
            img.color = new Color(c.r, c.g, c.b, RectTransformSnapperEngine.ReferenceAlpha);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            Vector2 realSize = GetRealTextureSize(sprite);
            if (realSize.x > 0 && realSize.y > 0)
            {
                rt.sizeDelta = realSize;
            }
            else
            {
                rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            }
            Selection.activeGameObject = go;
            RectTransformSnapperEngine.ActiveReferenceImage = go.transform;
            if (RectTransformSnapperEngine.ReferenceAlwaysOnTop)
            {
                go.transform.SetAsLastSibling();
            }
        }
        private void UpdateExistingReferenceOverlayColorAndAlpha()
        {
            if (!TryGetReferenceImage(out var refImage)) return;
            Undo.RecordObject(refImage, "Update Reference Appearance");
            var c = RectTransformSnapperEngine.ReferenceColor;
            refImage.color = new Color(c.r, c.g, c.b, RectTransformSnapperEngine.ReferenceAlpha);
            EditorUtility.SetDirty(refImage);
        }
        private void OpenManual()
        {
            var manual = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MANUAL_ASSET_PATH);
            if (manual != null)
            {
                Selection.activeObject = manual;
                EditorGUIUtility.PingObject(manual);
                AssetDatabase.OpenAsset(manual);
            }
            else
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Manual not found at:\n" + MANUAL_ASSET_PATH, "OK");
            }
        }
        private void TryLoadLogo()
        {
            if (_logoTex != null) return;
            string path = System.IO.Path.Combine(ICONS_BASE_PATH, LOGO_FILE_NAME).Replace("\\", "/");
            _logoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}