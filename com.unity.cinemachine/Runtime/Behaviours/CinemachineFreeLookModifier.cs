using UnityEngine;
using Cinemachine;
using System;
using Cinemachine.Utility;

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

    // GML TODO: move Orbit out of CinemachineNewFreeLook
    [Serializable]
    public struct OrbitSettings
    {
        [Tooltip("Value to take at the top of the axis range")]
        public CinemachineNewFreeLook.Orbit Top;

        [Tooltip("Value to take at the center of the axis range")]
        public CinemachineNewFreeLook.Orbit Center;

        [Tooltip("Value to take at the bottom of the axis range")]
        public CinemachineNewFreeLook.Orbit Bottom;

        /// <summary></summary>
        [Tooltip("Controls how taut is the line that connects the rigs' orbits, which determines final placement on the Y axis")]
        [Range(0f, 1f)]
        public float SplineCurvature;
    }


    [Tooltip("Defines how the camera distance changes as a function of vertical camera angle.")]
    [HideFoldout]
    public OrbitSettings Orbits; // GML TODO: move Orbit out of CinemachineNewFreeLook

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
    NoiseSettings m_SourceNoise;
    LensSettings m_SourceLens;
    struct OrbitCache
    {
        public Vector3 SourceRange; // x = min, y = max, z = center
        public Vector3 InferredRange; // x = min, y = max, z = center
        public OrbitSettings Orbits;

        public bool OrbitsChanged(ref OrbitSettings other)
        {
            return Orbits.SplineCurvature != other.SplineCurvature
                || Orbits.Top.m_Height != other.Top.m_Height || Orbits.Top.m_Radius != other.Top.m_Radius
                || Orbits.Center.m_Height != other.Center.m_Height || Orbits.Center.m_Radius != other.Center.m_Radius
                || Orbits.Bottom.m_Height != other.Bottom.m_Height || Orbits.Bottom.m_Radius != other.Bottom.m_Radius;
        }

        public Vector4[] CachedKnots;
        public Vector4[] CachedCtrl1;
        public Vector4[] CachedCtrl2;

        public Vector4[] SplineLookup;

        // 0 >= t >= 1
        public Vector4 RawSplineValue(float t)
        {
            int n = 1;
            if (t > 0.5f)
            {
                t -= 0.5f;
                n = 2;
            }
            Vector4 pos = SplineHelpers.Bezier3(
                t * 2f, CachedKnots[n], CachedCtrl1[n], CachedCtrl2[n], CachedKnots[n+1]);

            // 0 <= w <= 2, where 1 == center
            pos.w = SplineHelpers.Bezier1(
                t * 2f, CachedKnots[n].w, CachedCtrl1[n].w, CachedCtrl2[n].w, CachedKnots[n+1].w);
            return pos;
        }
    }
    OrbitCache m_OrbitCache;
    
    internal const int kPositionLookupSize = 32;

    void UpdateOrbitCache()
    {
        m_OrbitCache.InferredRange = new Vector3(
            Mathf.Atan2(Orbits.Bottom.m_Height, Orbits.Bottom.m_Radius) * Mathf.Rad2Deg,
            Mathf.Atan2(Orbits.Top.m_Height, Orbits.Top.m_Radius) * Mathf.Rad2Deg,
            Mathf.Atan2(Orbits.Center.m_Height, Orbits.Center.m_Radius) * Mathf.Rad2Deg);
        m_OrbitCache.Orbits = Orbits;

        float t = Orbits.SplineCurvature;
        m_OrbitCache.CachedKnots = new Vector4[5];
        m_OrbitCache.CachedCtrl1 = new Vector4[5];
        m_OrbitCache.CachedCtrl2 = new Vector4[5];
        m_OrbitCache.CachedKnots[1] = new Vector4(0, Orbits.Bottom.m_Height, -Orbits.Bottom.m_Radius, 0);
        m_OrbitCache.CachedKnots[2] = new Vector4(0, Orbits.Center.m_Height, -Orbits.Center.m_Radius, 1);
        m_OrbitCache.CachedKnots[3] = new Vector4(0, Orbits.Top.m_Height, -Orbits.Top.m_Radius, 2);
        m_OrbitCache.CachedKnots[0] = Vector4.Lerp(m_OrbitCache.CachedKnots[1], Vector4.zero, t);
        m_OrbitCache.CachedKnots[4] = Vector4.Lerp(m_OrbitCache.CachedKnots[3], Vector4.zero, t);
        SplineHelpers.ComputeSmoothControlPoints(
            ref m_OrbitCache.CachedKnots, ref m_OrbitCache.CachedCtrl1, ref m_OrbitCache.CachedCtrl2);

        // We have to sample the spline at even angular steps because the input
        // is angle, not spline position
        var stepSize = 1.0f / kPositionLookupSize;
        var range = m_OrbitCache.InferredRange.y - m_OrbitCache.InferredRange.x;
        m_OrbitCache.SplineLookup = new Vector4[kPositionLookupSize + 1];
        m_OrbitCache.SplineLookup[0] = m_OrbitCache.RawSplineValue(0);
        float tSpline = 0;
        var lastAngle = m_OrbitCache.InferredRange.x;
        var lastSplinePoint = m_OrbitCache.SplineLookup[0];
        for (int i = 1; i < kPositionLookupSize; ++i)
        {
            t = i * stepSize;
            var splinePoint = lastSplinePoint;
            var targetAngle = m_OrbitCache.InferredRange.x + t * range;
            float a = lastAngle;
            while (a < targetAngle && tSpline < 1)
            {
                tSpline += stepSize * 0.5f;
                splinePoint = m_OrbitCache.RawSplineValue(tSpline);
                a = UnityVectorExtensions.SignedAngle(Vector3.back, splinePoint, Vector3.right);
            }
            m_OrbitCache.SplineLookup[i] = Vector4.Lerp(
                lastSplinePoint, splinePoint, (targetAngle - lastAngle) / (a - lastAngle));

            if (a < targetAngle + stepSize * range)
            {
                lastSplinePoint = splinePoint;
                lastAngle = a;
            }
        }
        m_OrbitCache.SplineLookup[kPositionLookupSize] = m_OrbitCache.RawSplineValue(1);
    }

    /// Needed by inspector
    internal Vector3 GetCameraOffsetForNormalizedAxisValue(float t) => SplineValue(Mathf.Clamp01(t));

    // 0 >= t >= 1
    Vector4 SplineValue(float t) 
    {
        t *= kPositionLookupSize;
        var a = Mathf.Floor(t);
        var b = Mathf.Ceil(t);
        return Vector4.Lerp(m_OrbitCache.SplineLookup[(int)a], m_OrbitCache.SplineLookup[(int)b], t - a);
    }
    
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
        Orbits = new OrbitSettings
        { 
            SplineCurvature = 0,
            Top = new CinemachineNewFreeLook.Orbit { m_Height = 15, m_Radius = 3 },
            Center = new CinemachineNewFreeLook.Orbit { m_Height = 3, m_Radius = 10 },
            Bottom= new CinemachineNewFreeLook.Orbit { m_Height = -0.75f, m_Radius = 5 }
        };
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
                m_OrbitCache.SourceRange = m_Orbital.VerticalAxis.Range;
                m_OrbitCache.SourceRange.z = m_Orbital.VerticalAxis.Center;
                UpdateOrbitCache();
                m_Orbital.VerticalAxis.Range = m_OrbitCache.InferredRange;
                m_Orbital.VerticalAxis.Center = m_OrbitCache.InferredRange.z;
            }
        }
    }

    void OnDisable()
    {
        m_Orbital.VerticalAxis.Range = m_OrbitCache.SourceRange;
        m_Orbital.VerticalAxis.Center = m_OrbitCache.SourceRange.z;
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
        if (m_OrbitCache.OrbitsChanged(ref Orbits))
            UpdateOrbitCache();

        m_LastSplineValue = SplineValue(m_Orbital.VerticalAxis.GetNormalizedValue());

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

