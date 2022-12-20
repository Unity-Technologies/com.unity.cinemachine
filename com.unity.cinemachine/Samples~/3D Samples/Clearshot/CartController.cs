using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Examples
{
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
        float m_StartSpeed, m_TargetSpeed;
        float m_LerpTotalTime; // Lerp will take this many seconds
        float m_LerpTime; // Lerp time accumulator

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

            // Randomly select a new target speed for the player
            if (m_Randomize)
            {
                m_LerpTime = 0f;
                m_StartSpeed = m_FixedSpeed.Speed;
                m_TargetSpeed = UnityEngine.Random.Range(SpeedRange.x, SpeedRange.y);
                if (m_Cart.PositionUnits != PathIndexUnit.Distance && m_Cart.Spline != null)
                {
                    m_TargetSpeed =
                        m_Cart.Spline.Spline.ConvertIndexUnit(m_TargetSpeed, m_Cart.PositionUnits, PathIndexUnit.Distance);
                }

                m_LerpTotalTime = Mathf.Abs(m_TargetSpeed - m_StartSpeed); // lerp time based on speed difference
                m_Randomize = false;
            }

            // Calculate lerp time t
            m_LerpTime += Time.deltaTime;
            if (m_LerpTime > m_LerpTotalTime)
            {
                m_LerpTime = m_LerpTotalTime;
                m_Randomize = true;
            }

            var t = m_LerpTime / m_LerpTotalTime; // percentage [0,1]
            t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); // ease out, just so it is not linear

            // Lerp to selected speed
            m_FixedSpeed.Speed = Mathf.Lerp(m_StartSpeed, m_TargetSpeed, t);
        }

        /// <summary>Reset position back to 0.</summary>
        public void ResetPosition()
        {
            s_SlowDownLeader = true;
            m_Cart.SplinePosition = 0;
        }
    }
}
