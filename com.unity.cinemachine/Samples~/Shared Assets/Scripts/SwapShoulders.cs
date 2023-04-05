using Unity.Cinemachine;
using UnityEngine;

public class SwapShoulders : MonoBehaviour
{
    CinemachineThirdPersonFollow[] m_ThirdPersonFollows;
    void Start()
    {
        m_ThirdPersonFollows = GetComponentsInChildren<CinemachineThirdPersonFollow>(true);
    }

    public bool SwapAll = false;
    void Update()
    {
        if (SwapAll)
        {
            SwapAll = false;
            Swap();
        }
    }

    public void Swap()
    {
        for (var i = 0; i < m_ThirdPersonFollows.Length; i++)
            m_ThirdPersonFollows[i].CameraSide = Mathf.Abs(m_ThirdPersonFollows[i].CameraSide - 1);
    }
}
