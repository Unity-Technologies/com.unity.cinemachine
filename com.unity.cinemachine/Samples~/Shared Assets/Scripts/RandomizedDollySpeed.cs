using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Examples
{
    public class RandomizedDollySpeed : SplineAutoDolly.ISplineAutoDolly
    {
        [MinMaxRangeSlider(0.1f, 10f)]
        [Tooltip("Minimum and maximum speed the cart can travel")]
        public Vector2 Speed = new(2f, 6f);
        
        [Tooltip("How quickly the cart can change speed")]
        public float Acceleration = 1;

        float m_Speed;
        float m_TargetSpeed;

        void SplineAutoDolly.ISplineAutoDolly.Validate() => Speed.y = Mathf.Max(Speed.y, Speed.x);
        void SplineAutoDolly.ISplineAutoDolly.Reset() => m_Speed = m_TargetSpeed = (Speed.x + Speed.y) / 2;
        bool SplineAutoDolly.ISplineAutoDolly.RequiresTrackingTarget => false;

        public float GetSplinePosition(
            MonoBehaviour sender, Transform target, 
            SplineContainer spline, float currentPosition, 
            PathIndexUnit positionUnits, float deltaTime)
        {
            if (Application.isPlaying && deltaTime > 0)
            {
                if (Mathf.Abs(m_Speed - m_TargetSpeed) < 0.01f)
                    m_TargetSpeed = Random.Range(Speed.x, Speed.y);
                if (m_Speed < m_TargetSpeed)
                    m_Speed = Mathf.Min(m_TargetSpeed, m_Speed + Acceleration * deltaTime);
                if (m_Speed > m_TargetSpeed)
                    m_Speed = Mathf.Max(m_TargetSpeed, m_Speed - Acceleration * deltaTime);

                return currentPosition + m_Speed * deltaTime;
            }
            return currentPosition;
        }
    }
}
