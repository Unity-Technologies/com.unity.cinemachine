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

        private void OnCollisionEnter(Collision other)
        {
            GenerateImpactEvent(other.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            GenerateImpactEvent(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            GenerateImpactEvent(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GenerateImpactEvent(other.gameObject);
        }

        private float GetMassAndVelocity(GameObject other, out Vector3 vel)
        {
            vel = Vector3.zero;
            float mass = 1;
            if (m_ScaleImpactWithMass || m_ScaleImpactWithSpeed || m_UseImpactDirection)
            {
                if (mRigidBody != null)
                {
                    if (m_ScaleImpactWithMass)
                        mass *= mRigidBody.mass;
                    vel -= mRigidBody.velocity;
                }
                else if (mRigidBody2D != null)
                {
                    if (m_ScaleImpactWithMass)
                        mass *= mRigidBody2D.mass;
                    Vector3 v = mRigidBody2D.velocity;
                    vel -= v;
                }
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (m_ScaleImpactWithMass)
                        mass *= rb.mass;
                    vel += rb.velocity;
                }
                else
                {
                    var rb2d = other.GetComponent<Rigidbody2D>();
                    if (rb2d != null)
                    {
                        if (m_ScaleImpactWithMass)
                            mass *= rb2d.mass;
                        Vector3 v = rb2d.velocity;
                        vel += v;
                    }
                }
            }
            return mass;
        }

        private void GenerateImpactEvent(GameObject other)
        {
            // Check the filters
            if (!enabled)
                return;
            if (((1 << other.layer) & m_LayerMask) == 0)
                return;
            if (m_IgnoreTag.Length != 0 && other.CompareTag(m_IgnoreTag))
                return;

            // Calculate the signal direction and magnitude
            Vector3 vel = Vector3.zero;
            float mass = GetMassAndVelocity(other, out vel);
            if (m_ScaleImpactWithSpeed)
                mass *= vel.magnitude;
            Vector3 dir = Vector3.down;
            if (m_UseImpactDirection && !vel.AlmostZero())
                dir = vel.normalized;

            // Fire it off!
            GenerateImpulse(dir * mass);
        }
    }
}
