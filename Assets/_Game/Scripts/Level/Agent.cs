using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SaveId))]
public class Agent : MonoBehaviour
{
    enum CliffJumpPhase
    {
        None,
        WalkToEdge,
        Jumping,
    }

    enum LiftBoardingPhase
    {
        None,
        WalkToApproach,
        TweenToSeatWorld,
        TweenToSeatLocal,
        Riding,
        TweenExit,
    }

    readonly struct CliffJumpRequest
    {
        public readonly Vector3 EdgeWorldHint;
        public readonly Vector3 LandingWorldPosition;
        public readonly Quaternion LandingRotation;
        public readonly float JumpSpeed;
        public readonly float JumpPower;
        public readonly int NumJumps;

        public CliffJumpRequest(
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

    [SerializeField] private float followStartDistance = 4f;
    [SerializeField] private float followStopDistance = 3f;
    [SerializeField] private float minDistanceFromPlayer = 1.5f;
    [SerializeField] private float maxDistanceFromPlayer = 2.5f;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float playerRepathDistance = 1f;
    [SerializeField] private int sampleAttempts = 8;
    [SerializeField] private float navMeshSampleRadius = 1f;
    [SerializeField] private float occupiedCheckRadius = 0.35f;
    [SerializeField] private LayerMask occupiedMask;
    [SerializeField] private float breakableAttackDistance = 1.2f;
    [SerializeField] private int _breakableDamage = 25;

    [SerializeField] private AgentAnimator _agentAnimator;
    [SerializeField] private Hammer _hammer;

    private NavMeshAgent _agent;
    private Transform _player;
    private BreakableTrigger _breakableTrigger;
    private NavMeshPath _path;
    private Vector3 _lastPlayerPosition;
    private float _nextRepathTime;
    private bool _hasDestination;
    private bool _hasBreakableTarget;
    private Transform _lastBreakableTarget;
    private Breakable _breakableTarget;

    CliffJumpPhase _cliffJumpPhase;
    Vector3 _cliffJumpEdgePosition;
    Vector3 _cliffJumpLandingPosition;
    Quaternion _cliffJumpLandingRotation;
    float _cliffJumpSpeed;
    float _cliffJumpPower;
    int _cliffJumpNumJumps;
    float _nextCliffEdgeRepathTime;
    Tween _cliffJumpTween;
    readonly Queue<CliffJumpRequest> _cliffJumpQueue = new Queue<CliffJumpRequest>();

    LiftBoardingPhase _liftBoardingPhase;
    Lift _liftBoardingLift;
    Transform _liftSeatPoint;
    Vector3 _liftApproachPosition;
    float _nextLiftApproachRepathTime;
    float _seatBoardTweenDuration;
    Tween _liftTween;

    /// <summary> Уже сидит в кабине и едет с платформой. </summary>
    public bool IsLiftPassenger => _liftBoardingPhase == LiftBoardingPhase.Riding;

    /// <summary> Подход, твины посадки/высадки или едет в кабине. </summary>
    public bool IsInLiftBoardingOrRide => _liftBoardingPhase != LiftBoardingPhase.None;

    public bool IsInCliffJump =>
        _cliffJumpPhase != CliffJumpPhase.None || _cliffJumpQueue.Count > 0;

    [Inject]
    public void Construct(PlayerController player, BreakableTrigger breakableTrigger)
    {
        _player = player.transform;
        _breakableTrigger = breakableTrigger;
    }

    private void Awake()
    {
        if (!TryGetComponent<SaveId>(out _))
            gameObject.AddComponent<SaveId>();

        _agent = GetComponent<NavMeshAgent>();
        _path = new NavMeshPath();
    }

    private void OnEnable()
    {
        _hammer.OnHit += HitBreakable;
    }

    private void OnDisable()
    {
        _hammer.OnHit -= HitBreakable;
        _liftTween?.Kill();
        _liftTween = null;
        if (_liftBoardingPhase != LiftBoardingPhase.None && _liftBoardingPhase != LiftBoardingPhase.Riding)
            CancelLiftBoardingInternal();
        CancelCliffJumpDueToDisable();
    }

    void OnDestroy()
    {
        _cliffJumpTween?.Kill();
        _cliffJumpTween = null;
        _liftTween?.Kill();
        _liftTween = null;
    }

    private void Update()
    {
        if (_liftBoardingPhase == LiftBoardingPhase.Riding)
        {
            _agentAnimator.PlayIdleAnimation();
            return;
        }

        if (_liftBoardingPhase != LiftBoardingPhase.None)
        {
            UpdateLiftBoarding();
            return;
        }

        if (_cliffJumpPhase != CliffJumpPhase.None)
        {
            UpdateCliffJump();
            return;
        }

        if (!_agent.isOnNavMesh)
        {
            _hammer.StopHitAnimation();
            _agentAnimator.PlayIdleAnimation();
            return;
        }

        if (_hammer.IsPlaying)
        {
            WaitForHammer();
            return;
        }

        if (UpdateBreakableAttack())
        {
            return;
        }

        UpdateFollowing();
        UpdateAnimation();
    }

    private bool UpdateBreakableAttack()
    {
        Transform target = _breakableTrigger.Target;
        if (target == null || !target.TryGetComponent(out Breakable breakable) || breakable.IsBroken)
        {
            StopBreakableAttack();
            return false;
        }

        if (!_hasBreakableTarget || _lastBreakableTarget != target)
        {
            _hasBreakableTarget = true;
            _lastBreakableTarget = target;
            _breakableTarget = breakable;
            _nextRepathTime = 0f;
            StopFollowing();
        }

        Vector3 hitPoint = GetBreakableHitPoint(target);
        if (IsReadyToHitBreakable(hitPoint))
        {
            _agent.ResetPath();
            _hasDestination = false;
            FacePoint(hitPoint);
            _agentAnimator.PlayIdleAnimation();
            _hammer.PlayHitAnimation();
            return true;
        }

        _hammer.StopHitAnimation();
        TrySetBreakableDestination(hitPoint);
        UpdateAnimation();
        return true;
    }

    private void UpdateFollowing()
    {
        float distanceToPlayer = GetFlatDistanceToPlayer();
        if (distanceToPlayer <= followStopDistance)
        {
            StopFollowing();
        }
        else if (distanceToPlayer >= followStartDistance || _hasDestination)
        {
            if (ShouldPickNewDestination())
            {
                TrySetNewDestination();
            }
        }
    }

    public void StartFollowingPlayer()
    {
        if (!_agent.isOnNavMesh) return;

        _nextRepathTime = 0f;
        _hasDestination = false;
        TrySetNewDestination();
    }

    public AgentSaveData CaptureSaveData()
    {
        return new AgentSaveData
        {
            transform = new SaveTransformData(transform),
        };
    }

    public void RestoreSaveData(AgentSaveData data)
    {
        if (data == null || data.transform == null) return;

        CancelCliffJumpForExternalInterrupt();
        CancelLiftBoardingInternal();

        if (_agent != null)
            _agent.enabled = false;

        data.transform.ApplyTo(transform);

        if (_agent != null)
            _agent.enabled = true;

        StartFollowingPlayer();
    }

    public bool IsBoardingThisLift(Lift lift)
    {
        return _liftBoardingLift == lift && _liftBoardingPhase != LiftBoardingPhase.None;
    }

    public void BeginLiftBoarding(
        Lift lift,
        Vector3 approachWorldHint,
        Transform seatPoint,
        float seatBoardDuration)
    {
        if (lift == null || seatPoint == null)
            return;

        if (IsInLiftBoardingOrRide && _liftBoardingLift == lift)
            return;

        if (IsInLiftBoardingOrRide)
            CancelLiftBoardingInternal();

        CancelCliffJumpForExternalInterrupt();
        _hammer.StopHitAnimation();
        StopBreakableAttack();
        StopFollowing();

        if (!NavMesh.SamplePosition(approachWorldHint, out NavMeshHit approachHit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{nameof(Agent)}: подход к лифту — точка не на NavMesh.", this);
            return;
        }

        Vector3 sampled = approachHit.position;

        if (!HasCompletePath(sampled))
        {
            Debug.LogWarning($"{nameof(Agent)}: подход к лифту — нет полного NavMesh-пути.", this);
            return;
        }

        _liftBoardingLift = lift;
        _liftSeatPoint = seatPoint;
        _seatBoardTweenDuration = Mathf.Max(0.05f, seatBoardDuration);
        _liftApproachPosition = sampled;
        _nextLiftApproachRepathTime = Time.time + repathInterval;
        _agent.SetDestination(_liftApproachPosition);
        _liftBoardingPhase = LiftBoardingPhase.WalkToApproach;
    }

    public void NotifyLiftDeparting(Lift lift)
    {
        if (_liftBoardingLift != lift)
            return;

        switch (_liftBoardingPhase)
        {
            case LiftBoardingPhase.WalkToApproach:
                CancelLiftBoardingInternal();
                break;
            case LiftBoardingPhase.TweenToSeatWorld:
                OnLiftDepartedDuringSeatTween();
                break;
        }
    }

    void OnLiftDepartedDuringSeatTween()
    {
        _liftTween?.Kill();
        _liftTween = null;

        if (_liftSeatPoint == null)
        {
            CancelLiftBoardingInternal();
            return;
        }

        _liftBoardingPhase = LiftBoardingPhase.TweenToSeatLocal;
        transform.SetParent(_liftSeatPoint, true);

        Quaternion targetLocalRot = Quaternion.identity;
        _liftTween = DOTween.Sequence()
            .Append(transform.DOLocalMove(Vector3.zero, _seatBoardTweenDuration).SetEase(Ease.InOutQuad))
            .Join(transform.DOLocalRotateQuaternion(targetLocalRot, _seatBoardTweenDuration))
            .SetLink(gameObject)
            .OnComplete(FinishLiftSeatLocalTween);
    }

    void FinishLiftSeatLocalTween()
    {
        _liftTween = null;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        CompleteLiftSeated();
    }

    public void CancelLiftBoardingIfPendingFor(Lift lift)
    {
        if (_liftBoardingLift != lift)
            return;
        if (_liftBoardingPhase == LiftBoardingPhase.Riding || _liftBoardingPhase == LiftBoardingPhase.TweenExit)
            return;

        CancelLiftBoardingInternal();
    }

    public bool IsRidingOnLift(Lift lift) =>
        lift != null &&
        _liftBoardingLift == lift &&
        _liftBoardingPhase == LiftBoardingPhase.Riding;

    /// <summary>
    /// Игрок вышел из кабины без поездки: отменить незаконченную посадку или высадить сидящего на указанную точку этажа.
    /// </summary>
    public void ForceExitLiftWithoutRide(Lift lift, Transform exitPoint, float duration)
    {
        if (_liftBoardingLift != lift)
            return;

        switch (_liftBoardingPhase)
        {
            case LiftBoardingPhase.Riding:
                BeginLiftExit(lift, exitPoint, duration);
                break;
            case LiftBoardingPhase.TweenExit:
            case LiftBoardingPhase.None:
                break;
            default:
                CancelLiftBoardingInternal();
                break;
        }
    }

    public void CancelLiftBoarding()
    {
        CancelLiftBoardingInternal();
    }

    void CancelLiftBoardingInternal()
    {
        _liftTween?.Kill();
        _liftTween = null;

        if (_liftBoardingPhase != LiftBoardingPhase.None)
        {
            transform.SetParent(null, true);

            if (!_agent.enabled)
                _agent.enabled = true;

            if (_agent.isOnNavMesh)
                _agent.ResetPath();

            _hasDestination = false;

            _liftBoardingLift = null;
            _liftSeatPoint = null;
            _liftBoardingPhase = LiftBoardingPhase.None;

            if (_agent.isOnNavMesh)
                StartFollowingPlayer();
            else
                _agentAnimator.PlayIdleAnimation();
        }
    }

    void CompleteLiftSeated()
    {
        _liftBoardingPhase = LiftBoardingPhase.Riding;
        if (_liftBoardingLift != null)
            _liftBoardingLift.RegisterPassenger(this);

        if (_agent.enabled)
            _agent.enabled = false;

        _agentAnimator.PlayIdleAnimation();
    }

    public void BeginLiftExit(Lift lift, Transform exitPoint, float duration)
    {
        if (exitPoint == null)
        {
            CancelLiftBoardingInternal();
            return;
        }

        _liftTween?.Kill();
        _liftBoardingPhase = LiftBoardingPhase.TweenExit;
        _liftBoardingLift = lift;

        transform.SetParent(null, true);

        if (_agent.enabled)
            _agent.enabled = false;

        float d = Mathf.Max(0.05f, duration);
        _liftTween = DOTween.Sequence()
            .Append(transform.DOMove(exitPoint.position, d).SetEase(Ease.InOutQuad))
            .Join(transform.DORotateQuaternion(exitPoint.rotation, d))
            .SetLink(gameObject)
            .OnComplete(FinishLiftExitTween);
    }

    void FinishLiftExitTween()
    {
        _liftTween = null;
        _liftBoardingPhase = LiftBoardingPhase.None;
        _liftBoardingLift = null;
        _liftSeatPoint = null;

        if (!_agent.enabled)
            _agent.enabled = true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            _agent.Warp(hit.position);
        else
            _agent.Warp(transform.position);

        StartFollowingPlayer();
    }

    void UpdateLiftBoarding()
    {
        switch (_liftBoardingPhase)
        {
            case LiftBoardingPhase.WalkToApproach:
                UpdateWalkToLiftApproach();
                break;
            case LiftBoardingPhase.TweenToSeatWorld:
            case LiftBoardingPhase.TweenToSeatLocal:
            case LiftBoardingPhase.TweenExit:
                _agentAnimator.PlayIdleAnimation();
                break;
        }
    }

    void UpdateWalkToLiftApproach()
    {
        if (!_agent.isOnNavMesh)
        {
            CancelLiftBoardingInternal();
            return;
        }

        if (Time.time >= _nextLiftApproachRepathTime)
        {
            _nextLiftApproachRepathTime = Time.time + repathInterval;
            bool needRepath =
                !_agent.hasPath ||
                _agent.pathStatus != NavMeshPathStatus.PathComplete;
            if (needRepath)
                _agent.SetDestination(_liftApproachPosition);
        }

        if (_agent.velocity.sqrMagnitude > 0.01f)
            _agentAnimator.PlayRunAnimation();
        else
            _agentAnimator.PlayIdleAnimation();

        if (HasReachedDestination())
            StartLiftSeatWorldTween();
    }

    void StartLiftSeatWorldTween()
    {
        if (_liftSeatPoint == null)
        {
            CancelLiftBoardingInternal();
            return;
        }

        _liftBoardingPhase = LiftBoardingPhase.TweenToSeatWorld;
        _agent.ResetPath();

        if (_agent.enabled)
            _agent.enabled = false;

        Vector3 faceDir = _liftSeatPoint.position - transform.position;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(faceDir);

        Vector3 seatWorld = _liftSeatPoint.position;
        Quaternion seatWorldRot = _liftSeatPoint.rotation;

        _liftTween?.Kill();
        _liftTween = DOTween.Sequence()
            .Append(transform.DOMove(seatWorld, _seatBoardTweenDuration).SetEase(Ease.InOutQuad))
            .Join(transform.DORotateQuaternion(seatWorldRot, _seatBoardTweenDuration))
            .SetLink(gameObject)
            .OnComplete(FinishLiftSeatWorldTween);
    }

    void FinishLiftSeatWorldTween()
    {
        _liftTween = null;

        if (_liftSeatPoint == null)
        {
            CancelLiftBoardingInternal();
            return;
        }

        transform.SetParent(_liftSeatPoint, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        CompleteLiftSeated();
    }

    public void BeginCliffJump(
        Vector3 edgeWorldHint,
        Vector3 landingWorldPosition,
        Quaternion landingRotation,
        float jumpSpeed,
        float jumpPower,
        int numJumps)
    {
        if (IsInLiftBoardingOrRide)
            return;

        var request = new CliffJumpRequest(
            edgeWorldHint,
            landingWorldPosition,
            landingRotation,
            jumpSpeed,
            jumpPower,
            numJumps);

        if (_cliffJumpPhase != CliffJumpPhase.None)
        {
            _cliffJumpQueue.Enqueue(request);
            return;
        }

        if (!TryStartCliffJumpFromRequest(request))
        {
            // Не ставим в очередь: путь проверяется только с текущей позиции; следующий обрыв добавят при пересечении зоны.
        }
    }

    bool TryStartCliffJumpFromRequest(CliffJumpRequest request)
    {
        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump не начат — агент не на NavMesh.", this);
            return false;
        }

        if (!NavMesh.SamplePosition(request.EdgeWorldHint, out NavMeshHit edgeHit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump не начат — Edge не удалось найти на NavMesh.", this);
            return false;
        }

        Vector3 sampledEdge = edgeHit.position;

        if (!HasCompletePath(sampledEdge))
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump не начат — до Edge нет полного NavMesh-пути.", this);
            return false;
        }

        _hammer.StopHitAnimation();
        StopBreakableAttack();
        StopFollowing();

        _cliffJumpLandingPosition = request.LandingWorldPosition;
        _cliffJumpLandingRotation = request.LandingRotation;
        _cliffJumpSpeed = Mathf.Max(0.01f, request.JumpSpeed);
        _cliffJumpPower = Mathf.Max(0f, request.JumpPower);
        _cliffJumpNumJumps = Mathf.Max(1, request.NumJumps);

        _cliffJumpEdgePosition = sampledEdge;
        _agent.SetDestination(_cliffJumpEdgePosition);
        _nextCliffEdgeRepathTime = Time.time + repathInterval;
        _cliffJumpPhase = CliffJumpPhase.WalkToEdge;
        return true;
    }

    bool TryConsumeCliffJumpQueue()
    {
        while (_cliffJumpQueue.Count > 0)
        {
            CliffJumpRequest next = _cliffJumpQueue.Dequeue();
            if (TryStartCliffJumpFromRequest(next))
                return true;
        }

        return false;
    }

    void ClearCliffJumpQueue() => _cliffJumpQueue.Clear();

    void UpdateCliffJump()
    {
        if (_cliffJumpPhase == CliffJumpPhase.Jumping)
        {
            _agentAnimator.PlayIdleAnimation();
            return;
        }

        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"{nameof(Agent)}: Cliff jump прерван — агент сошёл с NavMesh во время подхода.", this);
            StopCliffJumpWalkState();
            return;
        }

        if (Time.time >= _nextCliffEdgeRepathTime)
        {
            _nextCliffEdgeRepathTime = Time.time + repathInterval;
            bool needRepath =
                !_agent.hasPath ||
                _agent.pathStatus != NavMeshPathStatus.PathComplete;
            if (needRepath)
                _agent.SetDestination(_cliffJumpEdgePosition);
        }

        if (_agent.velocity.sqrMagnitude > 0.01f)
            _agentAnimator.PlayRunAnimation();
        else
            _agentAnimator.PlayIdleAnimation();

        if (HasReachedDestination())
            StartCliffJumpTween();
    }

    void StartCliffJumpTween()
    {
        _cliffJumpPhase = CliffJumpPhase.Jumping;

        _agent.ResetPath();
        _agent.enabled = false;

        Vector3 faceDir = _cliffJumpLandingPosition - transform.position;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(faceDir);

        Vector3 jumpStart = transform.position;
        float jumpDistance = Vector3.Distance(jumpStart, _cliffJumpLandingPosition);
        float jumpDuration = Mathf.Max(0.05f, jumpDistance / _cliffJumpSpeed);
        Vector3[] path = BuildCliffJumpWaypoints(jumpStart, _cliffJumpLandingPosition, _cliffJumpPower, _cliffJumpNumJumps);

        _cliffJumpTween?.Kill();
        _cliffJumpTween = transform
            .DOPath(path, jumpDuration, PathType.CatmullRom, PathMode.Full3D, 12, null)
            .SetEase(Ease.InSine)
            .SetLink(gameObject)
            .OnComplete(FinishCliffJumpTween);
    }

    /// <summary>
    /// Прогресс по пути 0..1 от физического времени: быстрый отрыв, дольше у середины (зависание в воздухе), ускорение к приземлению.
    /// Формула u(t) = t + sin(2πt)/(2π), t ∈ [0,1] — монотонна, du/dt = 1 + cos(2πt).
    /// </summary>


    /// <summary>
    /// Абсолютные мировые точки пути: вершины «горбов» на хорде + промежуточные впадины на хорде (при numJumps &gt; 1).
    /// </summary>
    static Vector3[] BuildCliffJumpWaypoints(Vector3 start, Vector3 end, float jumpPower, int numJumps)
    {
        numJumps = Mathf.Max(1, numJumps);
        var list = new List<Vector3>(numJumps * 2 + 2);
        for (int j = 0; j < numJumps; j++)
        {
            float t0 = j / (float)numJumps;
            float t1 = (j + 1) / (float)numJumps;
            float tPeak = (t0 + t1) * 0.5f;
            Vector3 peak = Vector3.Lerp(start, end, tPeak);
            peak.y += jumpPower;
            list.Add(peak);
            if (j < numJumps - 1)
                list.Add(Vector3.Lerp(start, end, t1));
        }

        list.Add(end);
        return list.ToArray();
    }

    void FinishCliffJumpTween()
    {
        _cliffJumpTween = null;
        transform.rotation = _cliffJumpLandingRotation;

        if (!_agent.enabled)
            _agent.enabled = true;

        if (NavMesh.SamplePosition(_cliffJumpLandingPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            _agent.Warp(hit.position);
        else
            _agent.Warp(_cliffJumpLandingPosition);

        _cliffJumpPhase = CliffJumpPhase.None;
        if (!TryConsumeCliffJumpQueue())
            StartFollowingPlayer();
    }

    void StopCliffJumpWalkState()
    {
        _agent.ResetPath();
        _cliffJumpPhase = CliffJumpPhase.None;
        ClearCliffJumpQueue();
        _agentAnimator.PlayIdleAnimation();
    }

    void CancelCliffJumpForExternalInterrupt()
    {
        _cliffJumpTween?.Kill();
        _cliffJumpTween = null;
        if (_cliffJumpPhase == CliffJumpPhase.Jumping && _agent != null && !_agent.enabled)
            _agent.enabled = true;
        _cliffJumpPhase = CliffJumpPhase.None;
        ClearCliffJumpQueue();
    }

    void CancelCliffJumpDueToDisable()
    {
        _cliffJumpTween?.Kill();
        _cliffJumpTween = null;
        if (_cliffJumpPhase == CliffJumpPhase.Jumping && _agent != null && !_agent.enabled)
            _agent.enabled = true;
        _cliffJumpPhase = CliffJumpPhase.None;
        ClearCliffJumpQueue();
    }

    private float GetFlatDistanceToPlayer()
    {
        return GetFlatDistance(_player.position);
    }

    private float GetFlatDistance(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool ShouldPickNewDestination()
    {
        if (!_hasDestination)
        {
            return true;
        }

        if (Time.time < _nextRepathTime)
        {
            return false;
        }

        Vector3 playerOffset = _player.position - _lastPlayerPosition;
        playerOffset.y = 0f;
        if (playerOffset.sqrMagnitude >= playerRepathDistance * playerRepathDistance)
        {
            return true;
        }

        return HasReachedDestination();
    }

    private bool IsReadyToHitBreakable(Vector3 hitPoint)
    {
        return GetFlatDistance(hitPoint) <= breakableAttackDistance || (_hasDestination && HasReachedDestination());
    }

    private void TrySetNewDestination()
    {
        _nextRepathTime = Time.time + repathInterval;

        for (int i = 0; i < sampleAttempts; i++)
        {
            Vector3 point = GetRandomPointAroundPlayer();
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                continue;
            }

            if (IsOccupied(hit.position) || !HasCompletePath(hit.position))
            {
                continue;
            }

            _agent.SetDestination(hit.position);
            _lastPlayerPosition = _player.position;
            _hasDestination = true;
            return;
        }
    }

    private Vector3 GetRandomPointAroundPlayer()
    {
        float angle = Random.value * Mathf.PI * 2f;
        float radius = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return _player.position + offset;
    }

    private bool IsOccupied(Vector3 point)
    {
        return occupiedMask.value != 0 &&
               Physics.CheckSphere(point, occupiedCheckRadius, occupiedMask, QueryTriggerInteraction.Ignore);
    }

    private bool HasCompletePath(Vector3 point)
    {
        return _agent.CalculatePath(point, _path) && _path.status == NavMeshPathStatus.PathComplete;
    }

    private bool HasReachedDestination()
    {
        return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + occupiedCheckRadius;
    }

    private void WaitForHammer()
    {
        _agent.ResetPath();
        _hasDestination = false;
        _agentAnimator.PlayIdleAnimation();
    }

    private void HitBreakable()
    {
        if (_breakableTarget == null || _breakableTarget.IsBroken)
        {
            StopBreakableAttack();
            return;
        }

        _breakableTarget.TakeDamage(_breakableDamage);
    }

    private Vector3 GetBreakableHitPoint(Transform target)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        return targetCollider != null ? targetCollider.ClosestPoint(transform.position) : target.position;
    }

    private void TrySetBreakableDestination(Vector3 targetPosition)
    {
        if (Time.time < _nextRepathTime)
        {
            return;
        }

        _nextRepathTime = Time.time + repathInterval;
        Vector3 direction = transform.position - targetPosition;
        direction.y = 0f;
        if (direction == Vector3.zero)
        {
            direction = -transform.forward;
        }

        if (TrySetDestinationNear(targetPosition + direction.normalized * breakableAttackDistance, breakableAttackDistance))
        {
            return;
        }

        for (int i = 0; i < sampleAttempts; i++)
        {
            float angle = Mathf.PI * 2f * i / sampleAttempts;
            Vector3 point = targetPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * breakableAttackDistance;
            if (TrySetDestinationNear(point, navMeshSampleRadius))
            {
                return;
            }
        }
    }

    private bool TrySetDestinationNear(Vector3 point, float sampleRadius)
    {
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas) || !HasCompletePath(hit.position))
        {
            return false;
        }

        _agent.SetDestination(hit.position);
        _hasDestination = true;
        return true;
    }

    private void FacePoint(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void StopFollowing()
    {
        if (_hasDestination)
        {
            _agent.ResetPath();
            _hasDestination = false;
        }
    }

    private void StopBreakableAttack()
    {
        if (!_hasBreakableTarget)
        {
            return;
        }

        _hammer.StopHitAnimation();
        _agent.ResetPath();
        _hasDestination = false;
        _hasBreakableTarget = false;
        _lastBreakableTarget = null;
        _breakableTarget = null;
    }

    private void UpdateAnimation()
    {
        if (_agent.velocity.sqrMagnitude > 0.01f)
        {
            _agentAnimator.PlayRunAnimation();
        }
        else
        {
            _agentAnimator.PlayIdleAnimation();
        }
    }
}
