using UnityEngine;

#if CINEMACHINE_LWRP_7_1_3 || CINEMACHINE_PIXEL_PERFECT_2_0_3

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that tweaks the orthographic size
    /// of the virtual camera. It detects the presence of the Pixel Perfect Camera component and use the
    /// settings from that Pixel Perfect Camera to correct the orthographic size so that pixel art
    /// sprites would appear pixel perfect when the virtual camera becomes live.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    [ExecuteAlways]
    public class CinemachinePixelPerfect : CinemachineExtension
    {
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // This must run during the Body stage because CinemachineConfiner also runs during Body stage,
            // and CinemachinePixelPerfect needs to run before CinemachineConfiner as the confiner reads the
            // orthographic size. We also altered the script execution order to ensure this.
            if (stage != CinemachineCore.Stage.Body)
                return;

            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(vcam);
            if (brain == null || !brain.IsLive(vcam))
                return;

#if CINEMACHINE_LWRP_7_1_3
            UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera pixelPerfectCamera;
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
namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that tweaks the orthographic size
    /// of the virtual camera. It detects the presence of the Pixel Perfect Camera component and use the
    /// settings from that Pixel Perfect Camera to correct the orthographic size so that pixel art
    /// sprites would appear pixel perfect when the virtual camera becomes live.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    public class CinemachinePixelPerfect : MonoBehaviour {}
}
#endif
