using System.Collections.Generic;
using UnityEngine;

public class QuestWorldObjectivePointers : MonoBehaviour
{
    [SerializeField] GameObject _pointerPrefab;
    [SerializeField] Vector3 _groundLocalOffset;
    [SerializeField] Vector3 _overheadWorldOffset = new Vector3(0f, 1.5f, 0f);

    readonly List<GameObject> _instances = new List<GameObject>();
    readonly List<Transform> _focusAnchors = new List<Transform>();

    void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] != null)
                Destroy(_instances[i]);
        }

        _instances.Clear();
        _focusAnchors.Clear();
        QuestNavigationFocus.SetAnchors(_focusAnchors);
    }

    public void SetReachPoint(Transform anchor)
    {
        Clear();
        if (anchor == null)
            return;

        Spawn(anchor, _groundLocalOffset, offsetIsWorld: false, showGroundDecal: true);
        _focusAnchors.Add(anchor);
        QuestNavigationFocus.SetAnchors(_focusAnchors);
    }

    public void SetOverheadMarkers(IReadOnlyList<Transform> anchors)
    {
        Clear();
        if (anchors == null || anchors.Count == 0)
            return;

        for (int i = 0; i < anchors.Count; i++)
        {
            Transform anchor = anchors[i];
            if (anchor != null)
                Spawn(anchor, _overheadWorldOffset, offsetIsWorld: true, showGroundDecal: false);
        }

        for (int i = 0; i < anchors.Count; i++)
        {
            if (anchors[i] != null)
                _focusAnchors.Add(anchors[i]);
        }

        QuestNavigationFocus.SetAnchors(_focusAnchors);
    }

    void Spawn(Transform anchor, Vector3 offset, bool offsetIsWorld, bool showGroundDecal)
    {
        GameObject instance = Instantiate(_pointerPrefab, anchor.position, Quaternion.identity);
        _instances.Add(instance);

        QuestWorldPointerFollower follower = instance.GetComponent<QuestWorldPointerFollower>();
        follower.Configure(anchor, offset, offsetIsWorld, showGroundDecal);
    }
}
