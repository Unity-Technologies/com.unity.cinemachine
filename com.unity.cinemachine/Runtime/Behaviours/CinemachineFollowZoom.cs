using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for Cm Camera that adjusts
    /// the FOV of the lens to keep the target object at a constant size on the screen,
    /// regardless of camera and target position.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Follow Zoom")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.LookAt)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineFollowZoom.html")]
    public class CinemachineFollowZoom : CinemachineExtension
    {
        /// <summary>The shot width to maintain, in world units, at target distance.
        /// FOV will be adjusted as far as possible to maintain this width at the
        /// target distance from the camera.</summary>
        [Tooltip("The shot width to maintain, in world units, at target distance.")]
        [FormerlySerializedAs("m_Width")]
        public float Width = 2f;

        /// <summary>Increase this value to soften the aggressiveness of the follow-zoom.
        /// Small numbers are more responsive, larger numbers give a more heavy slowly responding camera. </summary>
        [Range(0f, 20f)]
        [Tooltip("Increase this value to soften the aggressiveness of the follow-zoom.  "
            + "Small numbers are more responsive, larger numbers give a more heavy slowly responding camera.")]
        [FormerlySerializedAs("m_Damping")]
        public float Damping = 1f;

        /// <summary>Range for the FOV that this behaviour will generate.</summary>
        [MinMaxRangeSlider(1f, 179f)]
        [Tooltip("Range for the FOV that this behaviour will generate.")]
        public Vector2 FovRange = new (3f, 60f);

        void Reset()
        {
            Width = 2f;
            Damping = 1f;
            FovRange = new (3f, 60f);
        }

        void OnValidate()
        {
            Width = Mathf.Max(0, Width);
            FovRange.y = Mathf.Clamp(FovRange.y, 1, 179);
            FovRange.x = Mathf.Clamp(FovRange.x, 1, FovRange.y);
        }

        class VcamExtraState : VcamExtraStateBase
        {
            public float m_PreviousFrameZoom = 0;
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Damping;
        
        /// <summary>Callback to preform the zoom adjustment</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            var extra = GetExtraState<VcamExtraState>(vcam);
            if (deltaTime < 0 || !vcam.PreviousStateIsValid)
                extra.m_PreviousFrameZoom = state.Lens.FieldOfView;

            // Set the zoom after the body has been positioned, but before the aim,
            // so that composer can compose using the updated fov.
            if (stage == CinemachineCore.Stage.Body)
            {
                // Try to reproduce the target width
                var targetWidth = Mathf.Max(Width, 0);
                var fov = 179f;
                var d = Vector3.Distance(state.GetCorrectedPosition(), state.ReferenceLookAt);
                if (d > UnityVectorExtensions.Epsilon)
                {
                    // Clamp targetWidth to FOV min/max
                    var minW = d * 2f * Mathf.Tan(FovRange.x * Mathf.Deg2Rad / 2f);
                    var maxW = d * 2f * Mathf.Tan(FovRange.y * Mathf.Deg2Rad / 2f);
                    targetWidth = Mathf.Clamp(targetWidth, minW, maxW);

                    // Apply damping
                    if (deltaTime >= 0 && Damping > 0 && vcam.PreviousStateIsValid)
                    {
                        var currentWidth = d * 2f * Mathf.Tan(extra.m_PreviousFrameZoom * Mathf.Deg2Rad / 2f);
                        var delta = targetWidth - currentWidth;
                        delta = vcam.DetachedLookAtTargetDamp(delta, Damping, deltaTime);
                        targetWidth = currentWidth + delta;
                    }
                    fov = 2f * Mathf.Atan(targetWidth / (2 * d)) * Mathf.Rad2Deg;
                }
                var lens = state.Lens;
                lens.FieldOfView = extra.m_PreviousFrameZoom = Mathf.Clamp(fov, FovRange.x, FovRange.y);
                state.Lens = lens;
            }
        }
    }
}
