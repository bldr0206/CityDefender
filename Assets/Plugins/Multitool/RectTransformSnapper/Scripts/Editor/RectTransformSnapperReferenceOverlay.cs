using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UI;
namespace Multitool.RectTransformSnapper
{
    [Overlay(typeof(SceneView), "Reference Overlay", true)]
    public class RectTransformSnapperReferenceOverlay : IMGUIOverlay, ITransientOverlay
    {
        #region Overlay_State
        private const string PREF_OVERLAY_VISIBLE = "RTS_RefOverlayVisible";
        private static bool _waitingForPicker;
        public static bool OverlayEnabled
        {
            get => EditorPrefs.GetBool(PREF_OVERLAY_VISIBLE, false);
            set
            {
                EditorPrefs.SetBool(PREF_OVERLAY_VISIBLE, value);
                SceneView.RepaintAll();
            }
        }
        bool ITransientOverlay.visible => OverlayEnabled;
        #endregion

        #region Window_Integration

        private static void TryRepaintMainWindowIfOpen()
        {

            var windows = Resources.FindObjectsOfTypeAll<RectTransformSnapperWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                var w = windows[i];
                if (w == null) continue;
                w.Repaint();
                return;
            }
        }

        #endregion

        #region GUI
        public override void OnGUI()
        {
            if (!OverlayEnabled) return;
            float oldLabel = EditorGUIUtility.labelWidth;
            float oldField = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = 70f;
            EditorGUIUtility.fieldWidth = 36f;
            GUILayout.BeginVertical(GUILayout.Width(190));
            GUILayout.BeginHorizontal(GUILayout.Width(190));
            using (new EditorGUI.DisabledScope(RectTransformSnapperEngine.AssignedCanvas == null))
            {
                if (GUILayout.Button("Add Ref", GUILayout.Width(80)))
                {
                    OpenSpritePicker();
                }
            }
            using (new EditorGUI.DisabledScope(RectTransformSnapperEngine.ActiveReferenceImage == null))
            {
                if (GUILayout.Button("Remove Ref", GUILayout.Width(80)))
                {
                    RemoveActiveReferenceOverlay();
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("\u00d7", "Close"), EditorStyles.miniButton, GUILayout.Width(18), GUILayout.Height(16)))
            {
                OverlayEnabled = false;
                TryRepaintMainWindowIfOpen();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                EditorGUIUtility.labelWidth = oldLabel;
                EditorGUIUtility.fieldWidth = oldField;
                return;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            using (new EditorGUI.DisabledScope(!HasValidReference()))
            {
                GUILayout.BeginHorizontal(GUILayout.Width(190));
                if (GUILayout.Button("Fit Ref to Canvas", GUILayout.ExpandWidth(true)))
                {
                    FitReferenceToCanvas();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal(GUILayout.Width(190));
            Color refColor = EditorGUILayout.ColorField(new GUIContent("Color"), RectTransformSnapperEngine.ReferenceColor);
            GUILayout.EndHorizontal();
            if (refColor != RectTransformSnapperEngine.ReferenceColor)
            {
                RectTransformSnapperEngine.ReferenceColor = refColor;
                ApplyReferenceAppearanceToActiveImage();
            }
            GUILayout.BeginHorizontal(GUILayout.Width(190));
            float alpha = EditorGUILayout.Slider(new GUIContent("Opacity"), RectTransformSnapperEngine.ReferenceAlpha, 0f, 1f);
            GUILayout.EndHorizontal();
            if (!Mathf.Approximately(alpha, RectTransformSnapperEngine.ReferenceAlpha))
            {
                RectTransformSnapperEngine.ReferenceAlpha = alpha;
                ApplyReferenceAppearanceToActiveImage();
            }
            GUILayout.EndVertical();
            EditorGUIUtility.labelWidth = oldLabel;
            EditorGUIUtility.fieldWidth = oldField;
            if (_waitingForPicker)
            {
                string cmd = Event.current.commandName;
                if (cmd == "ObjectSelectorClosed")
                {
                    _waitingForPicker = false;
                    var obj = EditorGUIUtility.GetObjectPickerObject();
                    if (obj is Sprite sp) ApplyPickedSprite(sp);
                }
            }
        }
        #endregion

        #region Reference_Operations
        private static void OpenSpritePicker()
        {
            if (RectTransformSnapperEngine.AssignedCanvas == null)
            {
                EditorUtility.DisplayDialog("Rect Transform Snapper", "Assign a Canvas first.", "OK");
                return;
            }
            _waitingForPicker = true;
            EditorGUIUtility.ShowObjectPicker<Sprite>(null, false, string.Empty, "RTS_REF_PICKER".GetHashCode());
        }
        private static bool HasValidReference()
        {
            var tr = RectTransformSnapperEngine.ActiveReferenceImage;
            if (tr == null) return false;
            var img = tr.GetComponent<Image>();
            return img != null && img.sprite != null;
        }
        private static void ApplyPickedSprite(Sprite sp)
        {
            var canvas = RectTransformSnapperEngine.AssignedCanvas;
            if (canvas == null) return;
            var canvasRT = canvas.GetComponent<RectTransform>();
            if (canvasRT == null) return;
            if (TryGetReferenceImage(out var existing))
            {
                Undo.RecordObject(existing, "Update Reference Sprite");
                existing.sprite = sp;
                var c = RectTransformSnapperEngine.ReferenceColor;
                existing.color = new Color(c.r, c.g, c.b, RectTransformSnapperEngine.ReferenceAlpha);
                EditorUtility.SetDirty(existing);
                var rtRef = existing.GetComponent<RectTransform>();
                if (rtRef != null)
                {
                    var r = sp.rect;
                    rtRef.sizeDelta = new Vector2(r.width, r.height);
                    EditorUtility.SetDirty(rtRef);
                }
                RectTransformSnapperEngine.ActiveReferenceImage = existing.transform;
            }
            else
            {
                CreateReferenceOverlay(sp, canvasRT);
            }
        }
        private static void CreateReferenceOverlay(Sprite sprite, RectTransform canvasRT)
        {
            if (sprite == null || canvasRT == null) return;
            GameObject go = new GameObject("Reference_" + sprite.name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Add Ref Overlay");
            go.transform.SetParent(canvasRT, false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            var c = RectTransformSnapperEngine.ReferenceColor;
            img.color = new Color(c.r, c.g, c.b, RectTransformSnapperEngine.ReferenceAlpha);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            var r = sprite.rect;
            rt.sizeDelta = new Vector2(r.width, r.height);
            RectTransformSnapperEngine.ActiveReferenceImage = go.transform;
        }
        private static void RemoveActiveReferenceOverlay()
        {
            var tr = RectTransformSnapperEngine.ActiveReferenceImage;
            if (tr == null) return;
            Undo.DestroyObjectImmediate(tr.gameObject);
            RectTransformSnapperEngine.ActiveReferenceImage = null;
        }
        private static void FitReferenceToCanvas()
        {
            var canvas = RectTransformSnapperEngine.AssignedCanvas;
            if (canvas == null) return;
            if (!TryGetReferenceImage(out var refImage) || refImage == null) return;
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
                if (canvasRT != null) targetSize = canvasRT.rect.size;
            }
            if (targetSize.x <= 0f || targetSize.y <= 0f) return;
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
        private static void ApplyReferenceAppearanceToActiveImage()
        {
            if (!TryGetReferenceImage(out var refImage)) return;
            Undo.RecordObject(refImage, "Update Reference Appearance");
            var c = RectTransformSnapperEngine.ReferenceColor;
            refImage.color = new Color(c.r, c.g, c.b, RectTransformSnapperEngine.ReferenceAlpha);
            EditorUtility.SetDirty(refImage);
            SceneView.RepaintAll();
        }
        private static bool TryGetReferenceImage(out Image image)
        {
            image = null;
            var canvas = RectTransformSnapperEngine.AssignedCanvas;
            if (canvas == null) return false;
            var rt = canvas.GetComponent<RectTransform>();
            if (rt == null) return false;
            for (int i = rt.childCount - 1; i >= 0; i--)
            {
                var child = rt.GetChild(i) as RectTransform;
                if (child == null) continue;
                if (!child.gameObject.name.StartsWith("Reference_")) continue;
                var img = child.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    image = img;
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}