using System;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class ThirdPersonFollowCameraSideSwapper : CinemachineExtension
    {
        [Tooltip("The rate of damping.  " +
            "This is the time it would take to reduce the original amount to a negligible percentage.")]
        public float Damping;
        
        CinemachineThirdPersonFollow[] m_ThirdPersonFollows;
        float[] m_CameraOppositeSide;

        protected override void OnEnable()
        {
            m_ThirdPersonFollows = GetComponentsInChildren<CinemachineThirdPersonFollow>(true);
            m_CameraOppositeSide = new float[m_ThirdPersonFollows.Length];
            for (var i = 0; i < m_ThirdPersonFollows.Length; i++)
                m_CameraOppositeSide[i] = m_ThirdPersonFollows[i].CameraSide;
        }

        bool m_StartSwap;
        bool m_AllDone;
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (m_StartSwap)
                {
                    for (var i = 0; i < m_ThirdPersonFollows.Length; i++)
                        m_CameraOppositeSide[i] = Mathf.Abs(m_CameraOppositeSide[i] - 1); // change direction of swap
                    
                    m_StartSwap = false;
                    m_AllDone = false;
                }
                if (m_AllDone)
                    return;
                
                m_AllDone = true;
                var dt = Time.deltaTime;
                for (var i = 0; i < m_ThirdPersonFollows.Length; i++)
                {
                    m_ThirdPersonFollows[i].CameraSide +=
                        Damper.Damp(m_CameraOppositeSide[i] - m_ThirdPersonFollows[i].CameraSide, Damping, dt);
                
                    if (Math.Abs(m_ThirdPersonFollows[i].CameraSide - m_CameraOppositeSide[i]) > UnityVectorExtensions.Epsilon)
                        m_AllDone = false;
                }
            }
        }

        public void Swap() => m_StartSwap = true;
    }
}
