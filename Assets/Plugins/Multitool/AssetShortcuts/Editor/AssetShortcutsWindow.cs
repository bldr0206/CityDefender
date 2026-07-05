using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Multitool.AssetShortcuts
{
    public class AssetShortcutsWindow : EditorWindow
    {
        [Serializable]
        private class Entry
        {
            public string guid;
            public string name;
            public string description;
            public bool isSceneObject;
            public string scenePath;
            public string hierarchyPath;
            public bool isFolder;
            public Color color = new Color(0.8f, 0.8f, 0.8f, 1f);

            public Color BgColor => new Color(color.r, color.g, color.b, 0.10f);
        }

        [Serializable]
        private class SaveData
        {
            public List<Entry> entries = new List<Entry>();
        }

        private const string PrefKey = "Multitool.AssetShortcuts";
        private const float EntH = 28f;
        private const float EntHDesc = 40f;
        private const float HandleW = 18f;
        private const float IconSize = 14f;
        private const float IconW = 18f;
        private const float BtnSz = 20f;
        private const float Pad = 2f;
        private const float Gap = 3f;
        private const int CornerR = 4;

        private static Texture2D s_roundedTex;
        private static GUIStyle s_roundedStyle;

        private SaveData _data = new SaveData();
        private Vector2 _scroll;
        private int _dragFrom = -1;
        private int _insertAt = -1;
        private bool _isDragging;
        private readonly List<Rect> _rects = new List<Rect>();
        private readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

        private GUIStyle _labelStyle;
        private GUIStyle _descStyle;
        private GUIStyle _handleStyle;
        private GUIStyle _hintStyle;

        [MenuItem("Window/Multitool/Asset Shortcuts")]
        public static void ShowWindow()
        {
            var w = GetWindow<AssetShortcutsWindow>(false, "Asset Shortcuts");
            w.minSize = new Vector2(180, 80);
        }

        private void OnEnable() => Load();
        private void OnDisable() => Save();

        private void Load()
        {
            var json = EditorPrefs.GetString(PrefKey, string.Empty);
            _data = string.IsNullOrEmpty(json) ? new SaveData() : TryDeserialize(json);
        }

        private static SaveData TryDeserialize(string json)
        {
            try { return JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
            catch { return new SaveData(); }
        }

        private void Save() => EditorPrefs.SetString(PrefKey, JsonUtility.ToJson(_data));

        private void InitStyles()
        {
            if (_labelStyle == null)
                _labelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    clipping = TextClipping.Clip,
                    richText = false,
                };
            if (_descStyle == null)
                _descStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 10,
                    clipping = TextClipping.Clip,
                    richText = false,
                };
            if (_handleStyle == null)
                _handleStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                };
            if (_hintStyle == null)
                _hintStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 11,
                    wordWrap = true,
                };
        }

        // --- Rounded rect helpers ---

        private static Texture2D GetRoundedTex()
        {
            if (s_roundedTex != null) return s_roundedTex;
            const int sz = 16;
            s_roundedTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pix = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    pix[y * sz + x] = new Color(1f, 1f, 1f, RoundedAlpha(x + 0.5f, y + 0.5f, sz, sz, CornerR));
            s_roundedTex.SetPixels(pix);
            s_roundedTex.Apply(false);
            return s_roundedTex;
        }

        private static float RoundedAlpha(float px, float py, int w, int h, float r)
        {
            float cx = Mathf.Clamp(px, r, w - r);
            float cy = Mathf.Clamp(py, r, h - r);
            float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
            return Mathf.Clamp01(r - dist + 0.5f);
        }

        private static GUIStyle GetRoundedStyle()
        {
            if (s_roundedStyle == null || s_roundedStyle.normal.background == null)
            {
                s_roundedTex = null;
                s_roundedStyle = new GUIStyle
                {
                    normal = { background = GetRoundedTex() },
                    border = new RectOffset(CornerR, CornerR, CornerR, CornerR),
                    padding = new RectOffset(0, 0, 0, 0),
                };
            }
            return s_roundedStyle;
        }

        private static void DrawRoundedRect(Rect r, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.Box(r, GUIContent.none, GetRoundedStyle());
            GUI.color = prev;
        }

        // --- Entry height ---

        private static float EntryHeight(Entry e) =>
            string.IsNullOrEmpty(e.description) ? EntH : EntHDesc;

        // --- OnGUI ---

        private void OnGUI()
        {
            InitStyles();
            HandleExternalDrop();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            while (_rects.Count < _data.entries.Count) _rects.Add(default(Rect));
            while (_rects.Count > _data.entries.Count) _rects.RemoveAt(_rects.Count - 1);

            for (int i = 0; i < _data.entries.Count; i++)
                DrawEntry(i);

            if (_isDragging && _insertAt == _data.entries.Count && _rects.Count > 0)
            {
                var last = _rects[_rects.Count - 1];
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(last.x + Pad, last.yMax - 2, last.width - Pad * 2, 2f), Color.cyan);
            }

            HandleReorderDrag();

            if (_data.entries.Count == 0)
                DrawEmptyHint();

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(int idx)
        {
            var e = _data.entries[idx];
            var r = GUILayoutUtility.GetRect(0, EntryHeight(e), GUILayout.ExpandWidth(true));

            if (Event.current.type != EventType.Layout && idx < _rects.Count)
                _rects[idx] = r;

            // Inset rounded background
            var bg = new Rect(r.x + Pad, r.y + Pad, r.width - Pad * 2, r.height - Pad * 2);

            if (Event.current.type == EventType.Repaint)
            {
                DrawRoundedRect(bg, e.BgColor);

                if (_isDragging && _insertAt == idx)
                    EditorGUI.DrawRect(new Rect(bg.x, bg.y, bg.width, 2f), Color.cyan);

                GUI.Label(new Rect(bg.x, bg.y, HandleW, bg.height), "≡", _handleStyle);

                var icon = GetIcon(e);
                if (icon != null)
                    GUI.DrawTexture(
                        new Rect(bg.x + HandleW, bg.y + (bg.height - IconSize) * 0.5f, IconSize, IconSize),
                        icon, ScaleMode.ScaleToFit);
            }

            // Drag handle input
            if (Event.current.type == EventType.MouseDown &&
                new Rect(bg.x, bg.y, HandleW, bg.height).Contains(Event.current.mousePosition) &&
                Event.current.button == 0)
            {
                _dragFrom = idx;
                _insertAt = idx;
                _isDragging = false;
                Event.current.Use();
            }

            // Right controls — vertically centered in bg
            float btnY = bg.y + (bg.height - BtnSz) * 0.5f;
            var clearR = new Rect(bg.xMax - BtnSz, btnY, BtnSz, BtnSz);
            var swR   = new Rect(bg.xMax - BtnSz * 2 - Gap, btnY, BtnSz, BtnSz);

            // Label area
            float labelX = bg.x + HandleW + IconW;
            float labelW = Mathf.Max(0, swR.x - Gap - labelX);
            bool hasDesc = !string.IsNullOrEmpty(e.description);
            _labelStyle.normal.textColor = e.color;

            if (hasDesc)
            {
                if (GUI.Button(new Rect(labelX, bg.y + 2, labelW, 31f), GUIContent.none, GUIStyle.none))
                    OnClick(e);
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.Label(new Rect(labelX, bg.y + 2, labelW, 17f), e.name, _labelStyle);
                    _descStyle.normal.textColor = new Color(e.color.r, e.color.g, e.color.b, 0.6f);
                    GUI.Label(new Rect(labelX, bg.y + 19, labelW, 14f), e.description, _descStyle);
                }
            }
            else
            {
                if (GUI.Button(new Rect(labelX, bg.y, labelW, bg.height), e.name, _labelStyle))
                    OnClick(e);
            }

            if (GUI.Button(swR, "···", EditorStyles.miniButton))
                PopupWindow.Show(swR, new EntryPopup(e, Save, this));

            // Clear button
            if (GUI.Button(clearR, "×", EditorStyles.miniButton))
            {
                _data.entries.RemoveAt(idx);
                Save();
                GUIUtility.ExitGUI();
            }
        }

        private void HandleReorderDrag()
        {
            if (_dragFrom < 0) return;
            var ev = Event.current;

            if (ev.type == EventType.MouseDrag)
            {
                _isDragging = true;
                _insertAt = ComputeInsertAt(ev.mousePosition);
                Repaint();
                ev.Use();
            }
            else if (ev.type == EventType.MouseUp)
            {
                if (_isDragging && _insertAt != _dragFrom && _insertAt != _dragFrom + 1)
                {
                    var item = _data.entries[_dragFrom];
                    _data.entries.RemoveAt(_dragFrom);
                    int to = _insertAt > _dragFrom ? _insertAt - 1 : _insertAt;
                    _data.entries.Insert(Mathf.Clamp(to, 0, _data.entries.Count), item);
                    Save();
                }
                _dragFrom = -1;
                _insertAt = -1;
                _isDragging = false;
                Repaint();
                ev.Use();
            }
        }

        private int ComputeInsertAt(Vector2 pos)
        {
            for (int i = 0; i < _rects.Count; i++)
                if (pos.y < _rects[i].y + _rects[i].height * 0.5f)
                    return i;
            return _rects.Count;
        }

        private void HandleExternalDrop()
        {
            var ev = Event.current;
            if (ev.type != EventType.DragUpdated && ev.type != EventType.DragPerform) return;
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (ev.type != EventType.DragPerform) return;

            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
                TryAdd(obj);
            Save();
            ev.Use();
        }

        private void TryAdd(UnityEngine.Object obj)
        {
            if (obj == null) return;
            var entry = new Entry();

            if (obj is GameObject go && !EditorUtility.IsPersistent(go))
            {
                entry.isSceneObject = true;
                entry.scenePath = go.scene.path;
                // If path is empty we're inside a prefab stage — store the prefab asset path
                if (string.IsNullOrEmpty(entry.scenePath))
                {
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null) entry.scenePath = stage.assetPath;
                }
                entry.hierarchyPath = BuildHierarchyPath(go);
                entry.name = go.name;
                entry.color = new Color(0.5f, 0.9f, 0.5f);
                foreach (var ex in _data.entries)
                    if (ex.isSceneObject && ex.hierarchyPath == entry.hierarchyPath && ex.scenePath == entry.scenePath) return;
            }
            else
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath)) return;
                entry.guid = AssetDatabase.AssetPathToGUID(assetPath);
                entry.isFolder = AssetDatabase.IsValidFolder(assetPath);
                entry.name = entry.isFolder
                    ? System.IO.Path.GetFileName(assetPath)
                    : System.IO.Path.GetFileNameWithoutExtension(assetPath);
                entry.color = entry.isFolder
                    ? new Color(0.55f, 0.75f, 1.0f)
                    : new Color(0.8f, 0.8f, 0.8f);
                foreach (var ex in _data.entries)
                    if (!string.IsNullOrEmpty(ex.guid) && ex.guid == entry.guid) return;
            }

            if (!string.IsNullOrEmpty(entry.guid))
                _iconCache.Remove(entry.guid);
            _data.entries.Add(entry);
        }

        private void OnClick(Entry e)
        {
            if (e.isSceneObject) { OpenSceneObject(e); return; }
            var path = AssetDatabase.GUIDToAssetPath(e.guid);
            if (string.IsNullOrEmpty(path)) return;
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null) return;
            Selection.activeObject = obj;
            if (e.isFolder)
            {
                EditorUtility.FocusProjectWindow();
                if (TryOpenFolderInProjectBrowser(obj.GetInstanceID())) return;
            }
            // Pre-scroll list area to max so PingObject frames from above → item appears at top
            TryPreScrollProjectBrowser();
            EditorGUIUtility.PingObject(obj);
        }

        private static void TryPreScrollProjectBrowser()
        {
            try
            {
                var pbType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
                if (pbType == null) return;
                var browsers = Resources.FindObjectsOfTypeAll(pbType);
                if (browsers.Length == 0) return;
                var browser = (EditorWindow)browsers[0];

                var laField = pbType.GetField("m_ListArea", BindingFlags.Instance | BindingFlags.NonPublic);
                var la = laField?.GetValue(browser);
                if (la == null) return;

                var stField = la.GetType().GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
                var st = stField?.GetValue(la);
                if (st == null) return;

                var spField = st.GetType().GetField("m_ScrollPosition",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (spField == null) return;

                spField.SetValue(st, new Vector2(0f, 999999f));
                stField.SetValue(la, st);
            }
            catch { }
        }

        private static bool TryOpenFolderInProjectBrowser(int folderInstanceID)
        {
            try
            {
                var pbType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
                if (pbType == null) return false;
                var browsers = Resources.FindObjectsOfTypeAll(pbType);
                if (browsers.Length == 0) return false;
                var browser = (EditorWindow)browsers[0];

                // ShowFolderContents is only valid in two-column mode (m_ViewMode == 1)
                var viewModeField = pbType.GetField("m_ViewMode", BindingFlags.Instance | BindingFlags.NonPublic);
                if (viewModeField == null || (int)viewModeField.GetValue(browser) != 1)
                    return false;

                var method = pbType.GetMethod("ShowFolderContents",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(int), typeof(bool) }, null);
                if (method == null) return false;
                method.Invoke(browser, new object[] { folderInstanceID, true });
                return true;
            }
            catch { return false; }
        }

        private static void OpenSceneObject(Entry e)
        {
            var parts = e.hierarchyPath.Split('/');
            GameObject found = null;

            bool isPrefab = !string.IsNullOrEmpty(e.scenePath) &&
                            e.scenePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

            if (isPrefab)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage == null || stage.assetPath != e.scenePath)
                {
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(e.scenePath));
                    stage = PrefabStageUtility.GetCurrentPrefabStage();
                }
                if (stage == null) return;
                var root = stage.prefabContentsRoot;
                if (root.name == parts[0])
                    found = WalkPath(root.transform, parts, 1);
            }
            else
            {
                var scene = string.IsNullOrEmpty(e.scenePath)
                    ? UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                    : UnityEngine.SceneManagement.SceneManager.GetSceneByPath(e.scenePath);

                if (!string.IsNullOrEmpty(e.scenePath) && (!scene.IsValid() || !scene.isLoaded))
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    EditorSceneManager.OpenScene(e.scenePath);
                    scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(e.scenePath);
                }

                if (!scene.IsValid()) return;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name != parts[0]) continue;
                    found = WalkPath(root.transform, parts, 1);
                    if (found != null) break;
                }
            }

            if (found == null) return;
            Selection.activeGameObject = found;
            EditorGUIUtility.PingObject(found);
        }

        private static GameObject WalkPath(Transform t, string[] parts, int depth)
        {
            if (depth >= parts.Length) return t.gameObject;
            foreach (Transform child in t)
                if (child.name == parts[depth]) return WalkPath(child, parts, depth + 1);
            return null;
        }

        private static string BuildHierarchyPath(GameObject go)
        {
            var sb = new StringBuilder(go.name);
            var p = go.transform.parent;
            while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
            return sb.ToString();
        }

        private Texture2D GetIcon(Entry e)
        {
            if (e.isSceneObject) return EditorGUIUtility.FindTexture("GameObject Icon");
            if (e.isFolder) return EditorGUIUtility.FindTexture("Folder Icon");
            if (string.IsNullOrEmpty(e.guid)) return null;

            if (_iconCache.TryGetValue(e.guid, out var cached)) return cached;

            var path = AssetDatabase.GUIDToAssetPath(e.guid);
            Texture2D icon = null;
            if (!string.IsNullOrEmpty(path))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null) icon = AssetPreview.GetMiniThumbnail(obj);
                if (icon == null) icon = AssetDatabase.GetCachedIcon(path) as Texture2D;
            }

            _iconCache[e.guid] = icon;
            return icon;
        }

        private void DrawEmptyHint()
        {
            var r = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f, 0.4f));
            GUI.Label(r, "Drag an asset, folder or GameObject here", _hintStyle);
        }

private sealed class EntryPopup : PopupWindowContent
        {
            private readonly Entry _e;
            private readonly Action _onSave;
            private readonly EditorWindow _parent;

            public EntryPopup(Entry e, Action onSave, EditorWindow parent)
            {
                _e = e; _onSave = onSave; _parent = parent;
            }

            public override Vector2 GetWindowSize() => new Vector2(300, 82);

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.Space(2);
                EditorGUI.BeginChangeCheck();
                _e.color = EditorGUILayout.ColorField("Color", _e.color);
                EditorGUILayout.LabelField("Description", EditorStyles.miniLabel);
                _e.description = EditorGUILayout.TextArea(_e.description, GUILayout.ExpandWidth(true), GUILayout.Height(36));
                if (EditorGUI.EndChangeCheck())
                {
                    _onSave();
                    _parent?.Repaint();
                }
            }
        }
    }
}
