using UnityEngine;
using Zenject;
using DG.Tweening;

public class QuestNavigationSystem : MonoBehaviour
{
    [SerializeField] private GameObject _questPointer;
    [SerializeField] private Vector2 _screenPadding = new Vector2(96f, 160f);
    [SerializeField] private float _rotationOffset;
    [SerializeField] private float _moveDistance = 24f;
    [SerializeField] private float _moveDuration = 0.45f;
    [SerializeField] private float _pulseScale = 1.12f;
    [SerializeField] private float _pulseDuration = 0.6f;

    RectTransform _root;
    RectTransform _questPointerRect;
    Camera _camera;
    Transform _player;
    Transform _target;
    Tween _moveTween;
    Tween _pulseTween;
    float _currentMoveOffset;

    [Inject]
    public void Construct(PlayerController player)
    {
        _player = player.transform;
    }

    void Awake()
    {
        _root = (RectTransform)transform;
        _questPointerRect = _questPointer.GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        _camera = canvas.worldCamera;
        HidePointer();
    }

    void OnEnable()
    {
        Actions.OnQuestTargetChanged += SetTarget;
    }

    void OnDisable()
    {
        Actions.OnQuestTargetChanged -= SetTarget;
        StopPointerAnimation();
    }

    void LateUpdate()
    {
        if (_target == null)
        {
            HidePointer();
            return;
        }

        Vector3 targetScreenPosition = _camera.WorldToScreenPoint(_target.position);
        Vector2 targetPosition = GetLocalPoint(targetScreenPosition);

        if (IsVisible(targetScreenPosition.z, targetPosition))
        {
            HidePointer();
            return;
        }

        Vector2 playerPosition = GetLocalPoint(_camera.WorldToScreenPoint(_player.position));
        Vector2 direction = GetDirection(playerPosition, targetPosition, targetScreenPosition.z);
        Vector2 pointerPosition = GetEdgePosition(playerPosition, direction);

        _questPointerRect.anchoredPosition = pointerPosition - direction * _currentMoveOffset;
        _questPointerRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _rotationOffset);
        ShowPointer();
    }

    void OnDestroy()
    {
        StopPointerAnimation();
    }

    void SetTarget(Transform target)
    {
        _target = target;
    }

    Vector2 GetLocalPoint(Vector3 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPosition, _camera, out Vector2 localPoint);
        return localPoint;
    }

    bool IsVisible(float depth, Vector2 targetPosition)
    {
        return depth > 0f && _root.rect.Contains(targetPosition);
    }

    Vector2 GetDirection(Vector2 playerPosition, Vector2 targetPosition, float targetDepth)
    {
        Vector2 direction = targetPosition - playerPosition;

        if (targetDepth < 0f)
        {
            direction = -direction;
        }

        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
    }

    Vector2 GetEdgePosition(Vector2 playerPosition, Vector2 direction)
    {
        Rect bounds = GetPointerBounds();
        Vector2 start = ClampToBounds(playerPosition, bounds);

        float distance = float.PositiveInfinity;

        if (Mathf.Abs(direction.x) > 0.001f)
        {
            float edgeX = direction.x > 0f ? bounds.xMax : bounds.xMin;
            distance = Mathf.Min(distance, (edgeX - start.x) / direction.x);
        }

        if (Mathf.Abs(direction.y) > 0.001f)
        {
            float edgeY = direction.y > 0f ? bounds.yMax : bounds.yMin;
            distance = Mathf.Min(distance, (edgeY - start.y) / direction.y);
        }

        return start + direction * distance;
    }

    Rect GetPointerBounds()
    {
        Rect rect = _root.rect;
        rect.xMin += _screenPadding.x;
        rect.xMax -= _screenPadding.x;
        rect.yMin += _screenPadding.y;
        rect.yMax -= _screenPadding.y;
        return rect;
    }

    Vector2 ClampToBounds(Vector2 position, Rect bounds)
    {
        return new Vector2(
            Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(position.y, bounds.yMin, bounds.yMax)
        );
    }

    void ShowPointer()
    {
        if (!_questPointer.activeSelf)
        {
            _questPointer.SetActive(true);
        }

        if (_moveTween == null)
        {
            StartPointerAnimation();
        }
    }

    void HidePointer()
    {
        if (_questPointer.activeSelf)
        {
            _questPointer.SetActive(false);
            StopPointerAnimation();
        }
    }

    void StartPointerAnimation()
    {
        _currentMoveOffset = 0f;
        _questPointerRect.localScale = Vector3.one;

        _moveTween = DOTween
            .To(() => _currentMoveOffset, value => _currentMoveOffset = value, _moveDistance, _moveDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        _pulseTween = _questPointerRect
            .DOScale(_pulseScale, _pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void StopPointerAnimation()
    {
        _moveTween?.Kill();
        _pulseTween?.Kill();
        _moveTween = null;
        _pulseTween = null;
        _currentMoveOffset = 0f;

        if (_questPointerRect != null)
        {
            _questPointerRect.localScale = Vector3.one;
        }
    }
}
