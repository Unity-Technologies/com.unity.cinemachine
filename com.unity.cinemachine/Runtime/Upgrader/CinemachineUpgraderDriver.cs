#if UNITY_EDITOR
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Temporary class for easy testing only.
    /// </summary>
    [ExecuteInEditMode]
    public class CinemachineUpgraderDriver : MonoBehaviour
    {
        public bool triggerUpgrade = false;
        public bool showHiddenGameObjects = true;
    }
}
#endif
