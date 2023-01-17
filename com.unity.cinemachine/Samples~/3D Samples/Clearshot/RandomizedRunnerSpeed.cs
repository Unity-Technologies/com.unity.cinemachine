using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Examples
{
    public class RandomizedRunnerSpeed : SplineAutoDolly.ISplineAutoDolly
    {
        [MinMaxRangeSlider(0.1f, 10f)]
        [Tooltip("Possible range of speed the cart will randomly take")]
        public Vector2 SpeedRange = new(2f, 5f);
        
        static bool s_SlowDownLeader = true;
        bool m_Randomize = true;
        float m_Speed = 0f;
        float m_StartSpeed, m_TargetSpeed;
        float m_LerpTotalTime; // Lerp will take this many seconds
        float m_LerpTime; // Lerp time accumulator


        public void Validate()
        {
            SpeedRange.x = Mathf.Max(0.1f, SpeedRange.x);
            SpeedRange.y = Mathf.Min(10f, SpeedRange.y);
        }

        public float GetSplinePosition(MonoBehaviour sender, Transform target, SplineContainer spline, float currentPosition, PathIndexUnit positionUnits, float deltaTime)
        {
            // Only works if playing
            if (Application.isPlaying && (spline != null && spline.Spline != null) && deltaTime > 0)
            {
                m_Speed = CalculateRandomizedSpeed(spline, currentPosition, positionUnits);
                return currentPosition + m_Speed * deltaTime;
            }

            return currentPosition;
        }

        float CalculateRandomizedSpeed(SplineContainer spline, float currentPosition, PathIndexUnit positionUnits)
        {
            // Slow down leader (first one to reach half point) to improve chances of at least 1 take over in one run
            if (s_SlowDownLeader)
            {
                var normalizedPos = spline.Spline.ConvertIndexUnit(currentPosition, positionUnits, PathIndexUnit.Normalized);
                if (normalizedPos > 0.5f)
                {
                    SpeedRange.y /= 2f;
                    s_SlowDownLeader = false;
                    m_Randomize = true;
                }
            }

            // Randomly select a new target speed for the player
            if (m_Randomize)
            {
                m_LerpTime = 0f;
                m_StartSpeed = m_Speed;
                m_TargetSpeed = UnityEngine.Random.Range(SpeedRange.x, SpeedRange.y);
                if (positionUnits != PathIndexUnit.Distance) 
                    m_TargetSpeed = spline.Spline.ConvertIndexUnit(m_TargetSpeed, positionUnits, PathIndexUnit.Distance);

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

            return Mathf.Lerp(m_StartSpeed, m_TargetSpeed, t);
        }
    }
}
