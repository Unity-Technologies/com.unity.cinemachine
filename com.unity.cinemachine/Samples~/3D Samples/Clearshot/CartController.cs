using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CinemachineSplineCart))]
public class CartController : MonoBehaviour
{
    [MinMaxRangeSlider(0.1f, 10f)]
    [Tooltip("Possible range of speed the cart will randomly take")]
    public Vector2 SpeedRange = new(2f, 5f);
    
    CinemachineSplineCart m_Cart;
    SplineAutoDolly.FixedSpeed m_FixedSpeed;
    void Start()
    {
        m_Cart = GetComponent<CinemachineSplineCart>();
        m_FixedSpeed = m_Cart.AutomaticDolly.Implementation as SplineAutoDolly.FixedSpeed;
    }

    static bool s_SlowDownLeader = true;
    bool m_Randomize = true;
    float m_TargetSpeed;
    const float k_SpeedEqualityTolerance = 0.1f;
    void Update()
    {
        // Slow down leader to improve chances of at least 1 take over in one run
        if (s_SlowDownLeader)
        {
            var normalizedPos = m_Cart.Spline.Spline.ConvertIndexUnit(
                m_Cart.SplinePosition, m_Cart.PositionUnits, PathIndexUnit.Normalized);
            if (normalizedPos > 0.5f)
            {
                SpeedRange.y /= 2f;
                s_SlowDownLeader = false;
            }
        }
        
        // Randomly select a speed for the player
        if (m_Randomize)
        {
            m_TargetSpeed = Random.Range(SpeedRange.x, SpeedRange.y);
            if (m_Cart.PositionUnits != PathIndexUnit.Distance && m_Cart.Spline != null)
            {
                m_TargetSpeed =
                    m_Cart.Spline.Spline.ConvertIndexUnit(m_TargetSpeed, m_Cart.PositionUnits, PathIndexUnit.Distance);
            }

            m_Randomize = false;
        }

        // Lerp to selected speed or request a new speed
        if (Math.Abs(m_TargetSpeed - m_FixedSpeed.Speed) > k_SpeedEqualityTolerance)
            m_FixedSpeed.Speed = Mathf.Lerp(m_FixedSpeed.Speed, m_TargetSpeed, Time.deltaTime);
        else
            m_Randomize = true;
    }
    
    /// <summary>Reset position back to 0.</summary>
    public void ResetPosition()
    {
        m_Cart.SplinePosition = 0;
    }
}
