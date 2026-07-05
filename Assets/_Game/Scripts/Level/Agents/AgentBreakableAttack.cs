using UnityEngine;

/// <summary>
/// Поведение «добить breakable»: подходит к цели из <see cref="BreakableTrigger"/> на дистанцию
/// удара, разворачивается и запускает молот; урон наносится по событию удара молота.
/// </summary>
public sealed class AgentBreakableAttack
{
    readonly Agent _ctx;
    readonly AgentLocomotion _loco;

    bool _hasTarget;
    Transform _lastTarget;
    Breakable _target;

    public AgentBreakableAttack(Agent ctx)
    {
        _ctx = ctx;
        _loco = ctx.Loco;
    }

    /// <summary> Событие удара молота — наносит урон текущей цели. </summary>
    public void OnHammerHit()
    {
        if (_target == null || _target.IsBroken)
        {
            Stop();
            return;
        }

        _target.TakeDamage(_ctx.BreakableDamage);
    }

    /// <summary> Обработать атаку в этом кадре. Возвращает true, если поведение активно. </summary>
    public bool Tick()
    {
        Transform target = _ctx.BreakableTrigger.Target;
        if (target == null || !target.TryGetComponent(out Breakable breakable) || breakable.IsBroken)
        {
            Stop();
            return false;
        }

        if (!_hasTarget || _lastTarget != target)
        {
            _hasTarget = true;
            _lastTarget = target;
            _target = breakable;
            _loco.ResetRepathClock();
            _loco.ClearGroundDestination();
        }

        Vector3 hitPoint = GetHitPoint(target);
        if (IsReadyToHit(hitPoint))
        {
            _loco.ResetPath();
            _loco.HasDestination = false;
            _loco.FacePoint(hitPoint);
            _loco.PlayIdle();
            _ctx.Hammer.PlayHitAnimation();
            return true;
        }

        _ctx.Hammer.StopHitAnimation();
        TrySetDestination(hitPoint);
        _loco.PlayLocomotionByVelocity();
        return true;
    }

    /// <summary> Молот доигрывает удар — стоять на месте и ждать. </summary>
    public void WaitForHammer()
    {
        _loco.ResetPath();
        _loco.HasDestination = false;
        _loco.PlayIdle();
    }

    public void Stop()
    {
        if (!_hasTarget)
            return;

        _ctx.Hammer.StopHitAnimation();
        _loco.ResetPath();
        _loco.HasDestination = false;
        _hasTarget = false;
        _lastTarget = null;
        _target = null;
    }

    bool IsReadyToHit(Vector3 hitPoint)
    {
        return _loco.FlatDistanceTo(hitPoint) <= _ctx.BreakableAttackDistance ||
               (_loco.HasDestination && _loco.HasReachedDestination());
    }

    Vector3 GetHitPoint(Transform target)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        return targetCollider != null ? targetCollider.ClosestPoint(_loco.Position) : target.position;
    }

    void TrySetDestination(Vector3 targetPosition)
    {
        if (!_loco.RepathReady)
            return;

        _loco.ScheduleRepath();
        Vector3 direction = _loco.Position - targetPosition;
        direction.y = 0f;
        if (direction == Vector3.zero)
            direction = -_loco.Forward;

        if (_loco.TrySampleGroundDestination(
                targetPosition + direction.normalized * _ctx.BreakableAttackDistance, _ctx.BreakableAttackDistance))
            return;

        for (int i = 0; i < _ctx.SampleAttempts; i++)
        {
            float angle = Mathf.PI * 2f * i / _ctx.SampleAttempts;
            Vector3 point = targetPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _ctx.BreakableAttackDistance;
            if (_loco.TrySampleGroundDestination(point, _loco.NavMeshSampleRadius))
                return;
        }
    }
}
