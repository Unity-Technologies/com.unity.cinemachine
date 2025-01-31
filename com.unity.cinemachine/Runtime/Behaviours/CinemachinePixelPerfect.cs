using UnityEngine;

#if CINEMACHINE_URP || CINEMACHINE_PIXEL_PERFECT_2_0_3

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera that tweaks the orthographic size
    /// of the camera. It detects the presence of the Pixel Perfect Camera component and use the
    /// settings from that Pixel Perfect Camera to correct the orthographic size so that pixel art
    /// sprites would appear pixel perfect when the camera becomes live.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Pixel Perfect")] // Hide in menu
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachinePixelPerfect.html")]
    public class CinemachinePixelPerfect : CinemachineExtension
    {
        /// <summary>Callback to tweak the orthographic size</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // This must run during the Body stage because CinemachineConfiner also runs during Body stage,
            // and CinemachinePixelPerfect needs to run before CinemachineConfiner as the confiner reads the
            // orthographic size. We also altered the script execution order to ensure this.
            if (stage != CinemachineCore.Stage.Body)
                return;

            var brain = CinemachineCore.FindPotentialTargetBrain(vcam);
            if (brain == null || !brain.IsLiveChild(vcam))
                return;

#if CINEMACHINE_URP
  #if UNITY_2023_2_OR_NEWER
            UnityEngine.Rendering.Universal.PixelPerfectCamera pixelPerfectCamera;
  #else
            UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera pixelPerfectCamera;
  #endif
#elif CINEMACHINE_PIXEL_PERFECT_2_0_3
            UnityEngine.U2D.PixelPerfectCamera pixelPerfectCamera;
#endif
            brain.TryGetComponent(out pixelPerfectCamera);
            if (pixelPerfectCamera == null || !pixelPerfectCamera.isActiveAndEnabled)
                return;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && !pixelPerfectCamera.runInEditMode)
                return;
#endif

            var lens = state.Lens;
            lens.OrthographicSize = pixelPerfectCamera.CorrectCinemachineOrthoSize(lens.OrthographicSize);
            state.Lens = lens;
        }
    }
}
#else

// We need this dummy MonoBehaviour for Unity to properly recognize this script asset.
namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera Camera that tweaks the orthographic size
    /// of the camera. It detects the presence of the Pixel Perfect Camera component and use the
    /// settings from that Pixel Perfect Camera to correct the orthographic size so that pixel art
    /// sprites would appear pixel perfect when the camera becomes live.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    [DisallowMultipleComponent]
    public class CinemachinePixelPerfect : MonoBehaviour {}
}
#endif
