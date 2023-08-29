using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePixelPerfect))]
    [CanEditMultipleObjects]
    class CinemachinePixelPerfectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

#if CINEMACHINE_URP || CINEMACHINE_PIXEL_PERFECT_2_0_3
            this.AddMissingCmCameraHelpBox(ux);
#else
            ux.Add(new HelpBox("This component is only valid within URP projects", HelpBoxMessageType.Warning));
#endif
            return ux;
        }
    }
}
