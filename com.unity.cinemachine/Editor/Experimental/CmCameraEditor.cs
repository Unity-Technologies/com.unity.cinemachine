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

        [MenuItem("CONTEXT/CmCamera/Adopt Game View Camera Settings")]
        static void AdoptGameViewCameraSettings(MenuCommand command)
        {
            var cam = command.context as CmCamera;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(cam);
            if (brain != null)
            {
                cam.m_Lens = brain.CurrentCameraState.Lens;
                cam.transform.position = brain.transform.position;
                cam.transform.rotation = brain.transform.rotation;
            }
        }

        [MenuItem("CONTEXT/CmCamera/Adopt Scene View Camera Settings")]
        static void AdoptSceneViewCameraSettings(MenuCommand command)
        {
            var cam = command.context as CmCamera;
            cam.m_Lens = CinemachineMenu.MatchSceneViewCamera(cam.transform);
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
            var ux = new VisualElement();
            var serializedTarget = new SerializedObject(Target);

            m_CameraUtility.AddCameraStatus(ux);
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Priority)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_StandbyUpdate)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Transitions)));
            
            InspectorUtility.AddHeader(ux, "Camera");
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Lens)));

            InspectorUtility.AddHeader(ux, "Procedural Motion");
            m_CameraUtility.AddSaveDuringPlayToggle(ux);
            m_CameraUtility.AddGameViewGuidesToggle(ux);
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Follow)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_LookAt)));
            m_CameraUtility.AddPipelineDropdowns(ux);

            InspectorUtility.AddHeader(ux, "Extensions");
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
                    new SerializedObject(cmCam).FindProperty(() => cmCam.m_Lens), 
                    cmCam.m_Lens, Target.m_Lens.UseHorizontalFOV);
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(cmCam,
                    new SerializedObject(cmCam).FindProperty(() => cmCam.m_Lens));
            }
            Handles.color = originalColor;
        }
#endif
    }
}
