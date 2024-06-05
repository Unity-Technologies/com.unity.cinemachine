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

        [Serializable]
        public struct FadeOutRangeSettings
        {
            [Tooltip("If not enabled, range will be automatically set to be between this virtual camera and LookAt target plus LookAtTargetRadius.")]
            public bool Manual;

            [Tooltip("Objects will be fully transparent at the near distance from the camera, and fully opaque at the far distance.")]
            [MinMaxRangeSlider(0, 20)]
            public Vector2 Range;
        }

        [EnabledProperty("Manual", "(automatic)")]
        public FadeOutRangeSettings FadeOutRange = new () { Range = new Vector2(0, 10) };

        [Tooltip("If true, MaxDistance will be set to " +
            "distance between this virtual camera and LookAt target plus LookAtTargetRadius.")]
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
                if (FadeOutMaterial == null || !FadeOutMaterial.HasProperty(k_MaxDistanceID) || !FadeOutMaterial.HasProperty(k_MinDistanceID)) 
                    return;
            
                var range = FadeOutRange.Range;
                if (!FadeOutRange.Manual && vcam.LookAt != null) 
                    range = new(0, Vector3.Distance(vcam.transform.position, vcam.LookAt.position) + LookAtTargetRadius);
                FadeOutMaterial.SetFloat(k_MinDistanceID, range.x);
                FadeOutMaterial.SetFloat(k_MaxDistanceID, range.y);
            }
        }
        
        void OnValidate()
        {
            LookAtTargetRadius = Math.Max(0, LookAtTargetRadius);
            FadeOutRange.Range.x = Math.Max(0, FadeOutRange.Range.x);
            FadeOutRange.Range.y = Math.Max(0, FadeOutRange.Range.y);
            FadeOutRange.Range.y = Math.Max(FadeOutRange.Range.x, FadeOutRange.Range.y);
        }
    }
}
