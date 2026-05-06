using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class Hammer : MonoBehaviour
{
    [SerializeField] private PlayableDirector _hammerAnimationDirector;
    [SerializeField] private TimelineAsset _hammerHitTimeline;

    private Renderer[] _renderers;
    private TimelineAsset _currentAnimation;

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

    public void PlayHitAnimation()
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
        _hammerAnimationDirector.Play();
    }

    public void StopHitAnimation()
    {
        if (_hammerAnimationDirector.state == PlayState.Playing)
        {
            return;
        }

        _currentAnimation = null;
        Hide();
    }

    private void HandleStopped(PlayableDirector director)
    {
        if (director != _hammerAnimationDirector)
        {
            return;
        }

        _currentAnimation = null;
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
    }
}
