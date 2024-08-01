#if UNITY_EDITOR && CINEMACHINE_UIELEMENTS
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// This component is added to the brain game object to display debug information in the game view.
/// It is a separate component in order to avoid having OnGUI in the CinemachineBrain, which allocates garbage.
/// </summary>
[AddComponentMenu("")] // Hide in menu
[ExecuteAlways]
class CinemachineDebugDisplay : MonoBehaviour
{
    void OnGUI()
    {
        if (CinemachineDebug.OnGUIHandlers != null && Event.current.type != EventType.Layout)
            CinemachineDebug.OnGUIHandlers();
    }
}
#endif
