using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{

#if !(CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D)
    /// <summary>If Physics or Physics 2D is part of the project, this would generate inpulse events.</summary>
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineCollisionImpulseSource : CinemachineImpulseSource {}
#else
    /// <summary>
    /// Generate an Impulse Event when this object's Collider collides with something
    /// or its trigger zone is entered.
    ///
    /// This component should be attached to a GameObject with a Collider or a Collider2D.
    /// Objects colliding with this (or entering its trigger zone if it's a trigger) will be
    /// filtered according to the layer and tag settings defined here, and if they
    /// pass the filter, they will cause an impulse event to be generated.
    ///
    /// Use CinemachineImpulseSource.ImpulseDefinition to define the characteristics of the impulse.
    /// </summary>
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Collision Impulse Source")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineCollisionImpulseSource.html")]
    public class CinemachineCollisionImpulseSource : CinemachineImpulseSource
    {
        /// <summary>Only collisions with objects on these layers will generate Impulse events.</summary>
        [Header("Trigger Object Filter")]
        [Tooltip("Only collisions with objects on these layers will generate Impulse events")]
        [FormerlySerializedAs("m_LayerMask")]
        public LayerMask LayerMask = 1;

        /// <summary>No Impulse events will be generated for collisions with objects having these tags</summary>
        [TagField]
        [Tooltip("No Impulse events will be generated for collisions with objects having these tags")]
        [FormerlySerializedAs("m_IgnoreTag")]
        public string IgnoreTag = string.Empty;

        /// <summary>If checked, signal direction will be affected by the direction of impact</summary>
        [Header("How To Generate The Impulse")]
        [Tooltip("If checked, signal direction will be affected by the direction of impact")]
        [FormerlySerializedAs("m_UseImpactDirection")]
        public bool UseImpactDirection = false;

        /// <summary>If checked, signal amplitude will be multiplied by the mass of the impacting object</summary>
        [Tooltip("If checked, signal amplitude will be multiplied by the mass of the impacting object")]
        [FormerlySerializedAs("m_ScaleImpactWithMass")]
        public bool ScaleImpactWithMass = false;

        /// <summary>If checked, signal amplitude will be multiplied by the speed of the impacting object</summary>
        [Tooltip("If checked, signal amplitude will be multiplied by the speed of the impacting object")]
        [FormerlySerializedAs("m_ScaleImpactWithSpeed")]
        public bool ScaleImpactWithSpeed = false;

#if CINEMACHINE_PHYSICS
        Rigidbody m_RigidBody;
#endif
#if CINEMACHINE_PHYSICS_2D
        Rigidbody2D m_RigidBody2D;
#endif

        void Reset()
        {
            LayerMask = 1;
            IgnoreTag = string.Empty;
            UseImpactDirection = false;
            ScaleImpactWithMass = false;
            ScaleImpactWithSpeed = false;
        }

        void Start()
        {
#if CINEMACHINE_PHYSICS
            TryGetComponent(out m_RigidBody);
#endif
#if CINEMACHINE_PHYSICS_2D
            TryGetComponent(out m_RigidBody2D);
#endif
        }

        void OnEnable() {} // For the Enabled checkbox

#if CINEMACHINE_PHYSICS
        void OnCollisionEnter(Collision c)
        {
            GenerateImpactEvent(c.collider, c.relativeVelocity);
        }

        void OnTriggerEnter(Collider c)
        {
            GenerateImpactEvent(c, Vector3.zero);
        }

        float GetMassAndVelocity(Collider other, ref Vector3 vel)
        {
            bool getVelocity = vel == Vector3.zero;
            float mass = 1;
            if (ScaleImpactWithMass || ScaleImpactWithSpeed || UseImpactDirection)
            {
                if (m_RigidBody != null)
                {
                    if (ScaleImpactWithMass)
                        mass *= m_RigidBody.mass;
                    if (getVelocity)
#if UNITY_2023_3_OR_NEWER
                        vel = -m_RigidBody.linearVelocity;
#else
                        vel = -m_RigidBody.velocity;
#endif
                }
                var rb = other != null ? other.attachedRigidbody : null;
                if (rb != null)
                {
                    if (ScaleImpactWithMass)
                        mass *= rb.mass;
                    if (getVelocity)
#if UNITY_2023_3_OR_NEWER
                        vel += rb.linearVelocity;
#else
                        vel += rb.velocity;
#endif

                }
            }
            return mass;
        }

        void GenerateImpactEvent(Collider other, Vector3 vel)
        {
            // Check the filters
            if (!enabled)
                return;

            if (other != null)
            {
                int layer = other.gameObject.layer;
                if (((1 << layer) & LayerMask) == 0)
                    return;
                if (IgnoreTag.Length != 0 && other.CompareTag(IgnoreTag))
                    return;
            }

            // Calculate the signal direction and magnitude
            float mass = GetMassAndVelocity(other, ref vel);
            if (ScaleImpactWithSpeed)
                mass *= Mathf.Sqrt(vel.magnitude);
            Vector3 dir = DefaultVelocity;
            if (UseImpactDirection && !vel.AlmostZero())
                dir = -vel.normalized * dir.magnitude;

            // Fire it off!
            GenerateImpulseWithVelocity(dir * mass);
        }
#endif

#if CINEMACHINE_PHYSICS_2D
        void OnCollisionEnter2D(Collision2D c)
        {
            GenerateImpactEvent2D(c.collider, c.relativeVelocity);
        }

        void OnTriggerEnter2D(Collider2D c)
        {
            GenerateImpactEvent2D(c, Vector3.zero);
        }

        float GetMassAndVelocity2D(Collider2D other2d, ref Vector3 vel)
        {
            bool getVelocity = vel == Vector3.zero;
            float mass = 1;
            if (ScaleImpactWithMass || ScaleImpactWithSpeed || UseImpactDirection)
            {
                if (m_RigidBody2D != null)
                {
                    if (ScaleImpactWithMass)
                        mass *= m_RigidBody2D.mass;
                    if (getVelocity)
#if CINEMACHINE_UNITY_6000_0_11F1_OR_NEWER
                        vel = -m_RigidBody2D.linearVelocity;
#else
                        vel = -m_RigidBody2D.velocity;
#endif
                }

                var rb2d = other2d != null ? other2d.attachedRigidbody : null;
                if (rb2d != null)
                {
                    if (ScaleImpactWithMass)
                        mass *= rb2d.mass;
                    if (getVelocity)
                    {
#if CINEMACHINE_UNITY_6000_0_11F1_OR_NEWER
                        Vector3 v = rb2d.linearVelocity;
#else
                        Vector3 v = rb2d.velocity;
#endif
                        vel += v;
                    }
                }
            }
            return mass;
        }

        void GenerateImpactEvent2D(Collider2D other2d, Vector3 vel)
        {
            // Check the filters
            if (!enabled)
                return;

            if (other2d != null)
            {
                int layer = other2d.gameObject.layer;
                if (((1 << layer) & LayerMask) == 0)
                    return;
                if (IgnoreTag.Length != 0 && other2d.CompareTag(IgnoreTag))
                    return;
            }

            // Calculate the signal direction and magnitude
            float mass = GetMassAndVelocity2D(other2d, ref vel);
            if (ScaleImpactWithSpeed)
                mass *= Mathf.Sqrt(vel.magnitude);
            Vector3 dir = DefaultVelocity;
            if (UseImpactDirection && !vel.AlmostZero())
                dir = -vel.normalized * dir.magnitude;

            // Fire it off!
            GenerateImpulseWithVelocity(dir * mass);
        }
#endif
    }
#endif
}
