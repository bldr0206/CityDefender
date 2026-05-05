using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MultitoolTracks;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.Timeline
{
    [ApplyDefaultUndo("Reverse")]
    [MenuEntry("Editing/Reverse", MenuPriority.ClipActionSection.split + 2)]
    [UsedImplicitly]
    class ReverseClipAction : ClipAction
    {
        public override ActionValidity Validate(IEnumerable<TimelineClip> clips)
        {
            var selectedClips = clips?.ToArray();
            if (selectedClips == null || selectedClips.Length != 1)
                return ActionValidity.Invalid;

            var clip = selectedClips[0];
            if (clip == null || clip.asset == null)
                return ActionValidity.Invalid;

            var track = clip.GetParentTrack();
            if (track == null || track.timelineAsset == null)
                return ActionValidity.Invalid;

            if (clip.asset is TweenTransformClip)
                return ActionValidity.Valid;

            var animAsset = clip.asset as AnimationPlayableAsset;
            if (animAsset != null && animAsset.clip != null && !animAsset.clip.empty
                && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(track.timelineAsset)))
                return ActionValidity.Valid;

            return ActionValidity.NotApplicable;
        }

        public override bool Execute(IEnumerable<TimelineClip> clips)
        {
            var clip = clips.First();
            var track = clip.GetParentTrack();
            var timelineAsset = track.timelineAsset;

            UndoExtensions.RegisterCompleteTimeline(timelineAsset, L10n.Tr("Reverse"));

            var success = false;
            if (clip.asset is TweenTransformClip tweenClip)
                success = ReverseTweenTransform(tweenClip);
            else if (clip.asset is AnimationPlayableAsset animAsset)
                success = ReverseAnimationClip(clip, animAsset, timelineAsset, track);

            if (success)
                TimelineEditor.Refresh(RefreshReason.ContentsModified);

            return success;
        }

        [TimelineShortcut("Editing/Reverse", KeyCode.None)]
        public static void HandleShortcut(ShortcutArguments args)
        {
            Invoker.InvokeWithSelectedClips<ReverseClipAction>();
        }

        static bool ReverseTweenTransform(TweenTransformClip clip)
        {
            Swap(ref clip.startTarget, ref clip.endTarget);
            Swap(ref clip.startPositionOffset, ref clip.endPositionOffset);
            Swap(ref clip.startRotationOffsetEuler, ref clip.endRotationOffsetEuler);
            Swap(ref clip.startPosition, ref clip.endPosition);
            Swap(ref clip.startScale, ref clip.endScale);
            Swap(ref clip.startScaleUniform, ref clip.endScaleUniform);
            Swap(ref clip.startRotationEuler, ref clip.endRotationEuler);

            EditorUtility.SetDirty(clip);
            return true;
        }

        static bool ReverseAnimationClip(TimelineClip timelineClip, AnimationPlayableAsset animAsset, TimelineAsset timelineAsset, TrackAsset track)
        {
            var sourceClip = animAsset.clip;
            if (sourceClip == null || sourceClip.empty)
                return false;

            var path = AssetDatabase.GetAssetPath(timelineAsset);
            if (string.IsNullOrEmpty(path))
                return false;

            var reversedClip = new AnimationClip
            {
                name = GetUniqueClipName(timelineAsset, track, sourceClip.name + "_Reversed"),
                frameRate = sourceClip.frameRate,
                legacy = sourceClip.legacy
            };

            AnimationUtility.SetAnimationClipSettings(reversedClip, AnimationUtility.GetAnimationClipSettings(sourceClip));
            ReverseFloatCurves(sourceClip, reversedClip);
            ReverseObjectCurves(sourceClip, reversedClip);
            ReverseEvents(sourceClip, reversedClip);

            AssetDatabase.AddObjectToAsset(reversedClip, timelineAsset);
            Undo.RegisterCreatedObjectUndo(reversedClip, L10n.Tr("Reverse"));
            Undo.RecordObject(animAsset, L10n.Tr("Reverse"));

            animAsset.clip = reversedClip;
            timelineClip.clipIn = GetReversedClipIn(timelineClip, sourceClip.length);

            EditorUtility.SetDirty(reversedClip);
            EditorUtility.SetDirty(animAsset);
            EditorUtility.SetDirty(timelineAsset);
            AssetDatabase.ImportAsset(path);
            return true;
        }

        static void ReverseFloatCurves(AnimationClip source, AnimationClip dest)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null || curve.length == 0)
                    continue;

                var reversedKeys = curve.keys
                    .Select(key => ReverseKey(key, source.length))
                    .OrderBy(key => key.time)
                    .ToArray();

                var reversedCurve = new AnimationCurve(reversedKeys)
                {
                    preWrapMode = curve.postWrapMode,
                    postWrapMode = curve.preWrapMode
                };
                AnimationUtility.SetEditorCurve(dest, binding, reversedCurve);
            }
        }

        static Keyframe ReverseKey(Keyframe key, float duration)
        {
            var reversedKey = new Keyframe(duration - key.time, key.value)
            {
                inTangent = -key.outTangent,
                outTangent = -key.inTangent,
                inWeight = key.outWeight,
                outWeight = key.inWeight,
                weightedMode = ReverseWeightedMode(key.weightedMode)
            };
            return reversedKey;
        }

        static WeightedMode ReverseWeightedMode(WeightedMode mode)
        {
            if (mode == WeightedMode.In)
                return WeightedMode.Out;
            if (mode == WeightedMode.Out)
                return WeightedMode.In;
            return mode;
        }

        static void ReverseObjectCurves(AnimationClip source, AnimationClip dest)
        {
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keyframes = AnimationUtility.GetObjectReferenceCurve(source, binding);
                if (keyframes == null || keyframes.Length == 0)
                    continue;

                var reversedKeys = keyframes
                    .Select(key => new ObjectReferenceKeyframe { time = source.length - key.time, value = key.value })
                    .OrderBy(key => key.time)
                    .ToArray();

                AnimationUtility.SetObjectReferenceCurve(dest, binding, reversedKeys);
            }
        }

        static void ReverseEvents(AnimationClip source, AnimationClip dest)
        {
            var events = AnimationUtility.GetAnimationEvents(source)
                .Select(evt =>
                {
                    evt.time = source.length - evt.time;
                    return evt;
                })
                .OrderBy(evt => evt.time)
                .ToArray();

            AnimationUtility.SetAnimationEvents(dest, events);
        }

        static double GetReversedClipIn(TimelineClip clip, float sourceLength)
        {
            var clipOut = clip.clipIn + clip.duration * clip.timeScale;
            var clipIn = sourceLength - clipOut;
            return clipIn > 0 ? clipIn : 0;
        }

        static string GetUniqueClipName(TimelineAsset timelineAsset, TrackAsset track, string baseName)
        {
            var path = AssetDatabase.GetAssetPath(timelineAsset);
            if (!string.IsNullOrEmpty(path))
            {
                var names = AssetDatabase.LoadAllAssetsAtPath(path).Where(x => x != null).Select(x => x.name).ToArray();
                return ObjectNames.GetUniqueName(names, baseName);
            }

            if (track != null && track.GetClips().Any())
                return ObjectNames.GetUniqueName(track.GetClips().Select(x => x.displayName).ToArray(), baseName);

            return baseName;
        }

        static void Swap<T>(ref T a, ref T b)
        {
            (a, b) = (b, a);
        }
    }
}
