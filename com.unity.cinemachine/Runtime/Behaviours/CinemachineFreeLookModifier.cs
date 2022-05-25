﻿using UnityEngine;
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
    public interface IModifierValueSource
    {
        float NormalizedModifierValue { get; }
    }

    public interface IModifiablePositionDamping
    {
        Vector3 PositionDamping { get; set; }
    }
    
    public interface IModifiableScreenPosition
    {
        Vector2 Screen { get; set; }
    }

    public interface IModifiableDistance
    {
        float Distance { get; set; }
    }

    public interface IModifiableNoise
    {
        (float, float) NoiseAmplitudeFrequency { get; set; }
    }

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

        /// <summary>Type of the cached component (null if no cached component).  If this modifier targets
        /// specific components, this value indicates the type of that component.
        /// The modifier should cache the component, for performance.  
        /// <see cref="ComponentModifier"/> for a base class for implementing this.
        public virtual Type CachedComponentType => null;

        /// <summary>Return true if cached vcam component is present or not required</summary>
        public virtual bool HasRequiredComponent => true;

        /// <summary>Called from OnEnable and from the inspector.  Refresh any performace-sensitive stuff.</summary>
        /// <param name="vcam">the virtual camera owner</param>
        public virtual void RefreshCache(CinemachineVirtualCameraBase vcam) {}
            
        /// <summary>
        /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to 
        /// modify relevant camera settings.  Original camera settings should be restored in .
        /// </summary>
        /// <param name="vcam">vcam owner</param>
        /// <param name="state">current vcam state.  May be modified in this function</param>
        /// <param name="deltaTime">current applicable deltaTime</param>
        /// <param name="modifierValue">The normalized value of the modifier variable.  
        /// This is the FreeLook's vertical axis.
        /// Ranges from -1 to 1, where 0 is center rig.</param>
        public virtual void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, 
            float modifierValue) {}

        /// <summary>
        /// Called from extension's PostPipelineStageCallback(Finalize).  Perform any necessary actions to state,
        /// and restore any camera parameters changed in <see cref="BeforePipeline"/>.
        /// </summary>
        /// <param name="vcam">vcam owner</param>
        /// <param name="state">current vcam state.  May be modified in this function</param>
        /// <param name="deltaTime">current applicable deltaTime</param>
        /// <param name="modifierValue">The normalized value of the modifier variable.  
        /// This is the FreeLook's vertical axis.
        /// Ranges from -1 to 1, where 0 is center rig.</param>
        public virtual void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float modifierValue) {}
    }

    /// <summary>
    /// Modifier for things inside a single CinemachineComponentBase.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ComponentModifier<T> : Modifier
    {
        /// <summary>The CinemachineComponentBase that will be modified.  Cached here for efficiency.</summary>
        protected T CachedComponent;

        /// <summary>True if the CmCamera has the component we intend to modify</summary>
        public override bool HasRequiredComponent => CachedComponent != null;

        /// <summary>The type of the component being modified</summary>
        public override Type CachedComponentType => typeof(T);

        /// <summary>Called from OnEnable and from the inspector.  Refreshes CachedComponent.</summary>
        /// <param name="vcam">the virtual camera owner</param>
        public override void RefreshCache(CinemachineVirtualCameraBase vcam) 
            => TryGetVcamComponent(vcam, out CachedComponent);
    }

    /// <summary>
    /// Builtin FreeLook modifier for camera tilt.  Applies a vertical rotation to the camera 
    /// at the end of the camera pipeline.
    /// </summary>
    public class TiltModifier : Modifier
    {
        [HideFoldout]
        public TopBottomRigs<float> Tilt;

        public override void Validate(CinemachineVirtualCameraBase vcam)
        {
            Tilt.Top = Mathf.Clamp(Tilt.Top, -30, 30);
            Tilt.Bottom = Mathf.Clamp(Tilt.Bottom, -30, 30);
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
            => Tilt = new TopBottomRigs<float>() { Top = -5, Bottom = 5 };

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float modifierValue)
        {
            float tilt = modifierValue > 0 
                ? Mathf.Lerp(0, Tilt.Top, modifierValue) 
                : Mathf.Lerp(Tilt.Bottom, 0, modifierValue + 1);

            // Tilt in local X
            var qTilted = state.RawOrientation * Quaternion.AngleAxis(tilt, Vector3.right);
            state.OrientationCorrection = Quaternion.Inverse(state.CorrectedOrientation) * qTilted;
        }
    }

    /// <summary>
    /// Builtin modifier for camera lens.  Applies the lens at the start of the camera pipeline.
    /// </summary>
    public class LensModifier : Modifier
    {
        [HideFoldout]
        public TopBottomRigs<LensSettings> Lens;

        public override void Validate(CinemachineVirtualCameraBase vcam) 
        {
            Lens.Top.Validate();
            Lens.Bottom.Validate();
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            Lens.Top = Lens.Bottom = vcam == null ? LensSettings.Default : vcam.State.Lens;
        }

        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float modifierValue) 
        {
            if (modifierValue >= 0)
                state.Lens.Lerp(Lens.Top, modifierValue);
            else
                state.Lens.Lerp(Lens.Bottom, -modifierValue);
        }
    }
    
    /// <summary>
    /// Builtin Freelook modifier for positional damping. Modifies positional damping at the start of the camera pipeline.
    /// </summary>
    public class PositionDampingModifier : ComponentModifier<IModifiablePositionDamping>
    {
        [HideFoldout]
        public TopBottomRigs<Vector3> Damping;

        public override void Validate(CinemachineVirtualCameraBase vcam)
        {
            Damping.Top = new Vector3(Mathf.Max(0, Damping.Top.x), Mathf.Max(0, Damping.Top.y), Mathf.Max(0, Damping.Top.z));
            Damping.Bottom = new Vector3(Mathf.Max(0, Damping.Bottom.x), Mathf.Max(0, Damping.Bottom.y), Mathf.Max(0, Damping.Bottom.z));
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            if (CachedComponent != null)
                Damping.Top = Damping.Bottom = CachedComponent.PositionDamping;
        }

        Vector3 m_CenterDamping;

        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float modifierValue) 
        {
            if (CachedComponent != null)
            {
                m_CenterDamping = CachedComponent.PositionDamping;
                CachedComponent.PositionDamping = modifierValue >= 0 
                    ? Vector3.Lerp(m_CenterDamping, Damping.Top, modifierValue)
                    : Vector3.Lerp(Damping.Bottom, m_CenterDamping, modifierValue + 1);
            }
        }

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float modifierValue)
        {
            // Restore the settings
            if (CachedComponent != null)
                CachedComponent.PositionDamping = m_CenterDamping;
        }
    }
    
    /// <summary>
    /// Builtin Freelook modifier for Composer's ScreenY. Modifies ScreenY at the start of the camera pipeline.
    /// </summary>
    public class ScreenPositionModifier : ComponentModifier<IModifiableScreenPosition>
    {
        [HideFoldout]
        public TopBottomRigs<Vector2> Screen;

        public override void Validate(CinemachineVirtualCameraBase vcam)
        {
            Screen.Top.x = Mathf.Clamp(Screen.Top.x, -0.5f, 1.5f);
            Screen.Top.y = Mathf.Clamp(Screen.Top.y, -0.5f, 1.5f);
            Screen.Bottom.x = Mathf.Clamp(Screen.Bottom.x, -0.5f, 1.5f);
            Screen.Bottom.y = Mathf.Clamp(Screen.Bottom.y, -0.5f, 1.5f);
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            if (CachedComponent != null)
            {
                Screen.Top = Screen.Bottom = new Vector2(CachedComponent.Screen.x, CachedComponent.Screen.y);
            }
        }

        Vector2 m_CenterScreen;
        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float modifierValue) 
        {
            if (CachedComponent != null)
            {
                m_CenterScreen = new Vector2(CachedComponent.Screen.x, CachedComponent.Screen.y);
                CachedComponent.Screen = modifierValue >= 0
                    ? Vector2.Lerp(m_CenterScreen, Screen.Top, modifierValue)
                    : Vector2.Lerp(Screen.Bottom, m_CenterScreen, modifierValue + 1);
            }
        }

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float modifierValue)
        {
            // Restore the settings
            if (CachedComponent != null)
            {
                CachedComponent.Screen = m_CenterScreen;
            }
        }
    }

    /// <summary>
    /// Builtin FreeLook modifier for camera distance.  Applies distance to the camera at the start of the camera pipeline.
    /// </summary>
    public class DistanceModifier : ComponentModifier<IModifiableDistance>
    {
        [HideFoldout]
        public TopBottomRigs<float> Distance;

        public override void Validate(CinemachineVirtualCameraBase vcam)
        {
            Distance.Top = Mathf.Max(0, Distance.Top);
            Distance.Bottom = Mathf.Max(0, Distance.Bottom);
        }

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            if (CachedComponent != null)
                Distance.Top = Distance.Bottom = CachedComponent.Distance;
        }

        float m_CenterDistance;

        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float modifierValue) 
        {
            if (CachedComponent != null)
            {
                m_CenterDistance = CachedComponent.Distance;
                CachedComponent.Distance = modifierValue >= 0 
                    ? Mathf.Lerp(m_CenterDistance, Distance.Top, modifierValue)
                    : Mathf.Lerp(Distance.Bottom, m_CenterDistance, modifierValue + 1);
            }
        }

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float modifierValue)
        {
            // Restore the settings
            if (CachedComponent != null)
                CachedComponent.Distance = m_CenterDistance;
        }
    }
    
    /// <summary>
    /// Builtin modifier for noise components such as <see cref="CinemachineBasicMultiChannelPerlin"/>.  
    /// Applies scaling to amplitude and frequency.
    /// </summary>
    public class NoiseModifier : ComponentModifier<IModifiableNoise>
    {
        [Serializable]
        public struct NoiseSettings
        {
            [Tooltip("Multiplier for the noise amplitude")]
            public float Amplitude;

            [Tooltip("Multiplier for the noise frequency")]
            public float Frequency;
        }
    
        [HideFoldout]
        public TopBottomRigs<NoiseSettings> Noise;

        (float, float) m_CenterNoise;

        public override void Reset(CinemachineVirtualCameraBase vcam) 
        {
            if (CachedComponent != null)
            {
                var value = CachedComponent.NoiseAmplitudeFrequency;
                Noise.Top = Noise.Bottom = new NoiseSettings { Amplitude = value.Item1, Frequency = value.Item2 };
            }
        }

        public override void BeforePipeline(
            CinemachineVirtualCameraBase vcam, 
            ref CameraState state, float deltaTime, float modifierValue) 
        {
            if (CachedComponent != null)
            {
                m_CenterNoise = CachedComponent.NoiseAmplitudeFrequency;
                if (modifierValue >= 0)
                    CachedComponent.NoiseAmplitudeFrequency = (
                        Mathf.Lerp(m_CenterNoise.Item1, Noise.Top.Amplitude, modifierValue),
                        Mathf.Lerp(m_CenterNoise.Item2, Noise.Top.Frequency, modifierValue));
                else
                    CachedComponent.NoiseAmplitudeFrequency = (
                        Mathf.Lerp(Noise.Bottom.Amplitude, m_CenterNoise.Item1, modifierValue + 1),
                        Mathf.Lerp(Noise.Bottom.Frequency, m_CenterNoise.Item2, modifierValue + 1));
            }
        }

        public override void AfterPipeline(
            CinemachineVirtualCameraBase vcam,
            ref CameraState state, float deltaTime,
            float modifierValue) 
        {
            // Restore the settings
            if (CachedComponent != null)
                CachedComponent.NoiseAmplitudeFrequency = m_CenterNoise;
        }
    }

    /// <summary>
    /// Helper struct to hold settings for Top, Middle, and Bottom orbits.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public struct TopBottomRigs<T>
    {
        /// <summary>Settings for top orbit</summary>
        [Tooltip("Value to take at the top of the axis range")]
        public T Top;

        /// <summary>Settings for bottom orbit</summary>
        [Tooltip("Value to take at the bottom of the axis range")]
        public T Bottom;
    }

    /// <summary>
    /// Collection of modifiers that will be applied to the camera every frame.
    /// </summary>
    [SerializeReference] [NoSaveDuringPlay] public List<Modifier> Modifiers = new List<Modifier>();

    IModifierValueSource m_ValueSource;
    float m_CurrentValue;
    static AnimationCurve s_EasingCurve;

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
    static void TryGetVcamComponent<T>(CinemachineVirtualCameraBase vcam, out T component)
    {
#pragma warning disable 618
        var legacyVcam = vcam as CinemachineVirtualCamera;
        if (legacyVcam != null)
        {
            if (!legacyVcam.GetComponentOwner().TryGetComponent(out component))
                component = default;
            return;
        }
#pragma warning restore 618
        if (vcam == null || !vcam.TryGetComponent(out component))
            component = default;
    }

    void RefreshComponentCache()
    {
        var vcam = VirtualCamera;
        TryGetVcamComponent(vcam, out m_ValueSource);
        for (int i = 0; i < Modifiers.Count; ++i)
            Modifiers[i].RefreshCache(vcam);
    }

    // Needed by inspector
    internal bool HasValueSource() { RefreshComponentCache(); return m_ValueSource != null; }

    /// <summary>Override this to do such things as offset the ReferenceLookAt.
    /// Base class implementation does nothing.</summary>
    /// <param name="vcam">The virtual camera being processed</param>
    /// <param name="curState">Input state that must be mutated</param>
    /// <param name="deltaTime">The current applicable deltaTime</param>
    public override void PrePipelineMutateCameraStateCallback(
        CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime) 
    {
        if (m_ValueSource != null)
        {
            // Apply easing
            if (s_EasingCurve == null)
            {
                s_EasingCurve = AnimationCurve.Linear(0f, 0f, 1, 1f);
#if false // nah, it doesn't look so great
// GML todo: find a nice way to amke a smooth curve.  Maybe a bezier?
                // Ease out, hard in
                var keys = s_EasingCurve.keys;
                keys[0].outTangent = 0;
                keys[1].inTangent = 1.4f;
                s_EasingCurve.keys = keys;
#endif
            }
            var v = m_ValueSource.NormalizedModifierValue;
            var sign = Mathf.Sign(v);
            m_CurrentValue = sign * s_EasingCurve.Evaluate(sign * v);
            for (int i = 0; i < Modifiers.Count; ++i)
                Modifiers[i].BeforePipeline(vcam, ref curState, deltaTime, m_CurrentValue);
        }
    }
            
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (m_ValueSource != null && stage == CinemachineCore.Stage.Finalize)
        {
            for (int i = 0; i < Modifiers.Count; ++i)
                Modifiers[i].AfterPipeline(vcam, ref state, deltaTime, m_CurrentValue);
        }
    }
}

