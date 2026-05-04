using UnityEngine;
using UnityEngine.Playables;

namespace MultitoolTracks
{
    public class TweenTransformBehaviour : PlayableBehaviour
    {
        public bool tweenPosition;
        public bool tweenScale;
        public bool tweenRotation;

        public Transform startTarget;
        public Transform endTarget;

        public Vector3 startPositionOffset;
        public Vector3 endPositionOffset;
        public Vector3 startRotationOffsetEuler;
        public Vector3 endRotationOffsetEuler;

        public Vector3 startPosition;
        public Vector3 endPosition;
        public Vector3 startScale;
        public Vector3 endScale;
        public float startScaleUniform;
        public float endScaleUniform;
        public Vector3 startRotationEuler;
        public Vector3 endRotationEuler;

        public SourceMode positionSourceMode;
        public SourceMode rotationSourceMode;

        public AxisMode positionAxisMode;
        public AxisMode scaleAxisMode;
        public AxisMode rotationAxisMode;

        public EasingType positionEasing;
        public EasingType scaleEasing;
        public EasingType rotationEasing;

        public Space space;
        public RotationMode rotationMode;
        public ScaleMode scaleMode;
        public ScaleValueMode scaleValueMode;
    }
}
