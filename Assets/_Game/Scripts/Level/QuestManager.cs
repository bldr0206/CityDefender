using UnityEngine;
using System;
using System.Collections.Generic;
using Zenject;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private List<Quest> _quests;
    [SerializeField] private GameObject _questDestinationMarkerPrefab;

    QuestPanel _questPanel;
    Quest _currentQuest;
    GameObject _questDestinationMarker;

    [Inject]
    public void Construct(QuestPanel questPanel)
    {
        _questPanel = questPanel;
    }

    void OnEnable()
    {
        Actions.OnQuestDestinationReached += CompleteReachPointQuest;
    }

    void OnDisable()
    {
        Actions.OnQuestDestinationReached -= CompleteReachPointQuest;
    }

    void Start()
    {
        RunNextQuest();
    }

    void RunNextQuest()
    {
        if (_quests.Count == 0)
        {
            _questPanel.Hide();
            return;
        }

        _currentQuest = RunQuest(_quests[0]);
        _questPanel.Show();
        _questPanel.UpdateQuestText(_currentQuest.title);
    }

    Quest RunQuest(Quest quest)
    {
        switch (quest.type)
        {
            case QuestType.ReachPoint:
                _questDestinationMarker = Instantiate(_questDestinationMarkerPrefab, quest.targetPoint.position, quest.targetPoint.rotation);
                _questDestinationMarker.GetComponent<QuestDestinationMarker>().Init(quest.id);
                break;
        }

        return quest;
    }

    void CompleteReachPointQuest(string questId)
    {
        if (_currentQuest == null || _currentQuest.id != questId) return;

        Destroy(_questDestinationMarker);
        _quests.Remove(_currentQuest);
        _currentQuest = null;
        RunNextQuest();
    }
}


[Serializable]
public class Quest
{
    public string id;
    public string title;
    public QuestType type;
    public Transform targetPoint;
    public int requiredAmount;
}

public enum QuestType
{
    ReachPoint,
}