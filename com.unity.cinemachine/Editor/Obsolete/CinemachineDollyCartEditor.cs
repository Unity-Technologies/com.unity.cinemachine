#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineDollyCart))]
    [CanEditMultipleObjects]
    class CinemachineDollyCartEditor : BaseEditor<CinemachineDollyCart>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            UpgradeManagerInspectorHelpers.IMGUI_DrawUpgradeControls(this, "CinemachineSplineCart");
            DrawRemainingPropertiesInInspector();
        }
    }
}
#endif
