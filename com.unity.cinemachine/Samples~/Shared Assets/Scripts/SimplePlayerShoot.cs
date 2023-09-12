using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This object manages player shooting.  It is expected to be on the player object, 
    /// or on a child SimplePlayerAimController object of the player.
    /// 
    /// If an AimTargetManager is specified, then the player will aim at that target.
    /// Otherwise, the player will aim in the forward direction of the player object,
    /// or of the SimplePlayerAimController object if it exists and is not decoupled
    /// from the player rotation
    /// </summary>
    class SimplePlayerShoot : MonoBehaviour, IInputAxisOwner
    {
        public GameObject BulletPrefab;
        public float MaxBulletsPerSec = 10;
        public float PlayerRotationTime = 0.2f;

        public InputAxis Fire = InputAxis.DefaultMomentary;
        
        [Tooltip("Target to Aim towards. If null, the aim is defined by the forward vector of this gameObject.")]
        public AimTargetManager AimTargetManager;

        [Tooltip("Event that's triggered when firing.")]
        public UnityEvent FireEvent;

        float m_LastFireTime;
        SimplePlayerAimController AimController;

        // We pool the bullets for improved performance
        readonly List<GameObject> m_BulletPool = new ();

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref Fire, Name = "Fire" });
        }

        void OnValidate()
        {
            MaxBulletsPerSec = Mathf.Max(1, MaxBulletsPerSec);
            PlayerRotationTime = Mathf.Max(0, PlayerRotationTime);
        }

        void Start()
        {
            TryGetComponent(out AimController);
        }

        void Update()
        {
            var now = Time.time;
            bool fireNow = BulletPrefab != null 
                && now - m_LastFireTime > 1 / MaxBulletsPerSec
                && Fire.Value > 0.1f;

            // Get the firing direction.  Special case: if there is a decoupled AimController,
            // firing direction is character forward, not AimController forward.
            var fwd = transform.forward;
            bool decoupled = AimController != null 
                && AimController.PlayerRotation == SimplePlayerAimController.CouplingMode.Decoupled;
            if (decoupled)
                fwd = transform.parent.forward;
            
            // Face the firing direction if appropriate
            if ((fireNow || now - m_LastFireTime <= PlayerRotationTime) && AimController != null && !decoupled)
                AimController.RecenterPlayer(PlayerRotationTime);

            if (fireNow)
            {
                m_LastFireTime = now;

                if (AimTargetManager != null)
                    fwd = AimTargetManager.GetAimDirection(transform.position, fwd).normalized;

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
