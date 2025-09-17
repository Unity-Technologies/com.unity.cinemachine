using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to aim the camera in response to the user's mouse or joystick input.
    ///
    /// This component does not change the camera's position.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Rotation Control/Cinemachine Pan Tilt")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Aim)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachinePanTilt.html")]
    public class CinemachinePanTilt
        : CinemachineComponentBase, IInputAxisOwner, IInputAxisResetSource
        , CinemachineFreeLookModifier.IModifierValueSource
    {
        /// <summary>Defines the reference frame against which pan and tilt rotations are made.</summary>
        public enum ReferenceFrames
        {
            /// <summary>Pan and tilt are relative to parent object's local axes,
            /// or World axes if no parent object</summary>
            ParentObject,

            /// <summary>Pan and tilt angles are relative to world axes</summary>
            World,

            /// <summary>Pan and tilt are relative to Tracking target's local axes</summary>
            TrackingTarget,

            /// <summary>Pan and tilt are relative to LookAt target's local axes</summary>
            LookAtTarget
        }

        /// <summary>Defines the reference frame against which pan and tilt rotations are made.</summary>
        public ReferenceFrames ReferenceFrame = ReferenceFrames.ParentObject;

        /// <summary>
        /// Defines the recentering target: Recentering goes here
        /// </summary>
        public enum RecenterTargetModes
        {
            /// <summary>Go to the Center value defined in the axis</summary>
            AxisCenter,

            /// <summary>Axis angles are relative to Follow target's forward</summary>
            TrackingTargetForward,

            /// <summary>Axis angles are relative to LookAt target's forward</summary>
            LookAtTargetForward
        }

        /// <summary>
        /// Defines the recentering target: recentering goes here
        /// </summary>
        public RecenterTargetModes RecenterTarget = RecenterTargetModes.AxisCenter;

        /// <summary>Axis representing the current horizontal rotation.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Tooltip("Axis representing the current horizontal rotation.  Value is in degrees "
            + "and represents a rotation about the Y axis.")]
        public InputAxis PanAxis = DefaultPan;

        /// <summary>Axis representing the current vertical rotation.  Value is in degrees
        /// and represents a rotation about the right vector</summary>
        [Tooltip("Axis representing the current vertical rotation.  Value is in degrees "
            + "and represents a rotation about the X axis.")]
        public InputAxis TiltAxis = DefaultTilt;

        Quaternion m_PreviousCameraRotation;

        /// <summary>
        /// Input axis controller registers here a delegate to call when the camera is reset
        /// </summary>
        Action m_ResetHandler;

        void OnValidate()
        {
            PanAxis.Validate();
            TiltAxis.Range.x = Mathf.Clamp(TiltAxis.Range.x, -90, 90);
            TiltAxis.Range.y = Mathf.Clamp(TiltAxis.Range.y, -90, 90);
            TiltAxis.Validate();
        }

        void Reset()
        {
            PanAxis = DefaultPan;
            TiltAxis = DefaultTilt;
            ReferenceFrame = ReferenceFrames.ParentObject;
            RecenterTarget = RecenterTargetModes.AxisCenter;
        }

        static InputAxis DefaultPan => new () { Value = 0, Range = new Vector2(-180, 180), Wrap = true, Center = 0, Recentering = InputAxis.RecenteringSettings.Default };
        static InputAxis DefaultTilt => new () { Value = 0, Range = new Vector2(-70, 70), Wrap = false, Center = 0, Recentering = InputAxis.RecenteringSettings.Default };

        /// <summary>Report the available input axes</summary>
        /// <param name="axes">Output list to which the axes will be added</param>
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref PanAxis, Name = "Look X (Pan)", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
            axes.Add(new () { DrivenAxis = () => ref TiltAxis, Name = "Look Y (Tilt)", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
        }

        /// <summary>Register a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to register</param>
        void IInputAxisResetSource.RegisterResetHandler(Action handler) => m_ResetHandler += handler;

        /// <summary>Unregister a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to unregister</param>
        void IInputAxisResetSource.UnregisterResetHandler(Action handler) => m_ResetHandler -= handler;

        float CinemachineFreeLookModifier.IModifierValueSource.NormalizedModifierValue
        {
            get
            {
                var r = TiltAxis.Range.y - TiltAxis.Range.x;
                return (TiltAxis.Value - TiltAxis.Range.x) / (r > 0.001f ? r : 1) * 2 - 1;
            }
        }

        /// <summary>Inspector checks this and displays warning if no handler</summary>
        bool IInputAxisResetSource.HasResetHandler => m_ResetHandler != null;

        /// <summary>True if component is enabled and has a LookAt defined</summary>
        public override bool IsValid => enabled;

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Aim;

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

            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid || !CinemachineCore.IsLive(VirtualCamera))
                m_ResetHandler?.Invoke();

            var referenceFrame = GetReferenceFrame(curState.ReferenceUp);
            var rot = referenceFrame * Quaternion.Euler(TiltAxis.Value, PanAxis.Value, 0);
            curState.RawOrientation = rot;

            if (VirtualCamera.PreviousStateIsValid)
                curState.RotationDampingBypass = curState.RotationDampingBypass
                    * UnityVectorExtensions.SafeFromToRotation(
                        m_PreviousCameraRotation * Vector3.forward,
                        rot * Vector3.forward, curState.ReferenceUp);
            m_PreviousCameraRotation = rot;

            var gotInputX = PanAxis.TrackValueChange();
            var gotInputY = TiltAxis.TrackValueChange();

            // Sync recentering if the recenter times match
            if (PanAxis.Recentering.Time == TiltAxis.Recentering.Time)
            {
                gotInputX |= gotInputY;
                gotInputY |= gotInputX;
            }

            if (Application.isPlaying)
            {
                var recenterTarget = GetRecenterTarget();
                PanAxis.UpdateRecentering(deltaTime, gotInputX, recenterTarget.x);
                TiltAxis.UpdateRecentering(deltaTime, gotInputY, recenterTarget.y);
            }
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation.
        /// Procedural placement then takes over
        /// </summary>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot) => SetAxesForRotation(rot);

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            m_ResetHandler?.Invoke(); // Cancel re-centering
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
            m_ResetHandler?.Invoke(); // cancel re-centering

            var up = VcamState.ReferenceUp;
            var fwd = GetReferenceFrame(up) * Vector3.forward;

            PanAxis.Value = 0;
            var targetFwd = targetRot * Vector3.forward;
            var a = fwd.ProjectOntoPlane(up);
            var b = targetFwd.ProjectOntoPlane(up);
            if (!a.AlmostZero() && !b.AlmostZero())
                PanAxis.Value = Vector3.SignedAngle(a, b, up);

            TiltAxis.Value = 0;
            fwd = Quaternion.AngleAxis(PanAxis.Value, up) * fwd;
            var right = Vector3.Cross(up, fwd);
            if (!right.AlmostZero())
                TiltAxis.Value = Vector3.SignedAngle(fwd, targetFwd, right);
        }

        Quaternion GetReferenceFrame(Vector3 up)
        {
            Transform target = null;
            switch (ReferenceFrame)
            {
                case ReferenceFrames.World: break;
                case ReferenceFrames.TrackingTarget: target = FollowTarget; break;
                case ReferenceFrames.LookAtTarget: target = LookAtTarget; break;
                case ReferenceFrames.ParentObject: target = VirtualCamera.transform.parent; break;
            }
            return (target != null) ? target.rotation : Quaternion.FromToRotation(Vector3.up, up);
        }

        /// <summary>
        /// Get the horizonmtal and vertical angles that correspong to "at rest" position.
        /// </summary>
        /// <returns>X is horizontal angle (rot Y) and Y is vertical angle (rot X)</returns>
        public Vector2 GetRecenterTarget()
        {
            Transform t = null;
            switch (RecenterTarget)
            {
                case RecenterTargetModes.TrackingTargetForward: t = VirtualCamera.Follow; break;
                case RecenterTargetModes.LookAtTargetForward: t = VirtualCamera.LookAt; break;
                default: break;
            }
            if (t != null)
            {
                var fwd = t.forward;
                var parent = VirtualCamera.transform.parent;
                if (parent != null)
                    fwd = parent.rotation * fwd;
                var v = Quaternion.FromToRotation(Vector3.forward, fwd).eulerAngles;
                return new Vector2(NormalizeAngle(v.y), NormalizeAngle(v.x));
            }
            return new Vector2(PanAxis.Center, TiltAxis.Center);

            static float NormalizeAngle(float angle) => ((angle + 180) % 360) - 180;
        }
    }
}
