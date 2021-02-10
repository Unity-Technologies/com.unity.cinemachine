using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    public float minY, maxY;
    public float speed;
    public bool on;

    float direction = 1;
    void FixedUpdate()
    {
        if (transform.position.y < minY)
        {
            direction = 1f;
        }

        if (transform.position.y > maxY)
        {
            direction = -1f;
        }
        
        var dir = new Vector3(0, direction * speed * Time.fixedDeltaTime, 0);
        transform.position += dir;
    }
}
