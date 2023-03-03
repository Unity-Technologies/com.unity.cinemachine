using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    class SimplePlayerShoot : MonoBehaviour, IInputAxisSource
    {
        public GameObject BulletPrefab;
        public float MaxBulletsPerSec = 10;
        public float PlayerRotationTime = 0.2f;

        public InputAxis Fire = InputAxis.DefaultMomentary;
        
        [Tooltip("Target to Aim towards. If null, the aim is defined by the forward vector of this gameObject.")]
        public Transform AimTarget;

        [Tooltip("Event that's triggered when firing.")]
        public UnityEvent FireEvent;

        float m_LastFireTime;

        // We pool the bullets for improved performance
        readonly List<GameObject> m_BulletPool = new ();

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref Fire, Name = "Fire" });
        }

        void OnValidate()
        {
            MaxBulletsPerSec = Mathf.Max(1, MaxBulletsPerSec);
            PlayerRotationTime = Mathf.Max(0, PlayerRotationTime);
        }

        void Update()
        {
            var now = Time.time;
            bool fireNow = BulletPrefab != null 
                && now - m_LastFireTime > 1 / MaxBulletsPerSec
                && Fire.Value > 0.1f;

            // Face the firing direction
            if ((fireNow || now - m_LastFireTime <= PlayerRotationTime) && TryGetComponent<SimplePlayerAimController>(out var aim))
                aim.RecenterPlayer(PlayerRotationTime);

            if (fireNow)
            {
                m_LastFireTime = now;

                var fwd = transform.forward;
                if (AimTarget is not null)
                    fwd = (AimTarget.position - transform.position).normalized;

                GameObject bullet = null;
                for (var i = 0; bullet == null && i < m_BulletPool.Count; ++i) // Look in the pool if one is available
                {
                    if (!m_BulletPool[i].activeInHierarchy) 
                    {
                        bullet = m_BulletPool[i];
                        m_BulletPool.Remove(bullet);
                    }
                }
                // Instantiate a bullet if none are found in the pool
                if (bullet == null)
                    bullet = Instantiate(BulletPrefab);

                bullet.SetActive(true);
                bullet.transform.SetPositionAndRotation(transform.position + fwd, Quaternion.LookRotation(fwd, transform.up));
                m_BulletPool.Add(bullet);
                FireEvent.Invoke();
            }
        }
    }
}
