using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class CinemachineCameraSetter: MonoBehaviour
    {
        [SerializeField] CinemachineBrain m_CinemachineBrain;
        [SerializeField] CinemachineCamera m_CinemachineCamera;
        void Start()
        {
            // Initializing the start position of the player.
            transform.position = new Vector3(CinemachineBrain.ActiveBrainCount * 2, 2, 0);

            // Shift one bit per brain Count.
            m_CinemachineBrain.ChannelMask = (OutputChannel.Channels)(1 << CinemachineBrain.ActiveBrainCount);
            m_CinemachineCamera.OutputChannel.Value = (OutputChannel.Channels)(1 << CinemachineBrain.ActiveBrainCount);
        }
    }
}