using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Will match a GameObject's position and rotation to a target's position
    /// and rotation, with damping
    /// </summary>
    [ExecuteAlways]
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
                // Match the player's position
                float t = Damper.Damp(100, PositionDamping, Time.deltaTime) * 0.01f;
                var pos = Vector3.Lerp(transform.position, Target.position, t);

                // Rotate my transform to make my up match the target's up
                var rot = transform.rotation;
                t = Damper.Damp(100, RotationDamping, Time.deltaTime) * 0.01f;
                var srcUp = transform.up;
                var dstUp = Target.up;
                var axis = Vector3.Cross(srcUp, dstUp);
                if (axis.sqrMagnitude > 0.000001f)
                {
                    var angle = UnityVectorExtensions.SignedAngle(srcUp, dstUp, axis) * t;
                    rot = Quaternion.AngleAxis(angle, axis) * rot;
                }
                transform.SetPositionAndRotation(pos, rot);
            }
        }
    }
}
