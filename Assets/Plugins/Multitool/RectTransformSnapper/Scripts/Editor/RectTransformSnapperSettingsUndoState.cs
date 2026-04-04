using UnityEngine;
using UnityEditor;
namespace Multitool.RectTransformSnapper
{
    public class RectTransformSnapperSettingsUndoState : ScriptableObject
    {
        public float snapStep;
        public int snapDivisor;
        public float offsetX;
        public float offsetY;
        public float dotSize;
        public Color dotColor;
        public int canvasOriginIndex;
        public float canvasSnapThreshold;
        public bool proportionalChildrenEnabled;
        public Color referenceColor;
        public float referenceAlpha;
        private static RectTransformSnapperSettingsUndoState _instance;
        internal static RectTransformSnapperSettingsUndoState Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CreateInstance<RectTransformSnapperSettingsUndoState>();
                    _instance.hideFlags = HideFlags.HideAndDontSave;
                    _instance.ReadFromEngine();
                }
                return _instance;
            }
        }
        internal void ReadFromEngine()
        {
            snapStep = RectTransformSnapperEngine.SnapStep;
            snapDivisor = RectTransformSnapperEngine.SnapDivisor;
            offsetX = RectTransformSnapperEngine.SnapOffsetPercentX;
            offsetY = RectTransformSnapperEngine.SnapOffsetPercentY;
            dotSize = RectTransformSnapperEngine.DotSize;
            dotColor = RectTransformSnapperEngine.DotColor;
            canvasOriginIndex = RectTransformSnapperEngine.CanvasOriginIndex;
            canvasSnapThreshold = RectTransformSnapperEngine.CanvasSnapThreshold;
            proportionalChildrenEnabled = RectTransformSnapperEngine.ProportionalChildrenEnabled;
            referenceColor = RectTransformSnapperEngine.ReferenceColor;
            referenceAlpha = RectTransformSnapperEngine.ReferenceAlpha;
        }
        internal void ApplyToEngine()
        {
            RectTransformSnapperEngine.SnapStep = Mathf.Max(1f, snapStep);
            RectTransformSnapperEngine.SnapDivisor = Mathf.Max(1, snapDivisor);
            RectTransformSnapperEngine.SnapOffsetPercentX = Mathf.Clamp(offsetX, -1f, 1f);
            RectTransformSnapperEngine.SnapOffsetPercentY = Mathf.Clamp(offsetY, -1f, 1f);
            RectTransformSnapperEngine.DotSize = Mathf.Clamp(dotSize, 1f, 4f);
            RectTransformSnapperEngine.DotColor = dotColor;
            RectTransformSnapperEngine.CanvasOriginIndex = Mathf.Clamp(canvasOriginIndex, 0, 8);
            RectTransformSnapperEngine.CanvasSnapThreshold = Mathf.Max(1f, canvasSnapThreshold);
            RectTransformSnapperEngine.ProportionalChildrenEnabled = proportionalChildrenEnabled;
            RectTransformSnapperEngine.ReferenceColor = referenceColor;
            RectTransformSnapperEngine.ReferenceAlpha = referenceAlpha;
        }
    }
}