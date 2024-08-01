using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePositionComposer))]
    [CanEditMultipleObjects]
    class CinemachinePositionComposerEditor : CinemachineComponentBaseEditor
    {
        readonly GameViewComposerGuides m_GameViewGuides = new();

        CinemachinePositionComposer Target => target as CinemachinePositionComposer;

        protected virtual void OnEnable()
        {
            m_GameViewGuides.GetComposition = () => Target.Composition;
            m_GameViewGuides.SetComposition = (s) => Target.Composition = s;
            m_GameViewGuides.Target = () => serializedObject;
            m_GameViewGuides.OnEnable();

            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
        }

        protected virtual void OnDisable()
        {
            m_GameViewGuides.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
        }

        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
                return;

            var vcam = Target.VirtualCamera;
            if (brain == null || brain != CinemachineCore.FindPotentialTargetBrain(vcam)
                || (brain.OutputCamera.activeTexture != null && CinemachineBrain.ActiveBrainCount > 1))
                return;

            // Screen guides
            bool isLive = targets.Length <= 1 && brain.IsLiveChild(vcam, true);
            m_GameViewGuides.OnGUI_DrawGuides(isLive, brain.OutputCamera, vcam.State.Lens);

            // Draw an on-screen gizmo for the target
            if (Target.FollowTarget != null && isLive)
                CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                    Target.TrackedPoint, brain.OutputCamera);
        }

        [EditorTool("Camera Distance Tool", typeof(CinemachinePositionComposer))]
        class CameraDistanceTool : EditorTool
        {
            GUIContent m_IconContent;
            public override GUIContent toolbarIcon => m_IconContent;
            void OnEnable()
            {
                m_IconContent = new GUIContent
                {
                    image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/FollowOffset.png"),
                    tooltip = "Adjust the Camera Distance",
                };
            }

            public override void OnToolGUI(EditorWindow window)
            {
                var composer = target as CinemachinePositionComposer;
                if (composer == null || !composer.IsValid)
                    return;

                var property = new SerializedObject(composer).FindProperty(() => composer.CameraDistance);

                var originalColor = Handles.color;
                var camPos = composer.VcamState.RawPosition;
                var targetForward = composer.VirtualCamera.State.GetFinalOrientation() * Vector3.forward;
                EditorGUI.BeginChangeCheck();
                Handles.color = CinemachineSceneToolHelpers.HelperLineDefaultColor;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newHandlePosition = Handles.Slider(cdHandleId, camPos, targetForward,
                    CinemachineSceneToolHelpers.CubeHandleCapSize(camPos), Handles.CubeHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    // Modify via SerializedProperty for OnValidate to get called automatically, and scene repainting too
                    property.floatValue -= CinemachineSceneToolHelpers.SliderHandleDelta(newHandlePosition, camPos, targetForward);
                    property.serializedObject.ApplyModifiedProperties();
                }

                var isDragged = GUIUtility.hotControl == cdHandleId;
                var isDraggedOrHovered = isDragged || HandleUtility.nearestControl == cdHandleId;
                if (isDraggedOrHovered)
                {
                    CinemachineSceneToolHelpers.DrawLabel(camPos, 
                        property.displayName + " (" + composer.CameraDistance.ToString("F1") + ")");
                }
                
                Handles.color = isDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSceneToolHelpers.HelperLineDefaultColor;
                Handles.DrawLine(camPos, composer.FollowTargetPosition + composer.TargetOffset);

                CinemachineSceneToolHelpers.SoloOnDrag(isDragged, composer.VirtualCamera, cdHandleId);
                
                Handles.color = originalColor;
            }
        }

        [EditorTool("LookAt Offset Tool", typeof(CinemachinePositionComposer))]
        class LookAtOffsetTool : EditorTool
        {
            GUIContent m_IconContent;
            public override GUIContent toolbarIcon => m_IconContent;
            void OnEnable()
            {
                m_IconContent = new GUIContent
                {
                    image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/TrackedObjectOffset.png"),
                    tooltip = "Adjust the LookAt Offset",
                };
            }

            public override void OnToolGUI(EditorWindow window)
            {
                var composer = target as CinemachinePositionComposer;
                if (composer == null || !composer.IsValid)
                    return;

                CinemachineSceneToolHelpers.DoTrackedObjectOffsetTool(
                    composer.VirtualCamera, 
                    new SerializedObject(composer).FindProperty(() => composer.TargetOffset),
                    CinemachineCore.Stage.Body);
            }
        }
    }
}
