using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Короткий shake игровой камеры по <see cref="Actions.OnBotHammerHit"/>.
/// Твиняет FollowOffset, потому что Cinemachine каждый кадр перезаписывает transform.
/// </summary>
public class CameraHitShake : MonoBehaviour
{
    [SerializeField] private float _duration = 0.14f;
    [SerializeField] private float _strength = 0.2f;
    [SerializeField] private int _vibrato = 12;

    private CinemachineFollow _follow;
    private Vector3 _restOffset;
    private Tween _tween;

    private void Awake()
    {
        _follow = GetComponent<CinemachineFollow>();
        _restOffset = _follow.FollowOffset;
    }

    private void OnEnable()
    {
        Actions.OnBotHammerHit += Shake;
    }

    private void OnDisable()
    {
        Actions.OnBotHammerHit -= Shake;
        _tween?.Kill();
    }

    private void Shake()
    {
        _tween?.Kill();
        _follow.FollowOffset = _restOffset;
        _tween = DOTween.Shake(
                () => _follow.FollowOffset,
                value =>
                {
                    value.x = _restOffset.x;
                    value.z = _restOffset.z;
                    _follow.FollowOffset = value;
                },
                _duration,
                new Vector3(0f, _strength, 0f),
                _vibrato)
            .SetTarget(_follow)
            .OnKill(() => _follow.FollowOffset = _restOffset);
    }
}
