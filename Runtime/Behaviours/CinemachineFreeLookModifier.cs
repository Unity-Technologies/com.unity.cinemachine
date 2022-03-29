using UnityEngine;
using Cinemachine;

/// <summary>
/// This is an add-on for Cinemachine virtual cameras containing the OrbitalFollow component.
/// It modifies the camera distance as a function of vertical angle.
/// </summary>
[SaveDuringPlay] [AddComponentMenu("")] // Hide in menu
public class CinemachineFreeLookModifier : CinemachineExtension
{
    [Tooltip("Defines how the camera distance scales as a function of vertical camera angle.  "
        + "X axis of graph goes from 0 to 1, Y axis is the multiplier that will be "
        + "applied to the base distance.")]
    public AnimationCurve DistanceScale;

    [Tooltip("Defines the vertical offset for the Conposer.ScreenY as a function of vertical camera angle.  "
        + "X axis of graph goes from 0 to 1, Y axis is the Screen Y adjustment that will be applied.")]
    public AnimationCurve VerticalOffset;

    CinemachineOrbitalFollow m_Orbital;

    // GML TODO: Hardcoding this is lame, but I don't know how else to do it without using reflection
    CinemachineComposer m_Composer;

    float m_BaseDistance;
    float m_BaseOffset;

    void Reset()
    {
        DistanceScale = AnimationCurve.EaseInOut(0, 0.5f, 1, 2);
        VerticalOffset = AnimationCurve.EaseInOut(0, 0f, 1, 0.3f);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        var vcam = VirtualCamera as CinemachineVirtualCamera;
        if (vcam != null)
        {
            m_Orbital = vcam.GetCinemachineComponent<CinemachineOrbitalFollow>();
            m_Composer = vcam.GetCinemachineComponent<CinemachineComposer>();
        }
    }

    /// <summary>Override this to do such things as offset the RefereceLookAt.
    /// Base class implementation does nothing.</summary>
    /// <param name="vcam">The virtual camera being processed</param>
    /// <param name="curState">Input state that must be mutated</param>
    /// <param name="deltaTime">The current applicable deltaTime</param>
    public override void PrePipelineMutateCameraStateCallback(
        CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime) 
    {
        // Store the base camera distance, for consistent scaling
        if (m_Orbital != null)
        {
            m_BaseDistance = m_Orbital.CameraDistance;

            // Scale the camera distance
            var t = m_Orbital.VerticalAxis.GetNormalizedValue();
            m_Orbital.CameraDistance = m_BaseDistance * DistanceScale.Evaluate(t);

            if (m_Composer != null)
            {
                // GML TODO: damping seems to interfere with this.  Need new damping algo.  I have an idea for one.
                m_BaseOffset = m_Composer.m_ScreenY;
                m_Composer.m_ScreenY = m_BaseOffset + VerticalOffset.Evaluate(t);
            }
        }
    }
            
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize && m_Orbital != null)
        {
            m_Orbital.CameraDistance = m_BaseDistance;
            if (m_Composer != null)
                m_Composer.m_ScreenY = m_BaseOffset;
        }
    }
}

