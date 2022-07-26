using System;
using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Simple script that makes this transform look at a target.
    /// </summary>
    public class BossLookAt : MonoBehaviour
    {
        [Tooltip("Look at this transform")]
        public Transform Target;

        void LateUpdate()
        {
            transform.LookAt(Target);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
    }
}
