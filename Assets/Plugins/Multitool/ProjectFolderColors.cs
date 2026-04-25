#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Multitool.ProjectFolderColors
{
    [InitializeOnLoad]
    public static class ProjectFolderColors
    {
        private const string PrefsKeyPrefix = "Multitool.ProjectFolderColors.";
        private const string ColorFolderMenu = "Assets/Color Folder...";
        private const string ClearFolderMenu = "Assets/Clear Folder Color";
        private const string ClearAllMenu = "Tools/Multitool/Project Folder Colors/Clear All Local Colors";

        private static readonly Dictionary<string, FolderColorEntry> EntriesByGuid = new Dictionary<string, FolderColorEntry>();
        private static readonly Dictionary<string, string> PathsByGuid = new Dictionary<string, string>();
        private static GUIStyle _rowLabelStyle;
        private static GUIStyle _gridLabelStyle;
        private static FolderColorData _data;

        static ProjectFolderColors()
        {
            Load();
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        [MenuItem(ColorFolderMenu, false, 2000)]
        private static void OpenColorWindow()
        {
            ProjectFolderColorWindow.Open(GetSelectedFolderGuids());
        }

        [MenuItem(ColorFolderMenu, true)]
        private static bool CanOpenColorWindow()
        {
            return GetSelectedFolderGuids().Count > 0;
        }

        [MenuItem(ClearFolderMenu, false, 2001)]
        private static void ClearSelectedFolderColors()
        {
            List<string> folderGuids = GetSelectedFolderGuids();
            if (folderGuids.Count == 0)
            {
                return;
            }

            foreach (string guid in folderGuids)
            {
                Remove(guid);
            }
        }

        [MenuItem(ClearFolderMenu, true)]
        private static bool CanClearSelectedFolderColors()
        {
            List<string> folderGuids = GetSelectedFolderGuids();
            for (int i = 0; i < folderGuids.Count; i++)
            {
                if (EntriesByGuid.ContainsKey(folderGuids[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MenuItem(ClearAllMenu, false, 120)]
        private static void ClearAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Project Folder Colors",
                    "Clear all local folder colors for this project?",
                    "Clear",
                    "Cancel"))
            {
                return;
            }

            _data.entries.Clear();
            Save();
        }

        public static bool TryGetExplicitRule(string folderGuid, out FolderColorEntry entry)
        {
            LoadIfNeeded();
            return EntriesByGuid.TryGetValue(folderGuid, out entry);
        }

        public static void Set(string folderGuid, Color color, bool recursive)
        {
            LoadIfNeeded();

            color.a = 1f;
            FolderColorEntry entry;
            if (!EntriesByGuid.TryGetValue(folderGuid, out entry))
            {
                entry = new FolderColorEntry { guid = folderGuid };
                _data.entries.Add(entry);
            }

            entry.color = color;
            entry.recursive = recursive;
            Save();
        }

        public static void Remove(string folderGuid)
        {
            LoadIfNeeded();

            _data.entries.RemoveAll(entry => entry.guid == folderGuid);
            Save();
        }

        public static List<string> GetSelectedFolderGuids()
        {
            string[] selectedGuids = Selection.assetGUIDs;
            List<string> folderGuids = new List<string>();

            for (int i = 0; i < selectedGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(selectedGuids[i]);
                if (AssetDatabase.IsValidFolder(path))
                {
                    folderGuids.Add(selectedGuids[i]);
                }
            }

            return folderGuids;
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            FolderColorEntry entry;
            if (!TryGetRuleForPath(guid, path, out entry))
            {
                return;
            }

            DrawFolder(selectionRect, path, entry.color);
        }

        private static void OnProjectChanged()
        {
            PathsByGuid.Clear();
            RepaintProjectWindow();
        }

        private static bool TryGetRuleForPath(string guid, string path, out FolderColorEntry result)
        {
            LoadIfNeeded();

            if (EntriesByGuid.TryGetValue(guid, out result))
            {
                return true;
            }

            result = null;
            int bestPathLength = -1;

            for (int i = 0; i < _data.entries.Count; i++)
            {
                FolderColorEntry entry = _data.entries[i];
                if (!entry.recursive)
                {
                    continue;
                }

                string parentPath = GetPath(entry.guid);
                if (string.IsNullOrEmpty(parentPath))
                {
                    continue;
                }

                bool isChild = path.Length > parentPath.Length
                               && path.StartsWith(parentPath, StringComparison.Ordinal)
                               && path[parentPath.Length] == '/';

                if (isChild && parentPath.Length > bestPathLength)
                {
                    result = entry;
                    bestPathLength = parentPath.Length;
                }
            }

            return result != null;
        }

        private static void DrawFolder(Rect selectionRect, string path, Color color)
        {
            bool isGridItem = selectionRect.height > EditorGUIUtility.singleLineHeight + 4f;
            Color backgroundColor = MakeBackgroundColor(color);

            EditorGUI.DrawRect(selectionRect, backgroundColor);

            if (isGridItem)
            {
                DrawGridFolder(selectionRect, path, color);
            }
            else
            {
                DrawRowFolder(selectionRect, path, color);
            }
        }

        private static void DrawRowFolder(Rect rect, string path, Color color)
        {
            const float iconSize = 16f;
            Rect iconRect = new Rect(rect.x + 2f, rect.y + 1f, iconSize, iconSize);
            Rect labelRect = new Rect(iconRect.xMax + 3f, rect.y, rect.width - iconSize - 5f, rect.height);

            DrawFolderIcon(iconRect, color);
            GetRowLabelStyle(color).Draw(labelRect, System.IO.Path.GetFileName(path), false, false, false, false);
        }

        private static void DrawGridFolder(Rect rect, string path, Color color)
        {
            float iconSize = Mathf.Min(64f, Mathf.Max(24f, rect.width - 12f));
            Rect iconRect = new Rect(rect.center.x - iconSize * 0.5f, rect.y + 4f, iconSize, iconSize);
            Rect labelRect = new Rect(rect.x + 2f, rect.yMax - 34f, rect.width - 4f, 32f);

            DrawFolderIcon(iconRect, color);
            GetGridLabelStyle(color).Draw(labelRect, System.IO.Path.GetFileName(path), false, false, false, false);
        }

        private static void DrawFolderIcon(Rect rect, Color color)
        {
            Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;
            if (icon == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
        }

        private static GUIStyle GetRowLabelStyle(Color color)
        {
            if (_rowLabelStyle == null)
            {
                _rowLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontStyle = FontStyle.Bold
                };
            }

            _rowLabelStyle.normal.textColor = color;
            return _rowLabelStyle;
        }

        private static GUIStyle GetGridLabelStyle(Color color)
        {
            if (_gridLabelStyle == null)
            {
                _gridLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    clipping = TextClipping.Clip,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };
            }

            _gridLabelStyle.normal.textColor = color;
            return _gridLabelStyle;
        }

        private static Color MakeBackgroundColor(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            bool isWhite = IsWhiteColor(color);
            float backgroundSaturation = isWhite ? 0f : Mathf.Clamp01(saturation * 0.8f);
            float backgroundValue = isWhite ? 0.46f : Mathf.Min(value * 0.22f, 0.24f);
            Color background = Color.HSVToRGB(hue, backgroundSaturation, backgroundValue);
            background.a = EditorGUIUtility.isProSkin ? 0.9f : 0.35f;
            return background;
        }

        internal static bool IsWhiteColor(Color color)
        {
            return color.r > 0.98f && color.g > 0.98f && color.b > 0.98f;
        }

        private static string GetPath(string guid)
        {
            string path;
            if (PathsByGuid.TryGetValue(guid, out path))
            {
                return path;
            }

            path = AssetDatabase.GUIDToAssetPath(guid);
            PathsByGuid[guid] = path;
            return path;
        }

        private static void LoadIfNeeded()
        {
            if (_data == null)
            {
                Load();
            }
        }

        private static void Load()
        {
            string json = EditorPrefs.GetString(GetPrefsKey(), string.Empty);
            _data = string.IsNullOrEmpty(json) ? new FolderColorData() : JsonUtility.FromJson<FolderColorData>(json);
            if (_data == null)
            {
                _data = new FolderColorData();
            }

            RebuildCache();
        }

        private static void Save()
        {
            RebuildCache();
            EditorPrefs.SetString(GetPrefsKey(), JsonUtility.ToJson(_data));
            RepaintProjectWindow();
        }

        private static void RebuildCache()
        {
            EntriesByGuid.Clear();
            PathsByGuid.Clear();

            for (int i = _data.entries.Count - 1; i >= 0; i--)
            {
                FolderColorEntry entry = _data.entries[i];
                if (string.IsNullOrEmpty(entry.guid))
                {
                    _data.entries.RemoveAt(i);
                    continue;
                }

                EntriesByGuid[entry.guid] = entry;
            }
        }

        private static string GetPrefsKey()
        {
            return PrefsKeyPrefix + PlayerSettings.productGUID;
        }

        private static void RepaintProjectWindow()
        {
            EditorApplication.RepaintProjectWindow();
        }

        [Serializable]
        public class FolderColorEntry
        {
            public string guid;
            public Color color = new Color(0.2f, 0.65f, 1f, 1f);
            public bool recursive;
        }

        [Serializable]
        private class FolderColorData
        {
            public List<FolderColorEntry> entries = new List<FolderColorEntry>();
        }
    }

    public class ProjectFolderColorWindow : EditorWindow
    {
        private const int PaletteColumns = 8;
        private const int PaletteRows = 8;
        private const float CellSize = 22f;
        private const float CellSpacing = 5f;
        private const float Padding = 10f;
        private const float RowSpacing = 8f;
        private const float PreviewHeight = 24f;
        private const float ButtonHeight = 28f;
        private const float ToggleRowHeight = 18f;
        private const float SpecialOptionsHeight = 22f;
        private static readonly Vector2 WindowSize = ComputeWindowSize();
        private static readonly Color[] Palette64 = CreatePalette64();

        private readonly List<string> _folderGuids = new List<string>();
        private Color _color = new Color(0.2f, 0.65f, 1f, 1f);
        private bool _recursive = true;
        private int _selectedPaletteIndex = -1;
        private bool _noColorSelected;
        private string _previewFolderName = "Folder";
        private GUIStyle _previewLabelStyle;

        public static void Open(List<string> folderGuids)
        {
            ProjectFolderColorWindow window = GetWindow<ProjectFolderColorWindow>(true, "Color Folder", true);
            window.minSize = WindowSize;
            window.maxSize = WindowSize;
            window.SetFolders(folderGuids);
            window.ShowUtility();
        }

        private void SetFolders(List<string> folderGuids)
        {
            _folderGuids.Clear();
            _folderGuids.AddRange(folderGuids);
            _previewFolderName = GetPreviewFolderName();

            ProjectFolderColors.FolderColorEntry entry;
            if (_folderGuids.Count > 0 && ProjectFolderColors.TryGetExplicitRule(_folderGuids[0], out entry))
            {
                _color = entry.color;
                _recursive = entry.recursive;
                _noColorSelected = false;
            }
            else
            {
                // No explicit rule on the first selected folder.
                _noColorSelected = true;
                _recursive = true;
            }

            _color.a = 1f;
            _selectedPaletteIndex = FindClosestPaletteIndex(_color);
        }

        private void OnGUI()
        {
            GUILayout.Space(Padding);
            DrawPalette();
            GUILayout.Space(RowSpacing);
            DrawPreview();
            GUILayout.Space(RowSpacing);
            DrawRecursiveToggle();
            GUILayout.Space(RowSpacing);
            DrawApplyButton();
        }

        private void DrawPalette()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawPaletteGrid();
            }
        }

        private void DrawPaletteGrid()
        {
            float paletteWidth = GetPaletteWidth();
            float paletteHeight = GetPaletteHeight();
            Rect paletteRect = GUILayoutUtility.GetRect(0f, paletteHeight, GUILayout.ExpandWidth(true), GUILayout.Height(paletteHeight));
            float startX = paletteRect.x + Mathf.Floor((paletteRect.width - paletteWidth) * 0.5f);

            for (int row = 0; row < PaletteRows; row++)
            {
                for (int col = 0; col < PaletteColumns; col++)
                {
                    int index = row * PaletteColumns + col;
                    Rect rect = new Rect(
                        startX + col * (CellSize + CellSpacing),
                        paletteRect.y + row * (CellSize + CellSpacing),
                        CellSize,
                        CellSize);

                    bool clicked = DrawPaletteCell(rect, Palette64[index], !_noColorSelected && index == _selectedPaletteIndex);
                    if (clicked)
                    {
                        _selectedPaletteIndex = index;
                        _color = Palette64[index];
                        _color.a = 1f;
                        _noColorSelected = false;
                        Repaint();
                    }
                }
            }

            GUILayout.Space(RowSpacing);
            DrawSpecialOptions();
        }

        private void DrawSpecialOptions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Padding);

                float buttonWidth = (CellSize * 4f) + (CellSpacing * 3f);
                using (new EditorGUI.DisabledScope(_noColorSelected))
                {
                    bool clearClicked = GUILayout.Button("No Color", GUILayout.Width(buttonWidth), GUILayout.Height(SpecialOptionsHeight));
                    if (clearClicked)
                    {
                        _noColorSelected = true;
                        Repaint();
                    }
                }

                GUILayout.Space(CellSpacing);

                using (new EditorGUI.DisabledScope(!_noColorSelected && _color.r > 0.98f && _color.g > 0.98f && _color.b > 0.98f))
                {
                    bool whiteClicked = GUILayout.Button("White", GUILayout.Width(buttonWidth), GUILayout.Height(SpecialOptionsHeight));
                    if (whiteClicked)
                    {
                        _noColorSelected = false;
                        _color = Color.white;
                        _color.a = 1f;
                        _selectedPaletteIndex = FindClosestPaletteIndex(_color);
                        Repaint();
                    }
                }

                GUILayout.Space(Padding);
            }
        }

        private void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Padding);
                EditorGUILayout.LabelField("Preview", GUILayout.Width(52f));

                Rect previewRect = GUILayoutUtility.GetRect(0f, PreviewHeight, GUILayout.ExpandWidth(true), GUILayout.Height(PreviewHeight));
                DrawFolderPreview(previewRect, _color);
                GUILayout.Space(Padding);
            }
        }

        private void DrawRecursiveToggle()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Padding);
                _recursive = EditorGUILayout.ToggleLeft("Apply recursively", _recursive);
                GUILayout.Space(Padding);
            }
        }

        private void DrawApplyButton()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Padding);
                if (GUILayout.Button("Apply", GUILayout.Height(ButtonHeight)))
                {
                    Apply();
                }
                GUILayout.Space(Padding);
            }
        }

        private static Vector2 ComputeWindowSize()
        {
            float paletteWidth = GetPaletteWidth();
            float width = (Padding * 2f) + paletteWidth;

            float paletteHeight = GetPaletteHeight();
            float height =
                Padding +
                paletteHeight +
                RowSpacing +
                RowSpacing +
                SpecialOptionsHeight +
                RowSpacing +
                PreviewHeight +
                RowSpacing +
                ToggleRowHeight +
                RowSpacing +
                ButtonHeight +
                Padding;

            return new Vector2(Mathf.Ceil(width), Mathf.Ceil(height));
        }

        private static float GetPaletteWidth()
        {
            return (PaletteColumns * CellSize) + ((PaletteColumns - 1) * CellSpacing);
        }

        private static float GetPaletteHeight()
        {
            return (PaletteRows * CellSize) + ((PaletteRows - 1) * CellSpacing);
        }

        private static bool DrawPaletteCell(Rect rect, Color color, bool selected)
        {
            Event e = Event.current;
            bool isHover = rect.Contains(e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0 && isHover)
            {
                e.Use();
                return true;
            }

            if (e.type != EventType.Repaint)
            {
                return false;
            }

            EditorGUI.DrawRect(rect, color);

            if (isHover)
            {
                Color hoverColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.14f) : new Color(0f, 0f, 0f, 0.10f);
                EditorGUI.DrawRect(rect, hoverColor);
            }

            if (selected)
            {
                Color outlineColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.95f) : new Color(0f, 0f, 0f, 0.9f);
                DrawOutline(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), outlineColor, 2f);
            }
            return false;
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawFolderPreview(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (_noColorSelected)
            {
                // Show default look (no custom background/text color).
                EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f));
            }
            else
            {
                EditorGUI.DrawRect(rect, MakePreviewBackgroundColor(color));
            }

            const float iconSize = 16f;
            Rect iconRect = new Rect(rect.x + 6f, rect.y + 4f, iconSize, iconSize);
            DrawPreviewFolderIcon(iconRect, _noColorSelected ? (EditorGUIUtility.isProSkin ? new Color(0.75f, 0.75f, 0.75f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f)) : color);

            Rect labelRect = new Rect(iconRect.xMax + 5f, rect.y, rect.width - iconSize - 14f, rect.height);
            Color labelColor = _noColorSelected
                ? (EditorGUIUtility.isProSkin ? new Color(0.86f, 0.86f, 0.86f, 1f) : new Color(0.12f, 0.12f, 0.12f, 1f))
                : color;
            GetPreviewLabelStyle(labelColor).Draw(labelRect, _previewFolderName, false, false, false, false);
        }

        private static void DrawPreviewFolderIcon(Rect rect, Color color)
        {
            Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;
            if (icon == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
        }

        private GUIStyle GetPreviewLabelStyle(Color color)
        {
            if (_previewLabelStyle == null)
            {
                _previewLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontStyle = FontStyle.Bold
                };
            }

            _previewLabelStyle.normal.textColor = color;
            return _previewLabelStyle;
        }

        private static Color MakePreviewBackgroundColor(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            bool isWhite = ProjectFolderColors.IsWhiteColor(color);
            float v = isWhite ? 0.46f : Mathf.Min(value * 0.22f, 0.24f);
            float s = isWhite ? 0f : Mathf.Clamp01(saturation * 0.8f);
            Color background = Color.HSVToRGB(hue, s, v);
            background.a = EditorGUIUtility.isProSkin ? 0.9f : 0.35f;
            return background;
        }

        private string GetPreviewFolderName()
        {
            if (_folderGuids.Count == 0)
            {
                return "Folder";
            }

            string path = AssetDatabase.GUIDToAssetPath(_folderGuids[0]);
            return string.IsNullOrEmpty(path) ? "Folder" : System.IO.Path.GetFileName(path);
        }

        private static int FindClosestPaletteIndex(Color target)
        {
            float best = float.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < Palette64.Length; i++)
            {
                Color c = Palette64[i];
                float d = (target.r - c.r) * (target.r - c.r)
                          + (target.g - c.g) * (target.g - c.g)
                          + (target.b - c.b) * (target.b - c.b);
                if (d < best)
                {
                    best = d;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static Color[] CreatePalette64()
        {
            Color[] colors = new Color[PaletteColumns * PaletteRows];
            Color topLeft = new Color(1.00f, 0.52f, 0.08f, 1f);
            Color topRight = new Color(0.16f, 1.00f, 0.28f, 1f);
            Color bottomLeft = new Color(1.00f, 0.12f, 0.88f, 1f);
            Color bottomRight = new Color(0.08f, 0.45f, 1.00f, 1f);

            for (int row = 0; row < PaletteRows; row++)
            {
                float y = row / (PaletteRows - 1f);
                Color left = Color.Lerp(topLeft, bottomLeft, y);
                Color right = Color.Lerp(topRight, bottomRight, y);

                for (int col = 0; col < PaletteColumns; col++)
                {
                    float x = col / (PaletteColumns - 1f);
                    Color color = Color.Lerp(left, right, x);
                    colors[row * PaletteColumns + col] = BoostPaletteColor(color, x, y);
                }
            }

            // First swatch: a vivid orange anchor.
            colors[0] = topLeft;
            return colors;
        }

        private static Color BoostPaletteColor(Color color, float x, float y)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);

            float distanceFromCenter = Mathf.Max(Mathf.Abs(x - 0.5f), Mathf.Abs(y - 0.5f)) * 2f;
            float saturationBoost = Mathf.Lerp(1.08f, 1.28f, distanceFromCenter);
            float valueBoost = Mathf.Lerp(1.02f, 1.07f, distanceFromCenter);

            saturation = Mathf.Clamp01(saturation * saturationBoost);
            value = Mathf.Clamp01(value * valueBoost);
            return Color.HSVToRGB(hue, saturation, value);
        }

        private void Apply()
        {
            for (int i = 0; i < _folderGuids.Count; i++)
            {
                if (_noColorSelected)
                {
                    ProjectFolderColors.Remove(_folderGuids[i]);
                }
                else
                {
                    ProjectFolderColors.Set(_folderGuids[i], _color, _recursive);
                }
            }

            Close();
        }

    }
}
#endif
