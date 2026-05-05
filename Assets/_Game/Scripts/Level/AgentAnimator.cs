using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AgentAnimator : MonoBehaviour
{
    [SerializeField] private PlayableDirector _agentAnimationDirector;
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
        _agentAnimationDirector.Stop();
        _currentAnimation = null;
    }

    private void PlayAnimation(TimelineAsset timeline)
    {
        if (_currentAnimation == timeline)
        {
            return;
        }

        _currentAnimation = timeline;
        _agentAnimationDirector.playableAsset = timeline;
        _agentAnimationDirector.Play();
    }
}
