using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace Cinemachine.Editor
{
    [CustomTimelineEditor(typeof(CinemachineTrack))]
    class CinemachineTrackEditor : TrackEditor
    {
        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            base.OnCreate(track, copiedFrom);
            if (CinemachineCore.Instance.BrainCount == 1)
                TimelineEditor.inspectedDirector.SetGenericBinding(track, CinemachineCore.Instance.GetActiveBrain(0));
        }
    }
}
