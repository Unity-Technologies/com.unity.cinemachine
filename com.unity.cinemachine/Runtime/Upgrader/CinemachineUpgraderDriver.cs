#if UNITY_EDITOR
using UnityEngine;

namespace Cinemachine.Upgrader
{
    [ExecuteInEditMode]
    public class CinemachineUpgraderDriver : MonoBehaviour
    {
        public bool triggerUpgrade = false;
        public bool showHiddenGameObjects = true;
        CinemachineUpgrader m_Upgrader;

        void Update()
        {
            CinemachineCore.sShowHiddenObjects = showHiddenGameObjects;
            if (triggerUpgrade)
            {
                triggerUpgrade = false;
                m_Upgrader = new CinemachineUpgrader();
                m_Upgrader.UpgradeAll();
            }
        }
    }
}
#endif
