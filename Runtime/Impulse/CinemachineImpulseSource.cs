using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An event-driven class that broadcasts an impulse signal to listeners.
    /// 
    /// This is the base class for custom impulse sources.  It contains an impulse
    /// definition, where the characteristics of the impulse signal are defined.
    /// 
    /// API methods are provided for actually broadcasting the impulse.  Call these
    /// methods from your custom code, or hook them up to game events in the Editor.
    /// 
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineImpulseSourceOverview.html")]
    public class CinemachineImpulseSource : MonoBehaviour
    {
        /// <summary>
        /// This defines the complete impulse signal that will be broadcast.
        /// </summary>
        [CinemachineImpulseDefinitionProperty]
        public CinemachineImpulseDefinition m_ImpulseDefinition = new CinemachineImpulseDefinition();

        private void OnValidate()
        {
            m_ImpulseDefinition.OnValidate();
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels,
        /// using a custom position and impact velocity</summary>
        /// <param name="position">The world-space position from which the impulse will emanate</param>
        /// <param name="velocity">The impact magnitude and direction</param>
        public void GenerateImpulseAt(Vector3 position, Vector3 velocity)
        {
            if (m_ImpulseDefinition != null)
                m_ImpulseDefinition.CreateEvent(position, velocity);
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, using
        /// a custom impact velocity, and this transfom's position.</summary>
        /// <param name="velocity">The impact magnitude and direction</param>
        public void GenerateImpulse(Vector3 velocity)
        {
            GenerateImpulseAt(transform.position, velocity);
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, using
        /// a custom impact force, with the standard direction, and this transfom's position.</summary>
        /// <param name="force">The impact magnitude.  1 is normal</param>
        public void GenerateImpulse(float force)
        {
            GenerateImpulseAt(transform.position, new Vector3(0, -force, 0));
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, 
        /// with default velocity = (0, -1, 0), and a default position which is
        /// this transform's location.</summary>
        public void GenerateImpulse()
        {
            GenerateImpulse(Vector3.down);
        }
    }
}
