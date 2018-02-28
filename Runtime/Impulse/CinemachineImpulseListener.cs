using UnityEngine;
 
namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera which post-processes
    /// the final position of the virtual camera.  It listens for CinemachineImpulse
    /// signals on the specified channels, and moves the camera in response to them.
    /// </summary>
    [ExecuteInEditMode]
    [SaveDuringPlay]
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineImpulseListener : CinemachineExtension
    {
        /// <summary>
        /// Impulse events on channels not included in the mask will be ignored.
        /// </summary>
        [Tooltip("Impulse events on channels not included in the mask will be ignored.")]
        [CinemachineImpulseChannelProperty]
        public int m_ChannelMask = 1;

        /// <summary>
        /// Gain to apply to the Impulse signal.
        /// </summary>
        [Tooltip("Gain to apply to the Impulse signal.  1 is normal.  Setting this to 0 completely mutes the signal.")]
        public float m_Gain = 1;

        // GML todo: add reaction configuration params here
 
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Aim)
            {
                Vector3 impulse = CinemachineImpulseManager.Instance.GetImpulseAt(
                    state.FinalPosition, m_ChannelMask);
                state.PositionCorrection += impulse * -m_Gain;
            }
        }
    }
}
