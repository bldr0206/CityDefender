using System.Collections.Generic;
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
    [SerializeField] private float inputSmoothTime = 0.04f;
    [SerializeField] private GameObject _aimObject;
    // PRIVATE FIELDS
    private Rigidbody _rigidbody;
    private Joystick _moveJoystick;
    private Transform _playerModelTransform;
    private Transform _aimTransform;
    private Vector2 _moveInput;
    private Vector2 _moveInputVelocity;
    private Vector3 _aimVelocity;
    private readonly List<Vector3> _wallNormals = new List<Vector3>();

    private bool _canMove = true;
    private bool _ignoreInputUntilReleased;
    private const float InputDeadZoneSqr = 0.0004f; // (~0.02)^2
    private const float WallNormalMaxY = 0.5f;

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
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _playerModelTransform = _playerModel.transform;
        _aimTransform = _aimObject.transform;
    }
    private void OnDestroy()
    {
        Actions.OnPlayerReachedFinish -= HandlePlayerReachedFinish;
        Actions.OnLevelStarted -= HandleLevelStarted;
    }
    private void Update()
    {
        UpdateInput();
        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        MoveRigidbody();
    }

    private void UpdateInput()
    {
        if (!_canMove)
        {
            _moveInput = Vector2.zero;
            return;
        }

        Vector2 input = _moveJoystick.Direction;
        if (_ignoreInputUntilReleased)
        {
            if (input.sqrMagnitude > InputDeadZoneSqr)
            {
                return;
            }

            _ignoreInputUntilReleased = false;
        }

        _moveInput = Vector2.SmoothDamp(
            _moveInput,
            Vector2.ClampMagnitude(input, 1f),
            ref _moveInputVelocity,
            inputSmoothTime
        );

        if (_moveInput.sqrMagnitude <= InputDeadZoneSqr)
        {
            _moveInput = Vector2.zero;
        }
    }

    private void MoveRigidbody()
    {
        if (!_canMove)
        {
            _wallNormals.Clear();
            return;
        }

        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.x = _moveInput.x * speed;
        velocity.z = _moveInput.y * speed;

        for (int i = 0; i < _wallNormals.Count; i++)
        {
            if (Vector3.Dot(velocity, _wallNormals[i]) < 0f)
            {
                velocity = Vector3.ProjectOnPlane(velocity, _wallNormals[i]);
            }
        }

        _rigidbody.linearVelocity = velocity;
        _wallNormals.Clear();
    }

    private void UpdateVisuals()
    {
        float inputMagnitudeSqr = _moveInput.sqrMagnitude;
        bool hasMoveInput = inputMagnitudeSqr > InputDeadZoneSqr;

        if (hasMoveInput)
        {
            Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);
            Quaternion targetRotation = Quaternion.LookRotation(move);
            _playerModelTransform.rotation = Quaternion.Slerp(_playerModelTransform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        float inputStrength = hasMoveInput ? Mathf.Clamp01(Mathf.Sqrt(inputMagnitudeSqr)) : 0f;
        float distance = Mathf.Lerp(aimObjectMinDistance, aimObjectMaxDistance, inputStrength);
        Vector3 aimPosition = _playerModelTransform.position + _playerModelTransform.forward * distance;
        _aimTransform.position = Vector3.SmoothDamp(
            _aimTransform.position,
            aimPosition,
            ref _aimVelocity,
            aimSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (Mathf.Abs(normal.y) < WallNormalMaxY)
            {
                _wallNormals.Add(normal);
            }
        }
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

        _moveInput = Vector2.zero;
        _moveInputVelocity = Vector2.zero;
        _aimVelocity = Vector3.zero;
    }
}