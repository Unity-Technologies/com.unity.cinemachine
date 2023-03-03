using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
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
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Impulse Source")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineImpulseSource.html")]
    public class CinemachineImpulseSource : MonoBehaviour
    {
        /// <summary>
        /// This defines the complete impulse signal that will be broadcast.
        /// </summary>
        [FormerlySerializedAs("m_ImpulseDefinition")]
        public CinemachineImpulseDefinition ImpulseDefinition = new CinemachineImpulseDefinition();

        /// <summary>
        /// The default direction and force of the Impulse Signal in the absence of any 
        /// specified overrides.  Overrides can be specified by calling the appropriate 
        /// GenerateImpulse method in the API.
        /// </summary>
        [Header("Default Invocation")]
        [Tooltip("The default direction and force of the Impulse Signal in the absense "
            + "of any specified overrides.  Overrides can be specified by calling the appropriate "
                + "GenerateImpulse method in the API.")]
        [FormerlySerializedAs("m_DefaultVelocity")]
        public Vector3 DefaultVelocity = Vector3.down;

        void OnValidate()
        {
            ImpulseDefinition.OnValidate();
        }

        void Reset()
        {
            ImpulseDefinition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = 1,
                ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
                CustomImpulseShape = new AnimationCurve(),
                ImpulseDuration = 0.2f,
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                DissipationDistance = 100,
                DissipationRate = 0.25f,
                PropagationSpeed = 343
            };
            DefaultVelocity = Vector3.down;
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels,
        /// using a custom position and impact velocity</summary>
        /// <param name="position">The world-space position from which the impulse will emanate</param>
        /// <param name="velocity">The impact magnitude and direction</param>
        public void GenerateImpulseAtPositionWithVelocity(Vector3 position, Vector3 velocity)
        {
            if (ImpulseDefinition != null)
                ImpulseDefinition.CreateEvent(position, velocity);
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
            GenerateImpulseAtPositionWithVelocity(transform.position, DefaultVelocity * force);
        }

        /// <summary>Broadcast the Impulse Signal onto the appropriate channels, 
        /// with default velocity = (0, -1, 0), and a default position which is
        /// this transform's location.</summary>
        public void GenerateImpulse()
        {
            GenerateImpulseWithVelocity(DefaultVelocity);
        }

        /// <summary>Legacy API: Please use GenerateImpulseAtPositionWithVelocity() instead.  
        /// Broadcast the Impulse Signal onto the appropriate channels,
        /// using a custom position and impact velocity</summary>
        /// <param name="position">The world-space position from which the impulse will emanate</param>
        /// <param name="velocity">The impact magnitude and direction</param>
        public void GenerateImpulseAt(Vector3 position, Vector3 velocity) 
            => GenerateImpulseAtPositionWithVelocity(position, velocity);

        /// <summary>Legacy API: Please use GenerateImpulseWithVelocity() instead.  
        /// Broadcast the Impulse Signal onto the appropriate channels, using
        /// a custom impact velocity, and this transfom's position.</summary>
        /// <param name="velocity">The impact magnitude and direction</param>
        public void GenerateImpulse(Vector3 velocity) => GenerateImpulseWithVelocity(velocity);

        /// <summary>Legacy API: Please use GenerateImpulseWithForce() instead.  
        /// Broadcast the Impulse Signal onto the appropriate channels, using
        /// a custom impact force, with the standard direction, and this transfom's position.</summary>
        /// <param name="force">The impact magnitude.  1 is normal</param>
        public void GenerateImpulse(float force) => GenerateImpulseWithForce(force);
    }
}
