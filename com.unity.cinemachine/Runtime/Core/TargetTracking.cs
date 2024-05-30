using System;
using UnityEngine;

namespace Unity.Cinemachine.TargetTracking
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
        LazyFollow = 5
    }

    /// <summary>How to calculate the angular damping for the target orientation</summary>
    public enum AngularDampingMode
    {
        /// <summary>Use Euler angles to specify damping values.
        /// Subject to gimbal-lock when pitch is steep.</summary>
        Euler,
        /// <summary>
        /// Use quaternions to calculate angular damping.
        /// No per-channel control, but not susceptible to gimbal-lock</summary>
        Quaternion
    }

    /// <summary>
    /// Settings to control damping for target tracking.
    /// </summary>
    [Serializable]
    public struct TrackerSettings
    {
        /// <summary>The coordinate space to use when interpreting the offset from the target</summary>
        [Tooltip("The coordinate space to use when interpreting the offset from the target.  This is also "
            + "used to set the camera's Up vector, which will be maintained when aiming the camera.")]
        public BindingMode BindingMode;

        /// <summary>How aggressively the camera tries to maintain the offset, per axis.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Tooltip("How aggressively the camera tries to maintain the offset, per axis.  Small numbers "
            + "are more responsive, rapidly translating the camera to keep the target's offset.  "
            + "Larger numbers give a more heavy slowly responding camera. Using different settings per "
            + "axis can yield a wide range of camera behaviors.")]
        public Vector3 PositionDamping;

        /// <summary>How to calculate the angular damping for the target orientation.
        /// Use Quaternion if you expect the target to take on very steep pitches, which would
        /// be subject to gimbal lock if Eulers are used.</summary>
        public AngularDampingMode AngularDampingMode;

        /// <summary>How aggressively the camera tries to track the target's rotation, per axis.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Tooltip("How aggressively the camera tries to track the target's rotation, per axis.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public Vector3 RotationDamping;

        /// <summary>How aggressively the camera tries to track the target's rotation.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target's rotation.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float QuaternionDamping;
            
        /// <summary>
        /// Get the default tracking settings
        /// </summary>
        public static TrackerSettings Default => new TrackerSettings
        {
            BindingMode = BindingMode.WorldSpace,
            PositionDamping = Vector3.one,
            AngularDampingMode = AngularDampingMode.Euler,
            RotationDamping = Vector3.one,
            QuaternionDamping = 1
        };

        /// <summary>
        /// Called from OnValidate().  Makes sure the settings are sensible.
        /// </summary>
        public void Validate()
        {
            PositionDamping.x = Mathf.Max(0, PositionDamping.x);
            PositionDamping.y = Mathf.Max(0, PositionDamping.y);
            PositionDamping.z = Mathf.Max(0, PositionDamping.z);
            RotationDamping.x = Mathf.Max(0, RotationDamping.x);
            RotationDamping.y = Mathf.Max(0, RotationDamping.y);
            RotationDamping.z = Mathf.Max(0, RotationDamping.z);
            QuaternionDamping = Mathf.Max(0, QuaternionDamping);
        }
    }

    /// <summary>
    /// Helpers for TrackerSettings
    /// </summary>
    public static class TrackerSettingsExtensions
    {
        /// <summary>
        /// Report maximum damping time needed for the current binding mode.
        /// </summary>
        /// <param name="s">The tracker settings</param>
        /// <returns>Highest damping setting in this mode</returns>
        public static float GetMaxDampTime(this TrackerSettings s) 
        { 
            var d = s.GetEffectivePositionDamping();
            var d2 = s.AngularDampingMode == AngularDampingMode.Euler 
                ? s.GetEffectiveRotationDamping() : new Vector3(s.QuaternionDamping, 0, 0);
            var a = Mathf.Max(d.x, Mathf.Max(d.y, d.z)); 
            var b = Mathf.Max(d2.x, Mathf.Max(d2.y, d2.z)); 
            return Mathf.Max(a, b); 
        }
        
        /// <summary>
        /// Get the effective position damping setting for current binding mode.
        /// For some binding modes, some axes are not damped.
        /// </summary>
        /// <param name="s">The tracker settings</param>
        /// <returns>The damping settings applicable for this binding mode</returns>
        internal static Vector3 GetEffectivePositionDamping(this TrackerSettings s)
        {
            return s.BindingMode == BindingMode.LazyFollow 
                ? new Vector3(0, s.PositionDamping.y, s.PositionDamping.z) : s.PositionDamping;
        }

        /// <summary>
        /// Get the effective rotation damping setting for current binding mode.
        /// For some binding modes, some axes are not damped.
        /// </summary>
        /// <param name="s">The tracker settings</param>
        /// <returns>The damping settings applicable for this binding mode</returns>
        internal static Vector3 GetEffectiveRotationDamping(this TrackerSettings s)
        {
            switch (s.BindingMode)
            {
                case BindingMode.LockToTargetNoRoll:
                    return new Vector3(s.RotationDamping.x, s.RotationDamping.y, 0);
                case BindingMode.LockToTargetWithWorldUp:
                    return new Vector3(0, s.RotationDamping.y, 0);
                case BindingMode.WorldSpace:
                case BindingMode.LazyFollow:
                    return Vector3.zero;
                default:
                    return s.RotationDamping;
            }
        }
    }

    /// <summary>
    /// Helper object for implementing target following with damping
    /// </summary>
    struct Tracker
    {
        /// <summary>State information for damping</summary>
        public Vector3 PreviousTargetPosition { get; private set; }

        /// <summary>State information for damping</summary>
        public Quaternion PreviousReferenceOrientation { get; private set; }

        Quaternion m_TargetOrientationOnAssign;
        Vector3 m_PreviousOffset;
        Transform m_PreviousTarget;
        
        /// <summary>Initializes the state for previous frame if appropriate.</summary>
        /// <param name="component">The component caller</param>
        /// <param name="deltaTime">Current effective deltaTime.</param>
        /// <param name="bindingMode">Current binding mode for damping and offset</param>
        /// <param name="up">Current effective world up direction.</param>
        public void InitStateInfo(
            CinemachineComponentBase component, float deltaTime, 
            BindingMode bindingMode, Vector3 up)
        {
            bool prevStateValid = deltaTime >= 0 && component.VirtualCamera.PreviousStateIsValid;
            if (m_PreviousTarget != component.FollowTarget || !prevStateValid)
            {
                m_PreviousTarget = component.FollowTarget;
                m_TargetOrientationOnAssign = component.FollowTargetRotation;
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
            BindingMode bindingMode, 
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
                        return m_TargetOrientationOnAssign;
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
                    case BindingMode.LazyFollow:
                    {
                        Vector3 fwd = (component.FollowTargetPosition - component.VcamState.RawPosition).ProjectOntoPlane(worldUp);
                        if (fwd.AlmostZero())
                            break;
                        return Quaternion.LookRotation(fwd, worldUp);
                    }
                }
            }
            // Gimbal lock situation - use previous orientation if it exists
            if (PreviousReferenceOrientation == new Quaternion(0, 0, 0, 0))
                return Quaternion.identity;
            return PreviousReferenceOrientation.normalized;
        }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="component">The component caller</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        /// <param name="up">Current camera up</param>
        /// <param name="desiredCameraOffset">Where we want to put the camera relative to the follow target</param>
        /// <param name="settings">Tracker settings</param>
        /// <param name="outTargetPosition">Resulting camera position</param>
        /// <param name="outTargetOrient">Damped target orientation</param>
        public void TrackTarget(
            CinemachineComponentBase component, 
            float deltaTime, Vector3 up, Vector3 desiredCameraOffset,
            in TrackerSettings settings,
            out Vector3 outTargetPosition, out Quaternion outTargetOrient)
        {
            var targetOrientation = GetReferenceOrientation(component, settings.BindingMode, up);
            var dampedOrientation = targetOrientation;
            bool prevStateValid = deltaTime >= 0 && component.VirtualCamera.PreviousStateIsValid;
            if (prevStateValid)
            {
                if (settings.AngularDampingMode == AngularDampingMode.Quaternion
                    && settings.BindingMode == BindingMode.LockToTarget)
                {
                    float t = component.VirtualCamera.DetachedFollowTargetDamp(
                        1, settings.QuaternionDamping, deltaTime);
                    dampedOrientation = Quaternion.Slerp(
                        PreviousReferenceOrientation, targetOrientation, t);
                }
                else if (settings.BindingMode != BindingMode.LazyFollow)
                {
                    var relative = (Quaternion.Inverse(PreviousReferenceOrientation)
                        * targetOrientation).eulerAngles;
                    for (int i = 0; i < 3; ++i)
                    {
                        if (relative[i] > 180)
                            relative[i] -= 360;
                        if (Mathf.Abs(relative[i]) < 0.01f) // correct for precision drift
                            relative[i] = 0;
                    }
                    relative = component.VirtualCamera.DetachedFollowTargetDamp(
                        relative, settings.GetEffectiveRotationDamping(), deltaTime);
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
                localDelta = component.VirtualCamera.DetachedFollowTargetDamp(
                    localDelta, settings.GetEffectivePositionDamping(), deltaTime);
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
            if (component.VirtualCamera.FollowTargetAttachment > 1 - UnityVectorExtensions.Epsilon)
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

        /// <summary>This is called to notify the user that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessly.</summary>
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
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        /// <param name="cameraOffsetLocalSpace">Camera offset from target in local space (relative to ReferenceOrientation)</param>
        public void ForceCameraPosition(
            CinemachineComponentBase component, 
            BindingMode bindingMode,
            Vector3 pos, Quaternion rot,
            Vector3 cameraOffsetLocalSpace)
        {
            // Infer target pos from camera
            var targetRot = bindingMode == BindingMode.LazyFollow 
                ? rot : GetReferenceOrientation(component, bindingMode, component.VirtualCamera.State.ReferenceUp);
            PreviousTargetPosition = pos - targetRot * cameraOffsetLocalSpace;
        }
    }
}
