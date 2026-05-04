using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CinemachinePriorityClip : PlayableAsset, ITimelineClipAsset
{
    public int Priority;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<CinemachinePriorityBehaviour> playable = ScriptPlayable<CinemachinePriorityBehaviour>.Create(graph);
        playable.GetBehaviour().Priority = Priority;
        return playable;
    }
}
