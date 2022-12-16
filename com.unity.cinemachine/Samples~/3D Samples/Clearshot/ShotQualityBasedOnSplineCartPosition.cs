using Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Calculates shot quality based on how far the Follow target has advances with its CinemachineSplineCart.
/// </summary>
public class ShotQualityBasedOnSplineCartPosition : CinemachineExtension, IShotQualityEvaluator
{
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize)
        {
            if (vcam.Follow != null &&
                vcam.Follow.TryGetComponent(out CinemachineSplineCart cart) &&
                cart.Spline != null)
            {
                state.ShotQuality = cart.Spline.Spline.ConvertIndexUnit(
                    cart.SplinePosition, cart.PositionUnits, PathIndexUnit.Distance);
            }
            else
                state.ShotQuality = 0;
        }
    }
}
