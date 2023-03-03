using System;
using Unity.Cinemachine.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Describes a blend between 2 CinemachineCameras, and holds the current state of the blend.
    /// </summary>
    public class CinemachineBlend
    {
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

        /// <summary>Validity test for the blend.  True if either camera is defined.</summary>
        public bool IsValid => ((CamA != null && CamA.IsValid) || (CamB != null && CamB.IsValid));

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
                var sb = CinemachineDebug.SBFromPool();
                if (CamB == null || !CamB.IsValid)
                    sb.Append("(none)");
                else
                {
                    sb.Append("[");
                    sb.Append(CamB.Name);
                    sb.Append("]");
                }
                sb.Append(" ");
                sb.Append((int)(BlendWeight * 100f));
                sb.Append("% from ");
                if (CamA == null || !CamA.IsValid)
                    sb.Append("(none)");
                else
                {
                    sb.Append("[");
                    sb.Append(CamA.Name);
                    sb.Append("]");
                }
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
            if (cam == CamA || cam == CamB)
                return true;
            if (CamA is BlendSourceVirtualCamera b && b.Blend.Uses(cam))
                return true;
            b = CamB as BlendSourceVirtualCamera;
            return b != null && b.Blend.Uses(cam);
        }

        /// <summary>Construct a blend</summary>
        /// <param name="a">First camera</param>
        /// <param name="b">Second camera</param>
        /// <param name="curve">Blend curve</param>
        /// <param name="duration">Duration of the blend, in seconds</param>
        /// <param name="t">Current time in blend, relative to the start of the blend</param>
        public CinemachineBlend(
            ICinemachineCamera a, ICinemachineCamera b, AnimationCurve curve, float duration, float t)
        {
            CamA = a;
            CamB = b;
            BlendCurve = curve;
            TimeInBlend = t;
            Duration = duration;
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
                return CameraState.Lerp(CamA.State, CamB.State, BlendWeight);
            }
        }
    }

    /// <summary>Definition of a Camera blend.  This struct holds the information
    /// necessary to generate a suitable AnimationCurve for a Cinemachine Blend.</summary>
    [Serializable]
    public struct CinemachineBlendDefinition
    {
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
    /// Point source for blending. It's not really a virtual camera, but takes
    /// a CameraState and exposes it as a virtual camera for the purposes of blending.
    /// </summary>
    internal class StaticPointVirtualCamera : ICinemachineCamera
    {
        public StaticPointVirtualCamera(CameraState state, string name) { State = state; Name = name; }
        public void SetState(CameraState state) { State = state; }

        public string Name { get; private set; }
        public string Description => string.Empty;
        public Transform LookAt { get; set; }
        public Transform Follow { get; set; }
        public CameraState State { get; private set; }
        public bool IsValid => true;
        public ICinemachineCamera ParentCamera => null;
        public bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false) => false;
        public void UpdateCameraState(Vector3 worldUp, float deltaTime) {}
        public void InternalUpdateCameraState(Vector3 worldUp, float deltaTime) {}
        public void OnTransitionFromCamera(ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) {}
        public void OnTargetObjectWarped(Transform target, Vector3 positionDelta) {}
    }

    /// <summary>
    /// Blend result source for blending.   This exposes a CinemachineBlend object
    /// as an ersatz virtual camera for the purposes of blending.  This achieves the purpose
    /// of blending the result oif a blend.
    /// </summary>
    internal class BlendSourceVirtualCamera : ICinemachineCamera
    {
        public BlendSourceVirtualCamera(CinemachineBlend blend) { Blend = blend; }
        public CinemachineBlend Blend { get; set; }

        public string Name => "Mid-blend";
        public string Description => Blend == null ? "(null)" : Blend.Description;
        public Transform LookAt { get; set; }
        public Transform Follow { get; set; }
        public CameraState State { get; private set; }
        public bool IsValid => Blend != null && Blend.IsValid; 
        public ICinemachineCamera ParentCamera => null;
        public bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false)
            => Blend != null && (vcam == Blend.CamA || vcam == Blend.CamB);
        public CameraState CalculateNewState(float deltaTime) => State;
        public void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            if (Blend != null)
            {
                Blend.UpdateCameraState(worldUp, deltaTime);
                State = Blend.State;
            }
        }
        public void InternalUpdateCameraState(Vector3 worldUp, float deltaTime) {}
        public void OnTransitionFromCamera(ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) {}
        public void OnTargetObjectWarped(Transform target, Vector3 positionDelta) {}
    }
}
