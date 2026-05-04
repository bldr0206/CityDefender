using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(CinemachinePriorityClip))]
[TrackBindingType(typeof(CinemachineVirtualCameraBase))]
public class CinemachinePriorityTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        ScriptPlayable<CinemachinePriorityMixerBehaviour> playable = ScriptPlayable<CinemachinePriorityMixerBehaviour>.Create(graph, inputCount);
        PlayableDirector director = go.GetComponent<PlayableDirector>();
        playable.GetBehaviour().Camera = director.GetGenericBinding(this) as CinemachineVirtualCameraBase;
        return playable;
    }
}
