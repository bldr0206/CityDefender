using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UI;
namespace Multitool.RectTransformSnapper
{
    [Overlay(typeof(SceneView), "Grid Settings", true)]
    public class RectTransformSnapperGridOverlay : IMGUIOverlay, ITransientOverlay
    {
        private const string PREF_OVERLAY_VISIBLE = "RTS_GridOverlayVisible";
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
        public override void OnGUI()
        {
            if (!OverlayEnabled) return;
            if (GUI.skin == null) return;
            float oldLabel = EditorGUIUtility.labelWidth;
            float oldField = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = 70f;
            EditorGUIUtility.fieldWidth = 36f;
            GUILayout.BeginVertical(GUILayout.Width(190));
            GUILayout.BeginHorizontal();
            bool en = EditorGUILayout.Toggle(new GUIContent("Enabled"), RectTransformSnapperEngine.Enabled);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("\u00d7", "Close"), GUI.skin.button, GUILayout.Width(18), GUILayout.Height(16)))
            {
                OverlayEnabled = false;
                RepaintMainWindow();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                EditorGUIUtility.labelWidth = oldLabel;
                EditorGUIUtility.fieldWidth = oldField;
                return;
            }
            GUILayout.EndHorizontal();
            if (en != RectTransformSnapperEngine.Enabled)
            {
                RectTransformSnapperEngine.Enabled = en;
                RectTransformSnapperEngine.ShowGrid = en;
                SceneView.RepaintAll();
                RepaintMainWindow();
            }
            GUILayout.BeginHorizontal(GUILayout.Width(190));
            int stepInt = Mathf.Max(1, Mathf.RoundToInt(RectTransformSnapperEngine.SnapStep));
            stepInt = EditorGUILayout.IntField(new GUIContent("Step"), stepInt);
            if (GUILayout.Button("÷2", GUILayout.Width(24))) stepInt = Mathf.Max(1, Mathf.RoundToInt(RectTransformSnapperEngine.SnapStep / 2f));
            if (GUILayout.Button("×2", GUILayout.Width(24))) stepInt = Mathf.Max(1, Mathf.RoundToInt(RectTransformSnapperEngine.SnapStep * 2f));
            GUILayout.EndHorizontal();
            if (!Mathf.Approximately(stepInt, RectTransformSnapperEngine.SnapStep))
            {
                var st = RectTransformSnapperSettingsUndoState.Instance;
                Undo.RecordObject(st, "Change Grid Step");
                st.snapStep = Mathf.Max(1f, stepInt);
                EditorUtility.SetDirty(st);
                RectTransformSnapperEngine.SnapStep = Mathf.Max(1f, stepInt);
                RepaintMainWindow();
            }
            GUILayout.BeginHorizontal(GUILayout.Width(190));
            int div = Mathf.Max(1, RectTransformSnapperEngine.SnapDivisor);
            div = EditorGUILayout.IntField(new GUIContent("Subdiv"), div);
            if (GUILayout.Button("÷2", GUILayout.Width(24))) div = Mathf.Max(1, RectTransformSnapperEngine.SnapDivisor / 2);
            if (GUILayout.Button("×2", GUILayout.Width(24))) div = Mathf.Max(1, RectTransformSnapperEngine.SnapDivisor * 2);
            GUILayout.EndHorizontal();
            div = Mathf.Max(1, div);
            if (div != RectTransformSnapperEngine.SnapDivisor)
            {
                var st = RectTransformSnapperSettingsUndoState.Instance;
                Undo.RecordObject(st, "Change Subdivisions");
                st.snapDivisor = div;
                EditorUtility.SetDirty(st);
                RectTransformSnapperEngine.SnapDivisor = div;
                RepaintMainWindow();
            }
            GUILayout.BeginHorizontal(GUILayout.Width(190));
            EditorGUILayout.PrefixLabel(new GUIContent("Offset"));
            float offX = RectTransformSnapperEngine.SnapOffsetPercentX;
            float offY = RectTransformSnapperEngine.SnapOffsetPercentY;
            GUILayout.Label("X", GUILayout.Width(12));
            offX = EditorGUILayout.FloatField(offX, GUILayout.Width(36));
            GUILayout.Space(4);
            GUILayout.Label("Y", GUILayout.Width(12));
            offY = EditorGUILayout.FloatField(offY, GUILayout.Width(36));
            GUILayout.EndHorizontal();
            offX = Mathf.Clamp(offX, -1f, 1f);
            offY = Mathf.Clamp(offY, -1f, 1f);
            bool changedOffX = !Mathf.Approximately(offX, RectTransformSnapperEngine.SnapOffsetPercentX);
            bool changedOffY = !Mathf.Approximately(offY, RectTransformSnapperEngine.SnapOffsetPercentY);
            if (changedOffX || changedOffY)
            {
                var st = RectTransformSnapperSettingsUndoState.Instance;
                Undo.RecordObject(st, "Change Grid Offset");
                st.offsetX = offX;
                st.offsetY = offY;
                EditorUtility.SetDirty(st);
                if (changedOffX) RectTransformSnapperEngine.SnapOffsetPercentX = offX;
                if (changedOffY) RectTransformSnapperEngine.SnapOffsetPercentY = offY;
                RepaintMainWindow();
            }
            GUILayout.EndVertical();
            EditorGUIUtility.labelWidth = oldLabel;
            EditorGUIUtility.fieldWidth = oldField;
        }
    }
}