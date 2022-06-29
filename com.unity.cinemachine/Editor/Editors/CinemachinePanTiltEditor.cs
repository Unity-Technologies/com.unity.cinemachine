using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePanTilt))]
    [CanEditMultipleObjects]
    internal sealed class CinemachinePanTiltEditor : UnityEditor.Editor
    {
        CinemachinePanTilt Target => target as CinemachinePanTilt;

        VisualElement m_NoControllerHelp;

        void OnEnable() => EditorApplication.update += UpdateHelpBox;
        void OnDisable() => EditorApplication.update -= UpdateHelpBox;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ReferenceFrame)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PanAxis)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TiltAxis)));

            ux.AddSpace();
            m_NoControllerHelp = ux.AddChild(InspectorUtility.CreateHelpBoxWithButton(
                "PanTilt has no input axis controller behaviour.", HelpBoxMessageType.Info,
                "Add Input Controller", () =>
            {
                Undo.SetCurrentGroupName("Add Input Axis Controller");
                for (int i = 0; i < targets.Length; ++i)
                {
                    var t = targets[i] as CinemachinePanTilt;
                    if (!t.HasInputHandler)
                    {
                        var controller = t.VirtualCamera.GetComponent<InputAxisController>();
                        if (controller == null)
                            Undo.AddComponent<InputAxisController>(t.VirtualCamera.gameObject);
                        else if (!controller.enabled)
                        {
                            Undo.RecordObject(controller, "enable controller");
                            controller.enabled = true;
                        }
                    }
                }
            }));

            UpdateHelpBox();
            return ux;
        }

        void UpdateHelpBox()
        {
            if (target == null)
                return;  // target was deleted
            bool noHandler = false;
            for (int i = 0; !noHandler && i < targets.Length; ++i)
                noHandler |= !(targets[i] as CinemachinePanTilt).HasInputHandler;
            if (m_NoControllerHelp != null)
                m_NoControllerHelp.SetVisible(noHandler);
        }
    }
}
