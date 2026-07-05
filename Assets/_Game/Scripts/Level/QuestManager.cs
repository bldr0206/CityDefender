using System.Collections;
using System.Collections.Generic;
using CityDef.Gameplay.Logic;
using UnityEngine;
using UnityEngine.Localization;
using Zenject;

/// <summary> Оркестратор квестов: очередь/завершение, последовательности и сейв; логика по типам квестов — в обработчиках. </summary>
public class QuestManager : MonoBehaviour
{
    [SerializeField] private QuestLevelConfig _questConfig;
    [SerializeField] private QuestWorldObjectivePointers _worldPointers;

    QuestPanel _questPanel;
    DialogueScreen _dialogueScreen;
    LevelSaveController _levelSaveController;
    PlayerCollector _playerCollector;

    Quest _currentQuest;
    string _activeCollectQuestId;
    int _currentCollectAmount;
    int _currentCollectTarget;
    bool _hasRestoredSave;

    readonly List<Quest> _allQuests = new List<Quest>();
    readonly List<string> _allQuestIds = new List<string>();
    readonly List<string> _completedQuestIds = new List<string>();

    QuestObjectiveMarkers _markers;
    QuestSequencePlayer _sequencePlayer;
    QuestReachPointHandler _reachPoint;
    QuestDeliverItemHandler _deliverItem;
    QuestOwnAgentsHandler _ownAgents;
    QuestBreakBreakablesHandler _breakBreakables;

    // ---- контекст, доступный обработчикам ----
    internal QuestPanel Panel => _questPanel;
    internal DialogueScreen DialogueScreen => _dialogueScreen;
    internal QuestObjectiveMarkers Markers => _markers;
    internal PlayerCollector PlayerCollector => _playerCollector;
    internal LevelSaveController LevelSaveController => _levelSaveController;
    internal QuestLevelConfig QuestConfig => _questConfig;
    internal Quest CurrentQuest => _currentQuest;
    internal string ActiveCollectQuestId { get => _activeCollectQuestId; set => _activeCollectQuestId = value; }
    internal int CollectAmount { get => _currentCollectAmount; set => _currentCollectAmount = value; }
    internal int CollectTarget { get => _currentCollectTarget; set => _currentCollectTarget = value; }

    internal bool IsCurrentQuest(QuestType type, string questId) =>
        _currentQuest != null && _currentQuest.type == type && _currentQuest.id == questId;

    internal void UpdateCounterProgress() => _questPanel.SetProgress(_currentCollectAmount, _currentCollectTarget);

    internal void SetActivePickableInteraction(string questId) => _deliverItem.SetInteraction(questId);
    internal void SetActiveBreakableDamage(string questId) => _breakBreakables.SetDamageEnabled(questId);

    [Inject]
    public void Construct(QuestPanel questPanel, DialogueScreen dialogueScreen, LevelSaveController levelSaveController, PlayerCollector playerCollector)
    {
        _questPanel = questPanel;
        _dialogueScreen = dialogueScreen;
        _levelSaveController = levelSaveController;
        _playerCollector = playerCollector;
    }

    void Awake()
    {
        _markers = new QuestObjectiveMarkers(_worldPointers);
        _sequencePlayer = new QuestSequencePlayer(this);
        _reachPoint = new QuestReachPointHandler(this);
        _deliverItem = new QuestDeliverItemHandler(this);
        _ownAgents = new QuestOwnAgentsHandler(this);
        _breakBreakables = new QuestBreakBreakablesHandler(this);
    }

    void OnEnable()
    {
        _reachPoint.Enable();
        _deliverItem.Enable();
        _ownAgents.Enable();
        _breakBreakables.Enable();
    }

    void OnDisable()
    {
        _reachPoint.Disable();
        _deliverItem.Disable();
        _ownAgents.Disable();
        _breakBreakables.Disable();
    }

    void Start()
    {
        InitializeQuestList();
        // ApplyLoadedData из LevelSceneLogic должен успеть выполниться до первого RunNextQuest:
        // Instantiate префаба уровня во время LoadLevel может вызвать этот Start синхронно,
        // до возврата в LevelSceneLogic.Start — иначе SaveAutoCheckpoint перезапишет autosave на диске.
        StartCoroutine(BootstrapQuestNextFrameIfNeeded());
    }

    IEnumerator BootstrapQuestNextFrameIfNeeded()
    {
        yield return null;
        if (_hasRestoredSave)
            yield break;

        _deliverItem.SetInteraction(null);
        _breakBreakables.SetDamageEnabled(null);
        RunNextQuest();
    }

    void RunNextQuest()
    {
        InitializeQuestList();
        _currentQuest = GetNextQuest();

        if (_currentQuest == null)
        {
            _questPanel.Hide();
            _markers.ClearWorldPointers();
            _deliverItem.SetInteraction(null);
            _breakBreakables.SetDamageEnabled(null);
            return;
        }

        _activeCollectQuestId = null;
        _questPanel.Hide();
        _levelSaveController.SaveAutoCheckpoint();
        _sequencePlayer.Play(_currentQuest.startSequence, BeginQuestAfterStartSequence);
    }

    void BeginQuestAfterStartSequence()
    {
        if (_currentQuest == null) return;

        _questPanel.Show();
        _questPanel.SetProgress(0, 0);
        _questPanel.UpdateQuestText(_currentQuest.title);
        RunCurrentQuest();
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
                _reachPoint.Run(quest);
                break;
            case QuestType.DeliverItem:
                _deliverItem.Run(quest);
                break;
            case QuestType.OwnAgents:
                _ownAgents.Run(quest);
                break;
            case QuestType.BreakBreakables:
                _breakBreakables.Run(quest);
                break;
        }
    }

    internal void CompleteCurrentQuest()
    {
        Quest completedQuest = _currentQuest;
        _markers.ClearWorldPointers();
        _markers.DestroyDestinationMarker();
        if (!_completedQuestIds.Contains(completedQuest.id))
            _completedQuestIds.Add(completedQuest.id);

        _currentQuest = null;
        _activeCollectQuestId = null;
        _currentCollectAmount = 0;
        _currentCollectTarget = 0;

        _questPanel.Hide();
        _deliverItem.SetInteraction(null);
        _breakBreakables.SetDamageEnabled(null);
        _sequencePlayer.Play(completedQuest.endSequence, RunNextQuest);
    }

    public QuestSaveData CaptureSaveData()
    {
        if (_currentQuest != null && _currentQuest.type == QuestType.BreakBreakables)
            _breakBreakables.SyncProgressFromWorld();

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

    public void RestoreSaveData(QuestSaveData data, bool resumeFromAutoCheckpoint = false)
    {
        _hasRestoredSave = true;
        InitializeQuestList();
        _markers.ClearWorldPointers();
        _markers.DestroyDestinationMarker();

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

        if (resumeFromAutoCheckpoint)
        {
            _questPanel.Hide();
            _sequencePlayer.Play(_currentQuest.startSequence, AfterCheckpointRestoreIntroSequence);
            return;
        }

        ApplyRestoredQuestGameplayAfterRestore();
    }

    void AfterCheckpointRestoreIntroSequence()
    {
        ApplyRestoredQuestGameplayAfterRestore();
        if (_currentQuest != null)
            _levelSaveController.SaveAutoCheckpoint();
    }

    void ApplyRestoredQuestGameplayAfterRestore()
    {
        if (_currentQuest == null)
            return;

        _questPanel.Show();
        _questPanel.UpdateQuestText(_currentQuest.title);

        if (_currentQuest.type == QuestType.ReachPoint)
        {
            _questPanel.SetProgress(0, 0);
            _reachPoint.Run(_currentQuest);
            return;
        }

        if (_currentQuest.type == QuestType.OwnAgents)
        {
            _ownAgents.Run(_currentQuest);
            return;
        }

        _activeCollectQuestId = _currentQuest.id;

        if (_currentQuest.type == QuestType.DeliverItem)
        {
            if (_currentCollectTarget <= 0)
            {
                _currentCollectTarget = _currentQuest.requiredAmount > 0
                    ? _currentQuest.requiredAmount
                    : _deliverItem.CountPickables(_currentQuest.id);
            }

            _deliverItem.SetInteraction(_currentQuest.id);
            _breakBreakables.SetDamageEnabled(null);
        }
        else if (_currentQuest.type == QuestType.BreakBreakables)
        {
            _deliverItem.SetInteraction(null);
            _breakBreakables.SetDamageEnabled(_currentQuest.id);
            _breakBreakables.SyncProgressFromWorld();
        }

        UpdateCounterProgress();

        if (_currentQuest.type == QuestType.DeliverItem)
            _deliverItem.RefreshPointers();
        else if (_currentQuest.type == QuestType.BreakBreakables)
            _breakBreakables.RefreshPointers();

        if (_currentQuest.type == QuestType.BreakBreakables
            && _currentCollectTarget > 0
            && _currentCollectAmount >= _currentCollectTarget)
            CompleteCurrentQuest();
    }

    void InitializeQuestList()
    {
        if (_allQuests.Count > 0) return;

        if (_questConfig == null || _questConfig.Quests == null || _questConfig.Quests.Count == 0)
            return;

        _allQuests.AddRange(_questConfig.Quests);
        for (int i = 0; i < _allQuests.Count; i++)
            _allQuestIds.Add(_allQuests[i].id);
    }

    Quest GetNextQuest()
    {
        string nextId = QuestQueue.NextIncomplete(_allQuestIds, _completedQuestIds);
        return nextId != null ? GetQuest(nextId) : null;
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

    void OnValidate()
    {
        if (_questConfig == null)
            Debug.LogWarning("QuestLevelConfig не назначен — квесты не будут доступны.", this);
    }
}
