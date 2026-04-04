using UnityEngine;
using UnityEditor;
namespace Multitool.RectTransformSnapper
{
    public static class RectTransformSnapperToolsMenu
    {
        #region Menu Paths (No MenuItem hotkeys - avoid conflicts with Shortcut Manager)
        private const string MENU_ROOT = "Tools/Multitool/RectTransformSnapper/";
        private const string MENU_ENABLE = MENU_ROOT + "Enable";
        private const string MENU_FIT_TO_GRID = MENU_ROOT + "Fit To Grid";
        private const string MENU_DEFAULT_SETTINGS_ROOT = MENU_ROOT + "Default Settings/";
        private const string MENU_SAVE_DEFAULTS = MENU_DEFAULT_SETTINGS_ROOT + "Save Current As Defaults";
        private const string MENU_RESET_TO_DEFAULTS = MENU_DEFAULT_SETTINGS_ROOT + "Reset To Defaults";
        private const string MENU_CLEAR_SAVED_DEFAULTS = MENU_DEFAULT_SETTINGS_ROOT + "Clear Saved Defaults";
        #endregion
        #region Enable/Disable RectTransformSnapper
        [MenuItem(MENU_ENABLE, false, 0)]
        public static void ToggleRectTransformSnapper()
        {
            RectTransformSnapperEngine.ToggleEnabledAndGrid();
        }
        [MenuItem(MENU_ENABLE, true)]
        public static bool ToggleRectTransformSnapperValidate()
        {
            Menu.SetChecked(MENU_ENABLE, RectTransformSnapperEngine.Enabled);
            return true;
        }
        #endregion
        #region Open Window
        [MenuItem("Tools/Multitool/RectTransformSnapper/Open Settings Window", false, 10)]
        public static void OpenSettingsWindow()
        {
            RectTransformSnapperWindow.OpenFromWindow();
        }
        #endregion
        #region Overlay Windows
        [MenuItem("Tools/Multitool/RectTransformSnapper/Grid Settings Overlay", false, 30)]
        public static void ToggleGridOverlay()
        {
            RectTransformSnapperGridOverlay.OverlayEnabled = !RectTransformSnapperGridOverlay.OverlayEnabled;
            SceneView.RepaintAll();
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Grid Settings Overlay", true)]
        public static bool ToggleGridOverlayValidate()
        {
            Menu.SetChecked("Tools/Multitool/RectTransformSnapper/Grid Settings Overlay", RectTransformSnapperGridOverlay.OverlayEnabled);
            return true;
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Alignment Overlay", false, 31)]
        public static void ToggleAlignmentOverlay()
        {
            RectTransformSnapperAlignmentOverlay.OverlayEnabled = !RectTransformSnapperAlignmentOverlay.OverlayEnabled;
            SceneView.RepaintAll();
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Alignment Overlay", true)]
        public static bool ToggleAlignmentOverlayValidate()
        {
            Menu.SetChecked("Tools/Multitool/RectTransformSnapper/Alignment Overlay", RectTransformSnapperAlignmentOverlay.OverlayEnabled);
            return true;
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Reference Overlay", false, 32)]
        public static void ToggleReferenceOverlay()
        {
            RectTransformSnapperReferenceOverlay.OverlayEnabled = !RectTransformSnapperReferenceOverlay.OverlayEnabled;
            SceneView.RepaintAll();
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Reference Overlay", true)]
        public static bool ToggleReferenceOverlayValidate()
        {
            Menu.SetChecked("Tools/Multitool/RectTransformSnapper/Reference Overlay", RectTransformSnapperReferenceOverlay.OverlayEnabled);
            return true;
        }
        #endregion
        #region Grid Operations
        [MenuItem("Tools/Multitool/RectTransformSnapper/Increase Grid Step", false, 50)]
        public static void IncreaseGridStep()
        {
            RectTransformSnapperEngine.MultiplyGridStep(2f);
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Decrease Grid Step", false, 51)]
        public static void DecreaseGridStep()
        {
            RectTransformSnapperEngine.MultiplyGridStep(0.5f);
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Increase Subdivisions", false, 52)]
        public static void IncreaseSubdivisions()
        {
            RectTransformSnapperEngine.MultiplySubdivisions(2f);
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Decrease Subdivisions", false, 53)]
        public static void DecreaseSubdivisions()
        {
            RectTransformSnapperEngine.MultiplySubdivisions(0.5f);
        }
        #endregion
        #region Fit Operations
        [MenuItem(MENU_FIT_TO_GRID, false, 70)]
        public static void FitToGrid()
        {
            if (Selection.transforms == null || Selection.transforms.Length == 0) return;
            RectTransformSnapperEngine.FitSelectedToGrid();
        }
        [MenuItem(MENU_FIT_TO_GRID, true)]
        public static bool FitToGridValidate()
        {
            return Selection.transforms != null && Selection.transforms.Length > 0;
        }
        #endregion
        #region Default Settings
        [MenuItem(MENU_SAVE_DEFAULTS, false, 120)]
        public static void SaveDefaultsFromCurrent()
        {
            RectTransformSnapperEngine.SaveDefaultsFromCurrent();
            Debug.Log("Rect Transform Snapper: saved current settings as defaults.");
        }
        [MenuItem(MENU_RESET_TO_DEFAULTS, false, 121)]
        public static void ResetToDefaults()
        {
            if (!EditorUtility.DisplayDialog("Reset To Defaults",
                "Are you sure you want to reset all settings to default values?\n\nThis will reset:\n- Grid settings\n- Snap parameters\n- Colors\n\nCanvas assignment will NOT be affected.",
                "Reset", "Cancel"))
                return;
            RectTransformSnapperEngine.ResetToDefaults();
            SceneView.RepaintAll();
        }
        [MenuItem(MENU_CLEAR_SAVED_DEFAULTS, false, 122)]
        public static void ClearSavedDefaults()
        {
            RectTransformSnapperEngine.ClearSavedDefaults();
            Debug.Log("Rect Transform Snapper: cleared saved defaults.");
        }
        [MenuItem(MENU_CLEAR_SAVED_DEFAULTS, true)]
        public static bool ClearSavedDefaultsValidate()
        {
            return RectTransformSnapperEngine.HasSavedDefaults;
        }
        #endregion
        #region Toggle Operations
        [MenuItem("Tools/Multitool/RectTransformSnapper/Toggle Resize Children", false, 71)]
        public static void ToggleResizeChildren()
        {
            RectTransformSnapperEngine.ToggleResizeChildren();
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Toggle Resize Children", true)]
        public static bool ToggleResizeChildrenValidate()
        {
            Menu.SetChecked("Tools/Multitool/RectTransformSnapper/Toggle Resize Children", RectTransformSnapperEngine.ProportionalChildrenEnabled);
            return true;
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Toggle Snap To Rect Edges", false, 72)]
        public static void ToggleSnapToRectEdges()
        {
            RectTransformSnapperEngine.ToggleSnapToRectEdges();
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Toggle Snap To Rect Edges", true)]
        public static bool ToggleSnapToRectEdgesValidate()
        {
            Menu.SetChecked("Tools/Multitool/RectTransformSnapper/Toggle Snap To Rect Edges", RectTransformSnapperEngine.SnapToRectEdges);
            return true;
        }
        #endregion
        #region Reference
        [MenuItem("Tools/Multitool/RectTransformSnapper/Set Reference", false, 90)]
        public static void SetReference()
        {
            RectTransformSnapperEngine.AddReferenceViaPicker();
        }
        [MenuItem("Tools/Multitool/RectTransformSnapper/Set Reference", true)]
        public static bool SetReferenceValidate()
        {
            return RectTransformSnapperEngine.AssignedCanvas != null;
        }
        #endregion
        #region Canvas
        [MenuItem("Tools/Multitool/RectTransformSnapper/Create New Canvas", false, 110)]
        public static void CreateNewCanvas()
        {
            RectTransformSnapperEngine.CreateNewCanvasAndAssign();
        }
        #endregion
    }
}