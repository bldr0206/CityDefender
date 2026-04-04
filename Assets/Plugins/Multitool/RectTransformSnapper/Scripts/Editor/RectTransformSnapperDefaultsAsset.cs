using UnityEngine;
using UnityEditor;
namespace Multitool.RectTransformSnapper
{
    public class RectTransformSnapperDefaultsAsset : ScriptableObject
    {
        // Core
        public bool enabled = false;
        public bool proportionalChildrenEnabled = true;
        public bool snapToRectEdges = true;

        // Grid / Canvas
        public int canvasOriginIndex = 4;
        public bool alignToCanvas = false;
        public float snapStep = 64f;
        public int snapDivisor = 1;
        public float snapOffsetPercentX = 0f;
        public float snapOffsetPercentY = 0f;
        public bool showGrid = true;
        public float dotSize = 1f;
        public Color dotColor = new Color(0.6784314f, 0.6784314f, 0.6784314f, 1f);

        // Reference
        public Color referenceColor = new Color(1f, 1f, 1f, 1f);
        public float referenceAlpha = 0.5f;
        public bool referenceAlwaysOnTop = true;

        public const string AssetPath = "Assets/Plugins/Multitool/RectTransformSnapper/Settings/RectTransformSnapperDefaults.asset";
        public static RectTransformSnapperDefaultsAsset Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RectTransformSnapperDefaultsAsset>(AssetPath);
            if (asset != null) return asset;
            var guids = AssetDatabase.FindAssets("t:RectTransformSnapperDefaultsAsset");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                asset = AssetDatabase.LoadAssetAtPath<RectTransformSnapperDefaultsAsset>(path);
            }
            return asset;
        }
    }
}