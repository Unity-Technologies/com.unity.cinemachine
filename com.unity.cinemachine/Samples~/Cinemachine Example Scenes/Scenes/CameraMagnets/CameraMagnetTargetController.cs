using UnityEngine;

namespace Cinemachine.Examples
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
            for (int i = 1; i < targetGroup.Targets.Count; ++i)
            {
                float distance = (targetGroup.Targets[playerIndex].Object.position -
                    targetGroup.Targets[i].Object.position).magnitude;
                if (distance < cameraMagnets[i - 1].Proximity)
                {
                    targetGroup.Targets[i].Weight = cameraMagnets[i - 1].MagnetStrength *
                        (1 - (distance / cameraMagnets[i - 1].Proximity));
                }
                else
                {
                    targetGroup.Targets[i].Weight = 0;
                }
            }
        }
    }
}