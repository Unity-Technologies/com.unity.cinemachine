using Cinemachine.Utility;
using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Property applied to CinemachineImpulseManager Channels.  
    /// Used for custom drawing in the inspector.
    /// </summary>
    public sealed class CinemachineImpulseDefinitionPropertyAttribute : PropertyAttribute {}

    /// <summary>
    /// Definition of an impulse signal that gets propagated to listeners.
    /// 
    /// Here you provide a Raw Signal source, and define an envelope for time-scaling
    /// it to craft the complete Impulse signal shape.  Also, you provide here parameters 
    /// that define how the signal dissipates with spatial distance from the source location.
    /// Finally, you specify the Impulse Channel on which the signal will be sent.
    /// 
    /// An API method is provided here to take these parameters, create an Impulse Event,
    /// and broadcast it on the channel.
    /// 
    /// When creating a custom Impulse Source class, you will have an instance of this class
    /// as a field in your custom class.  Be sure also to include the
    /// [CinemachineImpulseDefinition] attribute on the field, to get the right
    /// property drawer for it.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.API)]
    [Serializable]
    public class CinemachineImpulseDefinition
    {
        /// <summary>
        /// Impulse events generated here will appear on the channels included in the mask.
        /// </summary>
        [CinemachineImpulseChannelProperty]
        [Tooltip("Impulse events generated here will appear on the channels included in the mask.")]
        public int m_ImpulseChannel = 1;

        /// <summary>
        /// Defines the signal that will be generated.
        /// </summary>
        [Header("Signal Shape")]
        [Tooltip("Defines the signal that will be generated.")]
        [CinemachineEmbeddedAssetProperty(true)]
        public SignalSourceAsset m_RawSignal = null;

        /// <summary>
        /// Gain to apply to the amplitudes defined in the signal source asset.
        /// </summary>
        [Tooltip("Gain to apply to the amplitudes defined in the signal source.  1 is normal.  Setting this to 0 completely mutes the signal.")]
        public float m_AmplitudeGain = 1f;
        
        /// <summary>
        /// Scale factor to apply to the time axis.
        /// </summary>
        [Tooltip("Scale factor to apply to the time axis.  1 is normal.  Larger magnitudes will make the signal progress more rapidly.")]
        public float m_FrequencyGain = 1f;

        /// <summary>How to fit the signal into the envelope time</summary>
        public enum RepeatMode
        {
            /// <summary>Time-stretch the signal to fit the envelope</summary>
            Stretch,
            /// <summary>Loop the signal in time to fill the envelope</summary>
            Loop
        }
        /// <summary>How to fit the signal into the envelope time</summary>
        [Tooltip("How to fit the signal into the envelope time")]
        public RepeatMode m_RepeatMode = RepeatMode.Stretch;

        /// <summary>Randomize the signal start time</summary>
        [Tooltip("Randomize the signal start time")]
        public bool m_Randomize = true;

        /// <summary>
        /// This defines the time-envelope of the signal.  
        /// The raw signal will be time-scaled to fit in the envelope.
        /// </summary>
        [Tooltip("This defines the time-envelope of the signal.  The raw signal will be time-scaled to fit in the envelope.")]
        [CinemachineImpulseEnvelopeProperty]
        public CinemachineImpulseManager.EnvelopeDefinition m_TimeEnvelope
            = CinemachineImpulseManager.EnvelopeDefinition.Default();

        /// <summary>
        /// The signal will have full amplitude in this radius surrounding the impact point.  
        /// Beyond that it will dissipate with distance.
        /// </summary>
        [Header("Spatial Range")]
        [Tooltip("The signal will have full amplitude in this radius surrounding the impact point.  Beyond that it will dissipate with distance.")]
        public float m_ImpactRadius = 100;

        /// <summary>How the signal direction behaves as the listener moves away from the origin.</summary>
        [Tooltip("How the signal direction behaves as the listener moves away from the origin.")]
        public CinemachineImpulseManager.ImpulseEvent.DirectionMode m_DirectionMode 
            = CinemachineImpulseManager.ImpulseEvent.DirectionMode.Fixed;

        /// <summary>
        /// This defines how the signal will dissipate with distance beyond the impact radius.  
        /// </summary>
        [Tooltip("This defines how the signal will dissipate with distance beyond the impact radius.")]
        public CinemachineImpulseManager.ImpulseEvent.DissipationMode m_DissipationMode 
            = CinemachineImpulseManager.ImpulseEvent.DissipationMode.ExponentialDecay;

        /// <summary>
        /// At this distance beyond the impact radius, the signal will have dissipated to zero.  
        /// </summary>
        [Tooltip("At this distance beyond the impact radius, the signal will have dissipated to zero.")]
        public float m_DissipationDistance = 1000;

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
            m_ImpactRadius = Mathf.Max(0, m_ImpactRadius);
            m_DissipationDistance = Mathf.Max(0, m_DissipationDistance);
            m_TimeEnvelope.Validate();
            m_PropagationSpeed = Mathf.Max(1, m_PropagationSpeed);
        }

        /// <summary>Generate an impulse event at a location in space, 
        /// and broadcast it on the appropriate impulse channel</summary>
        public CinemachineImpulseManager.ImpulseEvent CreateEvent(
            Vector3 position, Vector3 velocity)
        {
            if (m_RawSignal == null || Mathf.Abs(m_TimeEnvelope.Duration) < UnityVectorExtensions.Epsilon)
                return null;

            CinemachineImpulseManager.ImpulseEvent e 
                = CinemachineImpulseManager.Instance.NewImpulseEvent();
            e.m_Envelope = m_TimeEnvelope;

            // Scale the time-envelope decay as the root of the amplitude scale
            e.m_Envelope = m_TimeEnvelope;
            if (m_TimeEnvelope.m_ScaleWithImpact)
                e.m_Envelope.m_DecayTime *= Mathf.Sqrt(velocity.magnitude);

            e.m_SignalSource = new SignalSource(this, velocity);
            e.m_Position = position;
            e.m_Radius = m_ImpactRadius;
            e.m_Channel = m_ImpulseChannel;
            e.m_DirectionMode = m_DirectionMode;
            e.m_DissipationMode = m_DissipationMode;
            e.m_DissipationDistance = m_DissipationDistance;
            e.m_PropagationSpeed = m_PropagationSpeed;
            CinemachineImpulseManager.Instance.AddImpulseEvent(e);

            return e;
        }

        // Wrap the raw signal to handle gain, RepeatMode, randomization, and velocity
        class SignalSource : ISignalSource6D
        {
            CinemachineImpulseDefinition m_Def;
            Vector3 m_Velocity;
            float m_StartTimeOffset = 0;

            public SignalSource(CinemachineImpulseDefinition def, Vector3 velocity)
            {
                m_Def = def;
                m_Velocity = velocity;
                if (m_Def.m_Randomize && m_Def.m_RawSignal.SignalDuration <= 0)
                    m_StartTimeOffset = UnityEngine.Random.Range(-1000f, 1000f);
            }

            public float SignalDuration { get { return m_Def.m_RawSignal.SignalDuration; } }

            public void GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
            {
                float time = m_StartTimeOffset + timeSinceSignalStart * m_Def.m_FrequencyGain;

                // Do we have to fit the signal into the envelope?
                float signalDuration = SignalDuration;
                if (signalDuration > 0)
                {
                    if (m_Def.m_RepeatMode == RepeatMode.Loop)
                        time %= signalDuration;
                    else if (m_Def.m_TimeEnvelope.Duration > UnityVectorExtensions.Epsilon)
                        time *= m_Def.m_TimeEnvelope.Duration / signalDuration; // stretch
                }

                m_Def.m_RawSignal.GetSignal(time, out pos, out rot);
                float gain = m_Velocity.magnitude;
                Vector3 dir = m_Velocity.normalized;
                gain *= m_Def.m_AmplitudeGain;
                pos *= gain;
                pos = Quaternion.FromToRotation(Vector3.down, m_Velocity) * pos;
                rot = Quaternion.SlerpUnclamped(Quaternion.identity, rot, gain);
            }
        }
    }
}
