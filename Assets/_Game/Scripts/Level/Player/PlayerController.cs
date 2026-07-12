using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UnityEngine.Playables;
using UnityEngine.Timeline;
public class PlayerController : MonoBehaviour
{
    // SERIALIZED FIELDS
    [SerializeField] private float speed = 5f;
    [SerializeField] private GameObject _playerModel;
    [SerializeField] private float inputSmoothTime = 0.04f;

    [SerializeField] PlayableDirector _playerAnimationDirector;
    [SerializeField] TimelineAsset _idleTimeline;
    [SerializeField] TimelineAsset _runTimeline;
    // PRIVATE FIELDS
    private Rigidbody _rigidbody;
    private Joystick _moveJoystick;
    private BreakableTrigger _breakableTrigger;
    private Transform _playerModelTransform;
    private Vector2 _moveInput;
    private Vector2 _moveInputVelocity;
    private readonly List<Vector3> _wallNormals = new List<Vector3>();
    private TimelineAsset _currentAnimation;

    private bool _canMove = true;
    private bool _cutsceneBlockingMovement;
    private bool _canMoveBeforePause = true;
    private bool _ignoreInputUntilReleased;
    private const float InputDeadZoneSqr = 0.0004f; // (~0.02)^2
    private const float WallNormalMaxY = 0.5f;

    // Скорость перемещения. Дебажный слайдер в меню паузы пишет сюда дискретные значения 1–10.
    public float MoveSpeed
    {
        get => speed;
        set => speed = value;
    }

    [Inject]
    public void Construct(Joystick moveJoystick, BreakableTrigger breakableTrigger)
    {
        _moveJoystick = moveJoystick;
        _breakableTrigger = breakableTrigger;
    }
    private void Awake()
    {
        Actions.OnPlayerReachedFinish += HandlePlayerReachedFinish;
        Actions.OnLevelStarted += HandleLevelStarted;
        Actions.OnGamePaused += HandleGamePaused;
        Actions.OnGameResumed += HandleGameResumed;
        Actions.OnCutsceneStarted += HandleCutsceneStarted;
        Actions.OnCutsceneEnded += HandleCutsceneEnded;
        Actions.OnQuestSequencePauseStarted += HandleQuestSequencePauseStarted;
        Actions.OnQuestSequencePauseEnded += HandleQuestSequencePauseEnded;

        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _playerModelTransform = _playerModel.transform;
        PlayAnimation(_idleTimeline);
    }
    private void OnDestroy()
    {
        Actions.OnPlayerReachedFinish -= HandlePlayerReachedFinish;
        Actions.OnLevelStarted -= HandleLevelStarted;
        Actions.OnGamePaused -= HandleGamePaused;
        Actions.OnGameResumed -= HandleGameResumed;
        Actions.OnCutsceneStarted -= HandleCutsceneStarted;
        Actions.OnCutsceneEnded -= HandleCutsceneEnded;
        Actions.OnQuestSequencePauseStarted -= HandleQuestSequencePauseStarted;
        Actions.OnQuestSequencePauseEnded -= HandleQuestSequencePauseEnded;
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
        if (!_canMove || _cutsceneBlockingMovement)
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
        if (!_canMove || _cutsceneBlockingMovement)
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
        PlayAnimation(hasMoveInput ? _runTimeline : _idleTimeline);

        Transform breakableTarget = _breakableTrigger.Target;
        if (breakableTarget != null)
        {
            RotateModel(breakableTarget.position - _playerModelTransform.position);
        }
        else if (hasMoveInput)
        {
            RotateModel(new Vector3(_moveInput.x, 0f, _moveInput.y));
        }
    }

    private void RotateModel(Vector3 direction)
    {
        direction.y = 0f;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        _playerModelTransform.rotation = Quaternion.Slerp(_playerModelTransform.rotation, targetRotation, Time.deltaTime * 10f);
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

    private void HandleGamePaused()
    {
        _canMoveBeforePause = _canMove;
        _canMove = false;
        StopMotionImmediately();
    }

    private void HandleGameResumed()
    {
        _canMove = _canMoveBeforePause;
        _ignoreInputUntilReleased = true;
        StopMotionImmediately();
    }

    private void HandleCutsceneStarted()
    {
        _cutsceneBlockingMovement = true;
        _ignoreInputUntilReleased = true;
        StopMotionImmediately();
    }

    private void HandleCutsceneEnded()
    {
        _cutsceneBlockingMovement = false;
        _ignoreInputUntilReleased = true;
        StopMotionImmediately();
    }

    private void HandleQuestSequencePauseStarted()
    {
        HandleCutsceneStarted();
    }

    private void HandleQuestSequencePauseEnded()
    {
        HandleCutsceneEnded();
    }

    private void StopMotionImmediately()
    {
        if (_moveJoystick != null)
            _moveJoystick.OnPointerUp(null);

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _moveInput = Vector2.zero;
        _moveInputVelocity = Vector2.zero;
        PlayAnimation(_idleTimeline);
    }

    public SaveTransformData CaptureSaveData()
    {
        return _rigidbody != null
            ? new SaveTransformData { position = _rigidbody.position, rotation = _rigidbody.rotation }
            : new SaveTransformData(transform);
    }

    public void RestoreSaveData(SaveTransformData data)
    {
        if (data == null) return;

        if (_rigidbody != null)
        {
            _rigidbody.position = data.position;
            _rigidbody.rotation = data.rotation;
            transform.SetPositionAndRotation(data.position, data.rotation);
            Physics.SyncTransforms();
        }
        else
        {
            data.ApplyTo(transform);
        }

        StopMotionImmediately();
    }

    private void PlayAnimation(TimelineAsset timeline)
    {
        if (_playerAnimationDirector == null || timeline == null || _currentAnimation == timeline) return;

        _currentAnimation = timeline;
        _playerAnimationDirector.playableAsset = timeline;
        _playerAnimationDirector.Play();
    }
}