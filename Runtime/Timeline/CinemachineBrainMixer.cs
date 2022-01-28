#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine.Playables;
using Cinemachine;

internal sealed class CinemachineBrainMixer : CinemachineMixerBase<CinemachineBrain>
{
    private ICameraOverrideStack m_BrainOverrideStack;
    private int m_BrainOverrideId = -1;

    protected override void ProcessBlendFrame(FrameData info, CinemachineBrain target, ICinemachineCamera camA, ICinemachineCamera camB, float blendWeight)
	{
        m_BrainOverrideStack = target;
        m_BrainOverrideId = target.SetCameraOverride(m_BrainOverrideId, camA, camB, blendWeight, GetDeltaTime(info.deltaTime));
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		base.OnPlayableDestroy(playable);

        if (m_BrainOverrideStack != null)
            m_BrainOverrideStack.ReleaseCameraOverride(m_BrainOverrideId); // clean up
        m_BrainOverrideId = -1;
    }
}
#endif
