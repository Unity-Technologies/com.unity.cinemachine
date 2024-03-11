using UnityEngine;
using UnityEditor;

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
            
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnDisable()
        {
            m_GameViewGuides.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();

            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
                return;

            if (brain == null || brain.OutputCamera == null
                    || (brain.OutputCamera.activeTexture != null && CinemachineBrain.ActiveBrainCount > 1))
                return;

            var vcam = Target.VirtualCamera;
            if (!brain.IsValidChannel(vcam))
                return;

            // Screen guides
            bool isLive = targets.Length <= 1 && brain.IsLiveChild(vcam, true);
            m_GameViewGuides.OnGUI_DrawGuides(isLive, brain.OutputCamera, vcam.State.Lens);

            // Draw an on-screen gizmo for the target
            if (Target.FollowTarget != null && isLive)
                CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                    Target.TrackedPoint, brain.OutputCamera);
        }

        void OnSceneGUI()
        {
            if (Target == null || !Target.IsValid)
                return;
            
            if (CinemachineSceneToolUtility.IsToolActive(typeof(TrackedObjectOffsetTool)))
            {
                CinemachineSceneToolHelpers.TrackedObjectOffsetTool(
                    Target.VirtualCamera, 
                    new SerializedObject(Target).FindProperty(() => Target.TargetOffset),
                    CinemachineCore.Stage.Body);
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var property = new SerializedObject(Target).FindProperty(() => Target.CameraDistance);

                var originalColor = Handles.color;
                var camPos = Target.VcamState.RawPosition;
                var targetForward = Target.VirtualCamera.State.GetFinalOrientation() * Vector3.forward;
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
                        property.displayName + " (" + Target.CameraDistance.ToString("F1") + ")");
                }
                
                Handles.color = isDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSceneToolHelpers.HelperLineDefaultColor;
                Handles.DrawLine(camPos, Target.FollowTargetPosition + Target.TargetOffset);

                CinemachineSceneToolHelpers.SoloOnDrag(isDragged, Target.VirtualCamera, cdHandleId);
                
                Handles.color = originalColor;
            }
        }
    }
}
