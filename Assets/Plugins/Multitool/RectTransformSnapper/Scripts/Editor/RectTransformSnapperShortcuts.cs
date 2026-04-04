using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
namespace Multitool.RectTransformSnapper
{
    public static class RectTransformSnapperShortcuts
    {
        #region Shortcut Names
        private const string SHORTCUT_ROOT = "Multitool/RectTransformSnapper/";
        private const string SC_ENABLE = SHORTCUT_ROOT + "Enable";
        private const string SC_OPEN_SETTINGS_WINDOW = SHORTCUT_ROOT + "Open Settings Window";
        private const string SC_TOGGLE_GRID_OVERLAY = SHORTCUT_ROOT + "Grid Settings Overlay";
        private const string SC_TOGGLE_ALIGNMENT_OVERLAY = SHORTCUT_ROOT + "Alignment Overlay";
        private const string SC_TOGGLE_REFERENCE_OVERLAY = SHORTCUT_ROOT + "Reference Overlay";
        private const string SC_TOGGLE_RESIZE_CHILDREN = SHORTCUT_ROOT + "Toggle Resize Children";
        private const string SC_TOGGLE_SNAP_TO_RECT_EDGES = SHORTCUT_ROOT + "Toggle Snap To Rect Edges";
        private const string SC_DECREASE_GRID_STEP = SHORTCUT_ROOT + "Decrease Grid Step";
        private const string SC_INCREASE_GRID_STEP = SHORTCUT_ROOT + "Increase Grid Step";
        private const string SC_DECREASE_SUBDIVISIONS = SHORTCUT_ROOT + "Decrease Subdivisions";
        private const string SC_INCREASE_SUBDIVISIONS = SHORTCUT_ROOT + "Increase Subdivisions";
        private const string SC_CREATE_NEW_CANVAS = SHORTCUT_ROOT + "Create New Canvas";
        private const string SC_SET_REFERENCE = SHORTCUT_ROOT + "Set Reference";
        private const string SC_FIT_TO_GRID = SHORTCUT_ROOT + "Fit To Grid";
        #endregion
        #region Toggle
        [Shortcut(SC_ENABLE, KeyCode.Quote, ShortcutModifiers.Control)]
        private static void ToggleEnable()
        {
            RectTransformSnapperEngine.ToggleEnabledAndGrid();
        }
        [Shortcut(SC_OPEN_SETTINGS_WINDOW)]
        private static void OpenSettingsWindow()
        {
            RectTransformSnapperWindow.OpenFromWindow();
        }
        [Shortcut(SC_TOGGLE_GRID_OVERLAY)]
        private static void ToggleGridOverlay()
        {
            RectTransformSnapperGridOverlay.OverlayEnabled = !RectTransformSnapperGridOverlay.OverlayEnabled;
            SceneView.RepaintAll();
        }
        [Shortcut(SC_TOGGLE_ALIGNMENT_OVERLAY)]
        private static void ToggleAlignmentOverlay()
        {
            RectTransformSnapperAlignmentOverlay.OverlayEnabled = !RectTransformSnapperAlignmentOverlay.OverlayEnabled;
            SceneView.RepaintAll();
        }
        [Shortcut(SC_TOGGLE_REFERENCE_OVERLAY)]
        private static void ToggleReferenceOverlay()
        {
            RectTransformSnapperReferenceOverlay.OverlayEnabled = !RectTransformSnapperReferenceOverlay.OverlayEnabled;
            SceneView.RepaintAll();
        }
        [Shortcut(SC_TOGGLE_RESIZE_CHILDREN)]
        private static void ToggleResizeChildren()
        {
            RectTransformSnapperEngine.ToggleResizeChildren();
        }
        [Shortcut(SC_TOGGLE_SNAP_TO_RECT_EDGES)]
        private static void ToggleSnapToRectEdges()
        {
            RectTransformSnapperEngine.ToggleSnapToRectEdges();
        }
        #endregion
        #region GridStep
        [Shortcut(SC_DECREASE_GRID_STEP)]
        private static void DecreaseGridStep()
        {
            RectTransformSnapperEngine.MultiplyGridStep(0.5f);
        }
        [Shortcut(SC_INCREASE_GRID_STEP)]
        private static void IncreaseGridStep()
        {
            RectTransformSnapperEngine.MultiplyGridStep(2f);
        }
        #endregion
        #region Subdivisions
        [Shortcut(SC_DECREASE_SUBDIVISIONS)]
        private static void DecreaseSubdivisions()
        {
            RectTransformSnapperEngine.MultiplySubdivisions(0.5f);
        }
        [Shortcut(SC_INCREASE_SUBDIVISIONS)]
        private static void IncreaseSubdivisions()
        {
            RectTransformSnapperEngine.MultiplySubdivisions(2f);
        }
        #endregion
        #region Canvas
        [Shortcut(SC_CREATE_NEW_CANVAS)]
        private static void CreateNewCanvas()
        {
            RectTransformSnapperEngine.CreateNewCanvasAndAssign();
        }
        #endregion
        #region Reference
        [Shortcut(SC_SET_REFERENCE)]
        private static void SetReference()
        {
            RectTransformSnapperEngine.AddReferenceViaPicker();
        }
        #endregion
        #region Fit
        [Shortcut(SC_FIT_TO_GRID, KeyCode.F, ShortcutModifiers.Alt)]
        private static void FitToGrid()
        {
            if (Selection.transforms == null || Selection.transforms.Length == 0) return;
            RectTransformSnapperEngine.FitSelectedToGrid();
        }
        #endregion
    }
}