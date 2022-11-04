using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CmCamera))]
    [CanEditMultipleObjects]
    class CmCameraEditor : UnityEditor.Editor 
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
                cam.Lens = brain.CurrentCameraState.Lens;
                cam.transform.SetPositionAndRotation(brain.transform.position, brain.transform.rotation);
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

            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
        }

        void OnDisable()
        {
            EditorApplication.update -= m_CameraUtility.SortComponents;
            m_CameraUtility.OnDisable();
            Undo.undoRedoPerformed -= ResetTarget;
            
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_CameraUtility.AddCameraStatus(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.StandbyUpdate)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PriorityAndChannel)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Transitions)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Lens)));

            ux.AddHeader("Global Settings");
            m_CameraUtility.AddGlobalControls(ux);

            ux.AddHeader("Procedural Motion");
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Target)));
            m_CameraUtility.AddPipelineDropdowns(ux);

            ux.AddSpace();
            m_CameraUtility.AddExtensionsDropdown(ux);

            return ux;
        }

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
                    cmCam.Lens, InspectorUtility.GetUseHorizontalFOV(Target.Lens.SourceCamera));
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(cmCam,
                    new SerializedObject(cmCam).FindProperty(() => cmCam.Lens));
            }
            Handles.color = originalColor;
        }
    }
}
