using UnityEngine;
using Cinemachine.Utility;
using System.Collections.Generic;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the the Body section of the component pipeline.
    /// Its job is to position the camera somewhere on a sphere centered at the Follow target.
    ///
    /// The position on the sphere, and the radius of the sphere, can be controlled by user input.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    public class CinemachineOrbitalFollow : CinemachineComponentBase, IInputAxisTarget
    {
        /// <summary>The coordinate space to use when interpreting the offset from the target</summary>
        [Tooltip("The coordinate space to use when interpreting the offset from the target.  This is also "
            + "used to set the camera's Up vector, which will be maintained when aiming the camera.")]
        public CinemachineTransposer.BindingMode BindingMode;
        
        /// <summary>The camera will be placed at this distance from the Follow target</summary>
        [Tooltip("The camera will be placed at this distance from the Follow target.")]
        public float CameraDistance;

        /// <summary>How to calculate the angular damping for the target orientation.
        /// Use Quaternion if you expect the target to take on very steep pitches, which would
        /// be subject to gimbal lock if Eulers are used.</summary>
        [Space]
        public CinemachineTransposer.AngularDampingMode RotationDampingMode;
        
        /// <summary>How aggressively the camera tries to track the target's orientation.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Tooltip("How aggressively the camera tries to track the target's orientation.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public Vector3 RotationDamping;

        /// <summary>How aggressively the camera tries to track the target's orientation.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Tooltip("How aggressively the camera tries to track the target's orientation.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float QuaternionDamping;

        /// <summary>How aggressively the camera tries to maintain the offset.
        /// Small numbers are more responsive, rapidly translating the camera to keep the
        /// target's offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Tooltip("How aggressively the camera tries to maintain the offset in the Z-axis.  "
            + "Small numbers are more responsive, rapidly translating the camera to keep the "
            + "target's z-axis offset.  Larger numbers give a more heavy slowly responding camera. "
            + "Using different settings per axis can yield a wide range of camera behaviors.")]
        public Vector3 PositionDamping;

        /// <summary>Axis representing the current horizontal rotation.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Space]
        [Tooltip("Axis representing the current horizontal rotation.  Value is in degrees "
            + "and represents a rotation about the up vector.")]
        public InputAxis HorizontalAxis;

        /// <summary>Axis representing the current vertical rotation.  Value is in degrees
        /// and represents a rotation about the right vector</summary>
        [Tooltip("Axis representing the current vertical rotation.  Value is in degrees "
            + "and represents a rotation about the right vector.")]
        public InputAxis VerticalAxis;

        /// <summary>Axis controlling the scale of the current distance.  Value is a scalar
        /// multiplier and is applied to the specified camera distance</summary>
        [Tooltip("Axis controlling the scale of the current distance.  Value is a scalar "
            + "multiplier and is applied to the specified camera distance.")]
        public InputAxis RadialAxis;

        /// <summary>
        /// PositionDamping speeds for each of the 3 axes of the offset from target
        /// </summary>
        Vector3 EffectivePositionDamping
        {
            get => BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp 
                ? new Vector3(0, PositionDamping.y, PositionDamping.z) : PositionDamping;
        }

        /// <summary>
        /// PositionDamping speeds for each of the 3 axes of the target's rotation
        /// </summary>
        Vector3 EffectiveRotationDamping
        {
            get
            {
                switch (BindingMode)
                {
                    case CinemachineTransposer.BindingMode.LockToTargetNoRoll:
                        return new Vector3(RotationDamping.x, RotationDamping.y, 0);
                    case CinemachineTransposer.BindingMode.LockToTargetWithWorldUp:
                        return new Vector3(0, RotationDamping.y, 0);
                    case CinemachineTransposer.BindingMode.WorldSpace:
                    case CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp:
                        return Vector3.zero;
                    default:
                        return RotationDamping;
                }
            }
        }
        
        // State information
        Vector3 m_PreviousWorldOffset;

        // Helper object to track the Follow target
        CinemachineTransposer.TargetTracker m_TargetTracker;

        Vector3 PositiveVector3(Vector3 v) => new Vector3(Mathf.Max(0, v.x), Mathf.Max(0, v.y), Mathf.Max(0, v.z));

        void OnValidate()
        {
            CameraDistance = Mathf.Max(0, CameraDistance);
            PositionDamping = PositiveVector3(PositionDamping);
            RotationDamping = PositiveVector3(PositionDamping);
            QuaternionDamping = Mathf.Max(0, QuaternionDamping);
            HorizontalAxis.Validate();
            VerticalAxis.Validate();
            RadialAxis.Validate();
        }

        void Reset()
        {
            BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
            RotationDampingMode = CinemachineTransposer.AngularDampingMode.Euler;
            PositionDamping = new Vector3(1, 1, 1);
            RotationDamping = new Vector3(1, 1, 1);
            QuaternionDamping = 1f;

            BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
            CameraDistance = 10;
            HorizontalAxis = new InputAxis 
            { 
                Value = 0, 
                Range = new Vector2(-180, 180), 
                Wrap = true, 
                Center = 0, 
                Recentering = InputAxis.RecenteringSettings.Default 
            };
            VerticalAxis = new InputAxis 
            { 
                Value = 0, 
                Range = new Vector2(-10, 45), 
                Wrap = false, 
                Center = 10, 
                Recentering = InputAxis.RecenteringSettings.Default 
            };
            RadialAxis = new InputAxis 
            { 
                Value = 1, 
                Range = new Vector2(1, 5), 
                Wrap = false, 
                Center = 1, 
                Recentering = InputAxis.RecenteringSettings.Default 
            };
        }

        /// <summary>True if component is enabled and has a valid Follow target</summary>
        public override bool IsValid { get => enabled && FollowTarget != null; }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage { get => CinemachineCore.Stage.Body; }
       
        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() 
        { 
            var d = EffectivePositionDamping;
            var d2 = RotationDampingMode == CinemachineTransposer.AngularDampingMode.Euler 
                ? EffectiveRotationDamping : new Vector3(QuaternionDamping, 0, 0);
            var a = Mathf.Max(d.x, Mathf.Max(d.y, d.z)); 
            var b = Mathf.Max(d2.x, Mathf.Max(d2.y, d2.z)); 
            return Mathf.Max(a, b); 
        }

        /// <summary>Report the available input axes</summary>
        public void GetInputAxes(List<IInputAxisTarget.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisTarget.AxisDescriptor { Axis = HorizontalAxis, Name = "Horizontal", AxisIndex = 0 });
            axes.Add(new IInputAxisTarget.AxisDescriptor { Axis = VerticalAxis, Name = "Vertical", AxisIndex = 1 });
            axes.Add(new IInputAxisTarget.AxisDescriptor { Axis = RadialAxis, Name = "Radial", AxisIndex = 2 });
        }

        IInputAxisTarget.ResetHandler m_ResetHandler;
        public void RegisterResetHandler(IInputAxisTarget.ResetHandler handler) => m_ResetHandler += handler;

        /// <summary>
        /// Unregister a handler that will be called when input needs to be reset
        /// </summary>
        /// <param name="handler">Then hanlder to unregister</param>
        public void UnregisterResetHandler(IInputAxisTarget.ResetHandler handler) => m_ResetHandler -= handler;

        /// <summary>Inspector checks this and displays warnng if no handler</summary>
        internal bool HasInputHandler => m_ResetHandler != null;

        /// <summary>Get the localspace offset from the target at which to place the camera</summary>
        Vector3 ComputeCameraOffset()
        {
            var rot = Quaternion.Euler(VerticalAxis.Value, HorizontalAxis.Value, 0);
            var offset = rot * new Vector3(0, 0, -CameraDistance * RadialAxis.Value);
            if (BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp)
            {
                offset.x = 0;
                offset.z = -Mathf.Abs(offset.z);
            }
            return offset;
        }
        
        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <param name="transitionParams">Transition settings for this vcam</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        {
            if (fromCam != null
                && transitionParams.m_InheritPosition
                && !CinemachineCore.Instance.IsLiveInBlend(VirtualCamera))
            {
                var state = fromCam.State;
                ForceCameraPosition(state.RawPosition, state.RawOrientation);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace pposition to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            base.ForceCameraPosition(pos, rot);
            m_TargetTracker.ForceCameraPosition(this, BindingMode, pos, rot, ComputeCameraOffset());

            m_ResetHandler?.Invoke();

            var targetPos = FollowTargetPosition;
            var dir = pos - targetPos;
            var distance = dir.magnitude;
            if (distance > 0.001f)
            {
                var up = VirtualCamera.State.ReferenceUp;
                var orient = m_TargetTracker.GetReferenceOrientation(this, BindingMode, up);
                dir /= distance;
                var localDir = orient * dir;
                var r = UnityVectorExtensions.SafeFromToRotation(Vector3.back, localDir, up).eulerAngles;
                VerticalAxis.Value = BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp ? 0 : r.x;
                HorizontalAxis.Value = r.y;
            }
            RadialAxis.Value = distance / CameraDistance;
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
        
        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            m_TargetTracker.InitStateInfo(this, deltaTime, BindingMode, curState.ReferenceUp);
            if (!IsValid)
                return;

            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid || !CinemachineCore.Instance.IsLive(VirtualCamera))
                m_ResetHandler?.Invoke();

            Vector3 offset = ComputeCameraOffset();
            if (BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp)
                HorizontalAxis.Value = 0;

            m_TargetTracker.TrackTarget(
                this, BindingMode, deltaTime, curState.ReferenceUp, offset, 
                EffectivePositionDamping, EffectiveRotationDamping, QuaternionDamping, RotationDampingMode,
                out Vector3 pos, out Quaternion orient);

            // Place the camera
            offset = orient * offset;
            curState.ReferenceUp = orient * Vector3.up;

            // Respect minimum target distance on XZ plane
            var targetPosition = FollowTargetPosition;
            pos += m_TargetTracker.GetOffsetForMinimumTargetDistance(
                this, pos, offset, curState.RawOrientation * Vector3.forward,
                curState.ReferenceUp, targetPosition);
            curState.RawPosition = pos + offset;

            if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                var lookAt = targetPosition;
                if (LookAtTarget != null)
                    lookAt = LookAtTargetPosition;
                var dir0 = m_TargetTracker.PreviousTargetPosition + m_PreviousWorldOffset - lookAt;
                var dir1 = curState.RawPosition - lookAt;
                if (dir0.sqrMagnitude > 0.01f && dir1.sqrMagnitude > 0.01f)
                    curState.PositionDampingBypass = UnityVectorExtensions.SafeFromToRotation(
                        dir0, dir1, curState.ReferenceUp).eulerAngles;
            }
            m_PreviousWorldOffset = offset;
        }
    }
}
