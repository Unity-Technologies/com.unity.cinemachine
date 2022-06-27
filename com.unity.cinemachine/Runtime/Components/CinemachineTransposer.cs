using System;
using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Body section of the component pipeline.
    /// Its job is to position the camera in a fixed relationship to the vcam's Follow
    /// target object, with offsets and damping.
    ///
    /// The Tansposer will only change the camera's position in space.  It will not
    /// re-orient or otherwise aim the camera.  To to that, you need to instruct
    /// the vcam in the Aim section of its pipeline.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineTransposer : CinemachineComponentBase
    {
        /// <summary>
        /// The coordinate space to use when interpreting the offset from the target
        /// </summary>
        public enum BindingMode
        {
            /// <summary>
            /// Camera will be bound to the Follow target using a frame of reference consisting
            /// of the target's local frame at the moment when the virtual camera was enabled,
            /// or when the target was assigned.
            /// </summary>
            LockToTargetOnAssign = 0,
            /// <summary>
            /// Camera will be bound to the Follow target using a frame of reference consisting
            /// of the target's local frame, with the tilt and roll zeroed out.
            /// </summary>
            LockToTargetWithWorldUp = 1,
            /// <summary>
            /// Camera will be bound to the Follow target using a frame of reference consisting
            /// of the target's local frame, with the roll zeroed out.
            /// </summary>
            LockToTargetNoRoll = 2,
            /// <summary>
            /// Camera will be bound to the Follow target using the target's local frame.
            /// </summary>
            LockToTarget = 3,
            /// <summary>Camera will be bound to the Follow target using a world space offset.</summary>
            WorldSpace = 4,
            /// <summary>Offsets will be calculated relative to the target, using Camera-local axes</summary>
            SimpleFollowWithWorldUp = 5
        }
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

        /// <summary>How to calculate the angular damping for the target orientation</summary>
        public enum AngularDampingMode
        {
            /// <summary>Use Euler angles to specify damping values.
            /// Subject to gimbal-lock fwhen pitch is steep.</summary>
            Euler,
            /// <summary>
            /// Use quaternions to calculate angular damping.
            /// No per-channel control, but not susceptible to gimbal-lock</summary>
            Quaternion
        }

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
        protected TargetTracker m_TargetTracker;

        /// <summary>Derived classes should call this from their OnValidate() implementation</summary>
        protected virtual void OnValidate()
        {
            m_FollowOffset = EffectiveOffset;
        }

        /// <summary>Hide the offset in int inspector.  Used by FreeLook.</summary>
        public bool HideOffsetInInspector { get; set; }

        /// <summary>Get the target offset, with sanitization</summary>
        public Vector3 EffectiveOffset
        {
            get
            {
                Vector3 offset = m_FollowOffset;
                if (m_BindingMode == BindingMode.SimpleFollowWithWorldUp)
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
        public override float GetMaxDampTime() 
        { 
            var d = Damping;
            var d2 = m_AngularDampingMode == AngularDampingMode.Euler 
                ? AngularDamping : new Vector3(m_AngularDamping, 0, 0);
            var a = Mathf.Max(d.x, Mathf.Max(d.y, d.z)); 
            var b = Mathf.Max(d2.x, Mathf.Max(d2.y, d2.z)); 
            return Mathf.Max(a, b); 
        }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            m_TargetTracker.InitStateInfo(this, deltaTime, m_BindingMode, curState.ReferenceUp);
            if (IsValid)
            {
                Vector3 offset = EffectiveOffset;
                m_TargetTracker.TrackTarget(
                    this, m_BindingMode, deltaTime, curState.ReferenceUp, offset, 
                    Damping, AngularDamping, m_AngularDamping, m_AngularDampingMode, 
                    out Vector3 pos, out Quaternion orient);
                offset = orient * offset;

                // Respect minimum target distance on XZ plane
                var targetPosition = FollowTargetPosition;
                pos += m_TargetTracker.GetOffsetForMinimumTargetDistance(
                    this, pos, offset, curState.RawOrientation * Vector3.forward,
                    curState.ReferenceUp, targetPosition);
                    
                curState.RawPosition = pos + offset;
                curState.ReferenceUp = orient * Vector3.up;
            }
        }

        /// <summary>This is called to notify the us that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessy.</summary>
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
        /// <param name="pos">Worldspace pposition to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            base.ForceCameraPosition(pos, rot);
            m_TargetTracker.ForceCameraPosition(this, m_BindingMode, pos, rot, EffectiveOffset);
        }
        
        internal Quaternion GetReferenceOrientation(Vector3 up)
        {
            return m_TargetTracker.GetReferenceOrientation(this, m_BindingMode, up);
        }
        
        /// <summary>
        /// Damping speeds for each of the 3 axes of the offset from target
        /// </summary>
        protected Vector3 Damping
        {
            get
            {
                switch (m_BindingMode)
                {
                    case BindingMode.SimpleFollowWithWorldUp:
                        return new Vector3(0, m_YDamping, m_ZDamping);
                    default:
                        return new Vector3(m_XDamping, m_YDamping, m_ZDamping);
                }
            }
        }

        /// <summary>
        /// Damping speeds for each of the 3 axes of the target's rotation
        /// </summary>
        protected Vector3 AngularDamping
        {
            get
            {
                switch (m_BindingMode)
                {
                    case BindingMode.LockToTargetNoRoll:
                        return new Vector3(m_PitchDamping, m_YawDamping, 0);
                    case BindingMode.LockToTargetWithWorldUp:
                        return new Vector3(0, m_YawDamping, 0);
                    case BindingMode.LockToTargetOnAssign:
                    case BindingMode.WorldSpace:
                    case BindingMode.SimpleFollowWithWorldUp:
                        return Vector3.zero;
                    default:
                        return new Vector3(m_PitchDamping, m_YawDamping, m_RollDamping);
                }
            }
        }

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        /// <param name="worldUp">Current effective world up</param>
        /// <returns>The position of the Follow target</returns>
        public virtual Vector3 GetTargetCameraPosition(Vector3 worldUp)
        {
            if (!IsValid)
                return Vector3.zero;
            return FollowTargetPosition + m_TargetTracker.GetReferenceOrientation(this, m_BindingMode, worldUp) * EffectiveOffset;
        }

        /// <summary>
        /// Helper object for implementing target following with damping
        /// </summary>
        public struct TargetTracker
        {
            /// <summary>State information for damping</summary>
            public Vector3 PreviousTargetPosition { get; private set; }

            /// <summary>State information for damping</summary>
            public Quaternion PreviousReferenceOrientation { get; private set; }

            Quaternion m_targetOrientationOnAssign;
            Vector3 m_PreviousOffset;
            Transform m_previousTarget;
        
            /// <summary>Initializes the state for previous frame if appropriate.</summary>
            /// <param name="component">The component caller</param>
            /// <param name="deltaTime">Current effective deltaTime.</param>
            /// <param name="bindingMode">Current binding mode for damping and offset</param>
            /// <param name="up">Current effective world up direction.</param>
            public void InitStateInfo(
                CinemachineComponentBase component, float deltaTime, 
                CinemachineTransposer.BindingMode bindingMode, Vector3 up)
            {
                bool prevStateValid = deltaTime >= 0 && component.VirtualCamera.PreviousStateIsValid;
                if (m_previousTarget != component.FollowTarget || !prevStateValid)
                {
                    m_previousTarget = component.FollowTarget;
                    m_targetOrientationOnAssign = component.FollowTargetRotation;
                }
                if (!prevStateValid)
                {
                    PreviousTargetPosition = component.FollowTargetPosition;
                    PreviousReferenceOrientation = GetReferenceOrientation(component, bindingMode, up);
                }
            }

            /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
            /// <param name="component">The component caller</param>
            /// <param name="bindingMode">Current binding mode for damping and offset</param>
            /// <param name="worldUp">Current effective world up</param>
            /// <returns>The rotation of the Follow target, as understood by the Transposer.  
            /// This is not necessarily the same thing as the actual target rotation</returns>
            public Quaternion GetReferenceOrientation(
                CinemachineComponentBase component, 
                CinemachineTransposer.BindingMode bindingMode, 
                Vector3 worldUp)
            {
                if (bindingMode == BindingMode.WorldSpace)
                    return Quaternion.identity;
                if (component.FollowTarget != null)
                {
                    Quaternion targetOrientation = component.FollowTarget.rotation;
                    switch (bindingMode)
                    {
                        case BindingMode.LockToTargetOnAssign:
                            return m_targetOrientationOnAssign;
                        case BindingMode.LockToTargetWithWorldUp:
                        {
                            Vector3 fwd = (targetOrientation * Vector3.forward).ProjectOntoPlane(worldUp);
                            if (fwd.AlmostZero())
                                break;
                            return Quaternion.LookRotation(fwd, worldUp);
                        }
                        case BindingMode.LockToTargetNoRoll:
                            return Quaternion.LookRotation(targetOrientation * Vector3.forward, worldUp);
                        case BindingMode.LockToTarget:
                            return targetOrientation;
                        case BindingMode.SimpleFollowWithWorldUp:
                        {
                            Vector3 fwd = (component.FollowTargetPosition - component.VcamState.RawPosition).ProjectOntoPlane(worldUp);
                            if (fwd.AlmostZero())
                                break;
                            return Quaternion.LookRotation(fwd, worldUp);
                        }
                    }
                }
                // Gimbal lock situation - use previous orientation if it exists
    #if UNITY_2019_1_OR_NEWER
                return PreviousReferenceOrientation.normalized;
    #else
                return PreviousReferenceOrientation.Normalized();
    #endif
            }

            /// <summary>Positions the virtual camera according to the transposer rules.</summary>
            /// <param name="component">The component caller</param>
            /// <param name="bindingMode">Current binding mode for damping and offset</param>
            /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
            /// <param name="up">Current camera up</param>
            /// <param name="desiredCameraOffset">Where we want to put the camera relative to the follow target</param>
            /// <param name="positionDamping">Positional damping amount</param>
            /// <param name="eulerDamping">Damping amount for euler rotation damping</param>
            /// <param name="quatDamping">Damping amount for quaternion rotation damping</param>
            /// <param name="rotationDampingMode">Damping mode: euler or quaternion</param>
            /// <param name="outTargetPosition">Resulting camera position</param>
            /// <param name="outTargetOrient">Damped target orientation</param>
            public void TrackTarget(
                CinemachineComponentBase component, 
                CinemachineTransposer.BindingMode bindingMode, 
                float deltaTime, Vector3 up, Vector3 desiredCameraOffset,
                Vector3 positionDamping, Vector3 eulerDamping, float quatDamping, 
                CinemachineTransposer.AngularDampingMode rotationDampingMode,
                out Vector3 outTargetPosition, out Quaternion outTargetOrient)
            {
                var targetOrientation = GetReferenceOrientation(component, bindingMode, up);
                var dampedOrientation = targetOrientation;
                bool prevStateValid = deltaTime >= 0 && component.VirtualCamera.PreviousStateIsValid;
                if (prevStateValid)
                {
                    if (rotationDampingMode == AngularDampingMode.Quaternion
                        && bindingMode == BindingMode.LockToTarget)
                    {
                        float t = component.VirtualCamera.DetachedFollowTargetDamp(1, quatDamping, deltaTime);
                        dampedOrientation = Quaternion.Slerp(
                            PreviousReferenceOrientation, targetOrientation, t);
                    }
                    else
                    {
                        var relative = (Quaternion.Inverse(PreviousReferenceOrientation)
                            * targetOrientation).eulerAngles;
                        for (int i = 0; i < 3; ++i)
                        {
                            if (Mathf.Abs(relative[i]) < 0.01f) // correct for precision drift
                                relative[i] = 0;
                            else if (relative[i] > 180)
                                relative[i] -= 360;
                        }
                        relative = component.VirtualCamera.DetachedFollowTargetDamp(relative, eulerDamping, deltaTime);
                        dampedOrientation = PreviousReferenceOrientation * Quaternion.Euler(relative);
                    }
                }
                PreviousReferenceOrientation = dampedOrientation;

                var targetPosition = component.FollowTargetPosition;
                var currentPosition = PreviousTargetPosition;
                var previousOffset = prevStateValid ? m_PreviousOffset : desiredCameraOffset;
                var offsetDelta = desiredCameraOffset - previousOffset;
                if (offsetDelta.sqrMagnitude > 0.01f)
                {
                    var q = UnityVectorExtensions.SafeFromToRotation(m_PreviousOffset, desiredCameraOffset, up);
                    currentPosition = targetPosition + q * (PreviousTargetPosition - targetPosition);
                }
                m_PreviousOffset = desiredCameraOffset;

                // Adjust for damping, which is done in camera-offset-local coords
                var positionDelta = targetPosition - currentPosition;
                if (prevStateValid)
                {
                    Quaternion dampingSpace = desiredCameraOffset.AlmostZero() 
                        ? component.VcamState.RawOrientation 
                        : Quaternion.LookRotation(dampedOrientation * desiredCameraOffset, up);
                    var localDelta = Quaternion.Inverse(dampingSpace) * positionDelta;
                    localDelta = component.VirtualCamera.DetachedFollowTargetDamp(localDelta, positionDamping, deltaTime);
                    positionDelta = dampingSpace * localDelta;
                }
                currentPosition += positionDelta;

                outTargetPosition = PreviousTargetPosition = currentPosition;
                outTargetOrient = dampedOrientation;
            }

            /// <summary>Return a new damped target position that respects the minimum 
            /// distance from the real target</summary>
            /// <param name="component">The component caller</param>
            /// <param name="dampedTargetPos">The effective position of the target, after damping</param>
            /// <param name="cameraOffset">Desired camera offset from target</param>
            /// <param name="cameraFwd">Current camera local +Z direction</param>
            /// <param name="up">Effective world up</param>
            /// <param name="actualTargetPos">The real undamped target position</param>
            /// <returns>New camera offset, potentially adjusted to respect minimum distance from target</returns>
            public Vector3 GetOffsetForMinimumTargetDistance(
                CinemachineComponentBase component, 
                Vector3 dampedTargetPos, Vector3 cameraOffset, 
                Vector3 cameraFwd, Vector3 up, Vector3 actualTargetPos)
            {
                var posOffset = Vector3.zero;
                if (component.VirtualCamera.FollowTargetAttachment > 1 - Epsilon)
                {
                    cameraOffset = cameraOffset.ProjectOntoPlane(up);
                    var minDistance = cameraOffset.magnitude * 0.2f;
                    if (minDistance > 0)
                    {
                        actualTargetPos = actualTargetPos.ProjectOntoPlane(up);
                        dampedTargetPos = dampedTargetPos.ProjectOntoPlane(up);
                        var cameraPos = dampedTargetPos + cameraOffset;
                        var d = Vector3.Dot(
                            actualTargetPos - cameraPos,
                            (dampedTargetPos - cameraPos).normalized);
                        if (d < minDistance)
                        {
                            var dir = actualTargetPos - dampedTargetPos;
                            var len = dir.magnitude;
                            if (len < 0.01f)
                                dir = -cameraFwd.ProjectOntoPlane(up);
                            else
                                dir /= len;
                            posOffset = dir * (minDistance - d);
                        }
                        PreviousTargetPosition += posOffset;
                    }
                }
                return posOffset;
            }

            /// <summary>This is called to notify the us that a target got warped,
            /// so that we can update its internal state to make the camera
            /// also warp seamlessy.</summary>
            /// <param name="positionDelta">The amount the target's position changed</param>
            public void OnTargetObjectWarped(Vector3 positionDelta)
            {
                PreviousTargetPosition += positionDelta;
            }

            /// <summary>
            /// Force the virtual camera to assume a given position and orientation
            /// </summary>
            /// <param name="component">The component caller</param>
            /// <param name="bindingMode">Current binding mode for damping and offset</param>
            /// <param name="pos">Worldspace pposition to take</param>
            /// <param name="rot">Worldspace orientation to take</param>
            /// <param name="cameraOffsetLocalSpace">Camera offset from target in local space (relative to ReferenceOrientation)</param>
            public void ForceCameraPosition(
                CinemachineComponentBase component, 
                CinemachineTransposer.BindingMode bindingMode,
                Vector3 pos, Quaternion rot,
                Vector3 cameraOffsetLocalSpace)
            {
                // Infer target pos from camera
                var targetRot = bindingMode == BindingMode.SimpleFollowWithWorldUp 
                    ? rot : GetReferenceOrientation(component, bindingMode, component.VirtualCamera.State.ReferenceUp);
                PreviousTargetPosition = pos - targetRot * cameraOffsetLocalSpace;
            }
        }
    }
}
