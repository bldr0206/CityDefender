using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Квест «разбей объекты»: включает урон по quest-breakable, синхронизирует прогресс
/// (всего/разбито) со сценой и ведёт стрелки на непобитые объекты.
/// </summary>
public sealed class QuestBreakBreakablesHandler
{
    readonly QuestManager _ctx;
    readonly List<Breakable> _breakables = new List<Breakable>();

    public QuestBreakBreakablesHandler(QuestManager ctx)
    {
        _ctx = ctx;
    }

    public void Enable()
    {
        Actions.OnQuestBreakableRegistered += RegisterBreakable;
        Actions.OnQuestBreakableUnregistered += UnregisterBreakable;
        Actions.OnQuestBreakableBroken += CompleteProgress;
    }

    public void Disable()
    {
        Actions.OnQuestBreakableRegistered -= RegisterBreakable;
        Actions.OnQuestBreakableUnregistered -= UnregisterBreakable;
        Actions.OnQuestBreakableBroken -= CompleteProgress;
    }

    public void Run(Quest quest)
    {
        _ctx.ActiveCollectQuestId = quest.id;

        _ctx.SetActivePickableInteraction(null);
        SetDamageEnabled(quest.id);
        SyncProgressFromWorld();
        _ctx.UpdateCounterProgress();
        RefreshPointers();

        if (_ctx.CollectTarget <= 0)
            _ctx.CompleteCurrentQuest();
    }

    /// <summary> Включить урон у breakable активного квеста, у остальных — выключить. </summary>
    public void SetDamageEnabled(string activeQuestId)
    {
        for (int i = 0; i < _breakables.Count; i++)
        {
            Breakable b = _breakables[i];
            if (b != null)
                b.SetQuestDamageEnabled(b.IsQuestBound && b.QuestId == activeQuestId);
        }
    }

    /// <summary> Пересчитать цель/прогресс по всем quest-breakable уровня (устойчиво к сейву). </summary>
    public void SyncProgressFromWorld()
    {
        Quest quest = _ctx.CurrentQuest;
        if (quest == null || quest.type != QuestType.BreakBreakables)
            return;

        string questId = quest.id;
        List<Breakable> all = _ctx.LevelSaveController.GetBreakablesInLevel();
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

        _ctx.CollectTarget = quest.requiredAmount > 0 ? quest.requiredAmount : total;
        _ctx.CollectAmount = broken;
    }

    public void RefreshPointers()
    {
        Quest quest = _ctx.CurrentQuest;
        if (quest == null || quest.type != QuestType.BreakBreakables || _ctx.ActiveCollectQuestId != quest.id)
            return;

        List<Transform> list = new List<Transform>();
        for (int i = 0; i < _breakables.Count; i++)
        {
            Breakable b = _breakables[i];
            if (b != null && b.QuestId == quest.id && !b.IsBroken)
                list.Add(b.transform);
        }

        _ctx.Markers.ShowOverhead(list);
    }

    void CompleteProgress(string questId)
    {
        if (!_ctx.IsCurrentQuest(QuestType.BreakBreakables, questId))
            return;

        SyncProgressFromWorld();
        _ctx.UpdateCounterProgress();
        RefreshPointers();

        if (_ctx.CollectTarget > 0 && _ctx.CollectAmount >= _ctx.CollectTarget)
            _ctx.CompleteCurrentQuest();
    }

    void RegisterBreakable(Breakable breakable)
    {
        if (breakable == null || !breakable.IsQuestBound || _breakables.Contains(breakable))
            return;

        _breakables.Add(breakable);
        breakable.SetQuestDamageEnabled(breakable.QuestId == _ctx.ActiveCollectQuestId);

        Quest quest = _ctx.CurrentQuest;
        if (quest != null
            && quest.type == QuestType.BreakBreakables
            && _ctx.ActiveCollectQuestId == breakable.QuestId
            && quest.requiredAmount <= 0
            && breakable.QuestId == quest.id)
        {
            SyncProgressFromWorld();
        }

        RefreshPointers();
    }

    void UnregisterBreakable(Breakable breakable)
    {
        _breakables.Remove(breakable);
        RefreshPointers();
    }
}
