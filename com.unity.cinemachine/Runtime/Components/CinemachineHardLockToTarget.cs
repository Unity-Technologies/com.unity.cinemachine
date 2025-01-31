using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to place the camera on the Follow Target.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Cinemachine Hard Lock to Target")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.Tracking)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineHardLockToTarget.html")]
    public class CinemachineHardLockToTarget : CinemachineComponentBase
    {
        /// <summary>
        /// How much time it takes for the position to catch up to the target's position
        /// </summary>
        [Tooltip("How much time it takes for the position to catch up to the target's position")]
        [FormerlySerializedAs("m_Damping")]
        public float Damping = 0;
        Vector3 m_PreviousTargetPosition;

        /// <summary>True if component is enabled and has a LookAt defined</summary>
        public override bool IsValid { get => enabled && FollowTarget != null; }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage { get => CinemachineCore.Stage.Body; }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Damping;

        /// <summary>Applies the composer rules and orients the camera accordingly</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for calculating damping.  If less than
        /// zero, then target will snap to the center of the dead zone.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;

            Vector3 dampedPos = FollowTargetPosition;
            if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                dampedPos = m_PreviousTargetPosition + VirtualCamera.DetachedFollowTargetDamp(
                    dampedPos - m_PreviousTargetPosition, Damping, deltaTime);
            m_PreviousTargetPosition = dampedPos;
            curState.RawPosition = dampedPos;
        }
    }
}

