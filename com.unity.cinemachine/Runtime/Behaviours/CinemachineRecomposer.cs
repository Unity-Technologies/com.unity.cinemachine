using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for Cm Camera that adds a final tweak to the camera
    /// composition.  It is intended for use in a Timeline context, where you want to hand-adjust
    /// the output of procedural or recorded camera aiming.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Recomposer")]
    [ExecuteAlways]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineRecomposer.html")]
    public class CinemachineRecomposer : CinemachineExtension
    {
        /// <summary>
        /// When to apply the adjustment
        /// </summary>
        [Tooltip("When to apply the adjustment")]
        [FormerlySerializedAs("m_ApplyAfter")]
        public CinemachineCore.Stage ApplyAfter;

        /// <summary>
        /// Tilt the camera by this much
        /// </summary>
        [Tooltip("Tilt the camera by this much")]
        [FormerlySerializedAs("m_Tilt")]
        public float Tilt;

        /// <summary>
        /// Pan the camera by this much
        /// </summary>
        [Tooltip("Pan the camera by this much")]
        [FormerlySerializedAs("m_Pan")]
        public float Pan;

        /// <summary>
        /// Roll the camera by this much
        /// </summary>
        [Tooltip("Roll the camera by this much")]
        [FormerlySerializedAs("m_Dutch")]
        public float Dutch;

        /// <summary>
        /// Scale the zoom by this amount (normal = 1)
        /// </summary>
        [Tooltip("Scale the zoom by this amount (normal = 1)")]
        [FormerlySerializedAs("m_ZoomScale")]
        [Delayed]
        public float ZoomScale;

        /// <summary>
        /// Lowering this value relaxes the camera's attention to the Follow target (normal = 1)
        /// </summary>
        [Range(0, 1)]
        [Tooltip("Lowering this value relaxes the camera's attention to the Follow target (normal = 1)")]
        [FormerlySerializedAs("m_FollowAttachment")]
        public float FollowAttachment;

        /// <summary>
        /// Lowering this value relaxes the camera's attention to the LookAt target (normal = 1)
        /// </summary>
        [Range(0, 1)]
        [Tooltip("Lowering this value relaxes the camera's attention to the LookAt target (normal = 1)")]
        [FormerlySerializedAs("m_LookAtAttachment")]
        public float LookAtAttachment;

        void Reset()
        {
            ApplyAfter = CinemachineCore.Stage.Finalize;
            Tilt = 0;
            Pan = 0;
            Dutch = 0;
            ZoomScale = 1;
            FollowAttachment = 1;
            LookAtAttachment = 1;
        }

        void OnValidate()
        {
            ZoomScale = Mathf.Max(0.01f, ZoomScale);
            FollowAttachment = Mathf.Clamp01(FollowAttachment);
            LookAtAttachment = Mathf.Clamp01(LookAtAttachment);
        }

        /// <summary>Callback to set the target attachment</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="curState">Input state that must be mutated</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        public override void PrePipelineMutateCameraStateCallback(
            CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime)
        {
            vcam.FollowTargetAttachment = FollowAttachment;
            vcam.LookAtTargetAttachment = LookAtAttachment;
        }

        /// <summary>Callback to tweak the settings</summary>
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
                var lens = state.Lens;

                // Tilt by local X
                var qTilted = state.RawOrientation * Quaternion.AngleAxis(Tilt, Vector3.right);
                // Pan in world space
                var qDesired = Quaternion.AngleAxis(Pan, state.ReferenceUp) * qTilted;
                state.OrientationCorrection = Quaternion.Inverse(state.GetCorrectedOrientation()) * qDesired;
                // And dutch at the end
                lens.Dutch += Dutch;
                // Finally zoom
                if (ZoomScale != 1)
                {
                    lens.OrthographicSize *= ZoomScale;
                    lens.FieldOfView *= ZoomScale;
                }
                state.Lens = lens;
            }
        }
    }
}
