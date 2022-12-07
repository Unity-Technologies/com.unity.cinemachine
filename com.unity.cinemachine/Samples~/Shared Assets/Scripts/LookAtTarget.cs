using System;
using Unity.Mathematics;
using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Orients the GameObject that this script is attached in such a way that it always faces the Target.
    /// </summary>
    [ExecuteAlways]
    public class LookAtTarget : MonoBehaviour
    {
        [Tooltip("Target to look at.")]
        public Transform Target;

        [Tooltip("Lock rotation along these axes to the initial value.")]
        public bool3 LockRotation;
        Vector3 m_Rotation;
        
        void OnEnable()
        {
            m_Rotation = transform.rotation.eulerAngles;
        }

        void Reset()
        {
            m_Rotation = transform.rotation.eulerAngles;
        }

        void Update()
        {
            if (Target != null)
            {
                var direction = Target.position - transform.position;
                transform.rotation = Quaternion.LookRotation(direction);

                if (LockRotation.x || LockRotation.y || LockRotation.z)
                {
                    var euler = transform.rotation.eulerAngles;
                    euler.x = LockRotation.x ? m_Rotation.x : euler.x;
                    euler.y = LockRotation.y ? m_Rotation.y : euler.y;
                    euler.z = LockRotation.z ? m_Rotation.z : euler.z;
                
                    transform.rotation = Quaternion.Euler(euler);
                }
            }
        }
    }
}
