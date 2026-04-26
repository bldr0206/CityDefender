using System;
using UnityEngine;
using Zenject;
public class PlayerController : MonoBehaviour
{
    // SERIALIZED FIELDS
    [SerializeField] private float speed = 5f;
    [SerializeField] private GameObject _playerModel;
    [SerializeField] private float aimObjectMinDistance = 1f;
    [SerializeField] private float aimObjectMaxDistance = 2f;
    [SerializeField] private float aimSmoothTime = 0.08f;
    [SerializeField] private GameObject _aimObject;
    // PRIVATE FIELDS
    private Rigidbody _rigidbody;
    private Joystick _moveJoystick;
    private Transform _playerModelTransform;
    private Transform _aimTransform;
    private Vector3 _aimVelocity;

    private bool _canMove = true;
    private bool _ignoreInputUntilReleased;
    private const float InputReleaseEpsilonSqr = 0.0004f; // (~0.02)^2

    [Inject]
    public void Construct(Joystick moveJoystick)
    {
        _moveJoystick = moveJoystick;
    }
    private void Awake()
    {
        Actions.OnPlayerReachedFinish += HandlePlayerReachedFinish;
        Actions.OnLevelStarted += HandleLevelStarted;

        _rigidbody = GetComponent<Rigidbody>();
        _playerModelTransform = _playerModel.transform;
        _aimTransform = _aimObject.transform;
    }
    private void OnDestroy()
    {
        Actions.OnPlayerReachedFinish -= HandlePlayerReachedFinish;
        Actions.OnLevelStarted -= HandleLevelStarted;
    }
    private void FixedUpdate()
    {
        MovementLogic();
    }
    void MovementLogic()
    {
        if (!_canMove)
        {
            return;
        }

        Vector2 input = _moveJoystick.Direction;
        if (_ignoreInputUntilReleased)
        {
            if (input.sqrMagnitude > InputReleaseEpsilonSqr)
                return;

            _ignoreInputUntilReleased = false;
        }
        Vector3 move = new Vector3(input.x, 0f, input.y);
        _rigidbody.MovePosition(_rigidbody.position + move * speed * Time.fixedDeltaTime);
        if (move.sqrMagnitude > 0f)// rotate Y axis in the direction of movement
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            _playerModelTransform.rotation = Quaternion.Slerp(_playerModelTransform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
        // Always update aim: with no input it smoothly returns to the minimum distance.
        float inputStrength = Mathf.Clamp01(move.magnitude);
        float distance = Mathf.Lerp(aimObjectMinDistance, aimObjectMaxDistance, inputStrength);
        Vector3 aimPosition = _playerModelTransform.position + _playerModelTransform.forward * distance;
        _aimTransform.position = Vector3.SmoothDamp(
            _aimTransform.position,
            aimPosition,
            ref _aimVelocity,
            aimSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );
    }

    private void HandlePlayerReachedFinish()
    {
        _canMove = false;
        _ignoreInputUntilReleased = true;
        StopMotionImmediately();
    }

    private void HandleLevelStarted()
    {
        _canMove = true;
        _ignoreInputUntilReleased = true;
        StopMotionImmediately();
    }

    private void StopMotionImmediately()
    {
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _aimVelocity = Vector3.zero;
    }
}