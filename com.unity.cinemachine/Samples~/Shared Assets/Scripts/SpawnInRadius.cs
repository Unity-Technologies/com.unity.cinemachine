using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class SpawnInRadius : MonoBehaviour
    {
        public GameObject Prefab;
        public float Radius = 40;
        public float Amount = 200;
        public bool DoIt;

        void Update()
        {
            if (DoIt && Prefab != null)
            {
                var spawner = transform;
                for (int i = 0; i < Amount; ++i)
                {
                    var a = Random.Range(0, 360);
                    var pos = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    pos = spawner.position + pos * (Mathf.Sqrt(Random.Range(0.0f, 1.0f)) * Radius);
                    Instantiate(Prefab, pos, spawner.rotation, spawner.parent);
                }
            }

            DoIt = false;
        }
    }
}