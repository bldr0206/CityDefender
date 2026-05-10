using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveId : MonoBehaviour
{
    [SerializeField] string _id;

    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(_id))
                _id = BuildFallbackId();

            return _id;
        }
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(_id))
            _id = BuildFallbackId();

        SaveableRegistry.Register(this);
    }

    void OnDestroy()
    {
        SaveableRegistry.Unregister(this);
    }

#if UNITY_EDITOR
    static bool s_ResolvingSaveIdDuplicateCluster;

    void OnValidate()
    {
        if (Application.isPlaying) return;
        if (s_ResolvingSaveIdDuplicateCluster) return;

        if (string.IsNullOrEmpty(_id))
        {
            _id = Guid.NewGuid().ToString("N");
            return;
        }

        SaveId[] all = UnityEngine.Object.FindObjectsByType<SaveId>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        var cluster = new List<SaveId>();
        for (int i = 0; i < all.Length; i++)
        {
            SaveId s = all[i];
            if (s != null && !string.IsNullOrEmpty(s._id) && s._id == _id)
                cluster.Add(s);
        }

        if (cluster.Count < 2) return;

        cluster.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
        if (cluster[0] != this) return;

        s_ResolvingSaveIdDuplicateCluster = true;
        try
        {
            for (int i = 0; i < cluster.Count; i++)
            {
                SaveId s = cluster[i];
                Undo.RecordObject(s, "Fix duplicate SaveId");
                s._id = Guid.NewGuid().ToString("N");
                EditorUtility.SetDirty(s);
            }

            Debug.LogWarning(
                $"SaveId: {cluster.Count} объектов делили один id — всем назначены новые id. Сохраните сцены и сделайте новый сейв.");
        }
        finally
        {
            s_ResolvingSaveIdDuplicateCluster = false;
        }
    }
#endif

    /// <summary>Задаёт id до активации объекта (Instantiate inactive → SetRuntimeId → SetActive).</summary>
    public void SetRuntimeId(string id)
    {
        _id = id;
    }

    string BuildFallbackId()
    {
        return $"{gameObject.scene.name}/{BuildPath(transform)}";
    }

    string BuildPath(Transform current)
    {
        string path = $"{current.GetSiblingIndex()}:{current.name}";
        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.GetSiblingIndex()}:{current.name}/{path}";
        }

        return path;
    }
}

public static class SaveableRegistry
{
    static readonly List<SaveId> SaveIds = new List<SaveId>();

    public static void Register(SaveId saveId)
    {
        if (saveId != null && !SaveIds.Contains(saveId))
            SaveIds.Add(saveId);
    }

    public static void Unregister(SaveId saveId)
    {
        SaveIds.Remove(saveId);
    }

    public static bool TryGet<T>(string id, out T component) where T : Component
    {
        component = null;
        SaveId saveId = GetSaveId(id);
        if (saveId == null) return false;

        return saveId.TryGetComponent(out component);
    }

    public static List<T> GetAll<T>() where T : Component
    {
        List<T> components = new List<T>();
        for (int i = SaveIds.Count - 1; i >= 0; i--)
        {
            SaveId saveId = SaveIds[i];
            if (saveId == null)
            {
                SaveIds.RemoveAt(i);
                continue;
            }

            if (saveId.TryGetComponent(out T component))
                components.Add(component);
        }

        return components;
    }

    static SaveId GetSaveId(string id)
    {
        for (int i = SaveIds.Count - 1; i >= 0; i--)
        {
            SaveId saveId = SaveIds[i];
            if (saveId == null)
            {
                SaveIds.RemoveAt(i);
                continue;
            }

            if (saveId.Id == id)
                return saveId;
        }

        return null;
    }
}
