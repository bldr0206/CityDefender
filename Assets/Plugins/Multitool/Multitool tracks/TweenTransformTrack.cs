using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MultitoolTracks
{
    [TrackColor(0.8f, 0.4f, 0.2f)]
    [TrackBindingType(typeof(Transform))]
    [TrackClipType(typeof(TweenTransformClip))]
    public class TweenTransformTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<TweenTransformMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
