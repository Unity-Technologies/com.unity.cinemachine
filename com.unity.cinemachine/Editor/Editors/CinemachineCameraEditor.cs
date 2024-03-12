using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineCamera))]
    [CanEditMultipleObjects]
    class CinemachineCameraEditor : UnityEditor.Editor 
    {
        CinemachineCamera Target => target as CinemachineCamera;

        [MenuItem("CONTEXT/CinemachineCamera/Adopt Game View Camera Settings")]
        static void AdoptGameViewCameraSettings(MenuCommand command)
        {
            var cam = command.context as CinemachineCamera;
            var brain = CinemachineCore.FindPotentialTargetBrain(cam);
            if (brain != null)
            {
                cam.Lens = brain.State.Lens;
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
            this.AddTransitionsSection(ux, new () { serializedObject.FindProperty(() => Target.BlendHint) });
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Lens)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Target)));

            ux.AddHeader("Global Settings");
            this.AddGlobalControls(ux);

            var defaultTargetLabel = new Label() { style = { alignSelf = Align.FlexEnd, opacity = 0.5f }};
            var row = ux.AddChild(new InspectorUtility.LabeledRow("<b>Procedural Components</b>", "", defaultTargetLabel));
            row.focusable = false;
            row.style.paddingTop = InspectorUtility.SingleLineHeight / 2;
            row.style.paddingBottom = EditorGUIUtility.standardVerticalSpacing;

            this.AddPipelineDropdowns(ux);

            ux.AddSpace();
            this.AddExtensionsDropdown(ux);

            ux.TrackAnyUserActivity(() => 
            {
                if (Target == null)
                    return; // object deleted
                var brain = CinemachineCore.FindPotentialTargetBrain(Target);
                var deltaTime = Application.isPlaying ? Time.deltaTime : -1;
                Target.InternalUpdateCameraState(brain == null ? Vector3.up : brain.DefaultWorldUp, deltaTime);
                bool haveDefault = Target.Target.TrackingTarget != Target.Follow;
                defaultTargetLabel.SetVisible(haveDefault);
                if (haveDefault)
                    defaultTargetLabel.text = "Default target: " + Target.Follow.name;
                CmCameraInspectorUtility.SortComponents(target as CinemachineVirtualCameraBase);
            });

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
                    cmCam.Lens, cmCam.Lens.UseHorizontalFOV);
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
