using UnityEngine;

public class SimpleBullet : MonoBehaviour
{
    public LayerMask CollisionLayers = 1;

    Vector3 m_Direction;
    float m_Speed;

    public void Fire(Vector3 direction, float speed) 
    {
        m_Direction = direction.normalized;
        m_Speed = speed;
    }

    void Update()
    {
        var t = transform;
        if (Physics.Raycast(
            t.position, m_Direction, out var hitInfo, m_Speed * Time.deltaTime, CollisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            t.position = hitInfo.point;
            m_Speed = 0;
        }
        t.position += m_Direction * m_Speed * Time.deltaTime;
    }
}
