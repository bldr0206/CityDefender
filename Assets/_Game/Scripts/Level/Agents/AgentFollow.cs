using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Поведение «следовать за игроком»: держится в кольце вокруг игрока, перепрокладывая
/// путь при его смещении. Наземное состояние (цель, такт) живёт в <see cref="AgentLocomotion"/>.
/// </summary>
public sealed class AgentFollow
{
    readonly Agent _ctx;
    readonly AgentLocomotion _loco;
    Vector3 _lastPlayerPosition;

    public AgentFollow(Agent ctx)
    {
        _ctx = ctx;
        _loco = ctx.Loco;
    }

    public void Start()
    {
        if (!_loco.IsOnNavMesh) return;

        _loco.ResetRepathClock();
        _loco.HasDestination = false;
        TrySetNewDestination();
    }

    public void Tick()
    {
        float distanceToPlayer = _loco.FlatDistanceTo(_ctx.Player.position);
        if (distanceToPlayer <= _ctx.FollowStopDistance)
        {
            Stop();
        }
        else if (distanceToPlayer >= _ctx.FollowStartDistance || _loco.HasDestination)
        {
            if (ShouldPickNewDestination())
                TrySetNewDestination();
        }
    }

    public void Stop() => _loco.ClearGroundDestination();

    bool ShouldPickNewDestination()
    {
        if (!_loco.HasDestination)
            return true;

        if (!_loco.RepathReady)
            return false;

        Vector3 playerOffset = _ctx.Player.position - _lastPlayerPosition;
        playerOffset.y = 0f;
        if (playerOffset.sqrMagnitude >= _ctx.PlayerRepathDistance * _ctx.PlayerRepathDistance)
            return true;

        return _loco.HasReachedDestination();
    }

    void TrySetNewDestination()
    {
        _loco.ScheduleRepath();

        for (int i = 0; i < _ctx.SampleAttempts; i++)
        {
            Vector3 point = GetRandomPointAroundPlayer();
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, _loco.NavMeshSampleRadius, NavMesh.AllAreas))
                continue;

            if (IsOccupied(hit.position) || !_loco.HasCompletePath(hit.position))
                continue;

            _loco.SetGroundDestination(hit.position);
            _lastPlayerPosition = _ctx.Player.position;
            return;
        }
    }

    Vector3 GetRandomPointAroundPlayer()
    {
        float angle = Random.value * Mathf.PI * 2f;
        float radius = Random.Range(_ctx.MinDistanceFromPlayer, _ctx.MaxDistanceFromPlayer);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return _ctx.Player.position + offset;
    }

    bool IsOccupied(Vector3 point)
    {
        return _ctx.OccupiedMask.value != 0 &&
               Physics.CheckSphere(point, _loco.OccupiedCheckRadius, _ctx.OccupiedMask, QueryTriggerInteraction.Ignore);
    }
}
