using UnityEngine.Splines;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Calculates shot quality based on how far the Follow target has advanced
    /// with its CinemachineSplineCart.
    /// </summary>
    public class ShotQualityBasedOnSplineCartPosition : CinemachineExtension, IShotQualityEvaluator
    {
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize)
            {
                state.ShotQuality = 0;
                if (vcam.Follow != null
                    && vcam.Follow.TryGetComponent(out CinemachineSplineCart cart)
                    && cart.Spline != null && cart.Spline.Spline != null)
                {
                    // Shot quality is just the normalized distance along the path
                    state.ShotQuality = cart.Spline.Spline.ConvertIndexUnit(
                        cart.SplinePosition, cart.PositionUnits, PathIndexUnit.Normalized);
                }
            }
        }
    }
}
