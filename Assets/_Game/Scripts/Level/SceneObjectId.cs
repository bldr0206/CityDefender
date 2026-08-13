using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Маркер объекта сцены со стабильным строковым id. Нужен, потому что ассет проекта
/// (ScriptableObject, напр. QuestLevelConfig) не может хранить прямую ссылку на Transform сцены —
/// вместо ссылки в ассете лежит id (<see cref="SceneObjectRef"/>), а в рантайме резолвится через реестр.
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneObjectId : MonoBehaviour
{
    [SerializeField] string _id;

    public string Id => _id;

    void OnEnable() => SceneObjectRegistry.Register(this);
    void OnDisable() => SceneObjectRegistry.Unregister(this);

    void Reset() => _id = gameObject.name;
}

/// <summary>Ссылка на объект сцены по id, хранимая в ассете. Резолвится через <see cref="SceneObjectRegistry"/>.</summary>
[Serializable]
public struct SceneObjectRef
{
    public string id;

    public Transform Resolve() => SceneObjectRegistry.Resolve(id);
}

/// <summary>Реестр активных <see cref="SceneObjectId"/>: id → Transform. Наполняется на время активности объектов.</summary>
public static class SceneObjectRegistry
{
    static readonly Dictionary<string, Transform> ById = new Dictionary<string, Transform>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => ById.Clear();

    public static void Register(SceneObjectId obj)
    {
        if (obj == null || string.IsNullOrEmpty(obj.Id)) return;
        ById[obj.Id] = obj.transform;
    }

    public static void Unregister(SceneObjectId obj)
    {
        if (obj == null || string.IsNullOrEmpty(obj.Id)) return;
        if (ById.TryGetValue(obj.Id, out Transform t) && t == obj.transform)
            ById.Remove(obj.Id);
    }

    public static Transform Resolve(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        ById.TryGetValue(id, out Transform t);
        return t;
    }
}
