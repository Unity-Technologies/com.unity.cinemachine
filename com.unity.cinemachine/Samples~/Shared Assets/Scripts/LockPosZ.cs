using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class LockPosZ : MonoBehaviour
    {
        public float ZPosiion;

        void LateUpdate()
        {
            var pos = transform.position;
            pos.z = ZPosiion;
            transform.position = pos;
        }
    }
}
