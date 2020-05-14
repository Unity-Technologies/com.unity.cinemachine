#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif

#if CINEMACHINE_TIMELINE && UNITY_2019_2_OR_NEWER

using UnityEngine.Timeline;
using UnityEditor.Timeline;
using Cinemachine.Editor;
using Cinemachine;
using UnityEditor;
using UnityEngine;

[CustomTimelineEditor(typeof(CinemachineShot))]
public class CinemachineShotClipEditor : ClipEditor
{
    public override ClipDrawOptions GetClipOptions(TimelineClip clip)
    {
        var shotClip = (CinemachineShot) clip.asset;
        var clipOptions = base.GetClipOptions(clip);
        if (shotClip != null)
        {
            var director = TimelineEditor.inspectedDirector;
            if (director != null)
            {
                var vcam = shotClip.VirtualCamera.Resolve(director);
                if (vcam == null)
                    clipOptions.errorText = "A virtual camera must be assigned";
                else
                    clipOptions.tooltip = vcam.Name;
            }
        }
        return clipOptions;
    }

    public override void OnClipChanged(TimelineClip clip)
    {
        var shotClip = (CinemachineShot) clip.asset;
        if (shotClip == null)
            return;
        if (shotClip.DisplayName != null && shotClip.DisplayName.Length != 0)
            clip.displayName = shotClip.DisplayName;
        else
        {
            var director = TimelineEditor.inspectedDirector;
            if (director != null)
            {
                var vcam = shotClip.VirtualCamera.Resolve(director);
                if (vcam != null)
                    clip.displayName = vcam.Name;
            }
        }
    }

    public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
    {
        base.OnCreate(clip, track, clonedFrom);
        if (CinemachineShotEditor.AutoCreateShotFromSceneView)
        {
            var asset = clip.asset as CinemachineShot;
            var vcam = CinemachineShotEditor.CreateStaticVcamFromSceneView();
            var d = TimelineEditor.inspectedDirector;
            if (d != null && d.GetReferenceValue(asset.VirtualCamera.exposedName, out bool idValid) == null)
            {
                asset.VirtualCamera.exposedName = System.Guid.NewGuid().ToString();
                d.SetReferenceValue(asset.VirtualCamera.exposedName, vcam);
            }
        }
    }

    public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
    {
        base.DrawBackground(clip, region);
        if (TargetPositionCache.CacheMode != TargetPositionCache.Mode.Disabled)
        {
            var range = TargetPositionCache.CacheTimeRange;
            if (!range.IsEmpty)
            {
                // Clip range to rect
                float start = (float)region.startTime;
                float end = (float)region.endTime;
                range.Start = Mathf.Max((float)clip.ToLocalTime(range.Start), start);
                range.End = Mathf.Min((float)clip.ToLocalTime(range.End), end);
                
                var r = region.position;
                var a = r.x + Mathf.Lerp(0, r.width, range.Start / (end - start));
                var b = r.x + Mathf.Lerp(0, r.width, range.End / (end - start));
                r.x = a; r.width = b-a;
                var color = Color.Lerp(GetDefaultHighlightColor(clip), Color.gray, 0.8f);
                color.a = 0.5f;
                EditorGUI.DrawRect(r, color);
            }
        }
    }
}


#endif
