using UnityEngine;

namespace Cinemachine.Examples
{
    public class CameraMagnetTargetController : MonoBehaviour
    {
        public CinemachineTargetGroup TargetGroup;

        const int k_PlayerIndex = 0;
        CameraMagnetProperty[] m_CameraMagnets;

        void Start()
        {
            m_CameraMagnets = GetComponentsInChildren<CameraMagnetProperty>();
        }

        void Update()
        {
            var targets = TargetGroup.Targets;
            for (int i = 1; i < targets.Count; ++i)
            {
                var target = targets[i];
                var cameraMagnet = m_CameraMagnets[i - 1];
                var distance = (targets[k_PlayerIndex].Object.position - target.Object.position).magnitude;
                target.Weight = distance < cameraMagnet.Proximity
                    ? target.Weight = cameraMagnet.MagnetStrength * (1 - distance / cameraMagnet.Proximity)
                    : 0;
            }
        }
    }
}
