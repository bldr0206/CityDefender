using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

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
    [Tooltip("Только для типа Pause: пауза в секундах. Панель квеста скрыта; Time.timeScale не меняется.")]
    public float pauseDuration = 0.5f;
}

public enum QuestSequenceStepType
{
    Cutscene,
    Dialogue,
    Pause,
}

public enum QuestType
{
    ReachPoint,
    DeliverItem,
    OwnAgents,
    BreakBreakables,
}
