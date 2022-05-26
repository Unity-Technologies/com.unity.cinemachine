using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CmCamera))]
    [CanEditMultipleObjects]
    sealed class CmCameraEditor : UnityEditor.Editor 
    {
        CmCamera Target => target as CmCamera;
        CmCameraInspectorUtility m_CameraUtility = new CmCameraInspectorUtility();

        static bool s_AdvancedLensExpanded;

        [MenuItem("CONTEXT/CmCamera/Adopt Game View Camera Settings")]
        static void AdoptGameViewCameraSettings(MenuCommand command)
        {
            var cam = command.context as CmCamera;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(cam);
            if (brain != null)
            {
                cam.Lens = brain.CurrentCameraState.Lens;
                cam.transform.position = brain.transform.position;
                cam.transform.rotation = brain.transform.rotation;
            }
        }

        [MenuItem("CONTEXT/CmCamera/Adopt Scene View Camera Settings")]
        static void AdoptSceneViewCameraSettings(MenuCommand command)
        {
            var cam = command.context as CmCamera;
            cam.Lens = CinemachineMenu.MatchSceneViewCamera(cam.transform);
        }

        void OnEnable()
        {
            m_CameraUtility.OnEnable(targets);
            EditorApplication.update += m_CameraUtility.SortComponents;
            Undo.undoRedoPerformed += ResetTarget;

#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
#endif
        }

        void OnDisable()
        {
            EditorApplication.update -= m_CameraUtility.SortComponents;
            m_CameraUtility.OnDisable();
            Undo.undoRedoPerformed -= ResetTarget;
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
#endif
        }

        public override VisualElement CreateInspectorGUI()
        {
            var serializedTarget = new SerializedObject(Target);
            var ux = new VisualElement();

            m_CameraUtility.AddCameraStatus(ux);
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Priority)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_StandbyUpdate)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Transitions)));
            
            ux.AddHeader("Camera");
            var lensProperty = serializedTarget.FindProperty(() => Target.Lens);
            ux.Add(new PropertyField(lensProperty));

            var modeOverrideProperty = lensProperty.FindPropertyRelative("ModeOverride");
            var advanced = ux.AddChild(new Foldout() { text = "Advanced", value = s_AdvancedLensExpanded });
            advanced.RegisterValueChangedCallback((evt) => 
            {
                s_AdvancedLensExpanded = evt.newValue;
                evt.StopPropagation();
            });
            advanced.Add(new HelpBox("Setting a mode override here implies changes to the Camera component when "
                + "Cinemachine activates this CM Camera, and the changes will remain after the CM "
                + "Camera deactivation. If you set a mode override in any CM Camera, you should set "
                + "one in all CM Cameras.", HelpBoxMessageType.Info));
            advanced.Add(new PropertyField(modeOverrideProperty));

            ux.AddHeader("Procedural Motion");
            m_CameraUtility.AddSaveDuringPlayToggle(ux);
            m_CameraUtility.AddGameViewGuidesToggle(ux);
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Target)));
            m_CameraUtility.AddPipelineDropdowns(ux);

            ux.AddSpace();
            m_CameraUtility.AddExtensionsDropdown(ux);

            return ux;
        }

#if UNITY_2021_2_OR_NEWER
        void OnSceneGUI()
        {
            var cmCam = Target;
            if (cmCam == null)
                return;

            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FoVTool)))
            {
                CinemachineSceneToolHelpers.FovToolHandle(cmCam, 
                    new SerializedObject(cmCam).FindProperty(() => cmCam.Lens), 
                    cmCam.Lens, Target.Lens.UseHorizontalFOV);
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(cmCam,
                    new SerializedObject(cmCam).FindProperty(() => cmCam.Lens));
            }
            Handles.color = originalColor;
        }
#endif
    }
}
