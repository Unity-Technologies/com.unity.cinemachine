using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An event-driven impulse that gets propagated to listeners
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [SaveDuringPlay]
    public class CinemachineImpulseSource : MonoBehaviour
    {
        /// <summary>
        /// Impulse events generated here will appear on the channels included in the mask.
        /// </summary>
        [Header("Broadcast")]
        [Tooltip("Impulse events generated here will appear on the channels included in the mask.")]
        [CinemachineImpulseChannelProperty]
        public int m_ImpulseChannel = 1;

        /// <summary>
        /// Defines the signal that will be generated.
        /// </summary>
        [Header("Signal Shape")]
        [Tooltip("Defines the signal that will be generated.")]
        [CinemachineEmbeddedAssetProperty(true)]
        public CinemachineImpulseDefinition m_RawSignalSource = null;

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
        public float m_ImpactRadius = 1;

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
        public float m_DissipationDistance = 100;

        protected virtual void OnValidate()
        {
            m_ImpactRadius = Mathf.Max(0, m_ImpactRadius);
            m_DissipationDistance = Mathf.Max(0, m_DissipationDistance);
            m_TimeEnvelope.Validate();
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels</summary>
        public void GenerateImpulse(Vector3 velocity)
        {
            if (m_RawSignalSource != null)
                m_RawSignalSource.CreateEvent(velocity, transform.position, m_ImpulseChannel,
                    m_TimeEnvelope, m_ImpactRadius, m_DissipationMode, m_DissipationDistance);
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, 
        /// with default velocity = (0, 1, 0)</summary>
        public void GenerateImpulse()
        {
            GenerateImpulse(Vector3.up);
        }
    }
}
