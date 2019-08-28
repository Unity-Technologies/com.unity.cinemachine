using UnityEngine;
using Cinemachine.Utility;
using Cinemachine;

/// <summary>
/// An add-on module for Cinemachine Virtual Camera that adds a final offset to the camera
/// </summary>
[AddComponentMenu("")] // Hide in menu
#if UNITY_2018_3_OR_NEWER
[ExecuteAlways]
#else
[ExecuteInEditMode]
#endif
public class CinemachineCameraOffset : CinemachineExtension
{
    [Tooltip("Offset the camera's position by this much (camera space)")]
    public Vector3 m_Offset = Vector3.zero;

    [Tooltip("When to apply the offset")]
    public CinemachineCore.Stage m_ApplyAfter = CinemachineCore.Stage.Aim;

    [Tooltip("If applying offset after aim, re-adjust the aim to preserve the screen position"
        + " of the LookAt target as much as possible")]
    public bool m_PreserveComposition;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == m_ApplyAfter)
        {
            bool preserveAim = m_PreserveComposition
                && state.HasLookAt && stage > CinemachineCore.Stage.Body;

            Vector3 screenOffset = Vector2.zero;
            if (preserveAim)
            {
                screenOffset = state.RawOrientation.GetCameraRotationToTarget(
                    state.ReferenceLookAt - state.CorrectedPosition, state.ReferenceUp);
            }

            Vector3 offset = state.RawOrientation * m_Offset;
            state.PositionCorrection += offset;
            if (!preserveAim)
                state.ReferenceLookAt += offset;
            else
            {
                var q = Quaternion.LookRotation(
                    state.ReferenceLookAt - state.CorrectedPosition, state.ReferenceUp);
                q = q.ApplyCameraRotation(-screenOffset, state.ReferenceUp);
                state.RawOrientation = q;
            }
        }
    }
}
