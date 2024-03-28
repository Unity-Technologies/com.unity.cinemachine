using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to match the orientation of the Follow target.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Rotation Control/Cinemachine Rotate With Follow Target")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Aim)]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.Tracking)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineRotateWithFollowTarget.html")]
    public class CinemachineRotateWithFollowTarget : CinemachineComponentBase
    {
        /// <summary>
        /// How much time it takes for the aim to catch up to the target's rotation
        /// </summary>
        [Tooltip("How much time it takes for the aim to catch up to the target's rotation")]
        public float Damping = 0;

        Quaternion m_PreviousReferenceOrientation = Quaternion.identity;

        /// <summary>True if component is enabled and has a Follow target defined</summary>
        public override bool IsValid { get => enabled && FollowTarget != null; }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage { get => CinemachineCore.Stage.Aim; }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Damping;

        /// <summary>Orients the camera to match the Follow target's orientation</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Not used.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;

            Quaternion dampedOrientation = FollowTargetRotation;
            if (deltaTime >= 0)
            {
                float t = VirtualCamera.DetachedFollowTargetDamp(1, Damping, deltaTime);
                dampedOrientation = Quaternion.Slerp(
                    m_PreviousReferenceOrientation, FollowTargetRotation, t);
            }
            m_PreviousReferenceOrientation = dampedOrientation;
            curState.RawOrientation = dampedOrientation;
            curState.ReferenceUp = dampedOrientation * Vector3.up;
        }
    }
}
