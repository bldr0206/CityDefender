#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Multitool.PrefabLocker
{
    /// <summary>
    /// Задаёт иконку замка для компонента PrefabLocker.
    /// </summary>
    [InitializeOnLoad]
    internal static class PrefabLockerIcon
    {
        private const string IconPath = "Assets/Plugins/Multitool/Prefab Locker/Icons/Icon_Lock.png";
        private static Texture2D _icon;

        static PrefabLockerIcon()
        {
            _icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        [InitializeOnLoadMethod]
        private static void ApplyIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
                return;

            GameObject temp = null;
            try
            {
                temp = new GameObject("PrefabLockerIconTemp", typeof(PrefabLocker));
                var comp = temp.GetComponent<PrefabLocker>();
                var script = MonoScript.FromMonoBehaviour(comp);
                if (script != null)
                    EditorGUIUtility.SetIconForObject(script, icon);
            }
            finally
            {
                if (temp != null)
                    UnityEngine.Object.DestroyImmediate(temp);
            }

            // Скрываем иконку в Scene View
            EditorApplication.delayCall += HideGizmoIcon;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (_icon == null)
                return;

            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null)
                return;

            if (go.GetComponent<PrefabLocker>() == null)
                return;

            // Вычисляем ширину имени объекта
            var content = new GUIContent(go.name);
            var style = EditorStyles.label;
            var nameWidth = style.CalcSize(content).x;

            // Иконка после имени (с небольшим отступом)
            const float iconSize = 14f;
            const float offset = 18f; // отступ от начала (место для стандартной иконки)
            var iconRect = new Rect(
                selectionRect.x + offset + nameWidth + 2f,
                selectionRect.y + (selectionRect.height - iconSize) * 0.5f,
                iconSize,
                iconSize
            );

            GUI.DrawTexture(iconRect, _icon, ScaleMode.ScaleToFit);
        }

        private static void HideGizmoIcon()
        {
            SetGizmoAndIconEnabled(typeof(PrefabLocker), false);
        }

        /// <summary>
        /// Отключает иконку компонента в Scene View через внутренний AnnotationUtility.
        /// </summary>
        private static void SetGizmoAndIconEnabled(Type type, bool enabled)
        {
            var asm = Assembly.GetAssembly(typeof(Editor));
            var annotationUtility = asm?.GetType("UnityEditor.AnnotationUtility");
            if (annotationUtility == null)
                return;

            // classId = 114 для MonoBehaviour
            const int classId = 114;
            var scriptName = type.Name;

            // Отключаем гизмо (OnDrawGizmos) в Scene View.
            var setGizmoEnabled = annotationUtility.GetMethod(
                "SetGizmoEnabled",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(string), typeof(int), typeof(bool) },
                null);

            setGizmoEnabled?.Invoke(null, new object[] { classId, scriptName, enabled ? 1 : 0, false });

            // Отключаем иконку в Scene View.
            var setIconEnabled = annotationUtility.GetMethod(
                "SetIconEnabled",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(string), typeof(int) },
                null);

            setIconEnabled?.Invoke(null, new object[] { classId, scriptName, enabled ? 1 : 0 });
        }
    }
}
#endif
