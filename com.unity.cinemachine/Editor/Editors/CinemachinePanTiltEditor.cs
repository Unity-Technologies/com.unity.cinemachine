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

        CmPipelineComponentInspectorUtility m_PipelineUtility;
        VisualElement m_NoControllerHelp;

        void OnEnable()
        {
            m_PipelineUtility = new (this);
            EditorApplication.update += UpdateHelpBox;
        }
        void OnDisable()
        {
            m_PipelineUtility.OnDisable();
            EditorApplication.update -= UpdateHelpBox;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);
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
                    if (!t.HasInputHandler && t.VirtualCamera != null)
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

            m_PipelineUtility.UpdateState();
            UpdateHelpBox();
            return ux;
        }

        void UpdateHelpBox()
        {
            if (target == null || m_NoControllerHelp == null)
                return;  // target was deleted
            bool noHandler = false;
            for (int i = 0; !noHandler && i < targets.Length; ++i)
                noHandler |= !(targets[i] as CinemachinePanTilt).HasInputHandler;
            if (m_NoControllerHelp != null)
                m_NoControllerHelp.SetVisible(noHandler);
        }
    }
}
