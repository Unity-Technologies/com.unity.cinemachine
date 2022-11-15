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
            var targets = TargetGroup.Targets;
            for (int i = 1; i < targets.Count; ++i)
            {
                var distance = (targets[m_PlayerIndex].Object.position - targets[i].Object.position).magnitude;
                if (distance < m_CameraMagnets[i - 1].Proximity)
                    targets[i].Weight =
                        m_CameraMagnets[i - 1].MagnetStrength * (1 - (distance / m_CameraMagnets[i - 1].Proximity));
                else
                    targets[i].Weight = 0;
            }
        }
    }
}
