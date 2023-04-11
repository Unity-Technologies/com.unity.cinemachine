using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class PlayerInitializer: MonoBehaviour
    {
        [SerializeField] CinemachineBrain m_CinemachineBrain;
        [SerializeField] CinemachineCamera m_CinemachineCamera;
        [Tooltip("Used to discard brains that are not linked to the player.")]
        [SerializeField] int m_NonPlayerBrain = 1;
        void Start()
        {
            // Increment to the next channel based on the brain count for the CinemachineBrain and the CinemachineCamera.
            transform.position = new Vector3(CinemachineBrain.ActiveBrainCount, 2, 0);

            // Shift one bit per brain Count.
            m_CinemachineBrain.ChannelMask = (OutputChannel.Channels)(1 << CinemachineBrain.ActiveBrainCount);
            m_CinemachineCamera.OutputChannel.Value = (OutputChannel.Channels)(1 << CinemachineBrain.ActiveBrainCount);

            // Re-parenting and naming the Camera to have a nice scene.
            var playerId = CinemachineBrain.ActiveBrainCount - m_NonPlayerBrain;
            transform.name = "Player " + playerId;
            var brainTransform = m_CinemachineBrain.transform;
            brainTransform.parent = null;
            brainTransform.name = $"{brainTransform.name} Player {playerId}";
            var cinemachineCameraTransform = m_CinemachineCamera.transform;
            cinemachineCameraTransform.parent = null;
            cinemachineCameraTransform.name = $"{cinemachineCameraTransform.name} Player {playerId}";
        }
    }
}