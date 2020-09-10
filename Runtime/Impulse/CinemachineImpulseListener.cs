using UnityEngine;
 
namespace Cinemachine
{
    /// <summary>
    /// An extension for Cinemachine Virtual Camera which post-processes
    /// the final position of the virtual camera.  It listens for CinemachineImpulse
    /// signals on the specified channels, and moves the camera in response to them.
    /// </summary>
    [SaveDuringPlay]
    [AddComponentMenu("")] // Hide in menu
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [HelpURL(Documentation.BaseURL + "manual/CinemachineImpulseListener.html")]
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
        [Tooltip("Gain to apply to the Impulse signal.  1 is normal strength.  "
            + "Setting this to 0 completely mutes the signal.")]
        public float m_Gain = 1;

        /// <summary>
        /// Enable this to perform distance calculation in 2D (ignore Z).
        /// </summary>
        [Tooltip("Enable this to perform distance calculation in 2D (ignore Z)")]
        public bool m_Use2DDistance = false;

        // GML todo: add reaction configuration params here
 
        /// <summary>React to any detected impulses</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Aim)
            {
                Vector3 impulsePos = Vector3.zero;
                Quaternion impulseRot = Quaternion.identity;
                if (CinemachineImpulseManager.Instance.GetImpulseAt(
                    state.FinalPosition, m_Use2DDistance, m_ChannelMask, 
                    out impulsePos, out impulseRot))
                {
                    state.PositionCorrection += impulsePos * -m_Gain;
                    impulseRot = Quaternion.SlerpUnclamped(Quaternion.identity, impulseRot, -m_Gain);
                    state.OrientationCorrection = state.OrientationCorrection * impulseRot;
                }
            }
        }
    }
}
