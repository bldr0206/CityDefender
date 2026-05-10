using UnityEngine;
using UnityEngine.AI;
using Zenject;

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
    private NavMeshPath _path;
    private Vector3 _lastPlayerPosition;
    private float _nextRepathTime;
    private bool _hasDestination;
    private bool _hasBreakableTarget;
    private Transform _lastBreakableTarget;
    private Breakable _breakableTarget;
    private bool _isLiftPassenger;

    public bool IsLiftPassenger => _isLiftPassenger;

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
    }

    private void Update()
    {
        if (_isLiftPassenger)
        {
            _agentAnimator.PlayIdleAnimation();
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

        if (_agent != null)
            _agent.enabled = false;

        data.transform.ApplyTo(transform);

        if (_agent != null)
            _agent.enabled = true;

        StartFollowingPlayer();
    }

    public void EnterLift(Transform seatPoint)
    {
        _isLiftPassenger = true;
        _hammer.StopHitAnimation();
        StopBreakableAttack();
        StopFollowing();

        if (_agent.enabled)
            _agent.enabled = false;

        transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
        transform.SetParent(seatPoint, true);
        _agentAnimator.PlayIdleAnimation();
    }

    public void ExitLift(Transform exitPoint)
    {
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);

        if (!_agent.enabled)
            _agent.enabled = true;

        if (NavMesh.SamplePosition(exitPoint.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            _agent.Warp(hit.position);

        _isLiftPassenger = false;
        StartFollowingPlayer();
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
