using UnityEngine;

namespace Cinemachine.Examples
{
    public class CameraMagnetTargetController : MonoBehaviour
    {
        public CinemachineTargetGroup TargetGroup;

        int m_PlayerIndex;
        CameraMagnetProperty[] m_CameraMagnets;

        void Start()
        {
            m_CameraMagnets = GetComponentsInChildren<CameraMagnetProperty>();
            m_PlayerIndex = 0;
        }

        void Update()
        {
            for (int i = 1; i < TargetGroup.Targets.Count; ++i)
            {
                float distance = (TargetGroup.Targets[m_PlayerIndex].Object.position -
                    TargetGroup.Targets[i].Object.position).magnitude;
                if (distance < m_CameraMagnets[i - 1].Proximity)
                {
                    TargetGroup.Targets[i].Weight = m_CameraMagnets[i - 1].MagnetStrength *
                        (1 - (distance / m_CameraMagnets[i - 1].Proximity));
                }
                else
                {
                    TargetGroup.Targets[i].Weight = 0;
                }
            }
        }
    }
}