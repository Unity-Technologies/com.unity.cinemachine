using UnityEngine;
using Unity.Cinemachine.TargetTracking;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Body section of the component pipeline.
    /// Its job is to position the camera in a fixed relationship to the vcam's Tracking
    /// Target object, with offsets and damping.
    ///
    /// This component will only change the camera's position in space.  It will not
    /// re-orient or otherwise aim the camera.  To to that, you need to instruct
    /// the camera in the Aim section of its pipeline.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Cinemachine Follow")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.Tracking)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineFollow.html")]
    public class CinemachineFollow : CinemachineComponentBase
    {
        /// <summary>Settings to control damping for target tracking.</summary>
        public TrackerSettings TrackerSettings = TrackerSettings.Default;

        /// <summary>The distance which the camera will attempt to maintain from the tracking target</summary>
        [Tooltip("The distance vector that the camera will attempt to maintain from the tracking target")]
        public Vector3 FollowOffset = Vector3.back * 10f;

        Tracker m_TargetTracker;

        /// <summary>Derived classes should call this from their OnValidate() implementation</summary>
        void OnValidate()
        {
            FollowOffset = EffectiveOffset;
            TrackerSettings.Validate();
        }

        private void Reset()
        {
            FollowOffset = Vector3.back * 10f;
            TrackerSettings = TrackerSettings.Default;
        }

        /// <summary>Get the target offset, with sanitization</summary>
        internal Vector3 EffectiveOffset
        {
            get
            {
                Vector3 offset = FollowOffset;
                if (TrackerSettings.BindingMode == BindingMode.LazyFollow)
                {
                    offset.x = 0;
                    offset.z = -Mathf.Abs(offset.z);
                }
                return offset;
            }
        }

        /// <summary>True if component is enabled and has a valid Follow target</summary>
        public override bool IsValid => enabled && FollowTarget != null;

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
            m_TargetTracker.InitStateInfo(this, deltaTime, TrackerSettings.BindingMode, Vector3.zero,curState.ReferenceUp);
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
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            base.ForceCameraPosition(pos, rot);
            var state = VcamState;
            state.RawPosition = pos;
            state.RawOrientation = rot;
            state.PositionCorrection = Vector3.zero;
            state.OrientationCorrection = Quaternion.identity;
            m_TargetTracker.OnForceCameraPosition(this, TrackerSettings.BindingMode, Vector3.zero, ref state);
        }

        internal Quaternion GetReferenceOrientation(Vector3 up)
        {
            var state = VcamState;
            return m_TargetTracker.GetReferenceOrientation(this, TrackerSettings.BindingMode, Vector3.zero, up, ref state);
        }

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        /// <param name="worldUp">Current effective world up</param>
        /// <returns>The position of the Follow target</returns>
        internal Vector3 GetDesiredCameraPosition(Vector3 worldUp)
        {
            if (!IsValid)
                return Vector3.zero;
            var state = VcamState;
            return FollowTargetPosition + m_TargetTracker.GetReferenceOrientation(
                this, TrackerSettings.BindingMode, Vector3.zero, worldUp, ref state) * EffectiveOffset;
        }
    }
}
