using System.Collections.Generic;
using CityDef.Gameplay.Logic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Поведение прыжка со скалы: подход по NavMesh к краю, затем твин по дуге на точку
/// приземления. Запросы, не стартующие сразу, копятся в очереди и берутся после приземления.
/// </summary>
public sealed class AgentCliffJump
{
    enum Phase
    {
        None,
        WalkToEdge,
        Jumping,
    }

    readonly struct Request
    {
        public readonly Vector3 EdgeWorldHint;
        public readonly Vector3 LandingWorldPosition;
        public readonly Quaternion LandingRotation;
        public readonly float JumpSpeed;
        public readonly float JumpPower;
        public readonly int NumJumps;

        public Request(
            Vector3 edgeWorldHint,
            Vector3 landingWorldPosition,
            Quaternion landingRotation,
            float jumpSpeed,
            float jumpPower,
            int numJumps)
        {
            EdgeWorldHint = edgeWorldHint;
            LandingWorldPosition = landingWorldPosition;
            LandingRotation = landingRotation;
            JumpSpeed = jumpSpeed;
            JumpPower = jumpPower;
            NumJumps = numJumps;
        }
    }

    readonly Agent _owner;
    readonly AgentLocomotion _loco;
    readonly Transform _transform;
    readonly Queue<Request> _queue = new Queue<Request>();

    Phase _phase;
    Vector3 _edgePosition;
    Vector3 _landingPosition;
    Quaternion _landingRotation;
    float _jumpSpeed;
    float _jumpPower;
    int _numJumps;
    float _nextEdgeRepathTime;
    Tween _tween;

    public AgentCliffJump(Agent owner)
    {
        _owner = owner;
        _loco = owner.Loco;
        _transform = owner.transform;
    }

    public bool IsActive => _phase != Phase.None || _queue.Count > 0;

    public void Begin(
        Vector3 edgeWorldHint,
        Vector3 landingWorldPosition,
        Quaternion landingRotation,
        float jumpSpeed,
        float jumpPower,
        int numJumps)
    {
        var request = new Request(
            edgeWorldHint,
            landingWorldPosition,
            landingRotation,
            jumpSpeed,
            jumpPower,
            numJumps);

        if (_phase != Phase.None)
        {
            _queue.Enqueue(request);
            return;
        }

        // Не ставим в очередь при провале: путь проверяется только с текущей позиции;
        // следующий обрыв добавят при пересечении зоны.
        TryStartFromRequest(request);
    }

    public void Tick()
    {
        if (_phase == Phase.Jumping)
        {
            _loco.PlayIdle();
            return;
        }

        if (!_loco.IsOnNavMesh)
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump прерван — агент сошёл с NavMesh во время подхода.", _owner);
            StopWalkState();
            return;
        }

        if (Time.time >= _nextEdgeRepathTime)
        {
            _nextEdgeRepathTime = Time.time + _loco.RepathInterval;
            if (_loco.NeedsRepath)
                _loco.SetDestinationRaw(_edgePosition);
        }

        _loco.PlayLocomotionByVelocity();

        if (_loco.HasReachedDestination())
            StartJumpTween();
    }

    /// <summary> Отмена извне (посадка в лифт, восстановление сейва, отключение объекта). </summary>
    public void Cancel()
    {
        _tween?.Kill();
        _tween = null;
        if (_phase == Phase.Jumping)
            _loco.SetAgentEnabled(true);
        _phase = Phase.None;
        _queue.Clear();
    }

    public void KillTweens()
    {
        _tween?.Kill();
        _tween = null;
    }

    bool TryStartFromRequest(Request request)
    {
        if (!_loco.IsOnNavMesh)
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump не начат — агент не на NavMesh.", _owner);
            return false;
        }

        if (!NavMesh.SamplePosition(request.EdgeWorldHint, out NavMeshHit edgeHit, _loco.NavMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump не начат — Edge не удалось найти на NavMesh.", _owner);
            return false;
        }

        Vector3 sampledEdge = edgeHit.position;

        if (!_loco.HasCompletePath(sampledEdge))
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump не начат — до Edge нет полного NavMesh-пути.", _owner);
            return false;
        }

        _owner.StopCombatAndFollow();

        _landingPosition = request.LandingWorldPosition;
        _landingRotation = request.LandingRotation;
        _jumpSpeed = Mathf.Max(0.01f, request.JumpSpeed);
        _jumpPower = Mathf.Max(0f, request.JumpPower);
        _numJumps = Mathf.Max(1, request.NumJumps);

        _edgePosition = sampledEdge;
        _loco.SetDestinationRaw(_edgePosition);
        _nextEdgeRepathTime = Time.time + _loco.RepathInterval;
        _phase = Phase.WalkToEdge;
        return true;
    }

    bool TryConsumeQueue()
    {
        while (_queue.Count > 0)
        {
            if (TryStartFromRequest(_queue.Dequeue()))
                return true;
        }

        return false;
    }

    void StartJumpTween()
    {
        _phase = Phase.Jumping;

        _loco.ResetPath();
        _loco.SetAgentEnabled(false);

        Vector3 faceDir = _landingPosition - _transform.position;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.0001f)
            _transform.rotation = Quaternion.LookRotation(faceDir);

        Vector3 jumpStart = _transform.position;
        float jumpDistance = Vector3.Distance(jumpStart, _landingPosition);
        float jumpDuration = Mathf.Max(0.05f, jumpDistance / _jumpSpeed);
        Vector3[] path = CliffJumpArc.BuildWaypoints(jumpStart, _landingPosition, _jumpPower, _numJumps);

        _tween?.Kill();
        _tween = _transform
            .DOPath(path, jumpDuration, PathType.CatmullRom, PathMode.Full3D, 12, null)
            .SetEase(Ease.InSine)
            .SetLink(_transform.gameObject)
            .OnComplete(FinishJumpTween);
    }

    void FinishJumpTween()
    {
        _tween = null;
        _transform.rotation = _landingRotation;

        _loco.SetAgentEnabled(true);

        if (NavMesh.SamplePosition(_landingPosition, out NavMeshHit hit, _loco.NavMeshSampleRadius, NavMesh.AllAreas))
            _loco.Warp(hit.position);
        else
            _loco.Warp(_landingPosition);

        _phase = Phase.None;
        if (!TryConsumeQueue())
            _owner.StartFollowingPlayer();
    }

    void StopWalkState()
    {
        _loco.ResetPath();
        _phase = Phase.None;
        _queue.Clear();
        _loco.PlayIdle();
    }
}
