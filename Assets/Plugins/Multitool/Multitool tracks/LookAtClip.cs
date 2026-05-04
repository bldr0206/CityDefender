using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MultitoolTracks
{
    [Serializable]
    [DisplayName("LookAt Clip")]
    public class LookAtClip : PlayableAsset, ITimelineClipAsset, IPropertyPreview
    {
        public ExposedReference<Transform> target;

        [Tooltip("Up vector for LookRotation. Default is world up.")]
        public Vector3 upVector = Vector3.up;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LookAtBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.target = target.Resolve(graph.GetResolver());
            behaviour.upVector = upVector;

            return playable;
        }

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            driver.AddFromName<Transform>("m_LocalRotation.x");
            driver.AddFromName<Transform>("m_LocalRotation.y");
            driver.AddFromName<Transform>("m_LocalRotation.z");
            driver.AddFromName<Transform>("m_LocalRotation.w");
        }
    }
}
