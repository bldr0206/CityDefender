using UnityEngine;
using UnityEngine.AI;
using Zenject;

/// <summary>
/// NPC-помощник. Диспетчер поведений (follow / breakable-attack / lift / cliff) и фасад
/// для сейва. Каждое поведение живёт отдельным классом и работает через общий
/// контекст <see cref="AgentLocomotion"/>; здесь — переключение фаз и координация прерываний.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SaveId))]
public class Agent : MonoBehaviour
{
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

    private AgentLocomotion _loco;
    private AgentFollow _follow;
    private AgentBreakableAttack _breakable;
    private AgentLiftBoarding _lift;
    private AgentCliffJump _cliff;

    // ---- контекст, доступный поведениям (тюнинг остаётся [SerializeField] на префабе) ----
    internal AgentLocomotion Loco => _loco;
    internal Transform Player => _player;
    internal BreakableTrigger BreakableTrigger => _breakableTrigger;
    internal Hammer Hammer => _hammer;
    internal float FollowStartDistance => followStartDistance;
    internal float FollowStopDistance => followStopDistance;
    internal float MinDistanceFromPlayer => minDistanceFromPlayer;
    internal float MaxDistanceFromPlayer => maxDistanceFromPlayer;
    internal float PlayerRepathDistance => playerRepathDistance;
    internal int SampleAttempts => sampleAttempts;
    internal LayerMask OccupiedMask => occupiedMask;
    internal float BreakableAttackDistance => breakableAttackDistance;
    internal int BreakableDamage => _breakableDamage;

    // ---- состояние, которое опрашивают Lift / AgentCliffJumpZone / TraderNPC ----
    public bool IsLiftPassenger => _lift.IsPassenger;
    public bool IsInLiftBoardingOrRide => _lift.IsBusy;
    public bool IsInCliffJump => _cliff.IsActive;

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
        _loco = new AgentLocomotion(_agent, _agentAnimator, transform, navMeshSampleRadius, occupiedCheckRadius, repathInterval);
        _follow = new AgentFollow(this);
        _breakable = new AgentBreakableAttack(this);
        _lift = new AgentLiftBoarding(this);
        _cliff = new AgentCliffJump(this);
    }

    private void OnEnable()
    {
        _hammer.OnHit += _breakable.OnHammerHit;
    }

    private void OnDisable()
    {
        _hammer.OnHit -= _breakable.OnHammerHit;
        _lift.OnDisableCleanup();
        _cliff.Cancel();
    }

    private void OnDestroy()
    {
        _lift.KillTweens();
        _cliff.KillTweens();
    }

    private void Update()
    {
        if (IsForSale)
        {
            _loco.PlayIdle();
            return;
        }

        if (_lift.IsPassenger)
        {
            _loco.PlayIdle();
            return;
        }

        if (_lift.IsBusy)
        {
            _lift.Tick();
            return;
        }

        if (_cliff.IsActive)
        {
            _cliff.Tick();
            return;
        }

        if (!_agent.isOnNavMesh)
        {
            _hammer.StopHitAnimation();
            _loco.PlayIdle();
            return;
        }

        if (_hammer.IsPlaying)
        {
            _breakable.WaitForHammer();
            return;
        }

        if (_breakable.Tick())
            return;

        _follow.Tick();
        _loco.PlayLocomotionByVelocity();
    }

    // ---- follow ----
    public void StartFollowingPlayer() => _follow.Start();

    // ---- витрина у торговца ----
    /// <summary> Агент выставлен на продажу: стоит у торговца, пока его не купят (не следует, не сохраняется). </summary>
    public bool IsForSale { get; private set; }

    /// <summary> Поставить агента на витрину торговца. </summary>
    public void PutUpForSale()
    {
        IsForSale = true;
        _loco.PlayIdle();
    }

    /// <summary> Покупка: агент переходит под контроль игрока и начинает следовать. </summary>
    public void SellToPlayer()
    {
        IsForSale = false;
        StartFollowingPlayer();
    }

    /// <summary> Остановить бой и следование перед посадкой/прыжком. </summary>
    internal void StopCombatAndFollow()
    {
        _hammer.StopHitAnimation();
        _breakable.Stop();
        _loco.ClearGroundDestination();
    }

    // ---- лифт (фасад для Lift.cs) ----
    public bool IsBoardingThisLift(Lift lift) => _lift.IsBoardingThisLift(lift);

    public void BeginLiftBoarding(Lift lift, Vector3 approachWorldHint, Transform seatPoint, float seatBoardDuration)
    {
        if (lift == null || seatPoint == null)
            return;

        if (_lift.IsBusy && _lift.IsSameLift(lift))
            return;

        if (_lift.IsBusy)
            _lift.CancelInternal();

        _cliff.Cancel();
        StopCombatAndFollow();
        _lift.Begin(lift, approachWorldHint, seatPoint, seatBoardDuration);
    }

    public void NotifyLiftDeparting(Lift lift) => _lift.NotifyDeparting(lift);
    public void CancelLiftBoardingIfPendingFor(Lift lift) => _lift.CancelIfPendingFor(lift);
    public bool IsRidingOnLift(Lift lift) => _lift.IsRidingOn(lift);
    public void ForceExitLiftWithoutRide(Lift lift, Transform exitPoint, float duration) => _lift.ForceExitWithoutRide(lift, exitPoint, duration);
    public void CancelLiftBoarding() => _lift.CancelInternal();
    public void BeginLiftExit(Lift lift, Transform exitPoint, float duration) => _lift.BeginExit(lift, exitPoint, duration);

    // ---- прыжок со скалы (фасад для AgentCliffJumpZone.cs) ----
    public void BeginCliffJump(
        Vector3 edgeWorldHint,
        Vector3 landingWorldPosition,
        Quaternion landingRotation,
        float jumpSpeed,
        float jumpPower,
        int numJumps)
    {
        if (_lift.IsBusy)
            return;

        _cliff.Begin(edgeWorldHint, landingWorldPosition, landingRotation, jumpSpeed, jumpPower, numJumps);
    }

    // ---- сейв ----
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

        _cliff.Cancel();
        _lift.CancelInternal();

        _loco.SetAgentEnabled(false);
        data.transform.ApplyTo(transform);
        _loco.SetAgentEnabled(true);

        StartFollowingPlayer();
    }
}
