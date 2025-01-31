using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for Cm Camera that adds a final offset to the camera
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Camera Offset")]
    [ExecuteAlways]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineCameraOffset.html")]
    public class CinemachineCameraOffset : CinemachineExtension
    {
        /// <summary>
        /// Offset the camera's position by this much (camera space)
        /// </summary>
        [Tooltip("Offset the camera's position by this much (camera space)")]
        [FormerlySerializedAs("m_Offset")]
        public Vector3 Offset = Vector3.zero;

        /// <summary>
        /// When to apply the offset
        /// </summary>
        [Tooltip("When to apply the offset")]
        [FormerlySerializedAs("m_ApplyAfter")]
        public CinemachineCore.Stage ApplyAfter = CinemachineCore.Stage.Aim;

        /// <summary>
        /// If applying offset after aim, re-adjust the aim to preserve the screen position
        /// of the LookAt target as much as possible
        /// </summary>
        [Tooltip("If applying offset after aim, re-adjust the aim to preserve the screen position"
            + " of the LookAt target as much as possible")]
        [FormerlySerializedAs("m_PreserveComposition")]
        public bool PreserveComposition;

        private void Reset()
        {
            Offset = Vector3.zero;
            ApplyAfter = CinemachineCore.Stage.Aim;
            PreserveComposition = false;
        }

        /// <summary>
        /// Applies the specified offset to the camera state
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == ApplyAfter)
            {
                bool preserveAim = PreserveComposition
                    && state.HasLookAt() && stage > CinemachineCore.Stage.Body;

                Vector3 screenOffset = Vector2.zero;
                if (preserveAim)
                {
                    screenOffset = state.RawOrientation.GetCameraRotationToTarget(
                        state.ReferenceLookAt - state.GetCorrectedPosition(), state.ReferenceUp);
                }

                Vector3 offset = state.RawOrientation * Offset;
                state.PositionCorrection += offset;
                if (!preserveAim)
                    state.ReferenceLookAt += offset;
                else
                {
                    var q = Quaternion.LookRotation(
                        state.ReferenceLookAt - state.GetCorrectedPosition(), state.ReferenceUp);
                    q = q.ApplyCameraRotation(-screenOffset, state.ReferenceUp);
                    state.RawOrientation = q;
                }
            }
        }
    }
}
