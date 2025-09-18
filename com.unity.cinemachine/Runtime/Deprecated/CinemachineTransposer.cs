#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using System;
using Unity.Cinemachine.TargetTracking;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use CinemachineOrbitalFollow instead.
    /// </summary>
    [Obsolete("CinemachineTransposer has been deprecated. Use CinemachineFollow instead")]
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineTransposer : CinemachineComponentBase
    {
        /// <summary>The coordinate space to use when interpreting the offset from the target</summary>
        [Tooltip("The coordinate space to use when interpreting the offset from the target.  This is also "
            + "used to set the camera's Up vector, which will be maintained when aiming the camera.")]
        public BindingMode m_BindingMode = BindingMode.LockToTargetWithWorldUp;

        /// <summary>The distance which the transposer will attempt to maintain from the transposer subject</summary>
        [Tooltip("The distance vector that the transposer will attempt to maintain from the Follow target")]
        public Vector3 m_FollowOffset = Vector3.back * 10f;

        /// <summary>How aggressively the camera tries to maintain the offset in the X-axis.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// x-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain the offset in the X-axis.  Small numbers "
            + "are more responsive, rapidly translating the camera to keep the target's x-axis offset.  "
            + "Larger numbers give a more heavy slowly responding camera. Using different settings per "
            + "axis can yield a wide range of camera behaviors.")]
        public float m_XDamping = 1f;

        /// <summary>How aggressively the camera tries to maintain the offset in the Y-axis.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// y-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain the offset in the Y-axis.  Small numbers "
            + "are more responsive, rapidly translating the camera to keep the target's y-axis offset.  "
            + "Larger numbers give a more heavy slowly responding camera. Using different settings per "
            + "axis can yield a wide range of camera behaviors.")]
        public float m_YDamping = 1f;

        /// <summary>How aggressively the camera tries to maintain the offset in the Z-axis.
        /// Small numbers are more responsive, rapidly translating the camera to keep the
        /// target's z-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain the offset in the Z-axis.  "
            + "Small numbers are more responsive, rapidly translating the camera to keep the "
            + "target's z-axis offset.  Larger numbers give a more heavy slowly responding camera. "
            + "Using different settings per axis can yield a wide range of camera behaviors.")]
        public float m_ZDamping = 1f;

        /// <summary>How to calculate the angular damping for the target orientation.
        /// Use Quaternion if you expect the target to take on very steep pitches, which would
        /// be subject to gimbal lock if Eulers are used.</summary>
        public AngularDampingMode m_AngularDampingMode = AngularDampingMode.Euler;

        /// <summary>How aggressively the camera tries to track the target rotation's X angle.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's X angle.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_PitchDamping = 0;

        /// <summary>How aggressively the camera tries to track the target rotation's Y angle.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's Y angle.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_YawDamping = 0;

        /// <summary>How aggressively the camera tries to track the target rotation's Z angle.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's Z angle.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_RollDamping = 0f;

        /// <summary>How aggressively the camera tries to track the target's orientation.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target's orientation.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_AngularDamping = 0f;

        /// <summary>
        /// Helper object that tracks the Follow target, with damping
        /// </summary>
        Tracker m_TargetTracker;

        /// <summary>Get the damping settings</summary>
        protected TrackerSettings TrackerSettings => new TrackerSettings
        {
            BindingMode = m_BindingMode,
            PositionDamping = new Vector3(m_XDamping, m_YDamping, m_ZDamping),
            RotationDamping = new Vector3(m_PitchDamping, m_YawDamping, m_RollDamping),
            AngularDampingMode = m_AngularDampingMode,
            QuaternionDamping = m_AngularDamping
        };

        /// <summary>Derived classes should call this from their OnValidate() implementation</summary>
        protected virtual void OnValidate()
        {
            m_FollowOffset = EffectiveOffset;
        }

        /// <summary>Hide the offset in int inspector.  Used by FreeLook.</summary>
        internal bool HideOffsetInInspector { get; set; }

        /// <summary>Get the target offset, with sanitization</summary>
        public Vector3 EffectiveOffset
        {
            get
            {
                Vector3 offset = m_FollowOffset;
                if (m_BindingMode == BindingMode.LazyFollow)
                {
                    offset.x = 0;
                    offset.z = -Mathf.Abs(offset.z);
                }
                return offset;
            }
        }

        /// <summary>True if component is enabled and has a valid Follow target</summary>
        public override bool IsValid { get { return enabled && FollowTarget != null; } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Body; } }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => TrackerSettings.GetMaxDampTime();

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            m_TargetTracker.InitStateInfo(this, deltaTime, m_BindingMode, Vector3.zero, curState.ReferenceUp);
            if (IsValid)
            {
                Vector3 offset = EffectiveOffset;
                m_TargetTracker.TrackTarget(
                    this, deltaTime, curState.ReferenceUp, offset, TrackerSettings, Vector3.zero, ref curState,
                    out Vector3 pos, out Quaternion orient);
                offset = orient * offset;

                curState.ReferenceUp = orient * Vector3.up;

                // Respect minimum target distance on XZ plane
                var targetPosition = FollowTargetPosition;
                pos += m_TargetTracker.GetOffsetForMinimumTargetDistance(
                    this, pos, offset, curState.RawOrientation * Vector3.forward,
                    curState.ReferenceUp, targetPosition);

                curState.RawPosition = pos + offset;
            }
        }

        /// <summary>This is called to notify the user that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == FollowTarget)
                m_TargetTracker.OnTargetObjectWarped(positionDelta);
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            base.ForceCameraPosition(pos, rot);
            var state = VcamState;
            state.RawPosition = pos;
            state.RawOrientation = rot;
            state.PositionCorrection = Vector3.zero;
            state.OrientationCorrection = Quaternion.identity;
            m_TargetTracker.OnForceCameraPosition(this, m_BindingMode, Vector3.zero, ref state);
        }

        internal Quaternion GetReferenceOrientation(Vector3 up)
        {
            var state = VcamState;
            return m_TargetTracker.GetReferenceOrientation(this, m_BindingMode, Vector3.zero, up, ref state);
        }

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        /// <param name="worldUp">Current effective world up</param>
        /// <returns>The position of the Follow target</returns>
        internal virtual Vector3 GetTargetCameraPosition(Vector3 worldUp)
        {
            if (!IsValid)
                return Vector3.zero;
            var state = VcamState;
            return FollowTargetPosition + m_TargetTracker.GetReferenceOrientation(
                this, m_BindingMode, Vector3.zero, worldUp, ref state) * EffectiveOffset;
        }

        // Helper to upgrade to CM3
        internal void UpgradeToCm3(CinemachineFollow c)
        {
            c.FollowOffset = m_FollowOffset;
            c.TrackerSettings = TrackerSettings;
        }
    }
}
#endif
