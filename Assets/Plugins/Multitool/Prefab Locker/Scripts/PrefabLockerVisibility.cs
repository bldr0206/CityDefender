#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

namespace Multitool.PrefabLocker
{
    [InitializeOnLoad]
    public static class PrefabLockerVisibility
    {
        private static bool _isChangingSelection;

        static PrefabLockerVisibility()
        {
            EditorApplication.hierarchyChanged += RefreshSoon;
            PrefabStage.prefabStageOpened += _ => RefreshSoon();
            PrefabStage.prefabStageClosing += _ => RefreshSoon();
            EditorApplication.playModeStateChanged += _ => RefreshSoon();
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGui;
            RefreshSoon();
        }

        public static void RefreshSoon()
        {
            EditorApplication.delayCall -= RefreshNow;
            EditorApplication.delayCall += RefreshNow;
        }

        public static void UnhideBranch(Transform root)
        {
            if (root == null)
                return;

            ClearHideFlagsRecursive(root, includeSelf: false);
        }

        private static void RefreshNow()
        {
            EditorApplication.delayCall -= RefreshNow;

            var lockers = Resources.FindObjectsOfTypeAll<PrefabLocker>();
            var currentStage = PrefabStageUtility.GetCurrentPrefabStage();

            // Важно: обрабатываем от меньшей глубины к большей, чтобы корневые блокеры
            // сначала очищали флаги, а вложенные потом скрывали свои ветки.
            System.Array.Sort(lockers, (a, b) => GetDepth(a.transform).CompareTo(GetDepth(b.transform)));

            foreach (var locker in lockers)
            {
                if (!IsInLiveStage(locker))
                    continue;

                ApplyBlocker(locker, currentStage);
            }
        }

        private static int GetDepth(Transform t)
        {
            var d = 0;
            while (t != null)
            {
                d++;
                t = t.parent;
            }
            return d;
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            var e = Event.current;
            if (e == null)
                return;

            if (e.type != EventType.MouseDown || e.button != 0 || e.alt || e.control || e.command || e.shift)
                return;

            var currentStage = PrefabStageUtility.GetCurrentPrefabStage();

            // Сначала пробуем pick без раскрытия — если объект уже видим (например, сам локер).
            var picked = HandleUtility.PickGameObject(e.mousePosition, false);
            if (picked != null)
            {
                var mapped = MapToBlockerRoot(picked, currentStage);
                if (mapped != null && mapped != picked)
                {
                    _isChangingSelection = true;
                    Selection.activeGameObject = mapped;
                    _isChangingSelection = false;
                    e.Use();
                    return;
                }
            }

            // Фоллбэк: раскрываем детей, делаем pick, сразу закрываем.
            var lockers = Resources.FindObjectsOfTypeAll<PrefabLocker>();
            var liveBlockers = ListPool<PrefabLocker>.Get();

            try
            {
                foreach (var locker in lockers)
                {
                    if (!IsInLiveStage(locker))
                        continue;

                    // Пропускаем корень редактируемого префаба — его дети и так видны.
                    if (currentStage != null && currentStage.prefabContentsRoot == locker.gameObject)
                        continue;

                    liveBlockers.Add(locker);
                    ClearHideFlagsRecursive(locker.transform, includeSelf: false, markDirty: false);
                }

                picked = HandleUtility.PickGameObject(e.mousePosition, false);
                if (picked == null)
                    return;

                var mapped = MapToBlockerRoot(picked, currentStage);
                if (mapped == null || mapped == picked)
                    return;

                _isChangingSelection = true;
                Selection.activeGameObject = mapped;
                _isChangingSelection = false;
                e.Use();
            }
            finally
            {
                foreach (var locker in liveBlockers)
                {
                    ApplyBlocker(locker, currentStage);
                }

                ListPool<PrefabLocker>.Release(liveBlockers);
            }
        }

        private static bool IsInLiveStage(Component component)
        {
            if (component == null)
                return false;

            var go = component.gameObject;
            if (go == null)
                return false;

            if (EditorUtility.IsPersistent(go))
                return false;

            var handle = StageUtility.GetStageHandle(go);
            return handle == StageUtility.GetMainStageHandle() || handle == StageUtility.GetCurrentStageHandle();
        }

        private static void ApplyBlocker(PrefabLocker blocker, PrefabStage currentStage)
        {
            var root = blocker.transform;

            // Сначала убираем наш флаг со всей ветки, чтобы избежать накопления.
            ClearHideFlagsRecursive(root, includeSelf: false);

            var editingThisPrefab = currentStage != null && currentStage.prefabContentsRoot == root.gameObject;
            if (editingThisPrefab)
                return;

            // Если компонент отключен, показываем всех детей
            if (!blocker.enabled)
            {
                // Компонент отключен - не скрываем детей (они уже показаны через ClearHideFlagsRecursive выше)
                return;
            }

            // Компонент включен - скрываем детей в сцене
            HideChildrenRecursive(root, hide: true);
        }

        private static void OnSelectionChanged()
        {
            if (_isChangingSelection)
                return;

            var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
                return;

            var buffer = ListPool<GameObject>.Get();
            buffer.Clear();

            foreach (var go in selection)
            {
                if (go == null)
                    continue;

                var mapped = MapToBlockerRoot(go, currentStage);
                if (mapped != null && !buffer.Contains(mapped))
                    buffer.Add(mapped);
            }

            if (buffer.Count > 0 && (selection.Length != buffer.Count || !SameSet(selection, buffer)))
            {
                _isChangingSelection = true;
                Selection.objects = buffer.ToArray();
                _isChangingSelection = false;
            }

            ListPool<GameObject>.Release(buffer);
        }

        private static GameObject MapToBlockerRoot(GameObject go, PrefabStage currentStage)
        {
            if (go == null)
                return null;

            // Обрабатываем только объекты активной сцены или текущего prefab stage.
            var stageHandle = StageUtility.GetStageHandle(go);
            var mainHandle = StageUtility.GetMainStageHandle();
            var currentHandle = StageUtility.GetCurrentStageHandle();
            if (stageHandle != mainHandle && stageHandle != currentHandle)
                return go;

            var editingRoot = currentStage != null ? currentStage.prefabContentsRoot : null;

            var t = go.transform;
            PrefabLocker topMostBlocker = null;

            // Идём от объекта вверх, ищем самый верхний блокер (не считая корень редактируемого префаба).
            while (t != null)
            {
                var blocker = t.GetComponent<PrefabLocker>();
                if (blocker != null && blocker.gameObject != editingRoot)
                {
                    // Запоминаем самый верхний (последний найденный при проходе вверх)
                    topMostBlocker = blocker;
                }
                t = t.parent;
            }

            if (topMostBlocker == null)
                return go;

            return topMostBlocker.gameObject;
        }

        private static bool SameSet(GameObject[] original, System.Collections.Generic.List<GameObject> mapped)
        {
            if (original.Length != mapped.Count)
                return false;

            for (var i = 0; i < original.Length; i++)
            {
                if (original[i] != mapped[i])
                    return false;
            }
            return true;
        }

        private static void ClearHideFlagsRecursive(Transform parent, bool includeSelf, bool markDirty = true)
        {
            if (includeSelf)
                SetHidden(parent.gameObject, false, markDirty);

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                SetHidden(child.gameObject, false, markDirty);
                ClearHideFlagsRecursive(child, includeSelf: false, markDirty);
            }
        }

        private static void HideChildrenRecursive(Transform parent, bool hide)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var forceVisible = child.GetComponent<PrefabLockerForceVisible>() != null;
                var shouldHide = hide && !forceVisible;

                SetHidden(child.gameObject, shouldHide);
                HideChildrenRecursive(child, shouldHide);
            }
        }

        private static void SetHidden(GameObject go, bool hidden, bool markDirty = true)
        {
            if (go == null)
                return;

            var flags = go.hideFlags;
            var newFlags = hidden
                ? flags | HideFlags.HideInHierarchy
                : flags & ~HideFlags.HideInHierarchy;

            if (newFlags == flags)
                return;

            go.hideFlags = newFlags;

            // Не помечаем PrefabStage как изменённый, чтобы не ловить бесконечные автосейвы.
            if (markDirty && !Application.isPlaying && StageUtility.GetStageHandle(go) == StageUtility.GetMainStageHandle())
                EditorUtility.SetDirty(go);
        }
    }
}
#endif
