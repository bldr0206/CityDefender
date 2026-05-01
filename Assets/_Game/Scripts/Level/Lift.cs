using UnityEngine;
using DG.Tweening;

public class Lift : MonoBehaviour
{
    [SerializeField] private Rigidbody platformRigidbody;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;
    [SerializeField] private float moveDuration = 2f;

    private Tween _moveTween;
    private bool _isMoving;
    private bool _isAtTop;

    private void Awake()
    {
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

        float toTop = Vector3.SqrMagnitude(platformRigidbody.position - topPoint.position);
        float toBottom = Vector3.SqrMagnitude(platformRigidbody.position - bottomPoint.position);
        _isAtTop = toTop < toBottom;
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

    public bool IsMoving()
    {
        return _isMoving;
    }
    public bool IsAtTop()
    {
        return _isAtTop;
    }
}
