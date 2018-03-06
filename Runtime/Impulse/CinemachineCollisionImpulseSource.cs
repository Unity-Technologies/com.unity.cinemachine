using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An event-driven impulse that gets propagated to listeners when the object's
    /// Collider collides with anything or its trigger zone is entered.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [SaveDuringPlay]
    public class CinemachineCollisionImpulseSource : MonoBehaviour
    {
        /// <summary>Only collisions with objects on these layers will generate Impulse events.</summary>
        [Tooltip("Only collisions with objects on these layers will generate Impulse events")]
        public LayerMask m_CollideAgainst = 1;

        /// <summary>No Impulse evemts will be generated for collisions with objects having these tags</summary>
        [TagField]
        [Tooltip("No Impulse evemts will be generated for collisions with objects having these tags")]
        public string m_IgnoreTag = string.Empty;

        /// <summary>
        /// Impulse events generated here will appear on the channels included in the mask.
        /// </summary>
        [Tooltip("Impulse events generated here will appear on the channels included in the mask.")]
        [CinemachineImpulseChannelProperty]
        public int m_ImpulseChannel = 1;

        /// <summary>Defines the signal that will be generated.</summary>
        [Tooltip("Defines the signal that will be generated.")]
        [CinemachineEmbeddedAssetProperty(true)]
        public CinemachineImpulseDefinition m_SignalDefinition = null;

        /// <summary>These values will be used if no relevant RigidBodies can be found</summary>
        [Serializable]
        public class DefaultValues
        {
            /// <summary>If there is no RigidBody component, this mass will affect the intensity of the impulse.</summary>
            [Tooltip("If there is no RigidBody component, this mass will affect the intensity of the impulse")]
            public float m_Mass = 1;

            /// <summary>If there is no RigidBody component, this velocity will affect the intensity and direction of the impulse.</summary>
            [Tooltip("If there is no RigidBody component, this velocity will affect the intensity and direction of the impulse")]
            public Vector3 m_Velocity = Vector3.down;
        }

        /// <summary>How to calculate the direction and intensity of the impact</summary>
        public enum CalculationMode
        {
            /// <summary>Ignore the RigidBodies, use the default values</summary>
            AlwaysUseDefaultValues,
            /// <summary>Ignore the other collider's RigidBody body</summary>
            UseMyMassAndVelocity,
            /// <summary>Ignore my RigidBody</summary>
            UseOthersMassAndVelocity,
            /// <summary>Use the sum of the values found in both RigidBodies (or the defaults, if no RigidBodies)</summary>
            UseCombinedMassesAndVelocities
        }

        /// <summary>How to calculate the direction and intensity of the impact</summary>
        [Tooltip("How to calculate the direction and intensity of the impact")]
        public CalculationMode m_CalculationMode = CalculationMode.UseCombinedMassesAndVelocities;

        /// <summary>These values will be used if no relevant RigidBodies can be found</summary>
        public DefaultValues m_DefaultIfNoRigidBody = new DefaultValues();


        /// <summary>Broadcast the Impulse Signal onto the appropriate channels</summary>
        public void OnImpact(Vector3 velocity)
        {
            if (m_SignalDefinition != null)
                m_SignalDefinition.CreateEvent(velocity, transform.position, m_ImpulseChannel);
        }

        Rigidbody mRigidBody;
        Rigidbody2D mRigidBody2D;

        private void OnValidate()
        {
            m_DefaultIfNoRigidBody.m_Mass = Mathf.Max(0, m_DefaultIfNoRigidBody.m_Mass);
        }

        private void Start()
        {
            mRigidBody = GetComponent<Rigidbody>();
            mRigidBody2D = GetComponent<Rigidbody2D>();
        }
        
        private void GenerateImpactEvent(GameObject other, bool otherCollided)
        {
            // Check the filters
            if (!enabled)
                return;
            if (((1 << other.layer) & m_CollideAgainst) == 0)
                return;
            if (m_IgnoreTag.Length != 0 && other.CompareTag(m_IgnoreTag))
                return;

            Vector3 vel = m_DefaultIfNoRigidBody.m_Velocity;
            float mass = m_DefaultIfNoRigidBody.m_Mass;
            if (m_CalculationMode != CalculationMode.AlwaysUseDefaultValues)
            {
                if (m_CalculationMode == CalculationMode.UseOthersMassAndVelocity)
                {
                    vel = Vector3.zero;
                    mass = 0;
                }
                else
                {
                    if (mRigidBody != null)
                    {
                        mass = mRigidBody.mass;
                        if (!mRigidBody.isKinematic)
                            vel = mRigidBody.velocity;
                    }
                    else if (mRigidBody2D != null)
                    {
                        mass = mRigidBody2D.mass;
                        if (!mRigidBody2D.isKinematic)
                            vel = mRigidBody2D.velocity;
                    }
                }
                if (m_CalculationMode != CalculationMode.UseMyMassAndVelocity)
                {
                    var rb = other.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        if (otherCollided)
                            vel -= rb.velocity;
                        else
                            vel += rb.velocity;
                        mass += rb.mass;
                    }
                    else
                    {
                        var rb2d = other.GetComponent<Rigidbody2D>();
                        if (rb2d != null)
                        {
                            if (otherCollided)
                                vel -= (Vector3)rb2d.velocity;
                            else
                                vel += (Vector3)rb2d.velocity;
                            mass += rb2d.mass;
                        }
                        else 
                        {
                            vel += m_DefaultIfNoRigidBody.m_Velocity;
                            mass += m_DefaultIfNoRigidBody.m_Mass;
                        }
                    }
                }
            }
            OnImpact(vel * mass);
        }

        private void OnCollisionEnter(Collision other)
        {
            GenerateImpactEvent(other.gameObject, true);
        }

        private void OnTriggerEnter(Collider other)
        {
            GenerateImpactEvent(other.gameObject, false);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            GenerateImpactEvent(other.gameObject, true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GenerateImpactEvent(other.gameObject, false);
        }

        private void OnEnable() {} // For the Enabled checkbox
    }
}
