using Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Animator))]
public class AnimateBasedOnCartSpeed : MonoBehaviour
{
    public CinemachineSplineCart SplineCart;
    Spline m_Spline;
    Animator m_Animator;
    void Start()
    {
        TryGetComponent(out m_Animator);
        if (SplineCart.Spline != null && SplineCart.Spline.Spline != null)
        {
            m_Spline = SplineCart.Spline.Spline;
            m_PreviousPosition = m_Spline.ConvertIndexUnit(
                SplineCart.SplinePosition, SplineCart.PositionUnits, PathIndexUnit.Distance);
        }
    }

    float m_PreviousPosition;
    void Update()
    {
        if (m_Animator != null)
        {
            if (SplineCart.AutomaticDolly.Implementation is SplineAutoDolly.FixedSpeed fixedSpeed)
            {
                m_Animator.SetFloat("SpeedZ", fixedSpeed.Speed);
            }
            else if (m_Spline != null)
            {
                var position = m_Spline.ConvertIndexUnit(
                    SplineCart.SplinePosition, SplineCart.PositionUnits, PathIndexUnit.Distance);
                var delta = position - m_PreviousPosition;
                m_PreviousPosition = position;
                
                m_Animator.SetFloat("SpeedZ", delta / Time.deltaTime);
            }
        }
    }
}
