using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
namespace Multitool.RectTransformSnapper
{
    [Overlay(typeof(SceneView), "Alignment", true)]
    public class RectTransformSnapperAlignmentOverlay : IMGUIOverlay, ITransientOverlay
    {
        private const string PREF_OVERLAY_VISIBLE = "RTS_AlignmentOverlayVisible";
        #region UI Constants
        private const float BUTTON_SIZE = 28f;
        private const float OVERLAY_WIDTH = 138f;
        private const float BUTTON_SPACING = 0;
        private const float GROUP_SPACING = 4f;
        #endregion
        public static bool OverlayEnabled
        {
            get => EditorPrefs.GetBool(PREF_OVERLAY_VISIBLE, false);
            set
            {
                EditorPrefs.SetBool(PREF_OVERLAY_VISIBLE, value);
                SceneView.RepaintAll();
                RepaintMainWindow();
            }
        }
        bool ITransientOverlay.visible => OverlayEnabled;
        #region UI Sync
        private static void RepaintMainWindow()
        {
            var windows = Resources.FindObjectsOfTypeAll<RectTransformSnapperWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null) windows[i].Repaint();
            }
        }
        #endregion
        public override void OnGUI()
        {
            if (!OverlayEnabled) return;
            float oldLabel = EditorGUIUtility.labelWidth;
            float oldField = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = 70f;
            EditorGUIUtility.fieldWidth = 36f;
            int selCount = Selection.transforms != null ? Selection.transforms.Length : 0;
            bool canAlign = selCount >= 2;
            bool canDistribute = selCount >= 3;
            GUILayout.BeginVertical(GUILayout.Width(OVERLAY_WIDTH));
            GUILayout.BeginHorizontal();
            bool alignToCanvas = EditorGUILayout.ToggleLeft(new GUIContent("Align to Canvas", "Align to Canvas boundaries instead of the selection bounding box."), RectTransformSnapperEngine.AlignToCanvas);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.IconContent("winbtn_win_close"), GUIStyle.none, GUILayout.Width(16), GUILayout.Height(16)))
            {
                OverlayEnabled = false;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                EditorGUIUtility.labelWidth = oldLabel;
                EditorGUIUtility.fieldWidth = oldField;
                return;
            }
            GUILayout.EndHorizontal();
            if (alignToCanvas != RectTransformSnapperEngine.AlignToCanvas)
            {
                RectTransformSnapperEngine.AlignToCanvas = alignToCanvas;
                SceneView.RepaintAll();
                RepaintMainWindow();
            }
            GUILayout.Space(4);
            using (new EditorGUI.DisabledScope(!canAlign))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Left), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                    RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Left);
                GUILayout.Space(BUTTON_SPACING);
                if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.CenterX), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                    RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.CenterX);
                GUILayout.Space(BUTTON_SPACING);
                if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Right), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                    RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Right);
                GUILayout.Space(GROUP_SPACING);
                using (new EditorGUI.DisabledScope(!canDistribute))
                {
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.HorizontalCenters), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                        RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.HorizontalCenters);
                    GUILayout.Space(BUTTON_SPACING);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.HorizontalSpacing), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                        RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.HorizontalSpacing);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(2);
            using (new EditorGUI.DisabledScope(!canAlign))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Top), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                    RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Top);
                GUILayout.Space(BUTTON_SPACING);
                if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.CenterY), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                    RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.CenterY);
                GUILayout.Space(BUTTON_SPACING);
                if (GUILayout.Button(RectTransformSnapperEditorIcons.GetAlign(RectTransformSnapperEngine.AlignMode.Bottom), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                    RectTransformSnapperEngine.AlignSelected(RectTransformSnapperEngine.AlignMode.Bottom);
                GUILayout.Space(GROUP_SPACING);
                using (new EditorGUI.DisabledScope(!canDistribute))
                {
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.VerticalCenters), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                        RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.VerticalCenters);
                    GUILayout.Space(BUTTON_SPACING);
                    if (GUILayout.Button(RectTransformSnapperEditorIcons.GetDistribute(RectTransformSnapperEngine.DistributeMode.VerticalSpacing), GUILayout.Width(BUTTON_SIZE), GUILayout.Height(BUTTON_SIZE)))
                        RectTransformSnapperEngine.DistributeSelected(RectTransformSnapperEngine.DistributeMode.VerticalSpacing);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            EditorGUIUtility.labelWidth = oldLabel;
            EditorGUIUtility.fieldWidth = oldField;
        }
    }
}