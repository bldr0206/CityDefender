using UnityEngine;
using System;
using System.Collections.Generic;
using Zenject;
using UnityEngine.Localization;


public class QuestManager : MonoBehaviour
{
    [SerializeField] private List<Quest> _quests;
    [SerializeField] private GameObject _questDestinationMarkerPrefab;
    [SerializeField] private QuestWorldObjectivePointers _worldPointers;

    QuestPanel _questPanel;
    DialogueScreen _dialogueScreen;
    Quest _currentQuest;
    GameObject _questDestinationMarker;
    readonly List<PickableItem> _questPickables = new List<PickableItem>();
    readonly List<Breakable> _questBreakables = new List<Breakable>();
    readonly List<Quest> _allQuests = new List<Quest>();
    readonly List<string> _completedQuestIds = new List<string>();
    string _activeCollectQuestId;
    int _currentCollectAmount;
    int _currentCollectTarget;
    bool _hasRestoredSave;
    LevelSaveController _levelSaveController;
    PlayerCollector _playerCollector;

    [Inject]
    public void Construct(QuestPanel questPanel, DialogueScreen dialogueScreen, LevelSaveController levelSaveController, PlayerCollector playerCollector)
    {
        _questPanel = questPanel;
        _dialogueScreen = dialogueScreen;
        _levelSaveController = levelSaveController;
        _playerCollector = playerCollector;
    }

    void OnEnable()
    {
        Actions.OnQuestDestinationReached += CompleteReachPointQuest;
        Actions.OnQuestPickableRegistered += RegisterQuestPickable;
        Actions.OnQuestPickableUnregistered += UnregisterQuestPickable;
        Actions.OnQuestItemTurnedIn += CompleteDeliverItem;
        Actions.OnQuestBreakableRegistered += RegisterQuestBreakable;
        Actions.OnQuestBreakableUnregistered += UnregisterQuestBreakable;
        Actions.OnQuestBreakableBroken += CompleteBreakBreakablesQuestProgress;
        Actions.OnAgentHired += HandleAgentHired;
        Actions.OnQuestCarryingPickablesChanged += HandleQuestCarryingPickablesChanged;
    }

    void OnDisable()
    {
        Actions.OnQuestDestinationReached -= CompleteReachPointQuest;
        Actions.OnQuestPickableRegistered -= RegisterQuestPickable;
        Actions.OnQuestPickableUnregistered -= UnregisterQuestPickable;
        Actions.OnQuestItemTurnedIn -= CompleteDeliverItem;
        Actions.OnQuestBreakableRegistered -= RegisterQuestBreakable;
        Actions.OnQuestBreakableUnregistered -= UnregisterQuestBreakable;
        Actions.OnQuestBreakableBroken -= CompleteBreakBreakablesQuestProgress;
        Actions.OnAgentHired -= HandleAgentHired;
        Actions.OnQuestCarryingPickablesChanged -= HandleQuestCarryingPickablesChanged;
    }

    void Start()
    {
        InitializeQuestList();
        if (_hasRestoredSave) return;

        SetQuestPickablesInteraction(null);
        SetQuestBreakablesDamageEnabled(null);
        RunNextQuest();
    }

    void RunNextQuest()
    {
        InitializeQuestList();
        _currentQuest = GetNextQuest();

        if (_currentQuest == null)
        {
            _questPanel.Hide();
            ClearQuestWorldPointers();
            SetQuestPickablesInteraction(null);
            SetQuestBreakablesDamageEnabled(null);
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

            case QuestType.DeliverItem:
                RunDeliverItemQuest(quest);
                break;

            case QuestType.OwnAgents:
                RunOwnAgentsQuest(quest);
                break;

            case QuestType.BreakBreakables:
                RunBreakBreakablesQuest(quest);
                break;
        }
    }

    static int GetOwnAgentsTarget(Quest quest)
    {
        return quest.requiredAmount > 0 ? quest.requiredAmount : 1;
    }

    void RunOwnAgentsQuest(Quest quest)
    {
        int target = GetOwnAgentsTarget(quest);
        RefreshOwnAgentsPointers(quest);
        SetQuestBreakablesDamageEnabled(null);

        UpdateOwnAgentsProgress(quest, target);
        if (Game.HiredAgentsCount >= target)
            CompleteCurrentQuest();
    }

    void UpdateOwnAgentsProgress(Quest quest, int target)
    {
        if (quest == null) return;
        int clamped = Mathf.Min(Game.HiredAgentsCount, target);
        _questPanel.SetProgress(clamped, target);
    }

    void HandleAgentHired()
    {
        if (_currentQuest == null || _currentQuest.type != QuestType.OwnAgents)
            return;

        int target = GetOwnAgentsTarget(_currentQuest);
        UpdateOwnAgentsProgress(_currentQuest, target);

        if (Game.HiredAgentsCount >= target)
            CompleteCurrentQuest();
    }

    void CompleteReachPointQuest(string questId)
    {
        if (!IsCurrentQuest(QuestType.ReachPoint, questId)) return;

        CompleteCurrentQuest();
    }

    void CompleteCurrentQuest()
    {
        Quest completedQuest = _currentQuest;
        ClearQuestWorldPointers();
        Destroy(_questDestinationMarker);
        _questDestinationMarker = null;
        if (!_completedQuestIds.Contains(completedQuest.id))
            _completedQuestIds.Add(completedQuest.id);

        _currentQuest = null;
        _activeCollectQuestId = null;
        _currentCollectAmount = 0;
        _currentCollectTarget = 0;

        _questPanel.SetProgress(0, 0);
        SetQuestPickablesInteraction(null);
        SetQuestBreakablesDamageEnabled(null);
        PlaySequence(completedQuest.endSequence, RunNextQuest);
    }

    void RunDeliverItemQuest(Quest quest)
    {
        _currentCollectAmount = 0;
        _currentCollectTarget = quest.requiredAmount > 0
            ? quest.requiredAmount
            : CountQuestPickables(quest.id);
        _activeCollectQuestId = quest.id;

        SetQuestPickablesInteraction(quest.id);
        SetQuestBreakablesDamageEnabled(null);
        UpdateCounterQuestProgress();
        RefreshDeliverItemPointers();

        if (_currentCollectTarget <= 0)
            CompleteCurrentQuest();
    }

    void RunBreakBreakablesQuest(Quest quest)
    {
        _activeCollectQuestId = quest.id;

        SetQuestPickablesInteraction(null);
        SetQuestBreakablesDamageEnabled(quest.id);
        SyncBreakBreakablesProgressFromWorld();
        UpdateCounterQuestProgress();
        RefreshBreakablePointers();

        if (_currentCollectTarget <= 0)
            CompleteCurrentQuest();
    }

    void RunReachPointQuest(Quest quest)
    {
        ClearQuestWorldPointers();
        if (_questDestinationMarker != null)
            Destroy(_questDestinationMarker);

        SetQuestBreakablesDamageEnabled(null);

        _questDestinationMarker = Instantiate(_questDestinationMarkerPrefab, quest.targetPoint.position, quest.targetPoint.rotation);
        _questDestinationMarker.GetComponent<QuestDestinationMarker>().Init(quest.id);
        if (_worldPointers != null)
            _worldPointers.SetReachPoint(_questDestinationMarker.transform);
    }

    void CompleteDeliverItem(string questId)
    {
        if (!IsCurrentQuest(QuestType.DeliverItem, questId)) return;

        _currentCollectAmount++;
        UpdateCounterQuestProgress();
        RefreshDeliverItemPointers();

        if (_currentCollectAmount >= _currentCollectTarget)
            CompleteCurrentQuest();
    }

    void CompleteBreakBreakablesQuestProgress(string questId)
    {
        if (!IsCurrentQuest(QuestType.BreakBreakables, questId)) return;

        SyncBreakBreakablesProgressFromWorld();
        UpdateCounterQuestProgress();
        RefreshBreakablePointers();

        if (_currentCollectTarget > 0 && _currentCollectAmount >= _currentCollectTarget)
            CompleteCurrentQuest();
    }

    void UpdateCounterQuestProgress()
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
            && _currentQuest.type == QuestType.DeliverItem
            && _activeCollectQuestId == item.QuestId
            && _currentQuest.requiredAmount <= 0
            && item.QuestId == _currentQuest.id)
        {
            _currentCollectTarget = CountQuestPickables(_currentQuest.id);
            UpdateCounterQuestProgress();
        }

        RefreshDeliverItemPointers();
    }

    void UnregisterQuestPickable(PickableItem item)
    {
        _questPickables.Remove(item);
        RefreshDeliverItemPointers();
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

    int CountForCurrentCounterQuest()
    {
        return _currentQuest.type == QuestType.BreakBreakables
            ? CountQuestBreakables(_currentQuest.id)
            : CountQuestPickables(_currentQuest.id);
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

    void RegisterQuestBreakable(Breakable breakable)
    {
        if (breakable == null || !breakable.IsQuestBound || _questBreakables.Contains(breakable)) return;

        _questBreakables.Add(breakable);
        ApplyQuestBreakableDamage(breakable);

        if (_currentQuest != null
            && _currentQuest.type == QuestType.BreakBreakables
            && _activeCollectQuestId == breakable.QuestId
            && _currentQuest.requiredAmount <= 0
            && breakable.QuestId == _currentQuest.id)
        {
            SyncBreakBreakablesProgressFromWorld();
        }

        RefreshBreakablePointers();
    }

    void UnregisterQuestBreakable(Breakable breakable)
    {
        _questBreakables.Remove(breakable);
        RefreshBreakablePointers();
    }

    int CountQuestBreakables(string questId)
    {
        int count = 0;

        for (int i = 0; i < _questBreakables.Count; i++)
        {
            Breakable b = _questBreakables[i];
            if (b != null && b.QuestId == questId && !b.IsBroken)
                count++;
        }

        return count;
    }

    void SetQuestBreakablesDamageEnabled(string activeQuestId)
    {
        for (int i = 0; i < _questBreakables.Count; i++)
        {
            Breakable b = _questBreakables[i];
            if (b != null)
                b.SetQuestDamageEnabled(b.IsQuestBound && b.QuestId == activeQuestId);
        }
    }

    void ApplyQuestBreakableDamage(Breakable breakable)
    {
        breakable.SetQuestDamageEnabled(breakable.QuestId == _activeCollectQuestId);
    }

    void ClearQuestWorldPointers()
    {
        if (_worldPointers != null)
            _worldPointers.Clear();
    }

    void RefreshDeliverItemPointers()
    {
        if (_worldPointers == null
            || _currentQuest == null
            || _currentQuest.type != QuestType.DeliverItem
            || _activeCollectQuestId != _currentQuest.id)
            return;

        List<Transform> list = new List<Transform>();
        for (int i = 0; i < _questPickables.Count; i++)
        {
            PickableItem item = _questPickables[i];
            if (item != null && item.QuestId == _currentQuest.id && !item.IsCollected)
                list.Add(item.transform);
        }

        if (_currentQuest.collectTurnInPoint != null
            && (_currentQuest.collectAlwaysShowTurnInPointer
                || (_playerCollector != null && _playerCollector.IsCarryingQuestPickable(_currentQuest.id))))
            list.Add(_currentQuest.collectTurnInPoint);

        if (list.Count == 0)
            _worldPointers.Clear();
        else
            _worldPointers.SetOverheadMarkers(list);
    }

    void HandleQuestCarryingPickablesChanged()
    {
        RefreshDeliverItemPointers();
    }

    void RefreshBreakablePointers()
    {
        if (_worldPointers == null
            || _currentQuest == null
            || _currentQuest.type != QuestType.BreakBreakables
            || _activeCollectQuestId != _currentQuest.id)
            return;

        List<Transform> list = new List<Transform>();
        for (int i = 0; i < _questBreakables.Count; i++)
        {
            Breakable b = _questBreakables[i];
            if (b != null && b.QuestId == _currentQuest.id && !b.IsBroken)
                list.Add(b.transform);
        }

        if (list.Count == 0)
            _worldPointers.Clear();
        else
            _worldPointers.SetOverheadMarkers(list);
    }

    void RefreshOwnAgentsPointers(Quest quest)
    {
        if (_worldPointers == null || quest == null)
            return;

        if (quest.targetPoint == null)
        {
            _worldPointers.Clear();
            return;
        }

        _worldPointers.SetOverheadMarkers(new List<Transform> { quest.targetPoint });
    }

    public QuestSaveData CaptureSaveData()
    {
        if (_currentQuest != null && _currentQuest.type == QuestType.BreakBreakables)
            SyncBreakBreakablesProgressFromWorld();

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
        ClearQuestWorldPointers();
        if (_questDestinationMarker != null)
            Destroy(_questDestinationMarker);

        _questDestinationMarker = null;

        _completedQuestIds.Clear();
        if (data != null && data.completedQuestIds != null)
            _completedQuestIds.AddRange(data.completedQuestIds);

        _currentQuest = data != null ? GetQuest(data.currentQuestId) : GetNextQuest();
        _activeCollectQuestId = null;
        _currentCollectAmount = 0;
        _currentCollectTarget = 0;
        if (data != null
            && _currentQuest != null
            && _currentQuest.type == QuestType.DeliverItem)
        {
            _currentCollectAmount = data.currentCollectAmount;
            _currentCollectTarget = data.currentCollectTarget;
        }

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

        if (_currentQuest.type == QuestType.OwnAgents)
        {
            RunOwnAgentsQuest(_currentQuest);
            return;
        }

        _activeCollectQuestId = _currentQuest.id;

        if (_currentQuest.type == QuestType.DeliverItem)
        {
            if (_currentCollectTarget <= 0)
            {
                _currentCollectTarget = _currentQuest.requiredAmount > 0
                    ? _currentQuest.requiredAmount
                    : CountForCurrentCounterQuest();
            }

            SetQuestPickablesInteraction(_currentQuest.id);
            SetQuestBreakablesDamageEnabled(null);
        }
        else if (_currentQuest.type == QuestType.BreakBreakables)
        {
            SetQuestPickablesInteraction(null);
            SetQuestBreakablesDamageEnabled(_currentQuest.id);
            SyncBreakBreakablesProgressFromWorld();
        }

        UpdateCounterQuestProgress();

        if (_currentQuest.type == QuestType.DeliverItem)
            RefreshDeliverItemPointers();
        else if (_currentQuest.type == QuestType.BreakBreakables)
            RefreshBreakablePointers();
        else if (_currentQuest.type == QuestType.OwnAgents)
            RefreshOwnAgentsPointers(_currentQuest);

        if (_currentQuest.type == QuestType.BreakBreakables
            && _currentCollectTarget > 0
            && _currentCollectAmount >= _currentCollectTarget)
            CompleteCurrentQuest();
    }

    void SyncBreakBreakablesProgressFromWorld()
    {
        if (_currentQuest == null || _currentQuest.type != QuestType.BreakBreakables)
            return;

        string questId = _currentQuest.id;
        List<Breakable> all = _levelSaveController.GetBreakablesInLevel();
        int total = 0;
        int broken = 0;

        for (int i = 0; i < all.Count; i++)
        {
            Breakable b = all[i];
            if (b == null || !b.IsQuestBound || b.QuestId != questId)
                continue;

            total++;
            if (b.IsBroken)
                broken++;
        }

        _currentCollectTarget = _currentQuest.requiredAmount > 0
            ? _currentQuest.requiredAmount
            : total;
        _currentCollectAmount = broken;
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
    [Tooltip("Reach Point: зона квеста. Own Agents: трейдер / магазин (стрелка навигации). Другие типы могут не использовать.")]
    public Transform targetPoint;

    [Tooltip("Deliver Item: точка сдачи (автомат бутылок, зона у двери с ключом и т.д.).")]
    public Transform collectTurnInPoint;

    [Tooltip("Deliver Item: показывать стрелку на точку сдачи сразу, до подбора предмета.")]
    public bool collectAlwaysShowTurnInPointer;

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
    DeliverItem,
    OwnAgents,
    BreakBreakables,
}