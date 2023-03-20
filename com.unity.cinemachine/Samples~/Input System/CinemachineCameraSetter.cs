using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraSetter : MonoBehaviour
{
    [SerializeField]
    CinemachineBrain m_CinemachineBrain;
    [SerializeField]
    CinemachineCamera m_CinemachineCamera;

    void Start()
    {
        transform.position = new Vector3(CinemachineCore.Instance.BrainCount, 2, 0);// Increment to the next channel based on the brain count for the CinemachineBrain and the CinemachineCamera.
        m_CinemachineBrain.ChannelMask = (OutputChannel.Channels) (1 << CinemachineCore.Instance.BrainCount); // Shift one bit per brain Count.
        m_CinemachineCamera.OutputChannel.Value = (OutputChannel.Channels) (1 << CinemachineCore.Instance.BrainCount);
    }
}
