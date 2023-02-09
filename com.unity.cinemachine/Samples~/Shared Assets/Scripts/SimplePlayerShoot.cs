using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cinemachine.Examples
{
    class SimplePlayerShoot : MonoBehaviour, IInputAxisSource
    {
        public SimpleBullet BulletPrefab;
        public float MaxBulletsPerSec = 10;
        public float BulletSpeed = 500;
        public float TimeInAir = 3;
        public float PlayerRotationTime = 0.2f;

        public InputAxis Fire = InputAxis.DefaultMomentary;
        
        [Tooltip("Target to Aim towards. If null, the aim is defined by the forward vector of this gameObject.")]
        public Transform AimTarget;

        [Tooltip("Event that's triggered when firing.")]
        public UnityEvent FireEvent;

        float m_LastFireTime;

        readonly List<SimpleBullet> m_BulletPool = new List<SimpleBullet>();

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
            BulletSpeed = Mathf.Max(1, BulletSpeed);
            TimeInAir = Mathf.Max(0.2f, TimeInAir);
            PlayerRotationTime = Mathf.Max(0, PlayerRotationTime);
        }

        void Update()
        {
            var now = Time.time;
            bool fireNow = BulletPrefab is not null 
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

                SimpleBullet bullet = null;
                for (var i = 0; i < m_BulletPool.Count; i++) // Look in the pool if one is available
                {
                    if (m_BulletPool[i].gameObject.activeInHierarchy) continue;
                    bullet = m_BulletPool[i];
                    m_BulletPool.Remove(bullet);
                    break;
                }
                
                bullet = bullet is null ? Instantiate(BulletPrefab.gameObject).GetComponent<SimpleBullet>() : bullet; // Instantiate a bullet if none are found in the pool
                bullet.transform.position = transform.position + fwd;
                bullet.transform.rotation = Quaternion.LookRotation(fwd, transform.up);
                bullet.Fire(fwd, BulletSpeed, TimeInAir);
                m_BulletPool.Add(bullet);
                FireEvent.Invoke();
            }
        }
    }
}
