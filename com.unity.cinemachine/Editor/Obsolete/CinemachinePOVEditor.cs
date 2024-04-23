#if !CINEMACHINE_NO_CM2_SUPPORT && !UNITY_7000_0_OR_NEWER
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachinePOV))]
    [CanEditMultipleObjects]
    class CinemachinePOVEditor : BaseEditor<CinemachinePOV>
    {
        private void OnEnable()
        {
            for (int i = 0; i < targets.Length; ++i)
                (targets[i] as CinemachinePOV).UpdateInputAxisProvider();
        }
    }
}
#endif
