using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace UnityEditor.Timeline
{
    [ApplyDefaultUndo("Split at Playhead")]
    [MenuEntry("Editing/Split at Playhead", MenuPriority.ClipActionSection.split + 1)]
    [UsedImplicitly]
    class SplitAtPlayheadClipAction : ClipAction
    {
        static double GetPlayheadTime()
        {
            var director = TimelineEditor.inspectedDirector;
            return director != null ? director.time : 0;
        }

        public override ActionValidity Validate(IEnumerable<TimelineClip> clips)
        {
            if (clips == null || clips.Count() != 1)
                return ActionValidity.Invalid;

            var clip = clips.First();
            if (clip == null || clip.asset == null)
                return ActionValidity.Invalid;

            if (clip.asset as AnimationPlayableAsset == null)
                return ActionValidity.NotApplicable;

            var splitTime = GetPlayheadTime();
            if (splitTime <= clip.start || splitTime >= clip.end)
                return ActionValidity.Invalid;

            var animAsset = clip.asset as AnimationPlayableAsset;
            if (animAsset?.clip == null || animAsset.clip.empty)
                return ActionValidity.Invalid;

            return ActionValidity.Valid;
        }

        public override bool Execute(IEnumerable<TimelineClip> clips)
        {
            var clip = clips.First();
            var splitTime = GetPlayheadTime();

            var success = SplitAnimationClipAtTime(clip, splitTime);
            if (success)
                TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);

            return success;
        }

        static bool SplitAnimationClipAtTime(TimelineClip originalClip, double splitTime)
        {
            var animAsset = originalClip.asset as AnimationPlayableAsset;
            if (animAsset == null || animAsset.clip == null)
                return false;

            var track = originalClip.GetParentTrack() as AnimationTrack;
            if (track == null)
                return false;

            var timelineAsset = track.timelineAsset;
            if (timelineAsset == null)
                return false;

            var sourceClip = animAsset.clip;
            var localSplitTime = (float)((splitTime - originalClip.start) * originalClip.timeScale + originalClip.clipIn);
            var sourceDuration = sourceClip.length;

            if (localSplitTime <= 0 || localSplitTime >= sourceDuration)
                return false;

            UndoExtensions.RegisterCompleteTimeline(timelineAsset, L10n.Tr("Split at Playhead"));

            var leftClipName = GetUniqueClipName(timelineAsset, track, sourceClip.name + "_L");
            var rightClipName = GetUniqueClipName(timelineAsset, track, sourceClip.name + "_R");

            var leftTimelineClip = track.CreateRecordableClip(leftClipName);
            var rightTimelineClip = track.CreateRecordableClip(rightClipName);

            if (leftTimelineClip == null || rightTimelineClip == null)
                return false;

            var leftAnimClip = leftTimelineClip.animationClip;
            var rightAnimClip = rightTimelineClip.animationClip;
            if (leftAnimClip == null || rightAnimClip == null)
                return false;

            leftAnimClip.frameRate = sourceClip.frameRate;
            rightAnimClip.frameRate = sourceClip.frameRate;
            leftAnimClip.legacy = sourceClip.legacy;
            rightAnimClip.legacy = sourceClip.legacy;

            SplitFloatCurves(sourceClip, leftAnimClip, localSplitTime, isLeft: true);
            SplitObjectCurves(sourceClip, leftAnimClip, localSplitTime, isLeft: true);
            SplitFloatCurves(sourceClip, rightAnimClip, localSplitTime, isLeft: false);
            SplitObjectCurves(sourceClip, rightAnimClip, localSplitTime, isLeft: false);

            EditorUtility.SetDirty(leftAnimClip);
            EditorUtility.SetDirty(rightAnimClip);

            CopyPlayableAssetProperties(animAsset, leftTimelineClip.asset as AnimationPlayableAsset);
            CopyPlayableAssetProperties(animAsset, rightTimelineClip.asset as AnimationPlayableAsset);

            leftTimelineClip.start = originalClip.start;
            leftTimelineClip.duration = splitTime - originalClip.start;
            leftTimelineClip.timeScale = originalClip.timeScale;
            leftTimelineClip.easeInDuration = 0;
            leftTimelineClip.easeOutDuration = 0;
            leftTimelineClip.displayName = originalClip.displayName + " (L)";

            rightTimelineClip.start = splitTime;
            rightTimelineClip.duration = originalClip.end - splitTime;
            rightTimelineClip.timeScale = originalClip.timeScale;
            rightTimelineClip.clipIn = 0;
            rightTimelineClip.easeInDuration = 0;
            rightTimelineClip.easeOutDuration = 0;
            rightTimelineClip.displayName = originalClip.displayName + " (R)";

            timelineAsset.DeleteClip(originalClip);

            return true;
        }

        #region Animation clip splitting

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

        static void SplitFloatCurves(AnimationClip source, AnimationClip dest, float splitTime, bool isLeft)
        {
            var bindings = AnimationUtility.GetCurveBindings(source);

            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null || curve.length == 0)
                    continue;

                AnimationCurve newCurve;

                if (isLeft)
                {
                    var leftKeys = curve.keys.Where(k => k.time <= splitTime).ToList();
                    var boundaryValue = curve.Evaluate(splitTime);
                    var hasBoundaryKey = leftKeys.Any(k => Mathf.Approximately(k.time, splitTime));

                    if (!hasBoundaryKey && leftKeys.Count > 0)
                    {
                        leftKeys.Add(new Keyframe(splitTime, boundaryValue));
                        leftKeys.Sort((a, b) => a.time.CompareTo(b.time));
                    }

                    if (leftKeys.Count == 0)
                        leftKeys.Add(new Keyframe(splitTime, boundaryValue));

                    newCurve = new AnimationCurve(leftKeys.ToArray());
                }
                else
                {
                    var rightKeys = curve.keys.Where(k => k.time >= splitTime).Select(k =>
                    {
                        var key = new Keyframe(k.time - splitTime, k.value);
                        key.inTangent = k.inTangent;
                        key.outTangent = k.outTangent;
                        key.inWeight = k.inWeight;
                        key.outWeight = k.outWeight;
                        key.weightedMode = k.weightedMode;
                        return key;
                    }).ToList();

                    var boundaryValue = curve.Evaluate(splitTime);
                    var hasBoundaryKey = rightKeys.Any(k => Mathf.Approximately(k.time, 0));

                    if (!hasBoundaryKey)
                    {
                        rightKeys.Insert(0, new Keyframe(0, boundaryValue));
                        rightKeys.Sort((a, b) => a.time.CompareTo(b.time));
                    }

                    if (rightKeys.Count == 0)
                        rightKeys.Add(new Keyframe(0, boundaryValue));

                    newCurve = new AnimationCurve(rightKeys.ToArray());
                }

                AnimationUtility.SetEditorCurve(dest, binding, newCurve);
            }
        }

        static void SplitObjectCurves(AnimationClip source, AnimationClip dest, float splitTime, bool isLeft)
        {
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(source);

            foreach (var binding in bindings)
            {
                var keyframes = AnimationUtility.GetObjectReferenceCurve(source, binding);
                if (keyframes == null || keyframes.Length == 0)
                    continue;

                ObjectReferenceKeyframe[] newKeyframes;

                var boundaryValue = keyframes.LastOrDefault(k => k.time <= splitTime).value;
                if (boundaryValue == null)
                    boundaryValue = keyframes.FirstOrDefault(k => k.time >= splitTime).value;

                if (isLeft)
                {
                    var leftKeys = keyframes.Where(k => k.time <= splitTime).ToList();
                    if (!leftKeys.Any(k => Mathf.Approximately(k.time, splitTime)) && boundaryValue != null)
                        leftKeys.Add(new ObjectReferenceKeyframe { time = splitTime, value = boundaryValue });

                    if (leftKeys.Count == 0 && boundaryValue != null)
                        leftKeys.Add(new ObjectReferenceKeyframe { time = splitTime, value = boundaryValue });

                    newKeyframes = leftKeys.OrderBy(k => k.time).ToArray();
                }
                else
                {
                    var rightKeys = keyframes.Where(k => k.time >= splitTime).Select(k =>
                        new ObjectReferenceKeyframe { time = k.time - splitTime, value = k.value }).ToList();

                    if (!rightKeys.Any(k => Mathf.Approximately(k.time, 0)) && boundaryValue != null)
                        rightKeys.Insert(0, new ObjectReferenceKeyframe { time = 0, value = boundaryValue });

                    if (rightKeys.Count == 0 && boundaryValue != null)
                        rightKeys.Add(new ObjectReferenceKeyframe { time = 0, value = boundaryValue });

                    newKeyframes = rightKeys.OrderBy(k => k.time).ToArray();
                }

                if (newKeyframes.Length > 0)
                    AnimationUtility.SetObjectReferenceCurve(dest, binding, newKeyframes);
            }
        }

        #endregion

        #region Asset property copy

        static void CopyPlayableAssetProperties(AnimationPlayableAsset source, AnimationPlayableAsset dest)
        {
            if (source == null || dest == null)
                return;

            dest.position = source.position;
            dest.eulerAngles = source.eulerAngles;
            dest.useTrackMatchFields = source.useTrackMatchFields;
            dest.matchTargetFields = source.matchTargetFields;
            dest.removeStartOffset = source.removeStartOffset;
            dest.applyFootIK = source.applyFootIK;
            dest.loop = source.loop;
        }

        #endregion
    }
}
