using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    [ExecuteInEditMode]
    public class Magnet : MonoBehaviour
    {
        [Range(0.1f, 50.0f)]
        public float Strength = 5.0f;

        [Range(0.1f, 50.0f)]
        public float Range = 5.0f;

        public Transform RangeVisualizer;

        void Update()
        {
            if (RangeVisualizer != null)
                RangeVisualizer.localScale = new Vector3(Range * 2.0f, Range * 2.0f, 1);
        }
    }
}
