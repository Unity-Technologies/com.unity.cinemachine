using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Examples
{
    [RequireComponent(typeof(CinemachineSplineCart))]
    public class RunnerController : MonoBehaviour
    {
        CinemachineSplineCart m_Cart;

        void Start() => m_Cart = GetComponent<CinemachineSplineCart>();

        static bool s_LeaderWasSlowed;
        bool m_IsTired;

        void Update()
        {
            // Slow down leader to improve chances of at least 1 take over in one run
            if (!s_LeaderWasSlowed)
            {
                if (m_Cart.Spline.Spline.ConvertIndexUnit(
                    m_Cart.SplinePosition, m_Cart.PositionUnits, PathIndexUnit.Normalized) > 0.5f)
                {
                    s_LeaderWasSlowed = true;
                    if (m_Cart.AutomaticDolly.Implementation is RandomizedDollySpeed speedControl)
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
            m_Cart.SplinePosition = 0;
            if (m_Cart.AutomaticDolly.Implementation is RandomizedDollySpeed speedControl)
            {
                m_Cart.AutomaticDolly.Implementation.Reset();
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
