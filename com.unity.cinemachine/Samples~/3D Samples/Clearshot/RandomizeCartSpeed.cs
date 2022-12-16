using System;
using Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CinemachineSplineCart))]
public class RandomizeCartSpeed : MonoBehaviour
{
    [MinMaxRangeSlider(0.1f, 10f)]
    [Tooltip("Range for the FOV that this behaviour will generate.")]
    public Vector2 SpeedRange = new(2f, 5f);
    
    CinemachineSplineCart m_Cart;
    SplineAutoDolly.FixedSpeed m_FixedSpeed;
    void Start()
    {
        m_Cart = GetComponent<CinemachineSplineCart>();
        m_FixedSpeed = m_Cart.AutomaticDolly.Implementation as SplineAutoDolly.FixedSpeed;
    }

    bool m_Randomize = true;
    float m_TargetSpeed;
    const float k_SpeedEqualityTolerance = 0.1f;
    void Update()
    {
        if (m_Randomize)
        {
            m_TargetSpeed = Random.Range(SpeedRange.x, SpeedRange.y);
            m_Randomize = false;
        }

        if (Math.Abs(m_TargetSpeed - m_FixedSpeed.Speed) > k_SpeedEqualityTolerance)
            m_FixedSpeed.Speed = Mathf.Lerp(m_FixedSpeed.Speed, m_TargetSpeed, Time.deltaTime);
        else
            m_Randomize = true;
    }
}
