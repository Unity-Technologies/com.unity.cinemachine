using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine.Examples
{
    class SimplePlayerShoot : MonoBehaviour, IInputAxisSource
    {
        public GameObject BulletPrefab;
        public float MaxBulletsPerSec = 10;
        public float BulletSpeed = 500;
        public float TimeInAir = 3;
        public InputAxis Fire = new InputAxis { Range = new Vector2(0, 1) };
        
        [Tooltip("Target to Aim towards. If null, the aim is defined by the forward vector of this gameObject.")]
        public Transform AimTarget;

        float m_LastFireTime;

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = Fire, Name = "Fire", AxisIndex = 2 });
        }

        void OnValidate()
        {
            MaxBulletsPerSec = Mathf.Max(1, MaxBulletsPerSec);
            BulletSpeed = Mathf.Max(1, BulletSpeed);
            TimeInAir = Mathf.Max(0.2f, TimeInAir);
        }

        void Update()
        {
            var now = Time.time;
            if (BulletPrefab != null 
                && now - m_LastFireTime > 1 / MaxBulletsPerSec
                && Fire.Value > 0.1f)
            {
                var fwd = transform.forward;
                if (AimTarget != null)
                    fwd = (AimTarget.position - transform.position).normalized;
                var go = Instantiate(BulletPrefab, transform.position + fwd, transform.rotation);
                if (go.TryGetComponent<SimpleBullet>(out var b))
                    b.Fire(fwd, BulletSpeed);
                Destroy(go, TimeInAir);
                m_LastFireTime = now;
            }
        }
    }
}
