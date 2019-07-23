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

        /// <summary>The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation</summary>
        [Tooltip("The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation")]
        [AxisStateProperty]
        public AxisState m_HorizontalAxis;

        /// <summary>The Vertical axis.  Value is -90..90. Controls the vertical orientation</summary>
        [Tooltip("The Vertical axis.  Value is -90..90. Controls the vertical orientation")]
        [AxisStateProperty]
        public AxisState m_VerticalAxis;

        void OnValidate()
        {
            m_HorizontalAxis.Validate();
            m_HorizontalAxis.HasRecentering = true;
            m_HorizontalAxis.m_Recentering.Validate();

            m_VerticalAxis.Validate();
            m_VerticalAxis.HasRecentering = true;
            m_VerticalAxis.m_Recentering.Validate();
        }

        private void Reset()
        {
            m_HorizontalAxis = new AxisState
            {
                Value = 0,
                m_MinValue = -180,
                m_MaxValue = 180,
                m_Wrap = true,
                m_SpeedMode = AxisState.SpeedMode.ValueMultiplier,
                m_MaxSpeed = 2f,
                m_AccelTime = 0.5f,
                m_DecelTime = 0.5f,
                m_InputAxisName = "Mouse X",
                m_Recentering = new AxisState.Recentering(false, 1, 2),
                HasRecentering = true
            };

            m_VerticalAxis = new AxisState
            {
                Value = 0,
                m_MinValue = -70,
                m_MaxValue = 70,
                m_Wrap = false,
                m_SpeedMode = AxisState.SpeedMode.ValueMultiplier,
                m_MaxSpeed = -2,
                m_AccelTime = 0.5f,
                m_DecelTime = 0.5f,
                m_InputAxisName = "Mouse Y",
                m_Recentering = new AxisState.Recentering(false, 1, 2),
                HasRecentering = true
            };
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
            m_HorizontalAxis.Value = m_HorizontalAxis.m_Recentering.DoRecentering(m_HorizontalAxis.Value, deltaTime, 0);
            m_VerticalAxis.Value = m_VerticalAxis.m_Recentering.DoRecentering(m_VerticalAxis.Value, deltaTime, 0);

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

                m_HorizontalAxis.m_InputAxisValue = 0;
                m_HorizontalAxis.m_Recentering.CancelRecentering();
                Vector3 targetFwd = targetRot * Vector3.forward;
                Vector3 a = fwd.ProjectOntoPlane(up);
                Vector3 b = targetFwd.ProjectOntoPlane(up);
                if (!a.AlmostZero() && !b.AlmostZero())
                    m_HorizontalAxis.Value = Vector3.SignedAngle(a, b, up);

                m_VerticalAxis.m_InputAxisValue = 0;
                m_VerticalAxis.m_Recentering.CancelRecentering();
                fwd = Quaternion.AngleAxis(m_HorizontalAxis.Value, up) * fwd;
                Vector3 right = Vector3.Cross(up, fwd);
                if (!right.AlmostZero())
                    m_VerticalAxis.Value = Vector3.SignedAngle(fwd, targetFwd, right);
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
                if (deltaTime >= 0 && CinemachineCore.Instance.IsLive(VirtualCamera))
                {
                    bool changed = m_HorizontalAxis.Update(deltaTime);
                    if (m_VerticalAxis.Update(deltaTime))
                        changed = true;
                    if (changed)
                    {
                        m_HorizontalAxis.m_Recentering.CancelRecentering();
                        m_VerticalAxis.m_Recentering.CancelRecentering();
                    }
                    m_HorizontalAxis.Value = m_HorizontalAxis.m_Recentering.DoRecentering(m_HorizontalAxis.Value, deltaTime, 0);
                    m_VerticalAxis.Value = m_VerticalAxis.m_Recentering.DoRecentering(m_VerticalAxis.Value, deltaTime, 0);
                }

                // If we have a transform parent, then apply POV in the local space of the parent
                Quaternion rot = Quaternion.Euler(m_VerticalAxis.Value, m_HorizontalAxis.Value, 0);
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
