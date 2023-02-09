using System.Collections;
using UnityEngine;

namespace Cinemachine.Examples
{
    public class SimpleBullet : MonoBehaviour
    {
        public LayerMask CollisionLayers = 1;
        public float Speed = 500;
        public float Lifespan = 3;

        float m_Speed;

        void OnValidate()
        {
            Speed = Mathf.Max(1, Speed);
            Lifespan = Mathf.Max(0.2f, Lifespan);
        }

        public void OnEnable()
        {
            m_Speed = Speed;
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
                }
                t.position += m_Speed * Time.deltaTime * t.forward;
            }
        }
        
        IEnumerator DeactivateAfter()
        {
            yield return new WaitForSeconds(Lifespan);
            gameObject.SetActive(false);
        }
    }
}
