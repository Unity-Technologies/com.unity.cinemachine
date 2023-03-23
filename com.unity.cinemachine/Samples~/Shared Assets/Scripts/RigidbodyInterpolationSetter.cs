using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Helper class to set the interpolation of a rigidbody.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyInterpolationSetter : MonoBehaviour
    {
        Rigidbody m_Rigidbody; 
        void Start() => m_Rigidbody = GetComponent<Rigidbody>();

        public void SetInterpolation(bool on) => 
            m_Rigidbody.interpolation = on ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
    }
}
