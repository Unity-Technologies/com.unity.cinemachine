using UnityEngine;

public class SimpleBullet : MonoBehaviour
{
    Vector3 m_Velocity;

    public void Fire(Vector3 direction, float speed) 
    {
        m_Velocity = direction.normalized * speed;
    }

    void Update()
    {
        transform.position += m_Velocity * Time.deltaTime;
    }
}
