﻿using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to aim the camera in response to the user's mouse or joystick input.
    ///
    /// This component does not change the camera's position.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Aim)]
    public class CinemachinePanTilt 
        : CinemachineComponentBase, IInputAxisTarget
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

        /// <summary>
        /// Input axis controller registers here a delegate to call when the camera is reset
        /// </summary>
        IInputAxisTarget.ResetHandler m_ResetHandler;

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
        }

        static InputAxis DefaultPan => new () { Value = 0, Range = new Vector2(-180, 180), Wrap = true, Center = 0 };
        static InputAxis DefaultTilt => new () { Value = 0, Range = new Vector2(-70, 70), Wrap = false, Center = 0 };
        
        /// <summary>Report the available input axes</summary>
        /// <param name="axes">Output list to which the axes will be added</param>
        void IInputAxisTarget.GetInputAxes(List<IInputAxisTarget.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisTarget.AxisDescriptor { Axis = PanAxis, Name = "Pan", AxisIndex = 0 });
            axes.Add(new IInputAxisTarget.AxisDescriptor { Axis = TiltAxis, Name = "Tilt", AxisIndex = 1 });
        }

        /// <summary>Register a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to register</param>
        void IInputAxisTarget.RegisterResetHandler(IInputAxisTarget.ResetHandler handler) => m_ResetHandler += handler;

        /// <summary>Unregister a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to unregister</param>
        void IInputAxisTarget.UnregisterResetHandler(IInputAxisTarget.ResetHandler handler) => m_ResetHandler -= handler;

        float CinemachineFreeLookModifier.IModifierValueSource.NormalizedModifierValue 
        {
            get
            {
                var r = TiltAxis.Range.y - TiltAxis.Range.x;
                return (TiltAxis.Value - TiltAxis.Range.x) / (r > 0.001f ? r : 1) * 2 - 1;
            }
        }

        /// <summary>Inspector checks this and displays warning if no handler</summary>
        internal bool HasInputHandler => m_ResetHandler != null;

        /// <summary>True if component is enabled and has a LookAt defined</summary>
        public override bool IsValid => enabled;

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Aim;

        /// <summary>Does nothing</summary>
        /// <param name="state"></param>
        /// <param name="deltaTime"></param>
        public override void PrePipelineMutateCameraState(ref CameraState state, float deltaTime) {}

        /// <summary>Applies the axis values and orients the camera accordingly</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for calculating damping.  Not used.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;
            var referenceFrame = GetReferenceFrame();
            var rot = referenceFrame * Quaternion.Euler(TiltAxis.Value, PanAxis.Value, 0);
            var up = referenceFrame * Vector3.up;
            curState.RawOrientation = Quaternion.FromToRotation(curState.ReferenceUp, up) * rot;
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
        /// <param name="transitionParams">Transition settings for this vcam</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        {
            m_ResetHandler?.Invoke(); // Cancel recentering
            if (fromCam != null && transitionParams.m_InheritPosition  
                && !CinemachineCore.Instance.IsLiveInBlend(VirtualCamera))
            {
                SetAxesForRotation(fromCam.State.RawOrientation);
                return true;
            }
            return false;
        }
        
        /// <summary>POV is controlled by input.</summary>
        public override bool RequiresUserInput => false;

        void SetAxesForRotation(Quaternion targetRot)
        {
            m_ResetHandler?.Invoke(); // Reset the axes

            Vector3 up = VcamState.ReferenceUp;
            Vector3 fwd = GetReferenceFrame() * Vector3.forward;

            PanAxis.Value = 0;
            Vector3 targetFwd = targetRot * Vector3.forward;
            Vector3 a = fwd.ProjectOntoPlane(up);
            Vector3 b = targetFwd.ProjectOntoPlane(up);
            if (!a.AlmostZero() && !b.AlmostZero())
                PanAxis.Value = Vector3.SignedAngle(a, b, up);

            TiltAxis.Value = 0;
            fwd = Quaternion.AngleAxis(PanAxis.Value, up) * fwd;
            Vector3 right = Vector3.Cross(up, fwd);
            if (!right.AlmostZero())
                TiltAxis.Value = Vector3.SignedAngle(fwd, targetFwd, right);
        }

        Quaternion GetReferenceFrame()
        {
            Transform target = null;
            switch (ReferenceFrame)
            {
                case ReferenceFrames.World: break;
                case ReferenceFrames.TrackingTarget: target = FollowTarget; break;
                case ReferenceFrames.LookAtTarget: target = LookAtTarget; break;
                case ReferenceFrames.ParentObject: target = VirtualCamera.transform.parent; break;
            }
            return (target != null) ? target.rotation : Quaternion.identity;
        }
    }
}
