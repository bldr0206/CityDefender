using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SaveId))]
public class Lift : MonoBehaviour
{
    [SerializeField] private Rigidbody platformRigidbody;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;
    [SerializeField] private float moveDuration = 2f;

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

        _isMoving = true;
        _moveTween?.Kill();
        _moveTween = platformRigidbody.DOMove(topPoint.position, moveDuration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                Debug.Log("Lift reached the top!");
                _isMoving = false;
                _isAtTop = true;
            });
    }

    public void MoveDown()
    {
        if (_isMoving) return;
        if (!_isAtTop) return;

        _isMoving = true;
        _moveTween?.Kill();
        _moveTween = platformRigidbody.DOMove(bottomPoint.position, moveDuration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                Debug.Log("Lift reached the bottom!");
                _isMoving = false;
                _isAtTop = false;
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

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}
