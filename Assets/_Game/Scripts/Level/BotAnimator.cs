using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class BotAnimator : MonoBehaviour
{
    [SerializeField] private PlayableDirector _botAnimationDirector;
    [SerializeField] private TimelineAsset _idleTimeline;
    [SerializeField] private TimelineAsset _runTimeline;

    private TimelineAsset _currentAnimation;

    public void PlayIdleAnimation()
    {
        PlayAnimation(_idleTimeline);
    }

    public void PlayRunAnimation()
    {
        PlayAnimation(_runTimeline);
    }

    public void StopAnimation()
    {
        _botAnimationDirector.Stop();
        _currentAnimation = null;
    }

    private void PlayAnimation(TimelineAsset timeline)
    {
        if (_currentAnimation == timeline)
        {
            return;
        }

        _currentAnimation = timeline;
        _botAnimationDirector.playableAsset = timeline;
        _botAnimationDirector.Play();
    }
}
