#if !CINEMACHINE_NO_CM2_SUPPORT && !UNITY_7000_0_OR_NEWER
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Empty tag for the CM3 project upgrader.  Object with this behaviour will not be upgraded.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    public class CinemachineDoNotUpgrade : MonoBehaviour {}
}
#endif
