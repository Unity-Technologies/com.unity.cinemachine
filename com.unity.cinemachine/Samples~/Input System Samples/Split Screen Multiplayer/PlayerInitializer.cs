using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class PlayerInitializer: MonoBehaviour
    {
        [SerializeField] CinemachineBrain m_CinemachineBrain;
        [SerializeField] CinemachineCamera m_CinemachineCamera;
        void Start()
        {
            // Initializing the start position of the player.
            transform.position = new Vector3(PlayerCounter.PlayerCount * 2, 2, 0);

            // Shift one bit per brain Count.
            m_CinemachineBrain.ChannelMask = (OutputChannels)(1 << PlayerCounter.PlayerCount);
            m_CinemachineCamera.OutputChannel = (OutputChannels)(1 << PlayerCounter.PlayerCount);
        }
    }
}