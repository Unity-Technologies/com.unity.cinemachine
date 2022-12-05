using UnityEngine;

namespace Cinemachine.Examples
{
    [ExecuteAlways]
    public class CameraMagnetProperty : MonoBehaviour
    {
        [Range(0.1f, 50.0f)]
        public float MagnetStrength = 5.0f;

        [Range(0.1f, 50.0f)]
        public float Proximity = 5.0f;

        public Transform ProximityVisualization;
        
        void Update()
        {
            if (ProximityVisualization != null)
                ProximityVisualization.localScale = new Vector3(Proximity * 2.0f, Proximity * 2.0f, 1);
        }
    }
}
