using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SaveId))]
public class Lift : MonoBehaviour
{
    [SerializeField] private Rigidbody platformRigidbody;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform[] passengerSeatPoints;
    [SerializeField] private Transform[] topExitPoints;
    [SerializeField] private Transform[] bottomExitPoints;
    [SerializeField] private float moveDuration = 2f;

    readonly List<Agent> _passengers = new();
    Tween _moveTween;
    bool _isMoving;
    bool _isAtTop;
    SaveId _saveId;

    public string SaveId => GetSaveId().Id;

    void Awake()
    {
        GetSaveId();
        if (platformRigidbody == null)
        {
            Debug.LogError($"{nameof(Lift)}: Platform Rigidbody is not assigned.", this);
            enabled = false;
            return;
        }
        if (bottomPoint == null || topPoint == null)
        {
            Debug.LogError($"{nameof(Lift)}: Bottom/Top points are not assigned.", this);
            enabled = false;
            return;
        }

        _isAtTop = ComputeIsAtTopFromPlatformPosition();
    }

    public void MoveUp()
    {
        if (_isMoving) return;
        if (_isAtTop) return;

        MoveTo(topPoint, topExitPoints, true);
    }

    public void MoveDown()
    {
        if (_isMoving) return;
        if (!_isAtTop) return;

        MoveTo(bottomPoint, bottomExitPoints, false);
    }

    void MoveTo(Transform targetPoint, Transform[] exitPoints, bool isAtTop)
    {
        BoardPassengers();
        _isMoving = true;
        _moveTween?.Kill();
        _moveTween = platformRigidbody.DOMove(targetPoint.position, moveDuration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                _isMoving = false;
                _isAtTop = isAtTop;
                ExitPassengers(exitPoints, targetPoint);
            });
    }

    public bool IsMoving() => _isMoving;

    public bool IsAtTop() => _isAtTop;

    public LiftSaveData CaptureSaveData()
    {
        return new LiftSaveData
        {
            id = SaveId,
            isAtTop = ComputeIsAtTopFromPlatformPosition(),
        };
    }

    public void RestoreSaveData(LiftSaveData data)
    {
        if (data == null || platformRigidbody == null || bottomPoint == null || topPoint == null) return;

        _moveTween?.Kill();
        _moveTween = null;
        _isMoving = false;
        _passengers.Clear();

        Transform targetPoint = data.isAtTop ? topPoint : bottomPoint;
        platformRigidbody.position = targetPoint.position;
        platformRigidbody.rotation = targetPoint.rotation;
        _isAtTop = data.isAtTop;
        Physics.SyncTransforms();
    }

    bool ComputeIsAtTopFromPlatformPosition()
    {
        if (platformRigidbody == null || bottomPoint == null || topPoint == null) return false;

        float toTop = Vector3.SqrMagnitude(platformRigidbody.position - topPoint.position);
        float toBottom = Vector3.SqrMagnitude(platformRigidbody.position - bottomPoint.position);
        return toTop < toBottom;
    }

    void BoardPassengers()
    {
        _passengers.Clear();
        if (passengerSeatPoints == null || passengerSeatPoints.Length == 0) return;

        List<Agent> agents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < agents.Count && _passengers.Count < passengerSeatPoints.Length; i++)
        {
            Agent agent = agents[i];
            if (agent.IsLiftPassenger) continue;

            agent.EnterLift(passengerSeatPoints[_passengers.Count]);
            _passengers.Add(agent);
        }
    }

    void ExitPassengers(Transform[] exitPoints, Transform fallbackPoint)
    {
        for (int i = 0; i < _passengers.Count; i++)
        {
            Transform exitPoint = GetPoint(exitPoints, i, fallbackPoint);
            _passengers[i].ExitLift(exitPoint);
        }

        _passengers.Clear();
    }

    Transform GetPoint(Transform[] points, int index, Transform fallbackPoint)
    {
        if (points != null && points.Length > 0)
            return points[Mathf.Min(index, points.Length - 1)];

        return fallbackPoint;
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}
