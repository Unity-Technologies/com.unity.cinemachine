using System;
using UnityEngine;
using UnityEngine.Events;

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
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineImpulseSourceOverview.html")]
    public class CinemachineImpulseSource1D : MonoBehaviour
    {
        /// <summary>
        /// This defines the complete impulse signal that will be broadcast.
        /// </summary>
        public CinemachineImpulseDefinition1D m_ImpulseDefinition = new CinemachineImpulseDefinition1D();

        public Vector3 m_DefaultVelocity;

        void OnValidate()
        {
            m_ImpulseDefinition.OnValidate();
        }

        void Reset()
        {
            m_ImpulseDefinition = new CinemachineImpulseDefinition1D
            {
                m_ImpulseChannel = 1,
                m_ImpulseShape = CinemachineImpulseDefinition1D.ImpulseShapes.Bump,
                m_CustomImpulseShape = new AnimationCurve(),
                m_ImpulseDuration = 0.2f,
                m_SpatialRange = CinemachineImpulseDefinition1D.PropagationModes.Uniform,
                m_EffectRadius = 100,
                m_DissipationRate = 0.25f,
                m_PropagationSpeed = 343
            };
            m_DefaultVelocity = Vector3.down;
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels,
        /// using a custom position and impact velocity</summary>
        /// <param name="position">The world-space position from which the impulse will emanate</param>
        /// <param name="velocity">The impact magnitude and direction</param>
        public void GenerateImpulseAtPositionWithVelocity(Vector3 position, Vector3 velocity)
        {
            if (m_ImpulseDefinition != null)
                m_ImpulseDefinition.CreateEvent(position, velocity);
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, using
        /// a custom impact velocity, and this transfom's position.</summary>
        /// <param name="velocity">The impact magnitude and direction</param>
        public void GenerateImpulseWithVelocity(Vector3 velocity)
        {
            GenerateImpulseAtPositionWithVelocity(transform.position, velocity);
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, using
        /// a custom impact force, with the standard direction, and this transfom's position.</summary>
        /// <param name="force">The impact magnitude.  1 is normal</param>
        public void GenerateImpulseWithForce(float force)
        {
            GenerateImpulseAtPositionWithVelocity(
                transform.position, m_DefaultVelocity * force);
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, 
        /// with default velocity = (0, -1, 0), and a default position which is
        /// this transform's location.</summary>
        public void GenerateImpulse()
        {
            GenerateImpulseWithVelocity(m_DefaultVelocity);
        }

        public bool m_Invoke;
        private void Update()
        {
            if (m_Invoke)
            {
                GenerateImpulse();
                m_Invoke = false;
            }
        }
    }
}
