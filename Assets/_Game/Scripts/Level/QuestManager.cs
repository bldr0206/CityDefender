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
    readonly List<Quest> _allQuests = new List<Quest>();
    readonly List<string> _completedQuestIds = new List<string>();
    string _activeCollectQuestId;
    int _currentCollectAmount;
    int _currentCollectTarget;
    bool _hasRestoredSave;
    LevelSaveController _levelSaveController;

    [Inject]
    public void Construct(QuestPanel questPanel, DialogueScreen dialogueScreen, LevelSaveController levelSaveController)
    {
        _questPanel = questPanel;
        _dialogueScreen = dialogueScreen;
        _levelSaveController = levelSaveController;
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
        InitializeQuestList();
        if (_hasRestoredSave) return;

        SetQuestPickablesInteraction(null);
        RunNextQuest();
    }

    void RunNextQuest()
    {
        InitializeQuestList();
        _currentQuest = GetNextQuest();

        if (_currentQuest == null)
        {
            _questPanel.Hide();
            Actions.QuestTargetChanged(null);
            SetQuestPickablesInteraction(null);
            return;
        }

        _activeCollectQuestId = null;
        _questPanel.Show();
        _questPanel.SetProgress(0, 0);
        _questPanel.UpdateQuestText(_currentQuest.title);

        PlaySequence(_currentQuest.startSequence, RunCurrentQuest);
    }

    void RunCurrentQuest()
    {
        if (_currentQuest != null)
            _levelSaveController.SaveAutoCheckpoint();

        RunQuest(_currentQuest);
    }

    void RunQuest(Quest quest)
    {
        switch (quest.type)
        {
            case QuestType.ReachPoint:
                RunReachPointQuest(quest);
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
        if (!_completedQuestIds.Contains(completedQuest.id))
            _completedQuestIds.Add(completedQuest.id);

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

    void RunReachPointQuest(Quest quest)
    {
        if (_questDestinationMarker != null)
            Destroy(_questDestinationMarker);

        _questDestinationMarker = Instantiate(_questDestinationMarkerPrefab, quest.targetPoint.position, quest.targetPoint.rotation);
        _questDestinationMarker.GetComponent<QuestDestinationMarker>().Init(quest.id);
        Actions.QuestTargetChanged(_questDestinationMarker.transform);
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

    public QuestSaveData CaptureSaveData()
    {
        return new QuestSaveData
        {
            currentQuestId = _currentQuest != null ? _currentQuest.id : null,
            currentCollectAmount = _currentCollectAmount,
            currentCollectTarget = _currentCollectTarget,
            completedQuestIds = new List<string>(_completedQuestIds),
        };
    }

    public string GetCurrentQuestSaveName()
    {
        if (_currentQuest == null)
            return "Level";

        string questName = _currentQuest.title.GetLocalizedString();
        if (string.IsNullOrWhiteSpace(questName))
            questName = _currentQuest.id;

        return string.IsNullOrWhiteSpace(questName) ? "Level" : questName;
    }

    public void RestoreSaveData(QuestSaveData data)
    {
        _hasRestoredSave = true;
        InitializeQuestList();
        if (_questDestinationMarker != null)
            Destroy(_questDestinationMarker);

        _completedQuestIds.Clear();
        if (data != null && data.completedQuestIds != null)
            _completedQuestIds.AddRange(data.completedQuestIds);

        _currentQuest = data != null ? GetQuest(data.currentQuestId) : GetNextQuest();
        _activeCollectQuestId = null;
        _currentCollectAmount = data != null ? data.currentCollectAmount : 0;
        _currentCollectTarget = data != null ? data.currentCollectTarget : 0;

        if (_currentQuest == null)
        {
            RunNextQuest();
            return;
        }

        _questPanel.Show();
        _questPanel.UpdateQuestText(_currentQuest.title);

        if (_currentQuest.type == QuestType.ReachPoint)
        {
            _questPanel.SetProgress(0, 0);
            RunReachPointQuest(_currentQuest);
            return;
        }

        _activeCollectQuestId = _currentQuest.id;
        if (_currentCollectTarget <= 0)
        {
            _currentCollectTarget = _currentQuest.requiredAmount > 0
                ? _currentQuest.requiredAmount
                : CountQuestPickables(_currentQuest.id);
        }

        SetQuestPickablesInteraction(_currentQuest.id);
        UpdateCollectItemsProgress();
        Actions.QuestTargetChanged(null);
    }

    void InitializeQuestList()
    {
        if (_allQuests.Count > 0) return;

        _allQuests.AddRange(_quests);
    }

    Quest GetNextQuest()
    {
        for (int i = 0; i < _allQuests.Count; i++)
        {
            if (!_completedQuestIds.Contains(_allQuests[i].id))
                return _allQuests[i];
        }

        return null;
    }

    Quest GetQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;

        for (int i = 0; i < _allQuests.Count; i++)
        {
            if (_allQuests[i].id == questId)
                return _allQuests[i];
        }

        return null;
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