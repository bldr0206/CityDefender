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

    [Inject]
    public void Construct(QuestPanel questPanel, DialogueScreen dialogueScreen)
    {
        _questPanel = questPanel;
        _dialogueScreen = dialogueScreen;
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
            Actions.QuestTargetChanged(null);
            return;
        }

        _currentQuest = _quests[0];
        _questPanel.Show();
        _questPanel.UpdateQuestText(_currentQuest.title);

        PlayCutscene(_currentQuest.startCutscenePrefab, PlayStartDialogue);
    }

    void PlayStartDialogue()
    {
        if (_currentQuest.startDialogueData != null)
        {
            _dialogueScreen.Play(_currentQuest.startDialogueData, RunCurrentQuest);
            return;
        }

        RunCurrentQuest();
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
        }
    }

    void CompleteReachPointQuest(string questId)
    {
        if (_currentQuest == null || _currentQuest.id != questId) return;

        Quest completedQuest = _currentQuest;
        Destroy(_questDestinationMarker);
        _quests.Remove(completedQuest);
        _currentQuest = null;

        if (completedQuest.endDialogueData != null)
        {
            _dialogueScreen.Play(completedQuest.endDialogueData, () => PlayEndCutscene(completedQuest));
            return;
        }

        PlayEndCutscene(completedQuest);
    }

    void PlayEndCutscene(Quest completedQuest)
    {
        PlayCutscene(completedQuest.endCutscenePrefab, RunNextQuest);
    }

    void PlayCutscene(QuestCutscene cutscenePrefab, Action onFinished)
    {
        if (cutscenePrefab == null)
        {
            onFinished?.Invoke();
            return;
        }

        Instantiate(cutscenePrefab).Play(onFinished);
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
    public QuestCutscene startCutscenePrefab;
    public QuestCutscene endCutscenePrefab;
    public DialogueData startDialogueData;
    public DialogueData endDialogueData;
}

public enum QuestType
{
    ReachPoint,
}