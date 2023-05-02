using System;
using System.Collections;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class SwapShoulders : MonoBehaviour
    {
        CinemachineThirdPersonFollow[] m_ThirdPersonFollows;
        float[] m_CameraOppositeSide;
        public void OnEnable()
        {
            m_ThirdPersonFollows = GetComponentsInChildren<CinemachineThirdPersonFollow>(true);
            m_CameraOppositeSide = new float[m_ThirdPersonFollows.Length];
        }

        Coroutine m_CurrentSwap;
        IEnumerator LerpedSwap()
        {
            for (var i = 0; i < m_ThirdPersonFollows.Length; i++) 
                m_CameraOppositeSide[i] = Mathf.Abs(m_ThirdPersonFollows[i].CameraSide - 1) < 0.5f ? 0 : 1;

            var allDone = false;
            while (!allDone)
            {
                allDone = true;
                var dt = Time.deltaTime;
                for (var i = 0; i < m_ThirdPersonFollows.Length; i++)
                {
                    m_ThirdPersonFollows[i].CameraSide +=
                        Damper.Damp(m_CameraOppositeSide[i] - m_ThirdPersonFollows[i].CameraSide, 0.5f, dt);
                    
                    if (Math.Abs(m_ThirdPersonFollows[i].CameraSide - m_CameraOppositeSide[i]) > UnityVectorExtensions.Epsilon)
                        allDone = false;
                }
                yield return null;
            }
        }

        public void Swap()
        {
            if (m_CurrentSwap != null)
            {
                StopCoroutine(m_CurrentSwap);
                m_CurrentSwap = null;
            }
            m_CurrentSwap = StartCoroutine(LerpedSwap());
        }
    }
}
