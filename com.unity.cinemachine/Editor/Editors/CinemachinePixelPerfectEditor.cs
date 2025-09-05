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

            var infoBox = ux.AddChild(new HelpBox(
                "This component is driving the Pixel Perfect Camera component on the Unity Camera.",
                HelpBoxMessageType.Info));
            var helpBox = ux.AddChild(new HelpBox(
                "This component requires an active Pixel Perfect Camera component on the Unity Camera.",
                HelpBoxMessageType.Warning));

            ux.TrackAnyUserActivity(() => 
            {
                var pp = target as CinemachinePixelPerfect;
                bool isValid = pp.HasValidPixelPerfectCamera();
                infoBox.SetVisible(isValid && pp.enabled);
                helpBox.SetVisible(!isValid);
            });
#else
            ux.Add(new HelpBox("This component is only valid within URP projects", HelpBoxMessageType.Warning));
#endif
            return ux;
        }
    }
}
