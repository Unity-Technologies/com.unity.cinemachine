#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine.Playables;
using Cinemachine;

internal sealed class CinemachineCameraMixer : CinemachineMixerBase<CinemachineTimelineCamera>
{
    private CinemachineTimelineCamera m_TimelineCamera;

    protected override void ProcessBlendFrame(FrameData info, CinemachineTimelineCamera target, ICinemachineCamera camA, ICinemachineCamera camB, float blendWeight)
    {
        m_TimelineCamera = target;
        target.SetBlendParameters(camA, camB, blendWeight);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        base.OnPlayableDestroy(playable);

        if (m_TimelineCamera != null)
            m_TimelineCamera.SetBlendParameters(null, null, 0);
    }
}
#endif
