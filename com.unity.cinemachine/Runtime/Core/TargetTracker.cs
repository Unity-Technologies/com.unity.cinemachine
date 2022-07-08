using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Helper object for implementing target following with damping
    /// </summary>
    public struct TargetTracker
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

        /// <summary>State information for damping</summary>
        public Vector3 PreviousTargetPosition { get; private set; }

        /// <summary>State information for damping</summary>
        public Quaternion PreviousReferenceOrientation { get; private set; }

        Quaternion m_TargetOrientationOnAssign;
        Vector3 m_PreviousOffset;
        Transform m_previousTarget;
        
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
            if (m_previousTarget != component.FollowTarget || !prevStateValid)
            {
                m_previousTarget = component.FollowTarget;
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
            return PreviousReferenceOrientation.normalized;
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
            BindingMode bindingMode, 
            float deltaTime, Vector3 up, Vector3 desiredCameraOffset,
            Vector3 positionDamping, Vector3 eulerDamping, float quatDamping, 
            AngularDampingMode rotationDampingMode,
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

        /// <summary>This is called to notify the us that a target got warped,
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
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        /// <param name="cameraOffsetLocalSpace">Camera offset from target in local space (relative to ReferenceOrientation)</param>
        public void ForceCameraPosition(
            CinemachineComponentBase component, 
            BindingMode bindingMode,
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
