using UnityEngine;
using Cinemachine;

/// <summary>
/// An add-on module for Cinemachine Virtual Camera that adds a final offset to the camera
/// </summary>
[AddComponentMenu("")] // Hide in menu
public class CinemachineCameraOffset : CinemachineExtension 
{
    [Tooltip("Offset the camera's position by this much (camera space)")]
    public Vector3 m_Offset = Vector3.zero;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        // Apply after the camera has been aimed.
        // To apply offset before the camera aims, change this to Body
        if (stage == CinemachineCore.Stage.Aim)
        {
            Vector3 offset = state.FinalOrientation * m_Offset;
            state.ReferenceLookAt += offset;
            state.PositionCorrection += offset;
        }
    }
}
