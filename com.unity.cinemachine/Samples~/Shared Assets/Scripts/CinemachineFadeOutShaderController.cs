using System;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// An example add-on module for Cinemachine Virtual Camera for controlling
    /// the FadeOut shader included in our example package.
    /// </summary>
    [ExecuteAlways]
    public class CinemachineFadeOutShaderController : CinemachineExtension
    {
        /// <summary>Radius of the look at target.</summary>
        [Tooltip("Radius of the look at target.")]
        public float LookAtTargetRadius = 1;

        /// <summary>
        /// The range is defined from the camera (x value) towards the look direction (y value) within which objects
        /// with FadeOut material are going to be transparent.
        /// The strength of the transparency depends on how far the objects are from the camera and the FadeOut range.
        /// </summary>
        [Tooltip("The range is defined from the camera (x value) towards the look direction (y value) within which " +
            "objects with FadeOut material are going to be transparent. \nThe strength of the transparency depends " +
            "on how far the objects are from the camera and the FadeOut range.")]
        [MinMaxRangeSlider(0, 20)]
        public Vector2 FadeOutRange = new (0f, 10f);

        /// <summary>
        /// If true, m_MaxDistance will be set to
        /// distance between this virtual camera and LookAt target minus m_LookAtTargetRadius.
        /// </summary>
        [Tooltip("If true, MaxDistance will be set to " +
            "distance between this virtual camera and LookAt target minus LookAtTargetRadius.")]
        public bool MaxDistanceControlledByCamera = true;
    
        /// <summary>Material using the FadeOut shader.</summary>
        [Tooltip("Material using the FadeOut shader.")]
        public Material FadeOutMaterial;

        static readonly int k_MaxDistanceID = Shader.PropertyToID("_MaxDistance");
        static readonly int k_MinDistanceID = Shader.PropertyToID("_MinDistance");

        /// <summary>Updates FadeOut shader on the specified FadeOutMaterial.</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize)
            {
                if (FadeOutMaterial == null || 
                    !FadeOutMaterial.HasProperty(k_MaxDistanceID) || 
                    !FadeOutMaterial.HasProperty(k_MinDistanceID)) 
                    return;
            
                if (MaxDistanceControlledByCamera && vcam.LookAt != null) 
                    FadeOutRange.y = Vector3.Distance(vcam.transform.position, vcam.LookAt.position) - LookAtTargetRadius;

                FadeOutMaterial.SetFloat(k_MinDistanceID, FadeOutRange.x);
                FadeOutMaterial.SetFloat(k_MaxDistanceID, FadeOutRange.y);
            }
        }
        
        void OnValidate()
        {
            LookAtTargetRadius = Math.Max(0, LookAtTargetRadius);
            FadeOutRange.x = Math.Max(0, FadeOutRange.x);
            FadeOutRange.y = Math.Max(0, FadeOutRange.y);
            FadeOutRange.y = Math.Max(FadeOutRange.x, FadeOutRange.y);
        }
    }
}
