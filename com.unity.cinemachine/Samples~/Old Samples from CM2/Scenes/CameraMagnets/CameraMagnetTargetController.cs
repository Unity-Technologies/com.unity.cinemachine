using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class CameraMagnetTargetController : MonoBehaviour
    {
        public CinemachineTargetGroup targetGroup;

        private int playerIndex;
        private CameraMagnetProperty[] cameraMagnets;

        // Start is called before the first frame update
        void Start()
        {
            cameraMagnets = GetComponentsInChildren<CameraMagnetProperty>();
            playerIndex = 0;
        }

        // Update is called once per frame
        void Update()
        {
            for (int i = 1; i < targetGroup.m_Targets.Length; ++i)
            {
                float distance = (targetGroup.m_Targets[playerIndex].Object.position -
                    targetGroup.m_Targets[i].Object.position).magnitude;
                if (distance < cameraMagnets[i - 1].Proximity)
                {
                    targetGroup.m_Targets[i].Weight = cameraMagnets[i - 1].MagnetStrength *
                        (1 - (distance / cameraMagnets[i - 1].Proximity));
                }
                else
                {
                    targetGroup.m_Targets[i].Weight = 0;
                }
            }
        }
    }
}