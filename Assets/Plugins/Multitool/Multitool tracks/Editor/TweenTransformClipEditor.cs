using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace MultitoolTracks
{
    [CustomTimelineEditor(typeof(TweenTransformClip))]
    public class TweenTransformClipEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.displayClipName = string.IsNullOrEmpty(GetTargetLabel(clip));
            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var label = GetTargetLabel(clip);
            if (!string.IsNullOrEmpty(label))
                EditorGUI.LabelField(region.position, label, EditorStyles.miniLabel);
        }

        static string GetTargetLabel(TimelineClip clip)
        {
            var asset = clip.asset as TweenTransformClip;
            if (asset == null)
                return null;

            var director = TimelineEditor.inspectedDirector;
            if (director == null)
                return null;

            var startTarget = director.GetReferenceValue(asset.startTarget.exposedName, out bool startFound) as Transform;
            var endTarget = director.GetReferenceValue(asset.endTarget.exposedName, out bool endFound) as Transform;

            if (startFound && startTarget != null && endFound && endTarget != null)
                return $"{startTarget.gameObject.name} → {endTarget.gameObject.name}";
            if (startFound && startTarget != null)
                return startTarget.gameObject.name;
            if (endFound && endTarget != null)
                return endTarget.gameObject.name;

            return null;
        }
    }
}
