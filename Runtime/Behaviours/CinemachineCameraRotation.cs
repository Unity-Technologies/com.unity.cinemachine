using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that adds a final offset to the camera
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [SaveDuringPlay]
    public class CinemachineCameraRotation : CinemachineExtension
    {
        [Tooltip("When to apply the offset")]
        public CinemachineCore.Stage m_ApplyAfter = CinemachineCore.Stage.Body;

        /// <summary>If set, update always, otherwise update only when playing and vcam is Live</summary>
        [Tooltip("If set, update always, otherwise update only when playing and vcam is Live")]
        public bool m_UpdateAlways;

        /// <summary>The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation</summary>
        [Tooltip("The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation")]
        public AxisBase m_HorizontalAxis;

        /// <summary>Controls the input method for the horizontal axis</summary>
        [Tooltip("Controls the input method for the horizontal axis")]
        public CinemachineInputAxisDriver m_HorizontalInput;

        /// <summary>Controls how automatic recentering of the Horizontal axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Horizontal axis is accomplished")]
        public AxisState.Recentering m_HorizontalRecentering;

        /// <summary>The Vertical axis.  Value is -90..90. Controls the vertical orientation</summary>
        [Tooltip("The Vertical axis.  Value is -90..90. Controls the vertical orientation")]
        public AxisBase m_VerticalAxis;

        /// <summary>Controls the input method for the vertical axis</summary>
        [Tooltip("Controls the input method for the vertical axis")]
        public CinemachineInputAxisDriver m_VerticalInput;

        /// <summary>Controls how automatic recentering of the Vertical axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Vertical axis is accomplished")]
        public AxisState.Recentering m_VerticalRecentering;

        void OnValidate()
        {
            m_HorizontalAxis.Validate();
            m_HorizontalInput.Validate();
            m_HorizontalRecentering.Validate();

            m_VerticalAxis.Validate();
            m_VerticalInput.Validate();
            m_VerticalRecentering.Validate();
        }

        private void Reset()
        {
            m_HorizontalAxis = new AxisBase { m_Value = 0, m_MinValue = -180, m_MaxValue = 180, m_Wrap = true };
            m_HorizontalInput = new CinemachineInputAxisDriver
            {
                multiplier = 2f,
                accelTime = 0.5f,
                decelTime = 0.5f,
                name = "Mouse X",
            };
            m_HorizontalRecentering = new AxisState.Recentering(false, 1, 2);

            m_VerticalAxis = new AxisBase { m_Value = 0, m_MinValue = -70, m_MaxValue = 70, m_Wrap = false };
            m_VerticalInput = new CinemachineInputAxisDriver
            {
                multiplier = -2,
                accelTime = 0.5f,
                decelTime = 0.5f,
                name = "Mouse Y",
            };
            m_VerticalRecentering = new AxisState.Recentering(false, 1, 2);
        }

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation must be called by any overridden method.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True to request a vcam update of internal state</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            var vcam = VirtualCamera as CinemachineVirtualCamera;
            if (fromCam != null && vcam != null && vcam.m_Transitions.m_InheritPosition)
            {
                var state = vcam.State;
                Vector3 up = state.ReferenceUp;
                Quaternion targetRot = fromCam.State.RawOrientation;
                Vector3 fwd = Vector3.forward;
                Transform parent = VirtualCamera.transform.parent;
                if (parent != null)
                    fwd = parent.rotation * fwd;

                m_HorizontalAxis.m_Value = 0;
                m_HorizontalInput.inputValue = 0;
                Vector3 targetFwd = targetRot * Vector3.forward;
                Vector3 a = fwd.ProjectOntoPlane(up);
                Vector3 b = targetFwd.ProjectOntoPlane(up);
                if (!a.AlmostZero() && !b.AlmostZero())
                    m_HorizontalAxis.m_Value = Vector3.SignedAngle(a, b, up);

                m_VerticalAxis.m_Value = 0;
                m_VerticalInput.inputValue = 0;
                fwd = Quaternion.AngleAxis(m_HorizontalAxis.m_Value, up) * fwd;
                Vector3 right = Vector3.Cross(up, fwd);
                if (!right.AlmostZero())
                    m_VerticalAxis.m_Value = Vector3.SignedAngle(fwd, targetFwd, right);
                return true;
            }
            return false;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == m_ApplyAfter)
            {
                // Only read joystick when game is playing
                if (m_UpdateAlways || (deltaTime >= 0 && CinemachineCore.Instance.IsLive(VirtualCamera)))
                {
                    bool changed = m_HorizontalInput.Update(deltaTime, ref m_HorizontalAxis);
                    if (m_VerticalInput.Update(deltaTime, ref m_VerticalAxis))
                        changed = true;
                    if (changed)
                    {
                        m_HorizontalRecentering.CancelRecentering();
                        m_VerticalRecentering.CancelRecentering();
                    }
                }
                m_HorizontalAxis.m_Value = m_HorizontalRecentering.DoRecentering(m_HorizontalAxis.m_Value, deltaTime, 0);
                m_VerticalAxis.m_Value = m_VerticalRecentering.DoRecentering(m_VerticalAxis.m_Value, deltaTime, 0);

                // If we have a transform parent, then apply POV in the local space of the parent
                Quaternion rot = Quaternion.Euler(m_VerticalAxis.m_Value, m_HorizontalAxis.m_Value, 0);
                Transform parent = VirtualCamera.transform.parent;
                if (parent != null)
                    rot = parent.rotation * rot;
                else
                    rot = rot * Quaternion.FromToRotation(Vector3.up, state.ReferenceUp);
                state.RawOrientation = rot;
            }
        }
    }
}
