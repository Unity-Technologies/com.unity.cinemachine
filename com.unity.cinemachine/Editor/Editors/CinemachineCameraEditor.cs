using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor.EditorTools;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineCamera))]
    [CanEditMultipleObjects]
    class CinemachineCameraEditor : CinemachineVirtualCameraBaseEditor
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

        void OnEnable() => Undo.undoRedoPerformed += ResetTarget;
        void OnDisable() => Undo.undoRedoPerformed -= ResetTarget;

        protected override void AddInspectorProperties(VisualElement ux)
        {
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendHint)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Lens)));

            var defaultTargetLabel = new ObjectField("");
            defaultTargetLabel.SetEnabled(false);
            var defaultTargetRow = ux.AddChild(new InspectorUtility.LabeledRow(
                "Default Target", "The default target is set in the parent object, and will be used if the Tracking Target is None",
                defaultTargetLabel));
            defaultTargetRow.focusable = false;
            defaultTargetLabel.style.marginLeft = 2;
            defaultTargetLabel.style.marginRight = -2;
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Target)));
            this.AddGroupTargetInfoBox(ux);

            ux.AddHeader("Global Settings");
            this.AddGlobalControls(ux);

            ux.AddHeader("Procedural Components");
            this.AddPipelineDropdowns(ux);

            ux.TrackAnyUserActivity(() =>
            {
                if (Target == null)
                    return; // object deleted
                if (!Application.isPlaying && (Target.isActiveAndEnabled || Target.IsLive))
                {
                    var brain = CinemachineCore.FindPotentialTargetBrain(Target);
                    Target.InternalUpdateCameraState(brain == null ? Vector3.up : brain.DefaultWorldUp, -1);
                }
                bool haveDefault = Target.Target.TrackingTarget != Target.Follow;
                defaultTargetRow.SetVisible(haveDefault);
                if (haveDefault)
                    defaultTargetLabel.value = Target.Follow;
                CmCameraInspectorUtility.SortComponents(target as CinemachineVirtualCameraBase);
            });
        }

        [EditorTool("Field of View Tool", typeof(CinemachineCamera))]
        class FoVTool : EditorTool
        {
            GUIContent m_IconContent;
            public override GUIContent toolbarIcon => m_IconContent;
            void OnEnable()
            {
                m_IconContent = new GUIContent
                {
                    image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/FOV.png"),
                    tooltip = "Adjust the Field of View of the Lens",
                };
            }

            public override void OnToolGUI(EditorWindow window)
            {
                var vcam = target as CinemachineCamera;
                if (target == null)
                    return;

                CinemachineSceneToolHelpers.DoFovToolHandle(
                    vcam, new SerializedObject(vcam).FindProperty(() => vcam.Lens), vcam.Lens, vcam.Lens.UseHorizontalFOV);
            }
        }

        [EditorTool("Far-Near Clip Tool", typeof(CinemachineCamera))]
        class FarNearClipTool : EditorTool
        {
            GUIContent m_IconContent;
            public override GUIContent toolbarIcon => m_IconContent;
            void OnEnable()
            {
                m_IconContent = new GUIContent
                {
                    image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/FarNearClip.png"),
                    tooltip = "Adjust the Far/Near Clip of the Lens",
                };
            }

            public override void OnToolGUI(EditorWindow window)
            {
                var vcam = target as CinemachineCamera;
                if (target == null)
                    return;

                CinemachineSceneToolHelpers.DoNearFarClipHandle(vcam, new SerializedObject(vcam).FindProperty(() => vcam.Lens));
            }
        }
    }
}
