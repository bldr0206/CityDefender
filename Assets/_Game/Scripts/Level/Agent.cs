using UnityEngine;
using UnityEngine.AI;
using Zenject;

[RequireComponent(typeof(NavMeshAgent))]
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

    [SerializeField] private AgentAnimator _agentAnimator;

    private NavMeshAgent _agent;
    private Transform _player;
    private NavMeshPath _path;
    private Vector3 _lastPlayerPosition;
    private float _nextRepathTime;
    private bool _hasDestination;

    [Inject]
    public void Construct(PlayerController player)
    {
        _player = player.transform;
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _path = new NavMeshPath();
    }

    private void Update()
    {
        if (!_agent.isOnNavMesh)
        {
            _agentAnimator.PlayIdleAnimation();
            return;
        }

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

        UpdateAnimation();
    }

    private float GetFlatDistanceToPlayer()
    {
        Vector3 offset = _player.position - transform.position;
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

        return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + occupiedCheckRadius;
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

    private void StopFollowing()
    {
        if (_hasDestination)
        {
            _agent.ResetPath();
            _hasDestination = false;
        }
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
