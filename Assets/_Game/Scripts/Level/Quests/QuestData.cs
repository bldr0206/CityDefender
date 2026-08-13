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
    [Tooltip("Reach Point: зона квеста. Own Bots: трейдер / магазин (стрелка навигации). Другие типы могут не использовать.")]
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
    public List<CutsceneShot> cutsceneShots = new List<CutsceneShot>();
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

/// <summary>Кадр катсцены: фокус камеры на объекте с шаблонным ракурсом, настраивается прямо в сиквенсе.</summary>
[Serializable]
public class CutsceneShot
{
    [Tooltip("Объект сцены, на который наводится камера (по SceneObjectId).")]
    public SceneObjectRef target;

    [Tooltip("Сколько секунд держать кадр после завершения перехода.")]
    public float duration = 1f;

    [Tooltip("TopDown — как камера игрока; Close — близкий наземный ракурс; ZoomIn — приближенный вид сверху.")]
    public CutsceneShotView view;

    [Tooltip("Smooth — плавный переход за 1 с; Instant — мгновенная смена кадра.")]
    public CutsceneShotTransition transition;
}

public enum CutsceneShotView
{
    TopDown,
    Close,
    ZoomIn,
}

public enum CutsceneShotTransition
{
    Smooth,
    Instant,
}

public enum QuestType
{
    ReachPoint,
    DeliverItem,
    OwnBots,
    BreakBreakables,
}
