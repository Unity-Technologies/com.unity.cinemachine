#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(Cinemachine3rdPersonFollow))]
    [CanEditMultipleObjects]
    class Cinemachine3rdPersonFollowEditor : BaseEditor<Cinemachine3rdPersonFollow>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            this.IMGUI_DrawMissingCmCameraHelpBox();
            DrawRemainingPropertiesInInspector();
        }
    }
}
#endif
