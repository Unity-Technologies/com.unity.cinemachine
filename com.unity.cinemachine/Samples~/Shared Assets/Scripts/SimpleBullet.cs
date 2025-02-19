using System.Collections;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class SimpleBullet : MonoBehaviour
    {
        public LayerMask CollisionLayers = 1;
        public float Speed = 500;
        public float Lifespan = 3;

        [Tooltip("Stretch factor in the direction of motion while flying")]
        public float Stretch = 6;

        float m_Speed;
        Vector3 m_SpawnPoint;

        void OnValidate()
        {
            Speed = Mathf.Max(1, Speed);
            Lifespan = Mathf.Max(0.2f, Lifespan);
        }

        void OnEnable()
        {
            m_Speed = Speed;
            m_SpawnPoint = transform.position;
            SetStretch(1);
            StartCoroutine(DeactivateAfter());
        }

        void Update()
        {
            if (m_Speed > 0)
            {
                var t = transform;
                if (UnityEngine.Physics.Raycast(
                    t.position, t.forward, out var hitInfo, m_Speed * Time.deltaTime, CollisionLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    t.position = hitInfo.point;
                    m_Speed = 0;
                    SetStretch(1);
                }
                var deltaPos = m_Speed * Time.deltaTime;
                t.position += deltaPos * t.forward;

                // Clamp the stretch to avoid the bullet stretching back past the spawn point.
                // This code assumes that the bullet length is 1.
                SetStretch(Mathf.Min(1 + deltaPos * Stretch, Vector3.Distance(t.position, m_SpawnPoint)));
            }
        }

        void SetStretch(float stretch)
        {
            var scale = transform.localScale;
            scale.z = stretch;
            transform.localScale = scale;
        }

        IEnumerator DeactivateAfter()
        {
            yield return new WaitForSeconds(Lifespan);
            gameObject.SetActive(false);
        }
    }
}
