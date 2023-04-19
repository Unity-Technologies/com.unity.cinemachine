using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class ConstantOrbitalRotation : MonoBehaviour
    {
        public CinemachineOrbitalFollow orbitalFollow;

        public float Speed;
        
        void Update()
        {
            orbitalFollow.HorizontalAxis.Value += Time.deltaTime * Speed;
        }
    }
}
