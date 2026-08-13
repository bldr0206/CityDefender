using CityDef.Gameplay.Logic;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

/// <summary>
/// NPC-помощник. Диспетчер поведений (follow / breakable-attack / lift / cliff) и фасад
/// для сейва. Каждое поведение живёт отдельным классом и работает через общий
/// контекст <see cref="BotLocomotion"/>; здесь — переключение фаз и координация прерываний.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SaveId))]
public class Bot : MonoBehaviour
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
    [SerializeField] private BotSpecializationConfig _specialization;

    [SerializeField] private BotAnimator _botAnimator;
    [SerializeField] private Hammer _hammer;

    private NavMeshAgent _agent;
    private Transform _player;
    private BreakableTrigger _breakableTrigger;
    private BotStats _stats;

    private BotLocomotion _loco;
    private BotFollow _follow;
    private BotBreakableAttack _breakable;
    private BotLiftBoarding _lift;
    private BotCliffJump _cliff;

    // ---- контекст, доступный поведениям (тюнинг остаётся [SerializeField] на префабе) ----
    internal BotLocomotion Loco => _loco;
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
    internal int BreakableDamage => Mathf.RoundToInt(_stats.GetValue(BotStatType.Mining));
    internal float MiningSpeed => _stats.GetValue(BotStatType.MiningSpeed);

    /// <summary> Характеристики бота (уровни поверх специализации). </summary>
    public BotStats Stats => _stats;

    /// <summary> Конфиг специализации бота (имя для UI, описания характеристик). </summary>
    public BotSpecializationConfig Specialization => _specialization;

    // ---- состояние, которое опрашивают Lift / BotCliffJumpZone / TraderNPC ----
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

        if (_specialization == null)
            Debug.LogWarning($"Bot '{name}': specialization is not set, all stats fall back to zero.", this);
        _stats = new BotStats(_specialization != null ? _specialization.Stats : null);

        _agent = GetComponent<NavMeshAgent>();
        _loco = new BotLocomotion(_agent, _botAnimator, transform, navMeshSampleRadius, occupiedCheckRadius, repathInterval);
        _follow = new BotFollow(this);
        _breakable = new BotBreakableAttack(this);
        _lift = new BotLiftBoarding(this);
        _cliff = new BotCliffJump(this);
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
    /// <summary> Бот выставлен на продажу: стоит у торговца, пока его не купят (не следует, не сохраняется). </summary>
    public bool IsForSale { get; private set; }

    /// <summary> Поставить бота на витрину торговца. </summary>
    public void PutUpForSale()
    {
        IsForSale = true;
        _loco.PlayIdle();
    }

    /// <summary> Покупка: бот переходит под контроль игрока и начинает следовать. </summary>
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

    // ---- прыжок со скалы (фасад для BotCliffJumpZone.cs) ----
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
    public BotSaveData CaptureSaveData()
    {
        return new BotSaveData
        {
            transform = new SaveTransformData(transform),
            specializationId = _specialization != null ? _specialization.Id : null,
            statLevels = _stats.CaptureSaveData(),
        };
    }

    public void RestoreSaveData(BotSaveData data)
    {
        if (data == null) return;

        _stats.RestoreSaveData(data.statLevels);
        if (data.transform == null) return;

        _cliff.Cancel();
        _lift.CancelInternal();

        _loco.SetBotEnabled(false);
        data.transform.ApplyTo(transform);
        _loco.SetBotEnabled(true);

        StartFollowingPlayer();
    }
}
