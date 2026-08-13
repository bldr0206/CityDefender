using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Поведение посадки/поездки/высадки на лифт: подход по NavMesh к точке у кабины,
/// твин в кресло (мир→локаль), поездка пассажиром и твин высадки на этаж.
/// Прерывания и координацию с боем/следованием делает <see cref="Bot"/>.
/// </summary>
public sealed class BotLiftBoarding
{
    enum Phase
    {
        None,
        WalkToApproach,
        TweenToSeatWorld,
        TweenToSeatLocal,
        Riding,
        TweenExit,
    }

    readonly Bot _owner;
    readonly BotLocomotion _loco;
    readonly Transform _transform;

    Phase _phase;
    Lift _lift;
    Transform _seatPoint;
    Vector3 _approachPosition;
    float _nextApproachRepathTime;
    float _seatBoardTweenDuration;
    Tween _tween;

    public BotLiftBoarding(Bot owner)
    {
        _owner = owner;
        _loco = owner.Loco;
        _transform = owner.transform;
    }

    /// <summary> Уже сидит в кабине и едет с платформой. </summary>
    public bool IsPassenger => _phase == Phase.Riding;

    /// <summary> Подход, твины посадки/высадки или едет в кабине. </summary>
    public bool IsBusy => _phase != Phase.None;

    public bool IsSameLift(Lift lift) => _lift == lift;

    public bool IsBoardingThisLift(Lift lift) => _lift == lift && _phase != Phase.None;

    public bool IsRidingOn(Lift lift) => lift != null && _lift == lift && _phase == Phase.Riding;

    /// <summary> Начать подход к лифту. Прерывание других поведений уже сделал <see cref="Bot"/>. </summary>
    public void Begin(Lift lift, Vector3 approachWorldHint, Transform seatPoint, float seatBoardDuration)
    {
        if (!NavMesh.SamplePosition(approachWorldHint, out NavMeshHit approachHit, _loco.NavMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{nameof(Bot)}: подход к лифту — точка не на NavMesh.", _owner);
            return;
        }

        Vector3 sampled = approachHit.position;

        if (!_loco.HasCompletePath(sampled))
        {
            Debug.LogWarning($"{nameof(Bot)}: подход к лифту — нет полного NavMesh-пути.", _owner);
            return;
        }

        _lift = lift;
        _seatPoint = seatPoint;
        _seatBoardTweenDuration = Mathf.Max(0.05f, seatBoardDuration);
        _approachPosition = sampled;
        _nextApproachRepathTime = Time.time + _loco.RepathInterval;
        _loco.SetDestinationRaw(_approachPosition);
        _phase = Phase.WalkToApproach;
    }

    public void Tick()
    {
        switch (_phase)
        {
            case Phase.WalkToApproach:
                UpdateWalkToApproach();
                break;
            case Phase.TweenToSeatWorld:
            case Phase.TweenToSeatLocal:
            case Phase.TweenExit:
                _loco.PlayIdle();
                break;
        }
    }

    public void NotifyDeparting(Lift lift)
    {
        if (_lift != lift)
            return;

        switch (_phase)
        {
            case Phase.WalkToApproach:
                CancelInternal();
                break;
            case Phase.TweenToSeatWorld:
                OnDepartedDuringSeatTween();
                break;
        }
    }

    public void CancelIfPendingFor(Lift lift)
    {
        if (_lift != lift)
            return;
        if (_phase == Phase.Riding || _phase == Phase.TweenExit)
            return;

        CancelInternal();
    }

    /// <summary>
    /// Игрок вышел из кабины без поездки: отменить незаконченную посадку или высадить сидящего на указанную точку этажа.
    /// </summary>
    public void ForceExitWithoutRide(Lift lift, Transform exitPoint, float duration)
    {
        if (_lift != lift)
            return;

        switch (_phase)
        {
            case Phase.Riding:
                BeginExit(lift, exitPoint, duration);
                break;
            case Phase.TweenExit:
            case Phase.None:
                break;
            default:
                CancelInternal();
                break;
        }
    }

    public void CancelInternal()
    {
        _tween?.Kill();
        _tween = null;

        if (_phase == Phase.None)
            return;

        _transform.SetParent(null, true);

        _loco.SetBotEnabled(true);

        if (_loco.IsOnNavMesh)
            _loco.ResetPath();

        _loco.HasDestination = false;

        _lift = null;
        _seatPoint = null;
        _phase = Phase.None;

        if (_loco.IsOnNavMesh)
            _owner.StartFollowingPlayer();
        else
            _loco.PlayIdle();
    }

    public void BeginExit(Lift lift, Transform exitPoint, float duration)
    {
        if (exitPoint == null)
        {
            CancelInternal();
            return;
        }

        _tween?.Kill();
        _phase = Phase.TweenExit;
        _lift = lift;

        _transform.SetParent(null, true);
        _loco.SetBotEnabled(false);

        float d = Mathf.Max(0.05f, duration);
        _tween = DOTween.Sequence()
            .Append(_transform.DOMove(exitPoint.position, d).SetEase(Ease.InOutQuad))
            .Join(_transform.DORotateQuaternion(exitPoint.rotation, d))
            .SetLink(_transform.gameObject)
            .OnComplete(FinishExitTween);
    }

    public void OnDisableCleanup()
    {
        _tween?.Kill();
        _tween = null;
        if (_phase != Phase.None && _phase != Phase.Riding)
            CancelInternal();
    }

    public void KillTweens()
    {
        _tween?.Kill();
        _tween = null;
    }

    void UpdateWalkToApproach()
    {
        if (!_loco.IsOnNavMesh)
        {
            CancelInternal();
            return;
        }

        if (Time.time >= _nextApproachRepathTime)
        {
            _nextApproachRepathTime = Time.time + _loco.RepathInterval;
            if (_loco.NeedsRepath)
                _loco.SetDestinationRaw(_approachPosition);
        }

        _loco.PlayLocomotionByVelocity();

        if (_loco.HasReachedDestination())
            StartSeatWorldTween();
    }

    void StartSeatWorldTween()
    {
        if (_seatPoint == null)
        {
            CancelInternal();
            return;
        }

        _phase = Phase.TweenToSeatWorld;
        _loco.ResetPath();
        _loco.SetBotEnabled(false);

        Vector3 faceDir = _seatPoint.position - _transform.position;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.0001f)
            _transform.rotation = Quaternion.LookRotation(faceDir);

        Vector3 seatWorld = _seatPoint.position;
        Quaternion seatWorldRot = _seatPoint.rotation;

        _tween?.Kill();
        _tween = DOTween.Sequence()
            .Append(_transform.DOMove(seatWorld, _seatBoardTweenDuration).SetEase(Ease.InOutQuad))
            .Join(_transform.DORotateQuaternion(seatWorldRot, _seatBoardTweenDuration))
            .SetLink(_transform.gameObject)
            .OnComplete(FinishSeatWorldTween);
    }

    void FinishSeatWorldTween()
    {
        _tween = null;

        if (_seatPoint == null)
        {
            CancelInternal();
            return;
        }

        _transform.SetParent(_seatPoint, true);
        _transform.localPosition = Vector3.zero;
        _transform.localRotation = Quaternion.identity;
        CompleteSeated();
    }

    void OnDepartedDuringSeatTween()
    {
        _tween?.Kill();
        _tween = null;

        if (_seatPoint == null)
        {
            CancelInternal();
            return;
        }

        _phase = Phase.TweenToSeatLocal;
        _transform.SetParent(_seatPoint, true);

        _tween = DOTween.Sequence()
            .Append(_transform.DOLocalMove(Vector3.zero, _seatBoardTweenDuration).SetEase(Ease.InOutQuad))
            .Join(_transform.DOLocalRotateQuaternion(Quaternion.identity, _seatBoardTweenDuration))
            .SetLink(_transform.gameObject)
            .OnComplete(FinishSeatLocalTween);
    }

    void FinishSeatLocalTween()
    {
        _tween = null;
        _transform.localPosition = Vector3.zero;
        _transform.localRotation = Quaternion.identity;
        CompleteSeated();
    }

    void CompleteSeated()
    {
        _phase = Phase.Riding;
        if (_lift != null)
            _lift.RegisterPassenger(_owner);

        _loco.SetBotEnabled(false);
        _loco.PlayIdle();
    }

    void FinishExitTween()
    {
        _tween = null;
        _phase = Phase.None;
        _lift = null;
        _seatPoint = null;

        _loco.SetBotEnabled(true);

        if (NavMesh.SamplePosition(_transform.position, out NavMeshHit hit, _loco.NavMeshSampleRadius, NavMesh.AllAreas))
            _loco.Warp(hit.position);
        else
            _loco.Warp(_transform.position);

        _owner.StartFollowingPlayer();
    }
}
