using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class QuestNavigationFocus : MonoBehaviour
{
    static IReadOnlyList<Transform> _anchors;

    Transform _player;
    Transform _lastSent;

    [Inject]
    public void Construct(PlayerController player)
    {
        _player = player.transform;
    }

    public static void SetAnchors(IReadOnlyList<Transform> anchors)
    {
        _anchors = anchors;
    }

    void LateUpdate()
    {
        if (_player == null)
            return;

        IReadOnlyList<Transform> anchors = _anchors;
        if (anchors == null || anchors.Count == 0)
        {
            ClearTargetIfNeeded();
            return;
        }

        Transform nearest = null;
        float bestSq = float.MaxValue;
        Vector3 p = _player.position;

        for (int i = 0; i < anchors.Count; i++)
        {
            Transform t = anchors[i];
            if (t == null)
                continue;

            float sq = (t.position - p).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                nearest = t;
            }
        }

        if (nearest == null)
        {
            ClearTargetIfNeeded();
            return;
        }

        if (nearest != _lastSent)
        {
            _lastSent = nearest;
            Actions.QuestTargetChanged(nearest);
        }
    }

    void ClearTargetIfNeeded()
    {
        if (_lastSent == null)
            return;

        _lastSent = null;
        Actions.QuestTargetChanged(null);
    }
}
