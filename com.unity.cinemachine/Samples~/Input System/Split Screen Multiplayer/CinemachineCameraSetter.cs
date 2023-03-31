using Unity.Cinemachine;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class CinemachineCameraSetter : MonoBehaviour
    {
        [SerializeField] CinemachineBrain m_CinemachineBrain;
        [SerializeField] CinemachineCamera m_CinemachineCamera;

        void Start()
        {
            transform.position =
                new Vector3(CinemachineBrain.ActiveBrainCount, 2,
                    0); // Increment to the next channel based on the brain count for the CinemachineBrain and the CinemachineCamera.
            m_CinemachineBrain.ChannelMask =
                (OutputChannel.Channels)(1 << CinemachineBrain.ActiveBrainCount); // Shift one bit per brain Count.
            m_CinemachineCamera.OutputChannel.Value = (OutputChannel.Channels)(1 << CinemachineBrain.ActiveBrainCount);
        }
    }
}
