using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Generate an Impulse Event this object's Collider collides with something 
    /// or its trigger zone is entered.
    /// 
    /// This component should be attached to a GameObject with a Collider or a Collider2D.
    /// Objects colliding with this (or entering its trigger zone if it's a trigger) will be
    /// filtered according to the layer and tag settings defined here, and if they
    /// pass the filter, they will cause an impulse event to be generated.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [SaveDuringPlay]
    public class CinemachineCollisionImpulseSource : CinemachineImpulseSource
    {
        /// <summary>Only collisions with objects on these layers will generate Impulse events.</summary>
        [Header("Trigger Object Filter")]
        [Tooltip("Only collisions with objects on these layers will generate Impulse events")]
        public LayerMask m_LayerMask = 1;

        /// <summary>No Impulse evemts will be generated for collisions with objects having these tags</summary>
        [TagField]
        [Tooltip("No Impulse evemts will be generated for collisions with objects having these tags")]
        public string m_IgnoreTag = string.Empty;

        /// <summary>If checked, signal direction will be affected by the direction of impact</summary>
        [Header("How To Generate The Impulse")]
        [Tooltip("If checked, signal direction will be affected by the direction of impact")]
        public bool m_UseImpactDirection = false;

        /// <summary>If checked, signal amplitude will be multiplied by the mass of the impacting object</summary>
        [Tooltip("If checked, signal amplitude will be multiplied by the mass of the impacting object")]
        public bool m_ScaleImpactWithMass = false;

        /// <summary>If checked, signal amplitude will be multiplied by the speed of the impacting object</summary>
        [Tooltip("If checked, signal amplitude will be multiplied by the speed of the impacting object")]
        public bool m_ScaleImpactWithSpeed = false;

        Rigidbody mRigidBody;
        Rigidbody2D mRigidBody2D;

        private void Start()
        {
            mRigidBody = GetComponent<Rigidbody>();
            mRigidBody2D = GetComponent<Rigidbody2D>();
        }
        
        private void OnEnable() {} // For the Enabled checkbox

        private void OnCollisionEnter(Collision c)
        {
            GenerateImpactEvent(c.collider, null, c.relativeVelocity);
        }

        private void OnTriggerEnter(Collider c)
        {
            GenerateImpactEvent(c, null, Vector3.zero);
        }

        private void OnCollisionEnter2D(Collision2D c)
        {
            GenerateImpactEvent(null, c.collider, c.relativeVelocity);
        }

        private void OnTriggerEnter2D(Collider2D c)
        {
            GenerateImpactEvent(null, c, Vector3.zero);
        }

        private float GetMassAndVelocity(Collider other, Collider2D other2d, ref Vector3 vel)
        {
            bool getVelocity = vel == Vector3.zero;
            float mass = 1;
            if (m_ScaleImpactWithMass || m_ScaleImpactWithSpeed || m_UseImpactDirection)
            {
                if (mRigidBody != null)
                {
                    if (m_ScaleImpactWithMass)
                        mass *= mRigidBody.mass;
                    if (getVelocity)
                        vel = -mRigidBody.velocity;
                }
                else if (mRigidBody2D != null)
                {
                    if (m_ScaleImpactWithMass)
                        mass *= mRigidBody2D.mass;
                    if (getVelocity)
                        vel = -mRigidBody2D.velocity;
                }

                var rb = other != null ? other.attachedRigidbody : null;
                if (rb != null)
                {
                    if (m_ScaleImpactWithMass)
                        mass *= rb.mass;
                    if (getVelocity)
                        vel += rb.velocity;
                }

                var rb2d = other != null ? other.attachedRigidbody : null;
                if (rb2d != null)
                {
                    if (m_ScaleImpactWithMass)
                        mass *= rb2d.mass;
                    if (getVelocity)
                    {
                        Vector3 v = rb2d.velocity;
                        vel += v;
                    }
                }
            }
            return mass;
        }

        private void GenerateImpactEvent(Collider other, Collider2D other2d, Vector3 vel)
        {
            // Check the filters
            if (!enabled)
                return;
            int layer = (other != null) ? other.gameObject.layer : ((other2d != null) ? other2d.gameObject.layer : 0);
            if (((1 << layer) & m_LayerMask) == 0)
                return;
            if (m_IgnoreTag.Length != 0 && other.CompareTag(m_IgnoreTag))
                return;

            // Calculate the signal direction and magnitude
            float mass = GetMassAndVelocity(other, other2d, ref vel);
            if (m_ScaleImpactWithSpeed)
                mass *= vel.magnitude;
            Vector3 dir = Vector3.down;
            if (m_UseImpactDirection && !vel.AlmostZero())
                dir = -vel.normalized;

            // Fire it off!
            GenerateImpulse(dir * mass);
        }
    }
}
