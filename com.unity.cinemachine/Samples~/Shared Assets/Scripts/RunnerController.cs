using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine.Samples
{
    [RequireComponent(typeof(CinemachineSplineCart))]
    public class RunnerController : MonoBehaviour
    {
        public float WaitTimeAtStart = 1;

        CinemachineSplineCart m_Cart;
        static bool s_LeaderWasSlowed;
        bool m_IsTired;
        float m_StartTime;

        void OnEnable()
        {
            m_Cart = GetComponent<CinemachineSplineCart>();
            ResetRace();
        }

        void Update()
        {
            m_Cart.AutomaticDolly.Enabled = Time.time > m_StartTime + WaitTimeAtStart;

            // Slow down leader to improve chances of at least 1 takeover per run
            if (!s_LeaderWasSlowed)
            {
                if (m_Cart.Spline.Spline.ConvertIndexUnit(
                    m_Cart.SplinePosition, m_Cart.PositionUnits, PathIndexUnit.Normalized) > 0.5f)
                {
                    s_LeaderWasSlowed = true;
                    if (m_Cart.AutomaticDolly.Method is RandomizedDollySpeed speedControl)
                    {
                        // Leader is tired!
                        speedControl.MinSpeed /= 2;
                        speedControl.MaxSpeed /= 2;
                        m_IsTired = true;
                    }
                }
            }
        }

        // This is called by the "Restart Race" UX button.
        public void ResetRace()
        {
            // Reset position to start
            m_Cart.SplinePosition = 0;
            m_StartTime = Time.time;
            m_Cart.AutomaticDolly.Enabled = false;

            // Reset slowdown mechanism
            if (m_Cart.AutomaticDolly.Method is RandomizedDollySpeed speedControl)
            {
                m_Cart.AutomaticDolly.Method.Reset();
                if (m_IsTired)
                {
                    // Restore the speed, leader is no longer tired
                    speedControl.MinSpeed *= 2;
                    speedControl.MaxSpeed *= 2;
                }
            }
            m_IsTired = false;
            s_LeaderWasSlowed = false;
        }
    }
}
