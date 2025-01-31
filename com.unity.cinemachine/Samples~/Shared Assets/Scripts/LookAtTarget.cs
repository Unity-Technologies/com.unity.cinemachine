using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Orients the GameObject that this script is attached in such a way that it always faces the Target.
    /// </summary>
    [ExecuteAlways]
    public class LookAtTarget : MonoBehaviour
    {
        [Tooltip("Target to look at.")]
        public Transform Target;

        [Tooltip("Lock rotation along the x axis to the initial value.")]
        public bool LockRotationX;
        [Tooltip("Lock rotation along the y axis to the initial value.")]
        public bool LockRotationY;
        [Tooltip("Lock rotation along the z axis to the initial value.")]
        public bool LockRotationZ;

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

                if (LockRotationX || LockRotationY || LockRotationZ)
                {
                    var euler = transform.rotation.eulerAngles;
                    euler.x = LockRotationX ? m_Rotation.x : euler.x;
                    euler.y = LockRotationY ? m_Rotation.y : euler.y;
                    euler.z = LockRotationZ ? m_Rotation.z : euler.z;
                    transform.rotation = Quaternion.Euler(euler);
                }
            }
        }
    }
}
