using System.Collections.Generic;
using UnityEngine;

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
    void OnValidate()
    {
        if (!Application.isPlaying && string.IsNullOrEmpty(_id))
            _id = System.Guid.NewGuid().ToString("N");
    }
#endif

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
