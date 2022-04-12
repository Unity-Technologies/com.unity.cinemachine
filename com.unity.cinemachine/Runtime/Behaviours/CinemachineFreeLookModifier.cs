using UnityEngine;
using Cinemachine;
using System;
using System.Collections.Generic;

/// <summary>
/// This is an add-on for Cinemachine virtual cameras containing the OrbitalFollow component.
/// It modifies the camera distance as a function of vertical angle.
/// </summary>
[SaveDuringPlay] [AddComponentMenu("")] // Hide in menu
[ExecuteAlways]
public class CinemachineFreeLookModifier : CinemachineExtension
{
    /// <summary>
    /// Interface for an object that will modify some aspect of a FreeLook camera
    /// based on the vertical axis value. 
    /// </summary>
    [Serializable]
    public abstract class Modifier
    {
        /// <summary>Called from OnValidate in the editor.  Validate and sanitize the fields.</summary>
        /// <param name="vcam">the virtual camera owner</param>
        public virtual void Validate(CinemachineVirtualCameraBase vcam) {}

        /// <summary>Called when the modifier is created.  Initialize fields with appropriate values.</summary>
        /// <param name="vcam">the virtual camera owner</param>
        public virtual void Reset(CinemachineVirtualCameraBase vcam) {}

        /// <summary>Called from OnEnable and from the inspector.  Refresh any internal component caches.</summary>
        /// <param name="vcam">the virtual camera owner</param>
        public virtual void RefreshCache(CinemachineVirtualCameraBase vcam) {}

        /// <summary>
        /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to 
        /// modify relevant camera settings.  Original camera settings should be restored in .
        /// </summary>
        /// <param name="vcam">vcam owner</param>
        /// <param name="state">current vcam state.  May be modified in this function</param>
        /// <param name="deltaTime">current applicable deltaTime</param>
        /// <param name="orbitalSplineValue">The value of the orbital Follow's vertical axis.  
        /// Ranges from -1 to 1, where 0 is center rig.</param>
        public virtual void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, 
            float orbitalSplineValue) {}

        /// <summary>
        /// Called from extension's PostPipelineStageCallback(Finalize).  Perform any necessary actions to state,
        /// and restore any camera parameters changed in <see cref="BeforePipeline"/>.
        /// </summary>
        /// <param name="vcam">vcam owner</param>
        /// <param name="state">current vcam state.  May be modofied in this function</param>
        /// <param name="deltaTime">current applicable deltaTime</param>
        /// <param name="orbitalSplineValue">The value of the orbital Follow's vertical axis.  
        /// Ranges from -1 to 1, where 0 is center rig.</param>
        public virtual void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float orbitalSplineValue) {}
    }

    /// <summary>
    /// Builtin FreeLook modifier for camera tilt.  Applies a vertical rotation to the camera 
    /// at the end of the camera pipeline.
    /// </summary>
    public class TiltModifier : Modifier
    {
        [HideFoldout]
        public TopCenterBottom<float> Tilt;

        public override void Validate(CinemachineVirtualCameraBase vcam)
        {
            Tilt.Top = Mathf.Clamp(Tilt.Top, -30, 30);
            Tilt.Center = Mathf.Clamp(Tilt.Center, -30, 30);
            Tilt.Bottom = Mathf.Clamp(Tilt.Bottom, -30, 30);
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
            => Tilt = new TopCenterBottom<float>() { Top = -3, Bottom = 3 };

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float orbitalSplineValue)
        {
            float tilt = orbitalSplineValue > 0 
                ? Mathf.Lerp(Tilt.Center, Tilt.Top, orbitalSplineValue) 
                : Mathf.Lerp(Tilt.Bottom, Tilt.Center, orbitalSplineValue + 1);

            // Tilt in local X
            var qTilted = state.RawOrientation * Quaternion.AngleAxis(tilt, Vector3.right);
            state.OrientationCorrection = Quaternion.Inverse(state.CorrectedOrientation) * qTilted;
        }
    }

    /// <summary>
    /// Builtin FreeLook modifier for camera tilt.  Applies a vertical rotation to the camera 
    /// at the end of the camera pipeline.
    /// </summary>
    public class PositionDampingModifier : Modifier
    {
        [HideFoldout]
        public TopCenterBottom<Vector3> Damping;

        public override void Validate(CinemachineVirtualCameraBase vcam)
        {
            Damping.Top = new Vector3(Mathf.Max(0, Damping.Top.x), Mathf.Max(0, Damping.Top.y), Mathf.Max(0, Damping.Top.z));
            Damping.Center = new Vector3(Mathf.Max(0, Damping.Center.x), Mathf.Max(0, Damping.Center.y), Mathf.Max(0, Damping.Center.z));
            Damping.Bottom = new Vector3(Mathf.Max(0, Damping.Bottom.x), Mathf.Max(0, Damping.Bottom.y), Mathf.Max(0, Damping.Bottom.z));
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            TryGetVcamComponent<CinemachineOrbitalFollow>(vcam, out var orbital);
            if (orbital != null)
            {
                Damping = new TopCenterBottom<Vector3>
                {
                    Top = orbital.PositionDamping,
                    Center = orbital.PositionDamping,
                    Bottom = orbital.PositionDamping,
                };
            }
        }

        public override void RefreshCache(CinemachineVirtualCameraBase vcam) => TryGetVcamComponent(vcam, out m_Orbital);

        CinemachineOrbitalFollow m_Orbital;
        Vector3 m_SourceDamping;

        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float orbitalSplineValue) 
        {
            if (m_Orbital != null)
            {
                m_SourceDamping = m_Orbital.PositionDamping;
                m_Orbital.PositionDamping = orbitalSplineValue >= 0 
                    ? Vector3.Lerp(Damping.Center, Damping.Top, orbitalSplineValue)
                    : Vector3.Lerp(Damping.Bottom, Damping.Center, orbitalSplineValue + 1);
            }
        }

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float orbitalSplineValue)
        {
            // Restore the settings
            if (m_Orbital != null)
                m_Orbital.PositionDamping = m_SourceDamping;
        }
    }
    
    /// <summary>
    /// Builtin modifier for camera lens.  Applies the lens at the start of the camera pipeline.
    /// </summary>
    public class LensModifier : Modifier
    {
        [HideFoldout]
        public TopCenterBottom<LensSettings> Lens;

        public override void Validate(CinemachineVirtualCameraBase vcam) 
        {
            Lens.Top.Validate();
            Lens.Center.Validate();
            Lens.Bottom.Validate();
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            var defaultLens = vcam == null ? LensSettings.Default : vcam.State.Lens;
            Lens = new TopCenterBottom<LensSettings> { Top = defaultLens, Center = defaultLens, Bottom = defaultLens };
        }

        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float orbitalSplineValue) 
        {
            if (orbitalSplineValue >= 0)
                state.Lens = LensSettings.Lerp(Lens.Center, Lens.Top, orbitalSplineValue);
            else
                state.Lens = LensSettings.Lerp(Lens.Bottom, Lens.Center, orbitalSplineValue + 1);
        }
    }

    /// <summary>
    /// Builtin modifier for <see cref="CinemachineBasicMultiChannelPerlin"/>.  Applies scaling to
    /// amplitude and frequency.
    /// </summary>
    public class NoiseModifier : Modifier
    {
        [Serializable]
        public struct NoiseSettings
        {
            [Tooltip("Multiplier for the noise amplitude")]
            public float Amplitude;

            [Tooltip("Multiplier for the noise frequency")]
            public float Frequency;

            public static NoiseSettings Default => new NoiseSettings { Amplitude = 1, Frequency = 1 };
        }
    
        [HideFoldout]
        public TopCenterBottom<NoiseSettings> Noise;

        CinemachineBasicMultiChannelPerlin m_NoiseComponent;
        NoiseSettings m_SourceNoise; // For storing and restoring the original settings

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            Noise = new TopCenterBottom<NoiseSettings>
            {
                Top = NoiseSettings.Default, 
                Center = NoiseSettings.Default, 
                Bottom = NoiseSettings.Default 
            };
        }

        public override void RefreshCache(CinemachineVirtualCameraBase vcam) => TryGetVcamComponent(vcam, out m_NoiseComponent);

        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float orbitalSplineValue) 
        {
            if (m_NoiseComponent != null)
            {
                m_SourceNoise.Amplitude = m_NoiseComponent.m_AmplitudeGain;
                m_SourceNoise.Frequency = m_NoiseComponent.m_FrequencyGain;
                if (orbitalSplineValue >= 0)
                {
                    m_SourceNoise.Amplitude = Mathf.Lerp(Noise.Center.Amplitude, Noise.Top.Amplitude, orbitalSplineValue);
                    m_SourceNoise.Frequency = Mathf.Lerp(Noise.Center.Frequency, Noise.Top.Frequency, orbitalSplineValue);
                }
                else
                {
                    m_SourceNoise.Amplitude = Mathf.Lerp(Noise.Bottom.Amplitude, Noise.Center.Amplitude, orbitalSplineValue + 1);
                    m_SourceNoise.Frequency = Mathf.Lerp(Noise.Bottom.Frequency, Noise.Center.Frequency, orbitalSplineValue + 1);
                }
            }
        }

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float orbitalSplineValue) 
        {
            // Restore the settings
            if (m_NoiseComponent != null)
            {
                m_NoiseComponent.m_AmplitudeGain = m_SourceNoise.Amplitude;
                m_NoiseComponent.m_FrequencyGain = m_SourceNoise.Frequency;
            }
        }
    }

    /// <summary>
    /// Helper struct to hold settings for Top, Middle, and Bottom orbits.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public struct TopCenterBottom<T>
    {
        /// <summary>Settings for top orbit</summary>
        [Tooltip("Value to take at the top of the axis range")]
        public T Top;

        /// <summary>Settings for center orbit</summary>
        [Tooltip("Value to take at the center of the axis range")]
        public T Center;

        /// <summary>Settings for bottom orbit</summary>
        [Tooltip("Value to take at the bottom of the axis range")]
        public T Bottom;
    }

    /// <summary>
    /// Collection of modifiers that will be pplied to the camera every frame.
    /// </summary>
    [SerializeReference] [NoSaveDuringPlay] public List<Modifier> Modifiers = new List<Modifier>();

    CinemachineOrbitalFollow m_Orbital;
    float m_CurrentSplineValue;

    void OnValidate()
    {
        var vcam = VirtualCamera;
        for (int i = 0; i < Modifiers.Count; ++i)
            Modifiers[i].Validate(vcam);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshComponentCache();
    }

    // GML todo: clean this up
    internal static void TryGetVcamComponent<T>(
        CinemachineVirtualCameraBase vcamBase, out T component) where T : CinemachineComponentBase
    {
#pragma warning disable 618
        var vcam = vcamBase as CinemachineVirtualCamera;
#pragma warning restore 618
        if (vcam != null)
            component = vcam.GetCinemachineComponent<T>();
        else if (vcamBase != null)
            vcamBase.TryGetComponent(out component);
        else
            component = null;
    }

    void RefreshComponentCache()
    {
        var vcam = VirtualCamera;
        TryGetVcamComponent(vcam, out m_Orbital);
        for (int i = 0; i < Modifiers.Count; ++i)
            Modifiers[i].RefreshCache(vcam);
    }

    // Needed by inspector
    internal bool HasOrbital() { RefreshComponentCache(); return m_Orbital != null; }

    /// <summary>Override this to do such things as offset the RefereceLookAt.
    /// Base class implementation does nothing.</summary>
    /// <param name="vcam">The virtual camera being processed</param>
    /// <param name="curState">Input state that must be mutated</param>
    /// <param name="deltaTime">The current applicable deltaTime</param>
    public override void PrePipelineMutateCameraStateCallback(
        CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime) 
    {
        if (m_Orbital != null)
        {
            m_CurrentSplineValue = m_Orbital.GetVerticalAxisNormalizedValue();
            for (int i = 0; i < Modifiers.Count; ++i)
                Modifiers[i].BeforePipeline(vcam, ref curState, deltaTime, m_CurrentSplineValue);
        }
    }
            
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (m_Orbital != null && stage == CinemachineCore.Stage.Finalize)
        {
            for (int i = 0; i < Modifiers.Count; ++i)
                Modifiers[i].AfterPipeline(vcam, ref state, deltaTime, m_CurrentSplineValue);
        }
    }
}

