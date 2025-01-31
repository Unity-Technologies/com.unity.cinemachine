#if !CINEMACHINE_NO_CM2_SUPPORT
using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use CinemachinePanTilt instead.
    /// </summary>
    [Obsolete("CinemachinePOV has been deprecated. Use CinemachinePanTilt instead")]
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Aim)]
    public class CinemachinePOV : CinemachineComponentBase, CinemachineFreeLookModifier.IModifierValueSource, AxisState.IRequiresInput
    {
        /// <summary>
        /// Defines the recentering target: Recentering goes here
        /// </summary>
        public enum RecenterTargetMode
        {
            /// <summary>
            /// Just go to 0
            /// </summary>
            None,

            /// <summary>
            /// Axis angles are relative to Follow target's forward
            /// </summary>
            FollowTargetForward,

            /// <summary>
            /// Axis angles are relative to LookAt target's forward
            /// </summary>
            LookAtTargetForward
        }

        /// <summary>
        /// Defines the recentering target: recentering goes here
        /// </summary>
        public RecenterTargetMode m_RecenterTarget = RecenterTargetMode.None;

        /// <summary>The Vertical axis.  Value is -90..90. Controls the vertical orientation</summary>
        [Tooltip("The Vertical axis.  Value is -90..90. Controls the vertical orientation")]
        public AxisState m_VerticalAxis = new AxisState(-70, 70, false, false, 300f, 0.1f, 0.1f, "Mouse Y", true);

        /// <summary>Controls how automatic recentering of the Vertical axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Vertical axis is accomplished")]
        public AxisState.Recentering m_VerticalRecentering = new AxisState.Recentering(false, 1, 2);

        /// <summary>The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation</summary>
        [Tooltip("The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation")]
        public AxisState m_HorizontalAxis = new AxisState(-180, 180, true, false, 300f, 0.1f, 0.1f, "Mouse X", false);

        /// <summary>Controls how automatic recentering of the Horizontal axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Horizontal axis is accomplished")]
        public AxisState.Recentering m_HorizontalRecentering = new AxisState.Recentering(false, 1, 2);

        /// <summary>Obsolete - no longer used</summary>
        [HideInInspector]
        [Tooltip("Obsolete - no longer used")]
        public bool m_ApplyBeforeBody;

        Quaternion m_PreviousCameraRotation;

        float CinemachineFreeLookModifier.IModifierValueSource.NormalizedModifierValue
        {
            get
            {
                var r = m_VerticalAxis.m_MaxValue - m_VerticalAxis.m_MinValue;
                return (m_VerticalAxis.Value - m_VerticalAxis.m_MinValue) / (r > 0.001f ? r : 1) * 2 - 1;
            }
        }

        /// <summary>True if component is enabled and has a LookAt defined</summary>
        public override bool IsValid { get { return enabled; } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Aim; } }

        private void OnValidate()
        {
            m_VerticalAxis.Validate();
            m_VerticalRecentering.Validate();
            m_HorizontalAxis.Validate();
            m_HorizontalRecentering.Validate();
        }

        /// <summary>
        /// Standard OnEnable call.  Updates the input axis provider.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateInputAxisProvider();
        }

        /// <summary>Returns true if this object requires user input from a IInputAxisProvider.</summary>
        /// <returns>Returns true when input is required.</returns>
        bool AxisState.IRequiresInput.RequiresInput() => true;

        /// <summary>
        /// API for the inspector.  Internal use only
        /// </summary>
        internal void UpdateInputAxisProvider()
        {
            m_HorizontalAxis.SetInputAxisProvider(0, null);
            m_VerticalAxis.SetInputAxisProvider(1, null);
            if (VirtualCamera != null)
            {
                var provider = VirtualCamera.GetComponent<AxisState.IInputAxisProvider>();
                if (provider != null)
                {
                    m_HorizontalAxis.SetInputAxisProvider(0, provider);
                    m_VerticalAxis.SetInputAxisProvider(1, provider);
                }
            }
        }

        /// <summary>Does nothing</summary>
        /// <param name="state">ignored</param>
        /// <param name="deltaTime">ignored</param>
        public override void PrePipelineMutateCameraState(ref CameraState state, float deltaTime) {}

        /// <summary>Applies the axis values and orients the camera accordingly</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for calculating damping.  Not used.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;

            // Only read joystick when game is playing
            if (deltaTime >= 0 && (!VirtualCamera.PreviousStateIsValid || !CinemachineCore.IsLive(VirtualCamera)))
                deltaTime = -1;
            if (deltaTime >= 0)
            {
                if (m_HorizontalAxis.Update(deltaTime))
                    m_HorizontalRecentering.CancelRecentering();
                if (m_VerticalAxis.Update(deltaTime))
                    m_VerticalRecentering.CancelRecentering();
            }
            var recenterTarget = GetRecenterTarget();
            m_HorizontalRecentering.DoRecentering(ref m_HorizontalAxis, deltaTime, recenterTarget.x);
            m_VerticalRecentering.DoRecentering(ref m_VerticalAxis, deltaTime, recenterTarget.y);

            // If we have a transform parent, then apply POV in the local space of the parent
            Quaternion rot = Quaternion.Euler(m_VerticalAxis.Value, m_HorizontalAxis.Value, 0);
            Transform parent = VirtualCamera.transform.parent;
            if (parent != null)
                rot = parent.rotation * rot;
            else
                rot = Quaternion.FromToRotation(Vector3.up, curState.ReferenceUp) * rot;
            curState.RawOrientation = rot;

            if (VirtualCamera.PreviousStateIsValid)
                curState.RotationDampingBypass = curState.RotationDampingBypass
                    * UnityVectorExtensions.SafeFromToRotation(
                        m_PreviousCameraRotation * Vector3.forward,
                        rot * Vector3.forward, curState.ReferenceUp);
            m_PreviousCameraRotation = rot;
        }

        /// <summary>
        /// Get the horizonmtal and vertical angles that correspong to "at rest" position.
        /// </summary>
        /// <returns>X is horizontal angle (rot Y) and Y is vertical angle (rot X)</returns>
        public Vector2 GetRecenterTarget()
        {
            Transform t = null;
            switch (m_RecenterTarget)
            {
                case RecenterTargetMode.FollowTargetForward: t = VirtualCamera.Follow; break;
                case RecenterTargetMode.LookAtTargetForward: t = VirtualCamera.LookAt; break;
                default: break;
            }
            if (t != null)
            {
                var fwd = t.forward;
                Transform parent = VirtualCamera.transform.parent;
                if (parent != null)
                    fwd = parent.rotation * fwd;
                var v = Quaternion.FromToRotation(Vector3.forward, fwd).eulerAngles;
                return new Vector2(NormalizeAngle(v.y), NormalizeAngle(v.x));
            }
            return Vector2.zero;
        }

        // Normalize angle value to [-180, 180] degrees.
        static float NormalizeAngle(float angle)
        {
            return ((angle + 180) % 360) - 180;
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation.
        /// Procedural placement then takes over
        /// </summary>
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            SetAxesForRotation(rot);
        }

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            m_HorizontalRecentering.DoRecentering(ref m_HorizontalAxis, -1, 0);
            m_VerticalRecentering.DoRecentering(ref m_VerticalAxis, -1, 0);
            m_HorizontalRecentering.CancelRecentering();
            m_VerticalRecentering.CancelRecentering();
            if (fromCam != null
                && (VirtualCamera.State.BlendHint & CameraState.BlendHints.InheritPosition) != 0
                && !CinemachineCore.IsLiveInBlend(VirtualCamera))
            {
                SetAxesForRotation(fromCam.State.RawOrientation);
                return true;
            }
            return false;
        }

        void SetAxesForRotation(Quaternion targetRot)
        {
            Vector3 up = VcamState.ReferenceUp;
            Vector3 fwd = Vector3.forward;
            Transform parent = VirtualCamera.transform.parent;
            if (parent != null)
                fwd = parent.rotation * fwd;

            m_HorizontalAxis.Value = 0;
            m_HorizontalAxis.Reset();
            Vector3 targetFwd = targetRot * Vector3.forward;
            Vector3 a = fwd.ProjectOntoPlane(up);
            Vector3 b = targetFwd.ProjectOntoPlane(up);
            if (!a.AlmostZero() && !b.AlmostZero())
                m_HorizontalAxis.Value = Vector3.SignedAngle(a, b, up);

            m_VerticalAxis.Value = 0;
            m_VerticalAxis.Reset();
            fwd = Quaternion.AngleAxis(m_HorizontalAxis.Value, up) * fwd;
            Vector3 right = Vector3.Cross(up, fwd);
            if (!right.AlmostZero())
                m_VerticalAxis.Value = Vector3.SignedAngle(fwd, targetFwd, right);
        }

        // Helper to upgrade to CM3
        internal void UpgradeToCm3(CinemachinePanTilt c)
        {
            c.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.ParentObject;
            c.RecenterTarget = (CinemachinePanTilt.RecenterTargetModes)m_RecenterTarget;

            c.PanAxis.Range = new Vector2(m_HorizontalAxis.m_MinValue, m_HorizontalAxis.m_MaxValue);
            c.PanAxis.Center = 0;
            c.PanAxis.Recentering = new ()
            {
                Enabled = m_HorizontalRecentering.m_enabled,
                Time = m_HorizontalRecentering.m_RecenteringTime,
                Wait = m_HorizontalRecentering.m_WaitTime
            };

            c.TiltAxis.Range = new Vector2(m_VerticalAxis.m_MinValue, m_VerticalAxis.m_MaxValue);
            c.TiltAxis.Center = 0;
            c.TiltAxis.Recentering = new ()
            {
                Enabled = m_VerticalRecentering.m_enabled,
                Time = m_VerticalRecentering.m_RecenteringTime,
                Wait = m_VerticalRecentering.m_WaitTime
            };
        }
    }
}
#endif
