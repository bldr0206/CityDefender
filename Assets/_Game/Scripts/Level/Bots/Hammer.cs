using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class Hammer : MonoBehaviour, INotificationReceiver
{
    [SerializeField] private PlayableDirector _hammerAnimationDirector;
    [SerializeField] private TimelineAsset _hammerHitTimeline;

    private Renderer[] _renderers;
    private TimelineAsset _currentAnimation;
    private bool _hitEmitted;

    public bool IsPlaying => _hammerAnimationDirector.state == PlayState.Playing;

    public event Action OnHit;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _hammerAnimationDirector.extrapolationMode = DirectorWrapMode.None;
        _hammerAnimationDirector.stopped += HandleStopped;
        _hammerAnimationDirector.Stop();
        Hide();
    }

    private void OnDestroy()
    {
        _hammerAnimationDirector.stopped -= HandleStopped;
    }

    public void PlayHitAnimation(float speedMultiplier = 1f)
    {
        Show();

        if (_currentAnimation != _hammerHitTimeline)
        {
            _currentAnimation = _hammerHitTimeline;
            _hammerAnimationDirector.playableAsset = _hammerHitTimeline;
        }

        if (_hammerAnimationDirector.state == PlayState.Playing)
        {
            return;
        }

        _hammerAnimationDirector.time = 0f;
        _hitEmitted = false;
        _hammerAnimationDirector.Play();

        if (speedMultiplier > 0f && _hammerAnimationDirector.playableGraph.IsValid())
            _hammerAnimationDirector.playableGraph.GetRootPlayable(0).SetSpeed(speedMultiplier);

        ConnectNotificationReceivers();
    }

    public void StopHitAnimation()
    {
        if (_hammerAnimationDirector.state == PlayState.Playing)
        {
            return;
        }

        _currentAnimation = null;
        _hitEmitted = false;
        Hide();
    }

    private void HandleStopped(PlayableDirector director)
    {
        if (director != _hammerAnimationDirector)
        {
            return;
        }

        _currentAnimation = null;
        _hitEmitted = false;
        Hide();
    }

    private void Show()
    {
        SetVisible(true);
    }

    private void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        foreach (Renderer renderer in _renderers)
        {
            renderer.enabled = isVisible;
        }
    }

    public void Hit()
    {
        if (_hitEmitted)
        {
            return;
        }

        _hitEmitted = true;
        OnHit?.Invoke();
    }

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is SignalEmitter)
            Hit();
    }

    private void ConnectNotificationReceivers()
    {
        PlayableGraph graph = _hammerAnimationDirector.playableGraph;
        for (int i = 0; i < graph.GetOutputCount(); i++)
            graph.GetOutput(i).AddNotificationReceiver(this);
    }
}
