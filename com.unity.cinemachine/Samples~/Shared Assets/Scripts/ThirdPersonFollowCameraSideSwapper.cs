using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class ThirdPersonFollowCameraSideSwapper : MonoBehaviour
    {
        [Tooltip("How long the shoulder swap will take")]
        public float Damping;
        
        List<CinemachineThirdPersonFollow> m_ThirdPersonFollows = new();
        float m_SwapDirection;

        void OnEnable()
        {
            GetComponentsInChildren(true, m_ThirdPersonFollows);
        }

        void Update()
        {
            bool allDone = true;
            if (m_SwapDirection != 0)
            {
                float swapTarget = m_SwapDirection > 0 ? 1 : 0;
                for (int i = 0; i < m_ThirdPersonFollows.Count; ++i)
                {
                    m_ThirdPersonFollows[i].CameraSide +=
                        Damper.Damp(swapTarget - m_ThirdPersonFollows[i].CameraSide, Damping, Time.deltaTime);
                    if (Mathf.Abs(m_ThirdPersonFollows[i].CameraSide - swapTarget) > UnityVectorExtensions.Epsilon)
                        allDone = false;
                }
            }
            if (allDone)
                m_SwapDirection = 0;
        }

        public void Swap()
        {
            m_SwapDirection *= -1;
            for (int i = 0; m_SwapDirection == 0 && i < m_ThirdPersonFollows.Count; ++i)
                m_SwapDirection = m_ThirdPersonFollows[i].CameraSide > 0.5f ? -1 : 1;
        }
    }
}
