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
        public float Stretch = 4;

        float m_Speed;
        float m_ScaleZ;

        void OnValidate()
        {
            Speed = Mathf.Max(1, Speed);
            Lifespan = Mathf.Max(0.2f, Lifespan);
        }

        void Awake()
        {
            m_ScaleZ = transform.localScale.z;
        }

        void OnEnable()
        {
            m_Speed = Speed;
            SetStretch(Stretch);
            StartCoroutine(DeactivateAfter());
        }

        void Update()
        {
            if (m_Speed > 0)
            {
                var t = transform;
                if (Physics.Raycast(
                    t.position, t.forward, out var hitInfo, m_Speed * Time.deltaTime, CollisionLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    t.position = hitInfo.point;
                    m_Speed = 0;
                    SetStretch(1);
                }
                t.position += m_Speed * Time.deltaTime * t.forward;
            }
        }

        void SetStretch(float stretch)
        {
            var scale = transform.localScale;
            scale.z = m_ScaleZ * stretch;
            transform.localScale = scale;
        }

        IEnumerator DeactivateAfter()
        {
            yield return new WaitForSeconds(Lifespan);
            gameObject.SetActive(false);
        }
    }
}
