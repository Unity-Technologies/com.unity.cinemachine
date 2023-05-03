#if !CINEMACHINE_NO_CM2_SUPPORT
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
