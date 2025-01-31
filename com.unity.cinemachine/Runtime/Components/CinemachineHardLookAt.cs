using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to aim the camera hard at the LookAt target.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Rotation Control/Cinemachine Hard Look At")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Aim)]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.LookAt)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineHardLookAt.html")]
    public class CinemachineHardLookAt : CinemachineComponentBase
    {
        /// <summary>True if component is enabled and has a LookAt defined</summary>
        public override bool IsValid { get => enabled && LookAtTarget != null; }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage { get => CinemachineCore.Stage.Aim; }

        /// <summary>
        /// True if this component tries to make the camera look at the Tracking Target.
        /// Used by inspector to warn the user of potential improper setup.
        /// </summary>
        internal override bool CameraLooksAtTarget { get => true; }

        /// <summary>
        /// Offset from the LookAt target's origin, in target's local space.  The camera will look at this point.
        /// </summary>
        [Tooltip("Offset from the LookAt target's origin, in target's local space.  The camera will look at this point.")]
        public Vector3 LookAtOffset = Vector3.zero;

        void Reset()
        {
            LookAtOffset = Vector3.zero;
        }

        /// <summary>Applies the composer rules and orients the camera accordingly</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for calculating damping.  If less than
        /// zero, then target will snap to the center of the dead zone.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (IsValid && curState.HasLookAt())
            {
                var offset = LookAtTargetRotation * LookAtOffset;
                Vector3 dir = ((curState.ReferenceLookAt + offset) - curState.GetCorrectedPosition());
                if (dir.magnitude > Epsilon)
                {
                    if (Vector3.Cross(dir.normalized, curState.ReferenceUp).magnitude < Epsilon)
                        curState.RawOrientation = Quaternion.FromToRotation(Vector3.forward, dir);
                    else
                        curState.RawOrientation = Quaternion.LookRotation(dir, curState.ReferenceUp);
                }
            }
        }
    }
}

