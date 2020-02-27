using UnityEngine;
using Cinemachine;

/// <summary>
/// An add-on module for Cinemachine Virtual Camera that adds a final tweak to the camera
/// comnposition.  It is intended for use in a Timeline context, where you want to hand-adjust
/// the output of procedural or recorded camera aiming.
/// </summary>
[AddComponentMenu("")] // Hide in menu
#if UNITY_2018_3_OR_NEWER
[ExecuteAlways]
#else
[ExecuteInEditMode]
#endif
public class CinemachineCompositionAdjustment : CinemachineExtension
{
    [Tooltip("Tilt the camera by this much")]
    public float m_Tilt;
    [Tooltip("Pan the camera by this much")]
    public float m_Pan;
    [Tooltip("Roll the camera by this much")]
    public float m_Dutch;

    [Tooltip("Scale the zoom by this amount (normal = 1)")]
    public float m_ZoomScale;

    [Tooltip("When to apply the adjustment")]
    public CinemachineCore.Stage m_ApplyAfter;

    private void Reset()
    {
        m_Tilt = 0;
        m_Pan = 0;
        m_Dutch = 0;
        m_ZoomScale = 1;
        m_ApplyAfter = CinemachineCore.Stage.Finalize;
    }

    private void OnValidate()
    {
        m_ZoomScale =Mathf.Max(0.01f, m_ZoomScale);
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == m_ApplyAfter)
        {
            var lens = state.Lens;

            // Tilt by local X
            var qTilted = state.RawOrientation * Quaternion.AngleAxis(m_Tilt, Vector3.right);
            // Pan in world space
            var qDesired = Quaternion.AngleAxis(m_Pan, state.ReferenceUp) * qTilted;
            state.OrientationCorrection = Quaternion.Inverse(state.CorrectedOrientation) * qDesired;
            // And dutch at the end
            lens.Dutch += m_Dutch;
            // Finally zoom
            if (m_ZoomScale != 1)
            {
                lens.OrthographicSize *= m_ZoomScale;
                lens.FieldOfView *= m_ZoomScale;
            }
            state.Lens = lens;
        }
    }
}

