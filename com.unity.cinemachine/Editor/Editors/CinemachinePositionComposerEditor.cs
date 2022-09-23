using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePositionComposer))]
    [CanEditMultipleObjects]
    class CinemachinePositionComposerEditor : UnityEditor.Editor
    {
        CinemachineScreenComposerGuides m_ScreenGuideEditor;
        GameViewEventCatcher m_GameViewEventCatcher;
        VisualElement m_NoTargetHelp;

        CinemachinePositionComposer Target => target as CinemachinePositionComposer;

        protected virtual void OnEnable()
        {
            m_ScreenGuideEditor = new CinemachineScreenComposerGuides();
            m_ScreenGuideEditor.GetHardGuide = () => Target.HardGuideRect;
            m_ScreenGuideEditor.GetSoftGuide = () => Target.SoftGuideRect;
            m_ScreenGuideEditor.SetHardGuide = (Rect r) => { Target.HardGuideRect = r; };
            m_ScreenGuideEditor.SetSoftGuide = (Rect r) => { Target.SoftGuideRect = r; };
            m_ScreenGuideEditor.Target = () => serializedObject;

            m_GameViewEventCatcher = new GameViewEventCatcher();
            m_GameViewEventCatcher.OnEnable();

            CinemachineDebug.OnGUIHandlers -= OnGUI;
            CinemachineDebug.OnGUIHandlers += OnGUI;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
            
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));

            EditorApplication.update += UpdateVisibility;
        }

        protected virtual void OnDisable()
        {
            m_GameViewEventCatcher.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGUI;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();

            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(TrackedObjectOffsetTool));

            EditorApplication.update -= UpdateVisibility;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_NoTargetHelp = ux.AddChild(new HelpBox("Position Composer requires a Tracking Target in the CmCamera.", HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TrackedObjectOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Lookahead)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraDistance)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DeadZoneDepth)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Composition)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UnlimitedSoftZone)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CenterOnActivate)));

            return ux;
        }

        void UpdateVisibility()
        {
            if (Target == null || m_NoTargetHelp == null)
                return;
            var noTarget = false;
            for (var i = 0; i < targets.Length; ++i)
                noTarget |= targets[i] != null && (targets[i] as CinemachinePositionComposer).FollowTarget == null;
            m_NoTargetHelp.SetVisible(noTarget);
        }

        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
                return;

            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
            if (brain == null || (brain.OutputCamera.activeTexture != null && CinemachineCore.Instance.BrainCount > 1))
                return;

            bool isLive = targets.Length <= 1 && brain.IsLive(Target.VirtualCamera, true);

            // Screen guides
            m_ScreenGuideEditor.OnGUI_DrawGuides(isLive, brain.OutputCamera, Target.VcamState.Lens, !Target.UnlimitedSoftZone);

            // Draw an on-screen gizmo for the target
            if (Target.FollowTarget != null && isLive)
            {
                Vector3 targetScreenPosition = brain.OutputCamera.WorldToScreenPoint(Target.TrackedPoint);
                if (targetScreenPosition.z > 0)
                {
                    targetScreenPosition.y = Screen.height - targetScreenPosition.y;

                    GUI.color = CinemachineComposerPrefs.TargetColour.Value;
                    Rect r = new Rect(targetScreenPosition, Vector2.zero);
                    float size = (CinemachineComposerPrefs.TargetSize.Value
                        + CinemachineScreenComposerGuides.kGuideBarWidthPx) / 2;
                    GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                    size -= CinemachineScreenComposerGuides.kGuideBarWidthPx;
                    if (size > 0)
                    {
                        Vector4 overlayOpacityScalar
                            = new Vector4(1f, 1f, 1f, CinemachineComposerPrefs.OverlayOpacity.Value);
                        GUI.color = Color.black * overlayOpacityScalar;
                        GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                    }
                }
            }
        }

        void OnSceneGUI()
        {
            if (Target == null || !Target.IsValid)
                return;
            
            if (CinemachineSceneToolUtility.IsToolActive(typeof(TrackedObjectOffsetTool)))
            {
                CinemachineSceneToolHelpers.TrackedObjectOffsetTool(
                    Target.VirtualCamera, 
                    new SerializedObject(Target).FindProperty(() => Target.TrackedObjectOffset),
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
                Handles.DrawLine(camPos, Target.FollowTargetPosition + Target.TrackedObjectOffset);

                CinemachineSceneToolHelpers.SoloOnDrag(isDragged, Target.VirtualCamera, cdHandleId);
                
                Handles.color = originalColor;
            }
        }
    }
}
