using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Describes a blend between 2 CinemachineCameras, and holds the current state of the blend.
    /// </summary>
    public class CinemachineBlend
    {
        /// <summary>
        /// Interface for implementing custom CameraState blending algorithm
        /// </summary>
        public interface IBlender
        {
            /// <summary>
            /// Interpolate a camera state between the two cameras being blended.
            /// </summary>
            /// <param name="CamA">The first camera</param>
            /// <param name="CamB">The second camera</param>
            /// <param name="t">Range 0...1 where 0 is CamA state and 1 is CamB state</param>
            /// <returns>The interpolated state.</returns>
            CameraState GetIntermediateState(ICinemachineCamera CamA, ICinemachineCamera CamB, float t);
        }

        /// <summary>First camera in the blend</summary>
        public ICinemachineCamera CamA;

        /// <summary>Second camera in the blend</summary>
        public ICinemachineCamera CamB;

        /// <summary>The curve that describes the way the blend transitions over time
        /// from the first camera to the second.  X-axis is normalized time (0...1) over which
        /// the blend takes place and Y axis is blend weight (0..1)</summary>
        public AnimationCurve BlendCurve;

        /// <summary>The current time relative to the start of the blend</summary>
        public float TimeInBlend;

        /// <summary>The current weight of the blend.  This is an evaluation of the
        /// BlendCurve at the current time relative to the start of the blend.
        /// 0 means camA, 1 means camB.</summary>
        public float BlendWeight
        {
            get
            {
                if (BlendCurve == null || BlendCurve.length < 2 || IsComplete)
                    return 1;
                return Mathf.Clamp01(BlendCurve.Evaluate(TimeInBlend / Duration));
            }
        }

        /// <summary>
        /// If non-null, the custom blender will be used to blend camera state.
        /// If null, then CameraState.Lerp will be used.
        /// </summary>
        public IBlender CustomBlender { get; set; }

        /// <summary>Validity test for the blend.  True if either camera is defined.</summary>
        public bool IsValid => (CamA != null && CamA.IsValid) || (CamB != null && CamB.IsValid);

        /// <summary>Duration in seconds of the blend.</summary>
        public float Duration;

        /// <summary>True if the time relative to the start of the blend is greater
        /// than or equal to the blend duration</summary>
        public bool IsComplete => TimeInBlend >= Duration || !IsValid;

        /// <summary>Text description of the blend, for debugging</summary>
        public string Description
        {
            get
            {
                if (CamB == null || !CamB.IsValid)
                    return "(none)";

                var sb = CinemachineDebug.SBFromPool();
                sb.Append(CamB.Name);
                sb.Append(" ");
                sb.Append((int)(BlendWeight * 100f));
                sb.Append("% from ");
                if (CamA == null || !CamA.IsValid)
                    sb.Append("(none)");
                else
                    sb.Append(CamA.Name);
                string text = sb.ToString();
                CinemachineDebug.ReturnToPool(sb);
                return text;
            }
        }

        /// <summary>Does the blend use a specific CinemachineCamera?</summary>
        /// <param name="cam">The camera to test</param>
        /// <returns>True if the camera is involved in the blend</returns>
        public bool Uses(ICinemachineCamera cam)
        {
            if (cam == null)
                return false;
            if (cam == CamA || cam == CamB)
                return true;
            if (CamA is NestedBlendSource b && b.Blend.Uses(cam))
                return true;
            b = CamB as NestedBlendSource;
            return b != null && b.Blend.Uses(cam);
        }

        /// <summary>Copy contents of a blend</summary>
        /// <param name="src">Copy fields from this blend</param>
        public void CopyFrom(CinemachineBlend src)
        {
            CamA = src.CamA;
            CamB = src.CamB;
            BlendCurve = src.BlendCurve;
            TimeInBlend = src.TimeInBlend;
            Duration = src.Duration;
            CustomBlender = src.CustomBlender;
        }

        /// <summary>
        /// Clears all fields except CamB.  This effectively cuts to the end of the blend
        /// </summary>
        public void ClearBlend()
        {
            CamA = null;
            BlendCurve = null;
            TimeInBlend = Duration = 0;
            CustomBlender = null;
        }

        /// <summary>Make sure the source cameras get updated.</summary>
        /// <param name="worldUp">Default world up.  Individual vcams may modify this</param>
        /// <param name="deltaTime">Time increment used for calculating time-based behaviours (e.g. damping)</param>
        public void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            // Make sure both cameras have been updated (they are not necessarily
            // enabled, and only enabled cameras get updated automatically
            // every frame)
            if (CamA != null && CamA.IsValid)
                CamA.UpdateCameraState(worldUp, deltaTime);
            if (CamB != null && CamB.IsValid)
                CamB.UpdateCameraState(worldUp, deltaTime);
        }

        /// <summary>Compute the blended CameraState for the current time in the blend.</summary>
        public CameraState State
        {
            get
            {
                if (CamA == null || !CamA.IsValid)
                {
                    if (CamB == null || !CamB.IsValid)
                        return CameraState.Default;
                    return CamB.State;
                }
                if (CamB == null || !CamB.IsValid)
                    return CamA.State;

                if (CustomBlender != null)
                    return CustomBlender.GetIntermediateState(CamA, CamB, BlendWeight);
                return CameraState.Lerp(CamA.State, CamB.State, BlendWeight);
            }
        }
    }

    /// <summary>Definition of a Camera blend.  This struct holds the information
    /// necessary to generate a suitable AnimationCurve for a Cinemachine Blend.</summary>
    [Serializable]
    public struct CinemachineBlendDefinition
    {
        /// <summary>
        /// Delegate for finding a blend definition to use when blending between 2 cameras.
        /// </summary>
        /// <param name="outgoing">The outgoing camera</param>
        /// <param name="incoming">The incoming camera</param>
        /// <returns>An appropriate blend definition,.  Must not be null.</returns>
        public delegate CinemachineBlendDefinition LookupBlendDelegate(
            ICinemachineCamera outgoing, ICinemachineCamera incoming);

        /// <summary>Supported predefined shapes for the blend curve.</summary>
        public enum Styles
        {
            /// <summary>Zero-length blend</summary>
            Cut,
            /// <summary>S-shaped curve, giving a gentle and smooth transition</summary>
            EaseInOut,
            /// <summary>Linear out of the outgoing shot, and easy into the incoming</summary>
            EaseIn,
            /// <summary>Easy out of the outgoing shot, and linear into the incoming</summary>
            EaseOut,
            /// <summary>Easy out of the outgoing, and hard into the incoming</summary>
            HardIn,
            /// <summary>Hard out of the outgoing, and easy into the incoming</summary>
            HardOut,
            /// <summary>Linear blend.  Mechanical-looking.</summary>
            Linear,
            /// <summary>Custom blend curve.</summary>
            Custom
        };

        /// <summary>The shape of the blend curve.</summary>
        [Tooltip("Shape of the blend curve")]
        [FormerlySerializedAs("m_Style")]
        public Styles Style;

        /// <summary>The duration (in seconds) of the blend, if not a cut.
        /// If style is a cut, then this value is ignored.</summary>
        [Tooltip("Duration of the blend, in seconds")]
        [FormerlySerializedAs("m_Time")]
        public float Time;

        /// <summary>
        /// Get the duration of the blend, in seconds.  Will return 0 if blend style is a cut.
        /// </summary>
        public float BlendTime => Style == Styles.Cut ? 0 : Time;

        /// <summary>Constructor</summary>
        /// <param name="style">The shape of the blend curve.</param>
        /// <param name="time">The duration (in seconds) of the blend</param>
        public CinemachineBlendDefinition(Styles style, float time)
        {
            Style = style;
            Time = time;
            CustomCurve = null;
        }

        /// <summary>
        /// A user-defined AnimationCurve, used only if style is Custom.
        /// Curve MUST be normalized, i.e. time range [0...1], value range [0...1].
        /// </summary>
        [FormerlySerializedAs("m_CustomCurve")]
        public AnimationCurve CustomCurve;

        static AnimationCurve[] s_StandardCurves;
        void CreateStandardCurves()
        {
            s_StandardCurves = new AnimationCurve[(int)Styles.Custom];

            s_StandardCurves[(int)Styles.Cut] = null;
            s_StandardCurves[(int)Styles.EaseInOut] = AnimationCurve.EaseInOut(0f, 0f, 1, 1f);

            s_StandardCurves[(int)Styles.EaseIn] = AnimationCurve.Linear(0f, 0f, 1, 1f);
            Keyframe[] keys = s_StandardCurves[(int)Styles.EaseIn].keys;
            keys[0].outTangent = 1.4f;
            keys[1].inTangent = 0;
            s_StandardCurves[(int)Styles.EaseIn].keys = keys;

            s_StandardCurves[(int)Styles.EaseOut] = AnimationCurve.Linear(0f, 0f, 1, 1f);
            keys = s_StandardCurves[(int)Styles.EaseOut].keys;
            keys[0].outTangent = 0;
            keys[1].inTangent = 1.4f;
            s_StandardCurves[(int)Styles.EaseOut].keys = keys;

            s_StandardCurves[(int)Styles.HardIn] = AnimationCurve.Linear(0f, 0f, 1, 1f);
            keys = s_StandardCurves[(int)Styles.HardIn].keys;
            keys[0].outTangent = 0;
            keys[1].inTangent = 3f;
            s_StandardCurves[(int)Styles.HardIn].keys = keys;

            s_StandardCurves[(int)Styles.HardOut] = AnimationCurve.Linear(0f, 0f, 1, 1f);
            keys = s_StandardCurves[(int)Styles.HardOut].keys;
            keys[0].outTangent = 3f;
            keys[1].inTangent = 0;
            s_StandardCurves[(int)Styles.HardOut].keys = keys;

            s_StandardCurves[(int)Styles.Linear] = AnimationCurve.Linear(0f, 0f, 1, 1f);
        }

        /// <summary>
        /// A normalized AnimationCurve specifying the interpolation curve
        /// for this camera blend. Y-axis values must be in range [0,1] (internally clamped
        /// within Blender) and time must be in range of [0, 1].
        /// </summary>
        public AnimationCurve BlendCurve
        {
            get
            {
                if (Style == Styles.Custom)
                {
                    CustomCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1, 1f);
                    return CustomCurve;
                }
                if (s_StandardCurves == null)
                    CreateStandardCurves();
                return s_StandardCurves[(int)Style];
            }
        }
    }

    /// <summary>
    /// Blend result source for blending.   This exposes a CinemachineBlend object
    /// as an ersatz virtual camera for the purposes of blending.  This achieves the purpose
    /// of blending the result oif a blend.
    /// </summary>
    public class NestedBlendSource : ICinemachineCamera
    {
        string m_Name;

        /// <summary>Contructor to wrap a CinemachineBlend object</summary>
        /// <param name="blend">The blend to wrap.</param>
        public NestedBlendSource(CinemachineBlend blend) { Blend = blend; }

        /// <summary>The CinemachineBlend object being wrapped.</summary>
        public CinemachineBlend Blend { get; internal set; }

        /// <inheritdoc />
        public string Name
        {
            get
            {
                // Cache the name only if name is requested
                m_Name ??= (Blend == null || Blend.CamB == null)? "(null)" : "mid-blend to " + Blend.CamB.Name;
                return m_Name;
            }
        }
        /// <inheritdoc />
        public string Description => Blend == null ? "(null)" : Blend.Description;
        /// <inheritdoc />
        public CameraState State { get; private set; }
        /// <inheritdoc />
        public bool IsValid => Blend != null && Blend.IsValid;
        /// <inheritdoc />
        public ICinemachineMixer ParentCamera => null;
        /// <inheritdoc />
        public void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            if (Blend != null)
            {
                Blend.UpdateCameraState(worldUp, deltaTime);
                State = Blend.State;
            }
        }
        /// <inheritdoc />
        public void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt) {}
    }
}
