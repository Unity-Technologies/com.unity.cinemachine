using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class ConstantOrbitalRotation : MonoBehaviour
{
    public CinemachineOrbitalFollow orbitalFollow;

    public float speed;

    // Update is called once per frame
    void Update()
    {
        orbitalFollow.HorizontalAxis.Value += Time.deltaTime * speed;
    }
}
