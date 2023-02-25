using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePanTilt))]
    [CanEditMultipleObjects]
    class CinemachinePanTiltEditor : UnityEditor.Editor
    {
        CinemachinePanTilt Target => target as CinemachinePanTilt;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ReferenceFrame)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PanAxis)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TiltAxis)));

            ux.AddSpace();
            var noControllerHelp = ux.AddChild(InspectorUtility.HelpBoxWithButton(
                "PanTilt has no input axis controller behaviour.", HelpBoxMessageType.Info,
                "Add Input Controller", () =>
            {
                Undo.SetCurrentGroupName("Add Input Axis Controller");
                for (int i = 0; i < targets.Length; ++i)
                {
                    var t = targets[i] as CinemachinePanTilt;
                    if (!t.HasInputHandler && t.VirtualCamera != null)
                    {
                        if (!t.VirtualCamera.TryGetComponent<InputAxisController>(out var controller))
                            Undo.AddComponent<InputAxisController>(t.VirtualCamera.gameObject);
                        else if (!controller.enabled)
                        {
                            Undo.RecordObject(controller, "enable controller");
                            controller.enabled = true;
                        }
                    }
                }
            }));

            ux.TrackAnyUserActivity(() =>
            {
                if (target == null || noControllerHelp == null)
                    return;  // target was deleted

                bool noHandler = false;
                for (int i = 0; !noHandler && i < targets.Length; ++i)
                    noHandler |= !(targets[i] as CinemachinePanTilt).HasInputHandler;
                noControllerHelp?.SetVisible(noHandler);
            });

            return ux;
        }
    }
}
