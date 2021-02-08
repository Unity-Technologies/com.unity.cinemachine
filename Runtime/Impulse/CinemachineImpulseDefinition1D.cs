using Cinemachine.Utility;
using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Definition of an impulse signal that gets propagated to listeners.
    /// 
    /// Here you provide a source signal shape and duration, Also, you provide here parameters 
    /// that define how the signal dissipates with spatial distance from the source location.
    /// Finally, you specify the Impulse Channel on which the signal will be sent.
    /// 
    /// An API method is provided here to take these parameters, create an Impulse Event,
    /// and broadcast it on the channel.
    /// 
    /// When creating a custom Impulse Source class, you will have an instance of this class
    /// as a field in your custom class.  
    /// </summary>
    [Serializable]
    public class CinemachineImpulseDefinition1D
    {
        /// <summary>
        /// Impulse events generated here will appear on the channels included in the mask.
        /// </summary>
        [CinemachineImpulseChannelProperty]
        [Tooltip("Impulse events generated here will appear on the channels included in the mask.")]
        public int m_ImpulseChannel = 1;

        /// <summary>Supported predefined shapes for the impulses.</summary>
        public enum ImpulseShapes
        {
            /// <summary>Custom shape</summary>
            Custom,
            /// <summary></summary>
            Recoil,
            /// <summary></summary>
            Bump,
            /// <summary></summary>
            Explosion,
            /// <summary></summary>
            Rumble
        };

        /// <summary>The shape of the impact signal.</summary>
        [Tooltip("Shape of the impact signal")]
        public ImpulseShapes m_ImpulseShape;

        /// <summary>
        /// A user-defined impulse shape, used only if m_ImpulseShape is Custom.
        /// Defines the signal that will be generated.  X axis must go from 0...1, 
        /// and Y axis is the scale that will be applied to the impact velocity.
        /// </summary>
        [Tooltip("Defines the custom shape of the impact signal that will be generated.")]
        public AnimationCurve m_CustomImpulseShape = new AnimationCurve();

        /// <summary>
        /// The time during which the impact signal will occur.  
        /// The signal shape will be stretched to fill that time.
        /// </summary>
        [Tooltip("The time during which the impact signal will occur.  "
            + "The signal shape will be stretched to fill that time.")]
        public float m_ImpulseDuration = 0.2f;

        /// <summary>
        /// This enum represents the various ways an impulse can travel through space
        /// </summary>
        public enum PropagationModes
        {
            /// <summary>The impulse is felt equally everywhere in space, at the same time</summary>
            Uniform,
            /// <summary>The impulse is felt only within a specified radius, and its strength 
            /// weakens for listeners that are farther away</summary>
            Dissipating,
            /// <summary>
            /// The impulse is felt only within a specified radius, and its strength 
            /// weakens for listeners that are farther away.  Also, the impulse travels outwardly
            /// from the impact point, at a specified velocity, similar to a sound wave.
            /// </summary>
            Propagating
        }

        /// <summary>
        /// How the impulse travels through space and time.
        /// </summary>
        [Tooltip("How the impulse travels through space and time.")]
        public PropagationModes m_SpatialRange;

        /// <summary>
        /// The signal will have full no effect outside this radius surrounding the impact point.
        /// </summary>
        [Tooltip("The signal will have full no effect outside this radius surrounding the impact point.")]
        public float m_EffectRadius = 100;

        /// <summary>
        /// This defines how the widely signal will spread within the effect radius before
        /// dissipating with distance from the impact point
        /// </summary>
        [Tooltip("This defines how the widely signal will spread within the effect radius before "
            + "dissipating with distance from the impact point")]
        [Range(0,1)]
        public float m_DissipationRate;

        /// <summary>
        /// The speed (m/s) at which the impulse propagates through space.  High speeds 
        /// allow listeners to react instantaneously, while slower speeds allow listeners in the 
        /// scene to react as if to a wave spreading from the source.  
        /// </summary>
        [Tooltip("The speed (m/s) at which the impulse propagates through space.  High speeds "
            + "allow listeners to react instantaneously, while slower speeds allow listeners in the "
            + "scene to react as if to a wave spreading from the source.")]
        public float m_PropagationSpeed = 343;  // speed of sound

        /// <summary>Call this from your behaviour's OnValidate to validate the fields here</summary>
        public void OnValidate()
        {
            RuntimeUtility.NormalizeCurve(m_CustomImpulseShape, true, false);
            m_ImpulseDuration = Mathf.Max(UnityVectorExtensions.Epsilon, m_ImpulseDuration);
            m_EffectRadius = Mathf.Max(UnityVectorExtensions.Epsilon, m_EffectRadius);
            m_DissipationRate = Mathf.Clamp01(m_DissipationRate);
            m_PropagationSpeed = Mathf.Max(1, m_PropagationSpeed);
        }

        static AnimationCurve[] sStandardShapes;
        static void CreateStandardShapes()
        {
            int max = 0;
            foreach (var value in Enum.GetValues(typeof(ImpulseShapes)))
                max = Mathf.Max(max, (int)value);
            sStandardShapes = new AnimationCurve[max + 1];

            sStandardShapes[(int)ImpulseShapes.Recoil] = new AnimationCurve(new Keyframe[] 
            { 
                new Keyframe(0, 1, -3.2f, -3.2f), 
                new Keyframe(1, 0, 0, 0) 
            });

            sStandardShapes[(int)ImpulseShapes.Bump] = new AnimationCurve(new Keyframe[] 
            { 
                new Keyframe(0, 0, -4.9f, -4.9f), 
                new Keyframe(0.2f, 0, 8.25f,  8.25f), 
                new Keyframe(1, 0, -0.25f, -0.25f) 
            });

            sStandardShapes[(int)ImpulseShapes.Explosion] = new AnimationCurve(new Keyframe[] 
            { 
                new Keyframe(0, -1.4f, -7.9f, -7.9f),
                new Keyframe(0.27f, 0.78f, 23.4f, 23.4f),
                new Keyframe(0.54f, -0.12f, 22.6f, 22.6f),
                new Keyframe(0.75f, 0.042f, 9.23f, 9.23f),
                new Keyframe(0.9f, -0.02f, 5.8f, 5.8f),
                new Keyframe(0.95f, -0.006f, -3.0f, -3.0f),
                new Keyframe(1, 0, 0, 0)
            });

            sStandardShapes[(int)ImpulseShapes.Rumble] = new AnimationCurve(new Keyframe[] 
            { 
                new Keyframe(0, 0, 0, 0),
                new Keyframe(0.333f, 1, 0, 0),
                new Keyframe(0.666f, 1, 0, 0),
                new Keyframe(1, 0, 0, 0)
            });
        }

        internal static AnimationCurve GetStandardCurve(ImpulseShapes shape)
        {
            if (sStandardShapes == null)
                CreateStandardShapes();
            return sStandardShapes[(int)shape];
        }

        internal AnimationCurve ImpulseCurve
        {
            get
            {
                if (m_ImpulseShape == ImpulseShapes.Custom)
                {
                    if (m_CustomImpulseShape == null)
                        m_CustomImpulseShape = AnimationCurve.EaseInOut(0f, 0f, 1, 1f);
                    return m_CustomImpulseShape;
                }
                return GetStandardCurve(m_ImpulseShape);
            }
        }

        /// <summary>Generate an impulse event at a location in space, 
        /// and broadcast it on the appropriate impulse channel</summary>
        /// <param name="position">Event originates at this position in world space</param>
        /// <param name="velocity">This direction is considered to be "down" for the purposes of the 
        /// event signal, and the magnitude of the signal will be scaled according to the 
        /// length of this vector</param>
        public void CreateEvent(Vector3 position, Vector3 velocity)
        {
            const float kBigNumber = 9999999.0f;

            if ((m_ImpulseShape == ImpulseShapes.Custom && m_CustomImpulseShape == null)
                || Mathf.Abs(m_EffectRadius) < UnityVectorExtensions.Epsilon
                || Mathf.Abs(m_ImpulseDuration) < UnityVectorExtensions.Epsilon)
                return;

            CinemachineImpulseManager.ImpulseEvent e 
                = CinemachineImpulseManager.Instance.NewImpulseEvent();
            e.m_Envelope = new CinemachineImpulseManager.EnvelopeDefinition
            {
                m_SustainTime = m_ImpulseDuration
            };

            e.m_SignalSource = new SignalSource(this, velocity);
            e.m_Position = position;
            e.m_Radius = m_SpatialRange == PropagationModes.Uniform ? kBigNumber : 0;
            e.m_Channel = m_ImpulseChannel;
            e.m_DirectionMode = CinemachineImpulseManager.ImpulseEvent.DirectionMode.Fixed;
            e.m_DissipationDistance = m_SpatialRange == PropagationModes.Uniform ? 0 : m_EffectRadius;
            e.m_PropagationSpeed = m_SpatialRange == PropagationModes.Propagating ? m_PropagationSpeed : kBigNumber;
            e.m_CustomDissipation = m_DissipationRate;

            CinemachineImpulseManager.Instance.AddImpulseEvent(e);
        }

        // Wrap the raw signal to handle gain, RepeatMode, randomization, and velocity
        class SignalSource : ISignalSource6D
        {
            CinemachineImpulseDefinition1D m_Def;
            Vector3 m_Velocity;

            public SignalSource(CinemachineImpulseDefinition1D def, Vector3 velocity)
            {
                m_Def = def;
                m_Velocity = velocity;
            }

            public float SignalDuration { get { return m_Def.m_ImpulseDuration; } }

            public void GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
            {
                pos = m_Velocity * m_Def.ImpulseCurve.Evaluate(timeSinceSignalStart / SignalDuration);
                rot = Quaternion.identity;
            }
        }
    }
}
