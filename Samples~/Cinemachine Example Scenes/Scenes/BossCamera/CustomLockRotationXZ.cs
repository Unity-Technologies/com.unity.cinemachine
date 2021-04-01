using UnityEngine;
using Cinemachine;

/// <summary>
/// An add-on module for Cinemachine Virtual Camera that locks the camera's Y co-ordinate
/// </summary>
[ExecuteAlways]
[SaveDuringPlay]
[AddComponentMenu("")] // Hide in menu
public class CustomLockRotationXZ : CinemachineExtension
{
    [Tooltip("Lock the camera's X rotation to this value (in angles)")]
    public float m_RotationX = 0;
    [Tooltip("Lock the camera's Z rotation to this value (in angles)")]
    public float m_RotationZ = 0;
    
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize)
        {
            var euler = state.RawOrientation.eulerAngles;
            state.RawOrientation = Quaternion.Euler(m_RotationX, euler.y, m_RotationZ);
        }
    }
}
