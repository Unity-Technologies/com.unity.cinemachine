using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineCamera))]
    [CanEditMultipleObjects]
    class CmCameraEditor : UnityEditor.Editor 
    {
        CinemachineCamera Target => target as CinemachineCamera;

        [MenuItem("CONTEXT/CinemachineCamera/Adopt Game View Camera Settings")]
        static void AdoptGameViewCameraSettings(MenuCommand command)
        {
            var cam = command.context as CinemachineCamera;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(cam);
            if (brain != null)
            {
                cam.Lens = brain.CurrentCameraState.Lens;
                cam.transform.SetPositionAndRotation(brain.transform.position, brain.transform.rotation);
            }
        }

        [MenuItem("CONTEXT/CinemachineCamera/Adopt Scene View Camera Settings")]
        static void AdoptSceneViewCameraSettings(MenuCommand command)
        {
            var cam = command.context as CinemachineCamera;
            cam.Lens = CinemachineMenu.MatchSceneViewCamera(cam.transform);
        }

        void OnEnable()
        {
            Undo.undoRedoPerformed += ResetTarget;

            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTarget;

            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddCameraStatus(ux);
            this.AddTransitionsSection(ux, new () { serializedObject.FindProperty(() => Target.Transitions) });
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Lens)));

            ux.AddHeader("Global Settings");
            this.AddGlobalControls(ux);

            ux.AddHeader("Procedural Motion");
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Target)));
            this.AddPipelineDropdowns(ux);

            ux.AddSpace();
            this.AddExtensionsDropdown(ux);

            ux.TrackAnyUserActivity(() => CmCameraInspectorUtility.SortComponents(target as CinemachineVirtualCameraBase));

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
