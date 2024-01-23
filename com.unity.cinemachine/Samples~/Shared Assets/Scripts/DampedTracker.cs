using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Will match a GameObject's position and rotation to a target's position 
    /// and rotation, with damping
    /// </summary>
    public class DampedTracker : MonoBehaviour
    {
        [Tooltip("The target to track")]
        public Transform Target;
        [Tooltip("How fast the GameObject moves to the target position")]
        public float PositionDamping = 1;
        [Tooltip("How fast the rotation aligns with target rotation")]
        public float RotationDamping = 1;

        void OnEnable()
        {
            if (Target != null)
                transform.SetPositionAndRotation(Target.position, Target.rotation);
        }

        void LateUpdate()
        {
            if (Target != null)
            {
                float t = Damper.Damp(1, PositionDamping, Time.deltaTime);
                var pos = Vector3.Lerp(transform.position, Target.position, t);

                t = Damper.Damp(1, RotationDamping, Time.deltaTime);
                var rot = Quaternion.Slerp(transform.rotation, Target.rotation, t);

                transform.SetPositionAndRotation(pos, rot);
            }
        }
    }
}
