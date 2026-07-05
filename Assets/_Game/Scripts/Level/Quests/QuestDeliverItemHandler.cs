using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Квест «сдать предметы»: считает подобранные и сданные quest-предметы, включает у них
/// взаимодействие и ведёт стрелки на предметы/точку сдачи.
/// </summary>
public sealed class QuestDeliverItemHandler
{
    readonly QuestManager _ctx;
    readonly List<PickableItem> _pickables = new List<PickableItem>();

    public QuestDeliverItemHandler(QuestManager ctx)
    {
        _ctx = ctx;
    }

    public void Enable()
    {
        Actions.OnQuestPickableRegistered += RegisterPickable;
        Actions.OnQuestPickableUnregistered += UnregisterPickable;
        Actions.OnQuestItemTurnedIn += CompleteTurnIn;
        Actions.OnQuestCarryingPickablesChanged += RefreshPointers;
    }

    public void Disable()
    {
        Actions.OnQuestPickableRegistered -= RegisterPickable;
        Actions.OnQuestPickableUnregistered -= UnregisterPickable;
        Actions.OnQuestItemTurnedIn -= CompleteTurnIn;
        Actions.OnQuestCarryingPickablesChanged -= RefreshPointers;
    }

    public void Run(Quest quest)
    {
        _ctx.CollectAmount = 0;
        _ctx.CollectTarget = quest.requiredAmount > 0 ? quest.requiredAmount : CountPickables(quest.id);
        _ctx.ActiveCollectQuestId = quest.id;

        SetInteraction(quest.id);
        _ctx.SetActiveBreakableDamage(null);
        _ctx.UpdateCounterProgress();
        RefreshPointers();

        if (_ctx.CollectTarget <= 0)
            _ctx.CompleteCurrentQuest();
    }

    /// <summary> Включить взаимодействие у предметов активного квеста, у остальных — выключить. </summary>
    public void SetInteraction(string activeQuestId)
    {
        for (int i = 0; i < _pickables.Count; i++)
        {
            PickableItem item = _pickables[i];
            if (item != null)
                item.SetInteractionEnabled(item.QuestId == activeQuestId);
        }
    }

    public int CountPickables(string questId)
    {
        int count = 0;
        for (int i = 0; i < _pickables.Count; i++)
        {
            PickableItem item = _pickables[i];
            if (item != null && item.QuestId == questId)
                count++;
        }

        return count;
    }

    public void RefreshPointers()
    {
        Quest quest = _ctx.CurrentQuest;
        if (quest == null || quest.type != QuestType.DeliverItem || _ctx.ActiveCollectQuestId != quest.id)
            return;

        List<Transform> list = new List<Transform>();
        for (int i = 0; i < _pickables.Count; i++)
        {
            PickableItem item = _pickables[i];
            if (item != null && item.QuestId == quest.id && !item.IsCollected)
                list.Add(item.transform);
        }

        if (quest.collectTurnInPoint != null
            && (quest.collectAlwaysShowTurnInPointer
                || (_ctx.PlayerCollector != null && _ctx.PlayerCollector.IsCarryingQuestPickable(quest.id))))
            list.Add(quest.collectTurnInPoint);

        _ctx.Markers.ShowOverhead(list);
    }

    void CompleteTurnIn(string questId)
    {
        if (!_ctx.IsCurrentQuest(QuestType.DeliverItem, questId))
            return;

        _ctx.CollectAmount++;
        _ctx.UpdateCounterProgress();
        RefreshPointers();

        if (_ctx.CollectAmount >= _ctx.CollectTarget)
            _ctx.CompleteCurrentQuest();
    }

    void RegisterPickable(PickableItem item)
    {
        if (item == null || !item.IsQuestBound || _pickables.Contains(item))
            return;

        _pickables.Add(item);
        item.SetInteractionEnabled(item.QuestId == _ctx.ActiveCollectQuestId);

        Quest quest = _ctx.CurrentQuest;
        if (quest != null
            && quest.type == QuestType.DeliverItem
            && _ctx.ActiveCollectQuestId == item.QuestId
            && quest.requiredAmount <= 0
            && item.QuestId == quest.id)
        {
            _ctx.CollectTarget = CountPickables(quest.id);
            _ctx.UpdateCounterProgress();
        }

        RefreshPointers();
    }

    void UnregisterPickable(PickableItem item)
    {
        _pickables.Remove(item);
        RefreshPointers();
    }
}
