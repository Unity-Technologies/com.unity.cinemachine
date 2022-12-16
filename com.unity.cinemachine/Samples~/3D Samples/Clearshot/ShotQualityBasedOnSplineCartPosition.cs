using Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public class ShotQualityBasedOnSplineCartPosition : CinemachineExtension, IShotQualityEvaluator
{
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Aim)
        {
            if (vcam.Follow.TryGetComponent(out CinemachineSplineCart cart) && cart.Spline != null)
                state.ShotQuality = cart.Spline.Spline.ConvertIndexUnit(
                    cart.SplinePosition, cart.PositionUnits, PathIndexUnit.Distance);
            else
                state.ShotQuality = 0;
        }
    }
}
