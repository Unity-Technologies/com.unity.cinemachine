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
        [Tooltip("Impulse events generated here will appear on the channels included in the mask.")]
        [CinemachineImpulseChannelProperty]
        public int m_ImpulseChannel = 1;

        /// <summary>
        /// Defines the signal that will be generated.
        /// </summary>
        [Tooltip("Defines the signal that will be generated.")]
        [CinemachineEmbeddedAssetProperty(true)]
        public CinemachineImpulseDefinition m_SignalDefinition = null;

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels</summary>
        public void GenerateImpulse(Vector3 velocity)
        {
            if (m_SignalDefinition != null)
                m_SignalDefinition.CreateEvent(velocity, transform.position, m_ImpulseChannel);
        }
    }
}
