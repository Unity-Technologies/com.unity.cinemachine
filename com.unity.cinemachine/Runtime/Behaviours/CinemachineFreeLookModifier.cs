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

    [Tooltip("Defines how the camera distance changes as a function of vertical camera angle.")]
    [HideFoldout]
    public Cinemachine3OrbitRig.Settings Orbits = Cinemachine3OrbitRig.Settings.Default;

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
    Vector4 m_LastSplineValue;

    // For storing and restoring the original orbitalFollow settings
    float m_SourceDistance;
    public Vector3 m_SourceRange; // x = min, y = max, z = center
    NoiseSettings m_SourceNoise;
    LensSettings m_SourceLens;

    Cinemachine3OrbitRig.OrbitSplineCache m_OrbitCache;

    /// Needed by inspector
    internal Vector3 GetCameraOffsetForNormalizedAxisValue(float t) => m_OrbitCache.CachedSplineValue(t);

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
        Orbits = Cinemachine3OrbitRig.Settings.Default;
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

            if (m_Orbital != null)
            {
                m_SourceRange = m_Orbital.VerticalAxis.Range;
                m_SourceRange.z = m_Orbital.VerticalAxis.Center;
                m_OrbitCache.UpdateOrbitCache(Orbits);
                m_Orbital.VerticalAxis.Range = m_OrbitCache.InferredRange;
                m_Orbital.VerticalAxis.Center = m_OrbitCache.InferredRange.z;
            }
        }
    }

    void OnDisable()
    {
        m_Orbital.VerticalAxis.Range = m_SourceRange;
        m_Orbital.VerticalAxis.Center = m_SourceRange.z;
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

        m_SourceDistance = m_Orbital.CameraDistance;
        if (m_OrbitCache.SettingsChanged(Orbits))
            m_OrbitCache.UpdateOrbitCache(Orbits);

        m_LastSplineValue = m_OrbitCache.CachedSplineValue(m_Orbital.VerticalAxis.GetNormalizedValue());

        m_Orbital.VerticalAxis.Range = m_OrbitCache.InferredRange;
        m_Orbital.VerticalAxis.Center = m_OrbitCache.InferredRange.z;
        Vector3 pos = m_LastSplineValue;
        m_Orbital.CameraDistance = pos.magnitude;

        var t = m_LastSplineValue.w;
        if (m_NoiseComponent != null && Noise.Enabled)
        {
            m_SourceNoise.Amplitude = m_NoiseComponent.m_AmplitudeGain;
            m_SourceNoise.Frequency = m_NoiseComponent.m_FrequencyGain;
            if (t > 1)
            {
                m_SourceNoise.Amplitude = Mathf.Lerp(Noise.Center.Amplitude, Noise.Top.Amplitude, t - 1);
                m_SourceNoise.Frequency = Mathf.Lerp(Noise.Center.Frequency, Noise.Top.Frequency, t - 1);
            }
            else
            {
                m_SourceNoise.Amplitude = Mathf.Lerp(Noise.Bottom.Amplitude, Noise.Center.Amplitude, t);
                m_SourceNoise.Frequency = Mathf.Lerp(Noise.Bottom.Frequency, Noise.Center.Frequency, t);
            }
        }

        if (Lens.Enabled && vcam is CinemachineVirtualCamera)
        {
            m_SourceLens = (vcam as CinemachineVirtualCamera).m_Lens;
            if (t > 1)
                curState.Lens = LensSettings.Lerp(Lens.Center, Lens.Top, t - 1);
            else
                curState.Lens = LensSettings.Lerp(Lens.Bottom, Lens.Center, t);
        }
    }
            
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize && m_Orbital != null)
        {
            // Restore the settings
            m_Orbital.CameraDistance = m_SourceDistance;
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
                var t = m_LastSplineValue.w;
                float tilt = t > 1 
                    ? Mathf.Lerp(Tilt.Center, Tilt.Top, t - 1) 
                    : Mathf.Lerp(Tilt.Bottom, Tilt.Center, t);

                // Tilt in local X
                var qTilted = state.RawOrientation * Quaternion.AngleAxis(tilt, Vector3.right);
                state.OrientationCorrection = Quaternion.Inverse(state.CorrectedOrientation) * qTilted;
            }
        }
    }
}

