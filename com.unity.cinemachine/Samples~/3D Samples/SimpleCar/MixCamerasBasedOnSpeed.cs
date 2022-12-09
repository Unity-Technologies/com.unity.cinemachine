using System;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineMixingCamera))]
public class MixCamerasBasedOnSpeed : MonoBehaviour
{
    public float MaxSpeed;
    public Rigidbody RigidBodyOfCar;
    CinemachineMixingCamera m_Mixer;
    void Start()
    {
        m_Mixer = GetComponent<CinemachineMixingCamera>();
    }

    void OnValidate()
    {
        MaxSpeed = Mathf.Max(1, MaxSpeed);
    }

    void Update()
    {
        if (RigidBodyOfCar == null)
            return;
        
        var t = Mathf.Clamp01(RigidBodyOfCar.velocity.magnitude / MaxSpeed);
        m_Mixer.Weight0 = 1 - t;
        m_Mixer.Weight1 = t;
    }
}
