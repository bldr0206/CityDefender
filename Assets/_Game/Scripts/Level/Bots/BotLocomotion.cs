using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Общий контекст движения бота: обёртка над NavMeshAgent и аниматором плюс
/// текущая цель на земле и такт перепрокладки пути. Разделяется поведениями
/// (follow, breakable-attack, lift, cliff), чтобы низкоуровневая навигация не дублировалась.
/// </summary>
public sealed class BotLocomotion
{
    readonly NavMeshAgent _agent;
    readonly BotAnimator _animator;
    readonly Transform _transform;
    readonly NavMeshPath _path;
    readonly float _navMeshSampleRadius;
    readonly float _occupiedCheckRadius;
    readonly float _repathInterval;

    float _nextRepathTime;

    public BotLocomotion(
        NavMeshAgent agent,
        BotAnimator animator,
        Transform transform,
        float navMeshSampleRadius,
        float occupiedCheckRadius,
        float repathInterval)
    {
        _agent = agent;
        _animator = animator;
        _transform = transform;
        _path = new NavMeshPath();
        _navMeshSampleRadius = navMeshSampleRadius;
        _occupiedCheckRadius = occupiedCheckRadius;
        _repathInterval = repathInterval;
    }

    /// <summary> Есть активная цель наземного следования (follow / подход к breakable). </summary>
    public bool HasDestination { get; set; }

    public bool IsOnNavMesh => _agent.isOnNavMesh;
    public Vector3 Velocity => _agent.velocity;
    public Vector3 Position => _transform.position;
    public Vector3 Forward => _transform.forward;
    public float NavMeshSampleRadius => _navMeshSampleRadius;
    public float OccupiedCheckRadius => _occupiedCheckRadius;
    public float RepathInterval => _repathInterval;

    /// <summary> Путь неполон или отсутствует — пора перепроложить. </summary>
    public bool NeedsRepath => !_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete;

    public void SetBotEnabled(bool value) => _agent.enabled = value;
    public void ResetPath() => _agent.ResetPath();
    public void Warp(Vector3 position) => _agent.Warp(position);

    /// <summary> Прямая установка цели без пометки наземной цели (подход лифта/скалы). </summary>
    public void SetDestinationRaw(Vector3 position) => _agent.SetDestination(position);

    public bool HasReachedDestination() =>
        !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + _occupiedCheckRadius;

    public bool HasCompletePath(Vector3 point) =>
        _agent.CalculatePath(point, _path) && _path.status == NavMeshPathStatus.PathComplete;

    // ---- такт перепрокладки для наземного следования ----
    public bool RepathReady => Time.time >= _nextRepathTime;
    public void ScheduleRepath() => _nextRepathTime = Time.time + _repathInterval;
    public void ResetRepathClock() => _nextRepathTime = 0f;

    /// <summary> Установить наземную цель и пометить её активной. </summary>
    public void SetGroundDestination(Vector3 position)
    {
        _agent.SetDestination(position);
        HasDestination = true;
    }

    /// <summary> Сбросить наземную цель (если была) и остановить движение. </summary>
    public void ClearGroundDestination()
    {
        if (!HasDestination) return;
        _agent.ResetPath();
        HasDestination = false;
    }

    /// <summary> Сэмплировать точку на NavMesh с полным путём и назначить её наземной целью. </summary>
    public bool TrySampleGroundDestination(Vector3 point, float sampleRadius)
    {
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas) ||
            !HasCompletePath(hit.position))
            return false;

        _agent.SetDestination(hit.position);
        HasDestination = true;
        return true;
    }

    public void FacePoint(Vector3 point)
    {
        Vector3 direction = point - _transform.position;
        direction.y = 0f;
        if (direction == Vector3.zero) return;

        _transform.rotation = Quaternion.LookRotation(direction);
    }

    public float FlatDistanceTo(Vector3 point)
    {
        Vector3 offset = point - _transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    public void PlayIdle() => _animator.PlayIdleAnimation();
    public void PlayRun() => _animator.PlayRunAnimation();

    public void PlayLocomotionByVelocity()
    {
        if (_agent.velocity.sqrMagnitude > 0.01f)
            _animator.PlayRunAnimation();
        else
            _animator.PlayIdleAnimation();
    }
}
