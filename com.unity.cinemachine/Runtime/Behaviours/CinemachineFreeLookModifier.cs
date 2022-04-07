using UnityEngine;
using Cinemachine;
using System;

/// <summary>
/// This is an add-on for Cinemachine virtual cameras containing the OrbitalFollow component.
/// It modifies the camera distance as a function of vertical angle.
/// </summary>
[SaveDuringPlay] [AddComponentMenu("")] // Hide in menu
[ExecuteAlways]
public class CinemachineFreeLookModifier : CinemachineExtension
{
    [Serializable]
    public struct TopCenterBottom<T>
    {
        [Tooltip("If set, then this value can be varied as a function of the Vertical Axis value")]
        public bool Enabled;

        [Tooltip("Value to take at the top of the axis range")]
        public T Top;

        [Tooltip("Value to take at the center of the axis range")]
        public T Center;

        [Tooltip("Value to take at the bottom of the axis range")]
        public T Bottom;
    }

    [Serializable]
    public struct NoiseSettings
    {
        [Tooltip("Multiplier for the noise amplitude")]
        public float Amplitude;

        [Tooltip("Multiplier for the noise frequency")]
        public float Frequency;

        public static NoiseSettings Default => new NoiseSettings { Amplitude = 1, Frequency = 1 };
    }

    [Tooltip("Screen space vertical adustment for the target position.  "
        + "0 is screen center, +-1 is top/bottom of screen")]
    [FoldoutWithEnabledButton]
    public TopCenterBottom<float> Tilt;

    [Tooltip("Multipliers for the noise settings (if any).  The valuse will be used to scale the camera noise")]
    [FoldoutWithEnabledButton]
    public TopCenterBottom<NoiseSettings> Noise;

    [Tooltip("Multiplier for the lens FOV.  The value will be multiplied to the lens FOV")]
    [FoldoutWithEnabledButton]
    public TopCenterBottom<LensSettings> Lens;

    CinemachineOrbitalFollow m_Orbital;
    CinemachineBasicMultiChannelPerlin m_NoiseComponent;
    float m_LastSplineValue;

    // For storing and restoring the original settings
    NoiseSettings m_SourceNoise;
    LensSettings m_SourceLens;

    void OnValidate()
    {
        Tilt.Top = Mathf.Clamp(Tilt.Top, -30, 30);
        Tilt.Center = Mathf.Clamp(Tilt.Center, -30, 30);
        Tilt.Bottom = Mathf.Clamp(Tilt.Bottom, -30, 30);
        Lens.Top.Validate();
        Lens.Center.Validate();
        Lens.Bottom.Validate();
    }

    void Reset()
    {
        Tilt = new TopCenterBottom<float>() { Enabled = true, Top = -3, Bottom = 3 };
        Noise = new TopCenterBottom<NoiseSettings>
        {
            Top = NoiseSettings.Default, 
            Center = NoiseSettings.Default, 
            Bottom = NoiseSettings.Default 
        };

        var vcam = VirtualCamera as CinemachineVirtualCamera;
        var defaultLens = vcam == null ? LensSettings.Default : vcam.m_Lens;
        Lens = new TopCenterBottom<LensSettings> { Top = defaultLens, Center = defaultLens, Bottom = defaultLens };
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        var vcam = VirtualCamera as CinemachineVirtualCamera;
        if (vcam != null)
        {
            m_Orbital = vcam.GetCinemachineComponent<CinemachineOrbitalFollow>();
            m_NoiseComponent = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
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
        // Can't do anything without the vertical axis
        if (m_Orbital == null)
            return;

        var t = m_LastSplineValue = m_Orbital.GetVerticalAxisNormalizedValue();
        if (m_NoiseComponent != null && Noise.Enabled)
        {
            m_SourceNoise.Amplitude = m_NoiseComponent.m_AmplitudeGain;
            m_SourceNoise.Frequency = m_NoiseComponent.m_FrequencyGain;
            if (t >= 0)
            {
                m_SourceNoise.Amplitude = Mathf.Lerp(Noise.Center.Amplitude, Noise.Top.Amplitude, t);
                m_SourceNoise.Frequency = Mathf.Lerp(Noise.Center.Frequency, Noise.Top.Frequency, t);
            }
            else
            {
                m_SourceNoise.Amplitude = Mathf.Lerp(Noise.Bottom.Amplitude, Noise.Center.Amplitude, t + 1);
                m_SourceNoise.Frequency = Mathf.Lerp(Noise.Bottom.Frequency, Noise.Center.Frequency, t + 1);
            }
        }

        if (Lens.Enabled && vcam is CinemachineVirtualCamera)
        {
            m_SourceLens = (vcam as CinemachineVirtualCamera).m_Lens;
            if (t >= 0)
                curState.Lens = LensSettings.Lerp(Lens.Center, Lens.Top, t);
            else
                curState.Lens = LensSettings.Lerp(Lens.Bottom, Lens.Center, t + 1);
        }
    }
            
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize && m_Orbital != null)
        {
            // Restore the settings
            if (m_NoiseComponent != null && Noise.Enabled)
            {
                m_NoiseComponent.m_AmplitudeGain = m_SourceNoise.Amplitude;
                m_NoiseComponent.m_FrequencyGain = m_SourceNoise.Frequency;
            }
            if (Lens.Enabled && vcam is CinemachineVirtualCamera)
                (vcam as CinemachineVirtualCamera).m_Lens = m_SourceLens;

            // Apply the tilt
            if (Tilt.Enabled)
            {
                var t = m_LastSplineValue;
                float tilt = t > 0 
                    ? Mathf.Lerp(Tilt.Center, Tilt.Top, t) 
                    : Mathf.Lerp(Tilt.Bottom, Tilt.Center, t + 1);

                // Tilt in local X
                var qTilted = state.RawOrientation * Quaternion.AngleAxis(tilt, Vector3.right);
                state.OrientationCorrection = Quaternion.Inverse(state.CorrectedOrientation) * qTilted;
            }
        }
    }
}

