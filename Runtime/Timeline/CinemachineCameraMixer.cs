#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine.Playables;
using Cinemachine;

internal sealed class CinemachineCameraMixer : CinemachineMixerBase<CinemachineTimelineCamera>
{
    protected override void ProcessBlendFrame(FrameData info, CinemachineTimelineCamera target, ICinemachineCamera camA, ICinemachineCamera camB, float blendWeight)
    {
        target.SetBlendParameters(camA, camB, blendWeight);
    }
}
#endif
