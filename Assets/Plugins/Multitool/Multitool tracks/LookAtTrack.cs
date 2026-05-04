using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MultitoolTracks
{
    [TrackColor(0.2f, 0.8f, 0.4f)]
    [TrackBindingType(typeof(Transform))]
    [TrackClipType(typeof(LookAtClip))]
    public class LookAtTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<LookAtMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
