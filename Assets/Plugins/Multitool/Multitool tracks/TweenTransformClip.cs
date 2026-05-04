using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MultitoolTracks
{
    [Serializable]
    [DisplayName("Tween Transform Clip")]
    public class TweenTransformClip : PlayableAsset, ITimelineClipAsset, IPropertyPreview
    {
        [Tooltip("Enable position tweening")]
        public bool tweenPosition = true;

        [Tooltip("Enable scale tweening")]
        public bool tweenScale = true;

        [Tooltip("Enable rotation tweening")]
        public bool tweenRotation = true;

        public ExposedReference<Transform> startTarget;
        public ExposedReference<Transform> endTarget;

        [Tooltip("Offset added to reference transform position (when Source = Target)")]
        public Vector3 startPositionOffset;
        [Tooltip("Offset added to reference transform position (when Source = Target)")]
        public Vector3 endPositionOffset;
        [Tooltip("Euler offset (degrees) applied to reference transform rotation (when Source = Target)")]
        public Vector3 startRotationOffsetEuler;
        [Tooltip("Euler offset (degrees) applied to reference transform rotation (when Source = Target)")]
        public Vector3 endRotationOffsetEuler;

        public Vector3 startPosition;
        public Vector3 endPosition;
        public Vector3 startScale = Vector3.one;
        public Vector3 endScale = Vector3.one;
        public float startScaleUniform = 1f;
        public float endScaleUniform = 1f;
        public Vector3 startRotationEuler;
        public Vector3 endRotationEuler;

        public SourceMode positionSourceMode = SourceMode.Value;
        public SourceMode rotationSourceMode = SourceMode.Value;

        public AxisMode positionAxisMode = AxisMode.All;
        public AxisMode scaleAxisMode = AxisMode.All;
        public AxisMode rotationAxisMode = AxisMode.All;

        public EasingType positionEasing = EasingType.InOutQuad;
        public EasingType scaleEasing = EasingType.InOutQuad;
        public EasingType rotationEasing = EasingType.InOutQuad;

        public Space space = Space.Local;
        public RotationMode rotationMode = RotationMode.Relative;
        public ScaleMode scaleMode = ScaleMode.Relative;
        public ScaleValueMode scaleValueMode = ScaleValueMode.Uniform;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TweenTransformBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.startTarget = startTarget.Resolve(graph.GetResolver());
            behaviour.endTarget = endTarget.Resolve(graph.GetResolver());
            behaviour.startPositionOffset = startPositionOffset;
            behaviour.endPositionOffset = endPositionOffset;
            behaviour.startRotationOffsetEuler = startRotationOffsetEuler;
            behaviour.endRotationOffsetEuler = endRotationOffsetEuler;
            behaviour.startPosition = startPosition;
            behaviour.endPosition = endPosition;
            behaviour.startScale = startScale;
            behaviour.endScale = endScale;
            behaviour.startScaleUniform = startScaleUniform;
            behaviour.endScaleUniform = endScaleUniform;
            behaviour.startRotationEuler = startRotationEuler;
            behaviour.endRotationEuler = endRotationEuler;
            behaviour.tweenPosition = tweenPosition;
            behaviour.tweenScale = tweenScale;
            behaviour.tweenRotation = tweenRotation;
            behaviour.positionSourceMode = positionSourceMode;
            behaviour.rotationSourceMode = rotationSourceMode;
            behaviour.positionAxisMode = positionAxisMode;
            behaviour.scaleAxisMode = scaleAxisMode;
            behaviour.rotationAxisMode = rotationAxisMode;
            behaviour.positionEasing = positionEasing;
            behaviour.scaleEasing = scaleEasing;
            behaviour.rotationEasing = rotationEasing;
            behaviour.space = space;
            behaviour.rotationMode = rotationMode;
            behaviour.scaleMode = scaleMode;
            behaviour.scaleValueMode = scaleValueMode;

            return playable;
        }

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            if (tweenPosition)
            {
                driver.AddFromName<Transform>("m_LocalPosition.x");
                driver.AddFromName<Transform>("m_LocalPosition.y");
                driver.AddFromName<Transform>("m_LocalPosition.z");
            }
            if (tweenRotation)
            {
                driver.AddFromName<Transform>("m_LocalRotation.x");
                driver.AddFromName<Transform>("m_LocalRotation.y");
                driver.AddFromName<Transform>("m_LocalRotation.z");
                driver.AddFromName<Transform>("m_LocalRotation.w");
            }
            if (tweenScale)
            {
                driver.AddFromName<Transform>("m_LocalScale.x");
                driver.AddFromName<Transform>("m_LocalScale.y");
                driver.AddFromName<Transform>("m_LocalScale.z");
            }
        }
    }
}
