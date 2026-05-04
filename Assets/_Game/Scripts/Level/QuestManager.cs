using UnityEngine;
using System;
using System.Collections.Generic;
using Zenject;
using UnityEngine.Localization;


public class QuestManager : MonoBehaviour
{
    [SerializeField] private List<Quest> _quests;
    [SerializeField] private GameObject _questDestinationMarkerPrefab;

    QuestPanel _questPanel;
    DialogueScreen _dialogueScreen;
    Quest _currentQuest;
    GameObject _questDestinationMarker;
    readonly List<PickableItem> _questPickables = new List<PickableItem>();
    string _activeCollectQuestId;
    int _currentCollectAmount;
    int _currentCollectTarget;

    [Inject]
    public void Construct(QuestPanel questPanel, DialogueScreen dialogueScreen)
    {
        _questPanel = questPanel;
        _dialogueScreen = dialogueScreen;
    }

    void OnEnable()
    {
        Actions.OnQuestDestinationReached += CompleteReachPointQuest;
        Actions.OnQuestPickableRegistered += RegisterQuestPickable;
        Actions.OnQuestPickableUnregistered += UnregisterQuestPickable;
        Actions.OnQuestItemTurnedIn += CompleteCollectItem;
    }

    void OnDisable()
    {
        Actions.OnQuestDestinationReached -= CompleteReachPointQuest;
        Actions.OnQuestPickableRegistered -= RegisterQuestPickable;
        Actions.OnQuestPickableUnregistered -= UnregisterQuestPickable;
        Actions.OnQuestItemTurnedIn -= CompleteCollectItem;
    }

    void Start()
    {
        SetQuestPickablesInteraction(null);
        RunNextQuest();
    }

    void RunNextQuest()
    {
        if (_quests.Count == 0)
        {
            _questPanel.Hide();
            Actions.QuestTargetChanged(null);
            SetQuestPickablesInteraction(null);
            return;
        }

        _currentQuest = _quests[0];
        _activeCollectQuestId = null;
        _questPanel.Show();
        _questPanel.SetProgress(0, 0);
        _questPanel.UpdateQuestText(_currentQuest.title);

        PlaySequence(_currentQuest.startSequence, RunCurrentQuest);
    }

    void RunCurrentQuest()
    {
        RunQuest(_currentQuest);
    }

    void RunQuest(Quest quest)
    {
        switch (quest.type)
        {
            case QuestType.ReachPoint:
                _questDestinationMarker = Instantiate(_questDestinationMarkerPrefab, quest.targetPoint.position, quest.targetPoint.rotation);
                _questDestinationMarker.GetComponent<QuestDestinationMarker>().Init(quest.id);
                Actions.QuestTargetChanged(_questDestinationMarker.transform);
                break;

            case QuestType.CollectItems:
                RunCollectItemsQuest(quest);
                break;
        }
    }

    void CompleteReachPointQuest(string questId)
    {
        if (!IsCurrentQuest(QuestType.ReachPoint, questId)) return;

        CompleteCurrentQuest();
    }

    void CompleteCurrentQuest()
    {
        Quest completedQuest = _currentQuest;
        Destroy(_questDestinationMarker);
        _questDestinationMarker = null;
        _quests.Remove(completedQuest);
        _currentQuest = null;
        _activeCollectQuestId = null;

        _questPanel.SetProgress(0, 0);
        Actions.QuestTargetChanged(null);
        SetQuestPickablesInteraction(null);
        PlaySequence(completedQuest.endSequence, RunNextQuest);
    }

    void RunCollectItemsQuest(Quest quest)
    {
        _currentCollectAmount = 0;
        _currentCollectTarget = quest.requiredAmount > 0
            ? quest.requiredAmount
            : CountQuestPickables(quest.id);
        _activeCollectQuestId = quest.id;

        SetQuestPickablesInteraction(quest.id);
        UpdateCollectItemsProgress();

        if (_currentCollectTarget <= 0)
            CompleteCurrentQuest();
    }

    void CompleteCollectItem(string questId)
    {
        if (!IsCurrentQuest(QuestType.CollectItems, questId)) return;

        _currentCollectAmount++;
        UpdateCollectItemsProgress();

        if (_currentCollectAmount >= _currentCollectTarget)
            CompleteCurrentQuest();
    }

    void UpdateCollectItemsProgress()
    {
        _questPanel.SetProgress(_currentCollectAmount, _currentCollectTarget);
    }

    bool IsCurrentQuest(QuestType type, string questId)
    {
        return _currentQuest != null
            && _currentQuest.type == type
            && _currentQuest.id == questId;
    }

    void RegisterQuestPickable(PickableItem item)
    {
        if (item == null || !item.IsQuestBound || _questPickables.Contains(item)) return;

        _questPickables.Add(item);
        ApplyQuestPickableInteraction(item);

        if (_currentQuest != null
            && _currentQuest.type == QuestType.CollectItems
            && _activeCollectQuestId == item.QuestId
            && _currentQuest.requiredAmount <= 0
            && item.QuestId == _currentQuest.id)
        {
            _currentCollectTarget = CountQuestPickables(_currentQuest.id);
            UpdateCollectItemsProgress();
        }
    }

    void UnregisterQuestPickable(PickableItem item)
    {
        _questPickables.Remove(item);
    }

    int CountQuestPickables(string questId)
    {
        int count = 0;

        for (int i = 0; i < _questPickables.Count; i++)
        {
            PickableItem item = _questPickables[i];
            if (item != null && item.QuestId == questId)
                count++;
        }

        return count;
    }

    void SetQuestPickablesInteraction(string activeQuestId)
    {
        for (int i = 0; i < _questPickables.Count; i++)
        {
            PickableItem item = _questPickables[i];
            if (item != null)
                item.SetInteractionEnabled(item.QuestId == activeQuestId);
        }
    }

    void ApplyQuestPickableInteraction(PickableItem item)
    {
        item.SetInteractionEnabled(item.QuestId == _activeCollectQuestId);
    }

    void PlaySequence(List<QuestSequenceStep> sequence, Action onFinished, int index = 0)
    {
        if (index >= sequence.Count)
        {
            onFinished?.Invoke();
            return;
        }

        QuestSequenceStep step = sequence[index];
        PlayStep(step, () => PlaySequence(sequence, onFinished, index + 1));
    }

    void PlayStep(QuestSequenceStep step, Action onFinished)
    {
        switch (step.type)
        {
            case QuestSequenceStepType.Cutscene:
                if (step.cutscenePrefab == null)
                {
                    onFinished?.Invoke();
                    return;
                }

                Instantiate(step.cutscenePrefab).Play(onFinished);
                break;

            case QuestSequenceStepType.Dialogue:
                if (step.dialogueData == null)
                {
                    onFinished?.Invoke();
                    return;
                }

                _dialogueScreen.Play(step.dialogueData, onFinished);
                break;
        }
    }
}


[Serializable]
public class Quest
{
    public string id;
    public LocalizedString title;
    public QuestType type;
    public Transform targetPoint;
    public int requiredAmount;
    public List<QuestSequenceStep> startSequence = new List<QuestSequenceStep>();
    public List<QuestSequenceStep> endSequence = new List<QuestSequenceStep>();
}

[Serializable]
public class QuestSequenceStep
{
    public QuestSequenceStepType type;
    public QuestCutscene cutscenePrefab;
    public DialogueData dialogueData;
}

public enum QuestSequenceStepType
{
    Cutscene,
    Dialogue,
}

public enum QuestType
{
    ReachPoint,
    CollectItems,
}