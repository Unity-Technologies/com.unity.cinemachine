using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Add-on for ThirdPersonController that keeps the player upright on the sphere surface
    /// </summary>
    public class PlayerOnSphere : MonoBehaviour
    {
        public Transform Sphere;

        void LateUpdate()
        {
            if (Sphere != null)
            {
                var up = (transform.position - Sphere.transform.position).normalized;
                var fwd = transform.forward.ProjectOntoPlane(up);
                transform.rotation = Quaternion.LookRotation(fwd, up);    
            }
        }
    }
}
