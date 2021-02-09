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
    public class CinemachineContinuousImpulseSource1D : MonoBehaviour
    {
        /// <summary>
        /// This defines the complete impulse signal that will be broadcast.
        /// </summary>
        public CinemachineImpulseDefinition1D m_ImpulseDefinition = new CinemachineImpulseDefinition1D();
        public Vector3 m_DefaultVelocity;

        float m_LastEventTime = 0;

        void OnValidate()
        {
            m_ImpulseDefinition.OnValidate();
        }

        void Reset()
        {
            m_ImpulseDefinition = new CinemachineImpulseDefinition1D
            {
                m_ImpulseChannel = 1,
                m_ImpulseShape = CinemachineImpulseDefinition1D.ImpulseShapes.Rumble,
                m_CustomImpulseShape = new AnimationCurve(),
                m_ImpulseDuration = 0.5f,
                m_SpatialRange = CinemachineImpulseDefinition1D.PropagationModes.Uniform,
                m_EffectRadius = 100,
                m_DissipationRate = 0.25f,
                m_PropagationSpeed = 343
            };
        }

        public bool m_Active;

        void Update()
        {
            var now = Time.time;
            float eventLength = m_ImpulseDefinition.m_ImpulseDuration * (2f / 5f);
            if (m_Active && now - m_LastEventTime > eventLength)
            {
                m_ImpulseDefinition.CreateEvent(transform.position, m_DefaultVelocity);
                m_LastEventTime = now;
            }
        }
    }
}
