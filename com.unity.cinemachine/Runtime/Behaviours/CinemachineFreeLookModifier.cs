using UnityEngine;
using System;
using System.Collections.Generic;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is an add-on for CinemachineCameras containing the OrbitalFollow component.
    /// It modifies the camera distance as a function of vertical angle.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine FreeLook Modifier")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineFreeLookModifier.html")]
    public class CinemachineFreeLookModifier : CinemachineExtension
    {
        /// <summary>
        /// Interface for CinemachineComponentBase-derived to expose a normalized value that
        /// can be consumed by CinemachineFreeLookModifier to drive the rig selection.
        /// </summary>
        public interface IModifierValueSource
        {
            /// <summary>
            /// This value will be 0 for the middle rig, -1 for the bottom rid, and 1 for the top rig.
            /// Values in-between represent a blend between rigs.
            /// </summary>
            float NormalizedModifierValue { get; }
        }

        /// <summary>
        /// Interface for CinemachineComponentBase-derived to allow its position damping to be driven.
        /// </summary>
        public interface IModifiablePositionDamping
        {
            /// <summary>Get/Set the position damping value</summary>
            Vector3 PositionDamping { get; set; }
        }

        /// <summary>
        /// Interface for CinemachineComponentBase-derived to allow its screen composition to be driven
        /// </summary>
        public interface IModifiableComposition
        {
            /// <summary>Get/set the screen position</summary>
            ScreenComposerSettings Composition { get; set; }
        }

        /// <summary>
        /// Interface for CinemachineComponentBase-derived to allow the camera distance to be modified
        /// </summary>
        public interface IModifiableDistance
        {
            /// <summary>Get/set the camera distance</summary>
            float Distance { get; set; }
        }

        /// <summary>
        /// Interface for CinemachineComponentBase-derived to allow the noise amplitude and frequency to be modified
        /// </summary>
        public interface IModifiableNoise
        {
            /// <summary>Get/set the noise amplitude and frequency</summary>
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

            /// <summary>
            /// Type of the cached component (null if no cached component).  If this modifier targets
            /// specific components, this value indicates the type of that component.
            /// The modifier should cache the component, for performance.
            /// <see cref="ComponentModifier"/> for a base class for implementing this.
            /// </summary>
            public virtual Type CachedComponentType => null;

            /// <summary>Return true if cached vcam component is present or if no cached component is required</summary>
            public virtual bool HasRequiredComponent => true;

            /// <summary>Called from OnEnable and from the inspector.  Refresh any performance-sensitive stuff.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public virtual void RefreshCache(CinemachineVirtualCameraBase vcam) {}

            /// <summary>
            /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to
            /// modify relevant camera settings.  Original camera settings should be restored in <see cref="AfterPipeline"/>.
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
        /// <typeparam name="T">The type of the thing for which this a modifier.</typeparam>
        public abstract class ComponentModifier<T> : Modifier
        {
            /// <summary>The CinemachineComponentBase that will be modified.  Cached here for efficiency.</summary>
            protected T CachedComponent;

            /// <summary>True if the CinemachineCamera has the component we intend to modify.</summary>
            public override bool HasRequiredComponent => CachedComponent != null;

            /// <summary>The type of the component being modified</summary>
            public override Type CachedComponentType => typeof(T);

            /// <summary>Called from OnEnable and from the inspector.  Refreshes CachedComponent.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void RefreshCache(CinemachineVirtualCameraBase vcam) => TryGetVcamComponent(vcam, out CachedComponent);
        }

        /// <summary>
        /// Builtin FreeLook modifier for camera tilt.  Applies a vertical rotation to the camera
        /// at the end of the camera pipeline.
        /// </summary>
        public class TiltModifier : Modifier
        {
            /// <summary>Values for the top and bottom rigs</summary>
            [HideFoldout]
            public TopBottomRigs<float> Tilt;

            /// <summary>Called from OnValidate to validate this component</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Validate(CinemachineVirtualCameraBase vcam)
            {
                Tilt.Top = Mathf.Clamp(Tilt.Top, -30, 30);
                Tilt.Bottom = Mathf.Clamp(Tilt.Bottom, -30, 30);
            }

            /// <summary>Called when the modifier is created.  Initialize fields with appropriate values.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Reset(CinemachineVirtualCameraBase vcam)
                => Tilt = new TopBottomRigs<float>() { Top = -5, Bottom = 5 };

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
                state.OrientationCorrection = Quaternion.Inverse(state.GetCorrectedOrientation()) * qTilted;
            }
        }

        /// <summary>
        /// Builtin modifier for camera lens.  Applies the lens at the start of the camera pipeline.
        /// </summary>
        public class LensModifier : Modifier
        {
            /// <summary>Settings for top orbit</summary>
            [Tooltip("Value to take at the top of the axis range")]
            [LensSettingsHideModeOverrideProperty]
            public LensSettings Top;

            /// <summary>Settings for bottom orbit</summary>
            [Tooltip("Value to take at the bottom of the axis range")]
            [LensSettingsHideModeOverrideProperty]
            public LensSettings Bottom;

            /// <summary>Called from OnValidate to validate this component</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Validate(CinemachineVirtualCameraBase vcam)
            {
                Top.Validate();
                Bottom.Validate();
            }

            /// <summary>Called when the modifier is created.  Initialize fields with appropriate values.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Reset(CinemachineVirtualCameraBase vcam)
            {
                if (vcam == null)
                    Top = Bottom = LensSettings.Default;
                else
                {
                    var state = vcam.State;
                    Top = Bottom = state.Lens;
                    Top.CopyCameraMode(ref state.Lens);
                    Bottom.CopyCameraMode(ref state.Lens);
                }
            }

            /// <summary>
            /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to
            /// modify relevant camera settings.  Original camera settings should be restored in <see cref="AfterPipeline"/>.
            /// </summary>
            /// <param name="vcam">vcam owner</param>
            /// <param name="state">current vcam state.  May be modified in this function</param>
            /// <param name="deltaTime">current applicable deltaTime</param>
            /// <param name="modifierValue">The normalized value of the modifier variable.
            /// This is the FreeLook's vertical axis.
            /// Ranges from -1 to 1, where 0 is center rig.</param>
            public override void BeforePipeline(
                CinemachineVirtualCameraBase vcam,
                ref CameraState state, float deltaTime, float modifierValue)
            {
                Top.CopyCameraMode(ref state.Lens);
                Bottom.CopyCameraMode(ref state.Lens);
                if (modifierValue >= 0)
                    state.Lens.Lerp(Top, modifierValue);
                else
                    state.Lens.Lerp(Bottom, -modifierValue);
            }
        }

        /// <summary>
        /// Builtin FreeLook modifier for positional damping. Modifies positional damping at the start of the camera pipeline.
        /// </summary>
        public class PositionDampingModifier : ComponentModifier<IModifiablePositionDamping>
        {
            /// <summary>Values for the top and bottom rigs</summary>
            [HideFoldout]
            public TopBottomRigs<Vector3> Damping;

            /// <summary>Called from OnValidate to validate this component</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Validate(CinemachineVirtualCameraBase vcam)
            {
                Damping.Top = new Vector3(Mathf.Max(0, Damping.Top.x), Mathf.Max(0, Damping.Top.y), Mathf.Max(0, Damping.Top.z));
                Damping.Bottom = new Vector3(Mathf.Max(0, Damping.Bottom.x), Mathf.Max(0, Damping.Bottom.y), Mathf.Max(0, Damping.Bottom.z));
            }

            /// <summary>Called when the modifier is created.  Initialize fields with appropriate values.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Reset(CinemachineVirtualCameraBase vcam)
            {
                if (CachedComponent != null)
                    Damping.Top = Damping.Bottom = CachedComponent.PositionDamping;
            }

            Vector3 m_CenterDamping;

            /// <summary>
            /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to
            /// modify relevant camera settings.  Original camera settings should be restored in <see cref="AfterPipeline"/>.
            /// </summary>
            /// <param name="vcam">vcam owner</param>
            /// <param name="state">current vcam state.  May be modified in this function</param>
            /// <param name="deltaTime">current applicable deltaTime</param>
            /// <param name="modifierValue">The normalized value of the modifier variable.
            /// This is the FreeLook's vertical axis.
            /// Ranges from -1 to 1, where 0 is center rig.</param>
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
        /// Builtin Freelook modifier for screen composition. Modifies composition at the start of the camera pipeline.
        /// </summary>
        public class CompositionModifier : ComponentModifier<IModifiableComposition>
        {
            /// <summary>Values for the top and bottom rigs</summary>
            [HideFoldout]
            public TopBottomRigs<ScreenComposerSettings> Composition;

            /// <summary>Called from OnValidate to validate this component</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Validate(CinemachineVirtualCameraBase vcam)
            {
                Composition.Top.Validate();
                Composition.Bottom.Validate();
            }

            /// <summary>Called when the modifier is created.  Initialize fields with appropriate values.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Reset(CinemachineVirtualCameraBase vcam)
            {
                if (CachedComponent != null)
                    Composition.Top = Composition.Bottom = CachedComponent.Composition;
            }

            ScreenComposerSettings m_SavedComposition;

            /// <summary>
            /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to
            /// modify relevant camera settings.  Original camera settings should be restored in <see cref="AfterPipeline"/>.
            /// </summary>
            /// <param name="vcam">vcam owner</param>
            /// <param name="state">current vcam state.  May be modified in this function</param>
            /// <param name="deltaTime">current applicable deltaTime</param>
            /// <param name="modifierValue">The normalized value of the modifier variable.
            /// This is the FreeLook's vertical axis.
            /// Ranges from -1 to 1, where 0 is center rig.</param>
            public override void BeforePipeline(
                CinemachineVirtualCameraBase vcam,
                ref CameraState state, float deltaTime, float modifierValue)
            {
                if (CachedComponent != null)
                {
                    m_SavedComposition = CachedComponent.Composition;
                    CachedComponent.Composition = modifierValue >= 0
                        ? ScreenComposerSettings.Lerp(m_SavedComposition, Composition.Top, modifierValue)
                        : ScreenComposerSettings.Lerp(Composition.Bottom, m_SavedComposition, modifierValue + 1);
                }
            }

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
            public override void AfterPipeline(
                CinemachineVirtualCameraBase vcam,
                ref CameraState state, float deltaTime,
                float modifierValue)
            {
                // Restore the settings
                if (CachedComponent != null)
                {
                    CachedComponent.Composition = m_SavedComposition;
                }
            }
        }

        /// <summary>
        /// Builtin FreeLook modifier for camera distance.  Applies distance to the camera at the start of the camera pipeline.
        /// </summary>
        public class DistanceModifier : ComponentModifier<IModifiableDistance>
        {
            /// <summary>Values for the top and bottom rigs</summary>
            [HideFoldout]
            public TopBottomRigs<float> Distance;

            /// <summary>Called from OnValidate to validate this component</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Validate(CinemachineVirtualCameraBase vcam)
            {
                Distance.Top = Mathf.Max(0, Distance.Top);
                Distance.Bottom = Mathf.Max(0, Distance.Bottom);
            }

            /// <summary>Called when the modifier is created.  Initialize fields with appropriate values.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Reset(CinemachineVirtualCameraBase vcam)
            {
                if (CachedComponent != null)
                    Distance.Top = Distance.Bottom = CachedComponent.Distance;
            }

            float m_CenterDistance;

            /// <summary>
            /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to
            /// modify relevant camera settings.  Original camera settings should be restored in <see cref="AfterPipeline"/>.
            /// </summary>
            /// <param name="vcam">vcam owner</param>
            /// <param name="state">current vcam state.  May be modified in this function</param>
            /// <param name="deltaTime">current applicable deltaTime</param>
            /// <param name="modifierValue">The normalized value of the modifier variable.
            /// This is the FreeLook's vertical axis.
            /// Ranges from -1 to 1, where 0 is center rig.</param>
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
            /// <summary>
            /// Settings to apply to the IModifiableNoise component
            /// </summary>
            [Serializable]
            public struct NoiseSettings
            {
                /// <summary>Multiplier for the noise amplitude</summary>
                [Tooltip("Multiplier for the noise amplitude")]
                public float Amplitude;

                /// <summary>Multiplier for the noise frequency</summary>
                [Tooltip("Multiplier for the noise frequency")]
                public float Frequency;
            }

            /// <summary>Values for the top and bottom rigs</summary>
            [HideFoldout]
            public TopBottomRigs<NoiseSettings> Noise;

            (float, float) m_CenterNoise;

            /// <summary>Called when the modifier is created.  Initialize fields with appropriate values.</summary>
            /// <param name="vcam">the virtual camera owner</param>
            public override void Reset(CinemachineVirtualCameraBase vcam)
            {
                if (CachedComponent != null)
                {
                    var value = CachedComponent.NoiseAmplitudeFrequency;
                    Noise.Top = Noise.Bottom = new NoiseSettings { Amplitude = value.Item1, Frequency = value.Item2 };
                }
            }

            /// <summary>
            /// Called from extension's PrePipelineMutateCameraState().  Perform any necessary actions to
            /// modify relevant camera settings.  Original camera settings should be restored in <see cref="AfterPipeline"/>.
            /// </summary>
            /// <param name="vcam">vcam owner</param>
            /// <param name="state">current vcam state.  May be modified in this function</param>
            /// <param name="deltaTime">current applicable deltaTime</param>
            /// <param name="modifierValue">The normalized value of the modifier variable.
            /// This is the FreeLook's vertical axis.
            /// Ranges from -1 to 1, where 0 is center rig.</param>
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
        /// <typeparam name="T">The type of the object whose value is held.</typeparam>
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
        /// The amount of easing to apply towards the center value. Zero easing
        /// blends linearly through the center value, while an easing of 1 smooths 
        /// the result as it passes over the center value.
        /// </summary>
        [Tooltip("The amount of easing to apply towards the center value. Zero easing "
            + "blends linearly through the center value, while an easing of 1 smooths "
            + "the result as it passes over the center value.")]
        [Range(0, 1)]
        public float Easing;

        /// <summary>
        /// Collection of modifiers that will be applied to the camera every frame.
        /// These will modify settings as a function of the FreeLook's Vertical axis value.
        /// </summary>
        [Tooltip("These will modify settings as a function of the FreeLook's Vertical axis value")]
        [SerializeReference] public List<Modifier> Modifiers = new ();

        IModifierValueSource m_ValueSource;
        float m_CurrentValue;
        AnimationCurve m_EasingCurve;
        float m_CachedEasingValue;

        void OnValidate()
        {
            var vcam = ComponentOwner;
            for (int i = 0; i < Modifiers.Count; ++i)
                Modifiers[i]?.Validate(vcam);
        }

        /// <summary>Called when component is enabled</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshComponentCache();
        }

        // GML todo: clean this up
        static void TryGetVcamComponent<T>(CinemachineVirtualCameraBase vcam, out T component)
        {
            if (vcam == null || !vcam.TryGetComponent(out component))
                component = default;
        }

        void RefreshComponentCache()
        {
            var vcam = ComponentOwner;
            TryGetVcamComponent(vcam, out m_ValueSource);
            for (int i = 0; i < Modifiers.Count; ++i)
                Modifiers[i]?.RefreshCache(vcam);
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
            if (m_ValueSource != null && vcam == ComponentOwner)
            {
                // Apply easing
                if (m_EasingCurve == null || m_CachedEasingValue != Easing)
                {
                    m_EasingCurve ??= AnimationCurve.Linear(0f, 0f, 1, 1f);
                    var keys = m_EasingCurve.keys;
                    keys[0].outTangent = (1 - Easing);
                    keys[1].inTangent = 1f + 2f * Easing;
                    m_EasingCurve.keys = keys;
                    m_CachedEasingValue = Easing;
                }
                var v = m_ValueSource.NormalizedModifierValue;
                var sign = Mathf.Sign(v);
                m_CurrentValue = sign * m_EasingCurve.Evaluate(Mathf.Abs(v));
                for (int i = 0; i < Modifiers.Count; ++i)
                    Modifiers[i]?.BeforePipeline(vcam, ref curState, deltaTime, m_CurrentValue);
            }
        }

        /// <summary>
        /// Callback to perform the requested rig modifications.
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (m_ValueSource != null && stage == CinemachineCore.Stage.Finalize && vcam == ComponentOwner)
            {
                for (int i = 0; i < Modifiers.Count; ++i)
                    Modifiers[i]?.AfterPipeline(vcam, ref state, deltaTime, m_CurrentValue);
            }
        }
    }
}
