using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Квестовые маркеры и навигация: наземный маркер точки-цели (ReachPoint) и
/// надголовные стрелки на объекты квеста через <see cref="QuestWorldObjectivePointers"/>.
/// </summary>
public sealed class QuestObjectiveMarkers
{
    readonly QuestWorldObjectivePointers _pointers;
    GameObject _destinationMarker;

    public QuestObjectiveMarkers(QuestWorldObjectivePointers pointers)
    {
        _pointers = pointers;
    }

    public void ClearWorldPointers()
    {
        if (_pointers != null)
            _pointers.Clear();
    }

    /// <summary> Показать надголовные стрелки на список объектов; пустой список — очистить. </summary>
    public void ShowOverhead(List<Transform> anchors)
    {
        if (_pointers == null)
            return;

        if (anchors.Count == 0)
            _pointers.Clear();
        else
            _pointers.SetOverheadMarkers(anchors);
    }

    public void ShowReachPoint(Transform anchor)
    {
        if (_pointers != null)
            _pointers.SetReachPoint(anchor);
    }

    /// <summary> Пересоздать наземный маркер точки-цели и вернуть его для инициализации. </summary>
    public GameObject SpawnDestinationMarker(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        DestroyDestinationMarker();
        _destinationMarker = Object.Instantiate(prefab, position, rotation);
        return _destinationMarker;
    }

    public void DestroyDestinationMarker()
    {
        if (_destinationMarker != null)
            Object.Destroy(_destinationMarker);
        _destinationMarker = null;
    }
}
