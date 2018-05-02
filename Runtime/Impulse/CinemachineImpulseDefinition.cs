using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Base class for a definition of an impulse signal that gets propagated to listeners
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.API)]
    public abstract class CinemachineImpulseDefinition : ScriptableObject
    {
        /// <summary>Generate an impulse at a location in space</summary>
        public abstract void CreateEvent(Vector3 velocity, Vector3 pos, int channel,
            CinemachineImpulseManager.EnvelopeDefinition timeEnvelope,
            float impactRadius, CinemachineImpulseManager.ImpulseEvent.DissipationMode dissipationMode,
            float dissipationDistance);
    }
}
