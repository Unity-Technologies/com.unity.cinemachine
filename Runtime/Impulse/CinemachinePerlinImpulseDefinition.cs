using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// A definition of an impulse signal that gets propagated to listeners
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    public class CinemachinePerlinImpulseDefinition : CinemachineImpulseDefinition
    {
        /// <summary>
        /// The asset containing the Perlin Profile.  
        /// Define the frequencies and amplitudes there to make a signal profile
        /// </summary>
        [Tooltip("The asset containing the Perlin Profile.  Define the frequencies and amplitudes there to make a make a signal profile.  Make your own or just use one of the many presets.")]
        [NoiseSettingsProperty]
        public NoiseSettings m_PerlinProfile;

        /// <summary>
        /// Gain to apply to the amplitudes defined in the Perlin Profile asset.
        /// </summary>
        [Tooltip("Gain to apply to the amplitudes defined in the Perlin Profile asset.  1 is normal.  Setting this to 0 completely mutes the signal.")]
        public float m_AmplitudeGain = 1f;

        /// <summary>
        /// Scale factor to apply to the frequencies defined in the Perlin Profile asset.
        /// </summary>
        [Tooltip("Scale factor to apply to the frequencies defined in the Perlin Profile asset.  1 is normal.  Larger magnitudes will make the signal shake more rapidly.")]
        public float m_FrequencyGain = 1f;

        /// <summary>Generate an impulse at a location in space</summary>
        public override void CreateEvent(
            Vector3 velocity, Vector3 pos, int channel,
            CinemachineImpulseManager.EnvelopeDefinition timeEnvelope,
            float impactRadius, CinemachineImpulseManager.ImpulseEvent.DissipationMode dissipationMode,
            float dissipationDistance)
        {
            if (m_PerlinProfile != null)
            {
                CinemachineImpulseManager.ImpulseEvent e 
                    = CinemachineImpulseManager.Instance.NewImpulseEvent();
                e.m_Envelope = timeEnvelope;
                e.m_SignalSource = new SignalSource(
                    m_PerlinProfile, velocity, m_AmplitudeGain, m_FrequencyGain);
                e.m_Position = pos;
                e.m_Radius = impactRadius;
                e.m_Channel = Mathf.Abs(channel);
                e.m_DissipationMode = dissipationMode;
                e.m_DissipationDistance = dissipationDistance;
                CinemachineImpulseManager.Instance.AddImpulseEvent(e);
            }
        }

        class SignalSource : CinemachineImpulseManager.IRawSignalSource
        {
            NoiseSettings m_PerlinProfile;
            Vector3 m_Velocity;
            float m_AmplitudeGain;
            float m_FrequencyGain;

            public SignalSource(
                NoiseSettings perlinProfile, Vector3 velocity,
                float amplitudeGain, float frequencyGain)
            {
                m_PerlinProfile = perlinProfile;
                m_Velocity = velocity;
                m_AmplitudeGain = amplitudeGain;
                m_FrequencyGain = frequencyGain;
            }

            /// <summary>Get the raw signal at this time</summary>
            /// <param name="timeSinceSignalStart">The time since in seconds since the start of the signal</param>
            /// <param name="pos">The position impulse signal</param>
            /// <param name="rot">The rotation impulse signal</param>
            /// <returns>true if non-trivial signal is returned</returns>
            public bool GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
            {
                if (!mInitialized)
                    Initialize();
                Vector3 signalPos = Vector3.zero;
                Vector3 signalRot = Vector3.zero;

                float time = timeSinceSignalStart * m_FrequencyGain;
                signalPos = NoiseSettings.GetCombinedFilterResults(
                    m_PerlinProfile.PositionNoise, time, mNoiseOffsets) * m_AmplitudeGain;
                signalRot = NoiseSettings.GetCombinedFilterResults(
                    m_PerlinProfile.OrientationNoise, time, mNoiseOffsets) * m_AmplitudeGain;

                float gain = m_Velocity.magnitude;
                signalPos *= gain;
                signalRot *= gain;
                if (gain > UnityVectorExtensions.Epsilon)
                {
                    Quaternion local = Quaternion.FromToRotation(Vector3.up, m_Velocity);
                    signalPos = local * signalPos;
                    signalRot = local * signalRot;
                }
                pos = signalPos;
                rot = Quaternion.Euler(signalRot);
                return gain > UnityVectorExtensions.Epsilon;
            }
    
            bool mInitialized = false;
            Vector3 mNoiseOffsets = Vector3.zero;

            void Initialize()
            {
                mInitialized = true;
                mNoiseOffsets = new Vector3(
                        Random.Range(-1000f, 1000f),
                        Random.Range(-1000f, 1000f),
                        Random.Range(-1000f, 1000f));
            }
        }
    }
}
