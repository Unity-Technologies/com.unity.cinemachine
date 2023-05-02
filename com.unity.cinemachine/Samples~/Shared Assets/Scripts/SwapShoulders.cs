using System;
using System.Collections;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class SwapShoulders : MonoBehaviour
    {
        CinemachineThirdPersonFollow[] m_ThirdPersonFollows;
        public void OnEnable()
        {
            m_ThirdPersonFollows = GetComponentsInChildren<CinemachineThirdPersonFollow>(true);
        }

        Coroutine m_CurrentSwap;
        IEnumerator LerpedSwap()
        {
            bool allDone = false;
            do
            {
                var dt = Time.deltaTime;
                for (var i = 0; i < m_ThirdPersonFollows.Length; i++)
                {
                    m_ThirdPersonFollows[i].CameraSide =
                        Damper.Damp(m_ThirdPersonFollows[i].CameraSide, 0.5f, dt);
                }

                yield return null;
            } while (!allDone);
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
