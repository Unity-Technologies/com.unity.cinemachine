using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// A definition of an impulse signal that gets propagated to listeners
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [SaveDuringPlay]
    public class CinemachineFixedImpulseDefinition : CinemachineImpulseDefinition
    {
        /// <summary>The raw signal shape along the X axis</summary>
        [Tooltip("The raw signal shape along the X axis")]
        public AnimationCurve m_RawSignalX;

        /// <summary>The raw signal shape along the Y axis</summary>
        [Tooltip("The raw signal shape along the Y axis")]
        public AnimationCurve m_RawSignalY;

        /// <summary>The raw signal shape along the Z axis</summary>
        [Tooltip("The raw signal shape along the Z axis")]
        public AnimationCurve m_RawSignalZ;

        /// <summary>How to fit the curve into the envelope time</summary>
        public enum RepeatMode
        {
            /// <summary>Time-stretch the curve to fit the envelope</summary>
            Stretch,
            /// <summary>Loop the curve in time to fill the envelope</summary>
            Loop
        }
        /// <summary>How to fit the curve into the envelope time</summary>
        [Tooltip("How to fit the curve into the envelope time")]
        public RepeatMode m_RepeatMode = RepeatMode.Stretch;

        /// <summary>Scale factor for the signal.  Signal is multiplied by this value</summary>
        [Tooltip("Scale factor for the signal.  Signal is multiplied by this value.")]
        public float m_AmplitudeGain = 1;

        /// <summary>Generate an impulse at a location in space</summary>
        public override void CreateEvent(
            Vector3 velocity, Vector3 pos, int channel,
            CinemachineImpulseManager.EnvelopeDefinition timeEnvelope,
            float impactRadius, CinemachineImpulseManager.ImpulseEvent.DissipationMode dissipationMode,
            float dissipationDistance)
        {
            if (timeEnvelope.Duration > UnityVectorExtensions.Epsilon
                && (m_RawSignalX != null || m_RawSignalY != null || m_RawSignalZ != null))
            {
                CinemachineImpulseManager.ImpulseEvent e 
                    = CinemachineImpulseManager.Instance.NewImpulseEvent();
                e.m_Envelope = timeEnvelope;
                e.m_SignalSource = new SignalSource(this, velocity, timeEnvelope.Duration);
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
            CinemachineFixedImpulseDefinition m_Definition;
            Vector3 m_Velocity;
            float m_EnvelopeDuration;

            public SignalSource(
                CinemachineFixedImpulseDefinition definition,
                Vector3 velocity, float envelopeDuration)
            {
                m_Definition = definition;
                m_Velocity = velocity;
                m_EnvelopeDuration = envelopeDuration;
            }

            /// <summary>Get the raw signal at this time</summary>
            /// <param name="timeSinceSignalStart">The time since in seconds since the start of the signal</param>
            /// <param name="pos">The position impulse signal</param>
            /// <param name="rot">The rotation impulse signal</param>
            /// <returns>true if non-trivial signal is returned</returns>
            public bool GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                float gain = m_Definition.m_AmplitudeGain * m_Velocity.magnitude;
                if (Mathf.Abs(gain) > UnityVectorExtensions.Epsilon)
                {
                    pos = new Vector3(
                        GetAxisValue(m_Definition.m_RawSignalX, timeSinceSignalStart),
                        GetAxisValue(m_Definition.m_RawSignalY, timeSinceSignalStart),
                        GetAxisValue(m_Definition.m_RawSignalZ, timeSinceSignalStart)) * gain;

                    Quaternion local = Quaternion.FromToRotation(Vector3.up, m_Velocity);
                    pos = local * pos;
                    return true;
                }
                return false;
            }

            float GetAxisValue(AnimationCurve axis, float timeSinceSignalStart)
            {
                if (axis == null)
                    return 0;
                var keys = axis.keys;
                if (keys.Length == 0)
                    return 0;
                float value = keys[0].value;
                float start = keys[0].time;
                float duration = keys[keys.Length-1].time - start;
                if (duration > UnityVectorExtensions.Epsilon)
                {
                    switch (m_Definition.m_RepeatMode)
                    {
                        default:
                        case RepeatMode.Stretch:
                            value = axis.Evaluate(
                                start + (timeSinceSignalStart / m_EnvelopeDuration) * duration);
                            break;
                        case RepeatMode.Loop:
                            value = axis.Evaluate(start + (timeSinceSignalStart % duration));
                            break;
                    }
                }
                return value;
            }
        }
    }
}
