using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Multitool.RectTransformSnapper
{
    internal static class RectTransformSnapperEditorIcons
    {
        private const string ALIGNMENT_ICONS_BASE_PATH = "Assets/Plugins/Multitool/RectTransformSnapper/Icons/Alignment/";
        private const string ICONS_BASE_PATH = "Assets/Plugins/Multitool/RectTransformSnapper/Icons/";
        #region Caches
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GUIContent> ContentCache = new Dictionary<string, GUIContent>();
        #endregion
        #region Public API
        public static GUIContent GetAlign(RectTransformSnapperEngine.AlignMode mode)
        {
            return mode switch
            {
                RectTransformSnapperEngine.AlignMode.Left => GetContent("align_left", "left.png", "Align Left"),
                RectTransformSnapperEngine.AlignMode.CenterX => GetContent("align_center_x", "horizontal.png", "Align Center (X)"),
                RectTransformSnapperEngine.AlignMode.Right => GetContent("align_right", "right.png", "Align Right"),
                RectTransformSnapperEngine.AlignMode.Top => GetContent("align_top", "top.png", "Align Top"),
                RectTransformSnapperEngine.AlignMode.CenterY => GetContent("align_center_y", "vertical.png", "Align Center (Y)"),
                RectTransformSnapperEngine.AlignMode.Bottom => GetContent("align_bottom", "bottom.png", "Align Bottom"),
                _ => GUIContent.none
            };
        }
        public static GUIContent GetCenter()
        {
            return GetContentFromBase("align_center_both", "Center.png", "Align Center (X & Y)");
        }
        public static GUIContent GetDistribute(RectTransformSnapperEngine.DistributeMode mode)
        {
            return mode switch
            {
                RectTransformSnapperEngine.DistributeMode.HorizontalCenters => GetContent("dist_centers_x", "distribute_horizontal.png", "Distribute Centers (X)"),
                RectTransformSnapperEngine.DistributeMode.VerticalCenters => GetContent("dist_centers_y", "distribute_vertical.png", "Distribute Centers (Y)"),
                RectTransformSnapperEngine.DistributeMode.HorizontalSpacing => GetContent("dist_spacing_x", "distribute_horizontal_interval.png", "Distribute Spacing (X)"),
                RectTransformSnapperEngine.DistributeMode.VerticalSpacing => GetContent("dist_spacing_y", "distribute_vertical_interval.png", "Distribute Spacing (Y)"),
                _ => GUIContent.none
            };
        }
        #endregion
        #region Internals
        private static GUIContent GetContent(string key, string fileName, string tooltip)
        {
            if (ContentCache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var tex = GetTexture(fileName);
            var gc = tex != null ? new GUIContent(tex, tooltip) : new GUIContent(string.Empty, tooltip);
            ContentCache[key] = gc;
            return gc;
        }
        private static GUIContent GetContentFromBase(string key, string fileName, string tooltip)
        {
            if (ContentCache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var tex = GetTextureFromBase(fileName);
            var gc = tex != null ? new GUIContent(tex, tooltip) : new GUIContent(string.Empty, tooltip);
            ContentCache[key] = gc;
            return gc;
        }
        private static Texture2D GetTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;
            if (TextureCache.TryGetValue(fileName, out var cached) && cached != null)
                return cached;
            var path = (ALIGNMENT_ICONS_BASE_PATH + fileName).Replace("\\", "/");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureCache[fileName] = tex;
            return tex;
        }
        private static Texture2D GetTextureFromBase(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;
            if (TextureCache.TryGetValue(fileName, out var cached) && cached != null)
                return cached;
            var path = (ICONS_BASE_PATH + fileName).Replace("\\", "/");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureCache[fileName] = tex;
            return tex;
        }
        #endregion
    }
}