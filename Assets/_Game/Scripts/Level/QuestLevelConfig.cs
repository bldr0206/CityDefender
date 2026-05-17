using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestLevelConfig", menuName = "Game/Quest Level Config")]
public class QuestLevelConfig : ScriptableObject
{
    [SerializeField] List<Quest> _quests = new List<Quest>();
    [SerializeField] GameObject _questDestinationMarkerPrefab;

    public IReadOnlyList<Quest> Quests => _quests;

    public GameObject QuestDestinationMarkerPrefab => _questDestinationMarkerPrefab;
}
