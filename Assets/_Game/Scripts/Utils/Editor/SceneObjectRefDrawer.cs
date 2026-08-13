using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует <see cref="SceneObjectRef"/> обычным object-полем с приёмом drag-and-drop из иерархии.
/// В ассет ссылку на сцену Unity сохранить не может, поэтому при дропе объекту проставляется
/// <see cref="SceneObjectId"/> (добавляется при отсутствии), а в ref пишется его id. Для показа id резолвится обратно.
/// </summary>
[CustomPropertyDrawer(typeof(SceneObjectRef))]
public sealed class SceneObjectRefDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label = EditorGUI.BeginProperty(position, label, property);

        SerializedProperty idProp = property.FindPropertyRelative("id");
        string id = idProp.stringValue;

        Transform current = ResolveInScenes(id);
        var content = current == null && !string.IsNullOrEmpty(id)
            ? new GUIContent($"⚠ {id}", "Объект с этим id не найден на загруженных сценах.")
            : label;

        EditorGUI.BeginChangeCheck();
        Object picked = EditorGUI.ObjectField(position, content, current, typeof(Transform), true);
        if (EditorGUI.EndChangeCheck())
            idProp.stringValue = picked == null ? string.Empty : EnsureId((Transform)picked);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

    static Transform ResolveInScenes(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (SceneObjectId anchor in Object.FindObjectsByType<SceneObjectId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (anchor != null && anchor.Id == id)
                return anchor.transform;
        }

        return null;
    }

    /// <summary>Гарантирует у объекта <see cref="SceneObjectId"/> с уникальным id и возвращает его.</summary>
    static string EnsureId(Transform t)
    {
        SceneObjectId anchor = t.GetComponent<SceneObjectId>();
        if (anchor == null)
            anchor = Undo.AddComponent<SceneObjectId>(t.gameObject);

        if (string.IsNullOrEmpty(anchor.Id) || IsIdTakenByOther(anchor.Id, anchor))
        {
            var so = new SerializedObject(anchor);
            so.FindProperty("_id").stringValue = MakeUniqueId(t.gameObject.name, anchor);
            so.ApplyModifiedProperties();
        }

        return anchor.Id;
    }

    static string MakeUniqueId(string baseId, SceneObjectId self)
    {
        if (string.IsNullOrEmpty(baseId)) baseId = "SceneObject";
        string candidate = baseId;
        int i = 1;
        while (IsIdTakenByOther(candidate, self))
            candidate = $"{baseId}_{i++}";

        return candidate;
    }

    static bool IsIdTakenByOther(string id, SceneObjectId self)
    {
        foreach (SceneObjectId anchor in Object.FindObjectsByType<SceneObjectId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (anchor != null && anchor != self && anchor.Id == id)
                return true;
        }

        return false;
    }
}
