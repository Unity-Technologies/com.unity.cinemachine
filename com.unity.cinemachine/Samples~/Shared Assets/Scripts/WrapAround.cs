using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Implements continuous motion by wrapping the position around a range.
    /// </summary>
    public class WrapAround : MonoBehaviour
    {
        public enum AxisSelection { XAxis = 0, YAxis = 1, ZAxis = 2 };
        public AxisSelection Axis = AxisSelection.ZAxis;
        public float MinRange;
        public float MaxRange;

        private void OnValidate()
        {
            MaxRange = Mathf.Max(MinRange, MaxRange);
        }

        void LateUpdate()
        {
            // Wrap the axis around the range
            var pos = transform.position;
            var newPos = pos;
            if (newPos[(int)Axis] < MinRange)
                newPos[(int)Axis] += MaxRange - MinRange;
            if (newPos[(int)Axis] > MaxRange)
                newPos[(int)Axis] += MinRange - MaxRange;

            var delta = newPos - pos;
            if (!delta.AlmostZero())
            {
                transform.position = newPos;

                // Handle objects driven by a Rigidbody.
                // We don't use Rigidbody.MovePosition() because it's a warp and we want to bypass interpolation.
                if (TryGetComponent<Rigidbody>(out var rb))
                    rb.position = newPos;

                // Notify any CinemachineCameras that are targeting this object
                CinemachineCore.OnTargetObjectWarped(transform, delta);
            }
        }
    }
}
