using UnityEngine;

/// <summary>
/// Квест «дойти до точки»: ставит наземный маркер цели и завершается по событию входа в зону.
/// </summary>
public sealed class QuestReachPointHandler
{
    readonly QuestManager _ctx;

    public QuestReachPointHandler(QuestManager ctx)
    {
        _ctx = ctx;
    }

    public void Enable() => Actions.OnQuestDestinationReached += CompleteReachPoint;
    public void Disable() => Actions.OnQuestDestinationReached -= CompleteReachPoint;

    public void Run(Quest quest)
    {
        _ctx.Markers.ClearWorldPointers();
        _ctx.Markers.DestroyDestinationMarker();
        _ctx.SetActiveBreakableDamage(null);

        GameObject markerPrefab = _ctx.QuestConfig != null ? _ctx.QuestConfig.QuestDestinationMarkerPrefab : null;
        if (markerPrefab == null || quest.targetPoint == null)
        {
            Debug.LogError("QuestLevelConfig, префаб маркера цели или targetPoint не задан.", _ctx);
            return;
        }

        GameObject marker = _ctx.Markers.SpawnDestinationMarker(markerPrefab, quest.targetPoint.position, quest.targetPoint.rotation);
        marker.GetComponent<QuestDestinationMarker>().Init(quest.id);
        _ctx.Markers.ShowReachPoint(marker.transform);
    }

    void CompleteReachPoint(string questId)
    {
        if (!_ctx.IsCurrentQuest(QuestType.ReachPoint, questId))
            return;

        _ctx.CompleteCurrentQuest();
    }
}
