using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Квест «найми помощников»: следит за числом нанятых ботов и ведёт стрелку на трейдера/магазин.
/// </summary>
public sealed class QuestOwnBotsHandler
{
    readonly QuestManager _ctx;

    public QuestOwnBotsHandler(QuestManager ctx)
    {
        _ctx = ctx;
    }

    public void Enable() => Actions.OnBotHired += HandleBotHired;
    public void Disable() => Actions.OnBotHired -= HandleBotHired;

    public void Run(Quest quest)
    {
        int target = GetTarget(quest);
        RefreshPointers(quest);
        _ctx.SetActiveBreakableDamage(null);

        UpdateProgress(quest, target);
        if (Game.HiredBotsCount >= target)
            _ctx.CompleteCurrentQuest();
    }

    public void RefreshPointers(Quest quest)
    {
        if (quest == null)
            return;

        if (quest.targetPoint == null)
        {
            _ctx.Markers.ClearWorldPointers();
            return;
        }

        _ctx.Markers.ShowOverhead(new List<Transform> { quest.targetPoint });
    }

    static int GetTarget(Quest quest) => quest.requiredAmount > 0 ? quest.requiredAmount : 1;

    void UpdateProgress(Quest quest, int target)
    {
        if (quest == null)
            return;

        int clamped = Mathf.Min(Game.HiredBotsCount, target);
        _ctx.Panel.SetProgress(clamped, target);
    }

    void HandleBotHired()
    {
        Quest quest = _ctx.CurrentQuest;
        if (quest == null || quest.type != QuestType.OwnBots)
            return;

        int target = GetTarget(quest);
        UpdateProgress(quest, target);

        if (Game.HiredBotsCount >= target)
            _ctx.CompleteCurrentQuest();
    }
}
