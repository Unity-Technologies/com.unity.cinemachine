using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePositionComposer))]
    [CanEditMultipleObjects]
    internal class CinemachinePositionComposerEditor : UnityEditor.Editor
    {
        CinemachineScreenComposerGuides m_ScreenGuideEditor;
        GameViewEventCatcher m_GameViewEventCatcher;

        CinemachinePositionComposer Target => target as CinemachinePositionComposer;

        protected virtual void OnEnable()
        {
            m_ScreenGuideEditor = new CinemachineScreenComposerGuides();
            m_ScreenGuideEditor.GetHardGuide = () => { return Target.HardGuideRect; };
            m_ScreenGuideEditor.GetSoftGuide = () => { return Target.SoftGuideRect; };
            m_ScreenGuideEditor.SetHardGuide = (Rect r) => { Target.HardGuideRect = r; };
            m_ScreenGuideEditor.SetSoftGuide = (Rect r) => { Target.SoftGuideRect = r; };
            m_ScreenGuideEditor.Target = () => { return serializedObject; };

            m_GameViewEventCatcher = new GameViewEventCatcher();
            m_GameViewEventCatcher.OnEnable();

            CinemachineDebug.OnGUIHandlers -= OnGUI;
            CinemachineDebug.OnGUIHandlers += OnGUI;
            if (CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides)
                InspectorUtility.RepaintGameView();
            
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnDisable()
        {
            m_GameViewEventCatcher.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGUI;
            if (CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides)
                InspectorUtility.RepaintGameView();

            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(TrackedObjectOffsetTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var serializedTarget = new SerializedObject(Target);
            var ux = new VisualElement();

            var noTargetHelp = ux.AddChild(new HelpBox("A Tracking target is required.", HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.TrackedObjectOffset)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Lookahead)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.CameraDistance)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.DeadZoneDepth)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Composition)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.UnlimitedSoftZone)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.CenterOnActivate)));

            var groupFraming = ux.AddChild(new VisualElement());
            groupFraming.AddSpace();

            groupFraming.Add(new PropertyField(serializedTarget.FindProperty(() => Target.GroupFramingMode)));
            groupFraming.Add(new PropertyField(serializedTarget.FindProperty(() => Target.GroupFramingSize)));

            var nonOrthoControls = groupFraming.AddChild(new VisualElement());

            var adjustmentModeProperty = serializedTarget.FindProperty(() => Target.AdjustmentMode);
            nonOrthoControls.Add(new PropertyField(adjustmentModeProperty));

            var dollyRange = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.DollyRange)));
            var distanceRange = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.TargetDistanceRange)));
            var fovRange = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.FovRange)));

            var orthoControls = groupFraming.AddChild(new VisualElement());
            orthoControls.Add(new PropertyField(serializedTarget.FindProperty(() => Target.OrthoSizeRange)));

            ux.TrackPropertyValue(adjustmentModeProperty, (prop) =>
            {
                bool haveDolly = prop.intValue != (int)CinemachinePositionComposer.AdjustmentModes.ZoomOnly;
                bool haveZoom = prop.intValue != (int)CinemachinePositionComposer.AdjustmentModes.DollyOnly;

                fovRange.SetVisible(haveZoom);
                dollyRange.SetVisible(haveDolly);
                distanceRange.SetVisible(haveDolly);
            });
            
            // GML: This is rather evil.  Is there a better (event-driven) way?
            UpdateVisibility();
            ux.schedule.Execute(UpdateVisibility).Every(250);

            void UpdateVisibility()
            {
                groupFraming.SetVisible(Target.AbstractFollowTargetGroup != null);

                bool ortho = Target.VcamState.Lens.Orthographic;
                nonOrthoControls.SetVisible(!ortho);
                orthoControls.SetVisible(ortho);

                bool noTarget = false;
                for (int i = 0; i < targets.Length; ++i)
                    noTarget |= targets[i] != null && (targets[i] as CinemachinePositionComposer).FollowTarget == null;
                if (noTargetHelp != null)
                    noTargetHelp.SetVisible(noTarget);
            }
            return ux;
        }

        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (Target == null || !CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides || !Target.isActiveAndEnabled)
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

                    GUI.color = CinemachineSettings.ComposerSettings.TargetColour;
                    Rect r = new Rect(targetScreenPosition, Vector2.zero);
                    float size = (CinemachineSettings.ComposerSettings.TargetSize
                        + CinemachineScreenComposerGuides.kGuideBarWidthPx) / 2;
                    GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                    size -= CinemachineScreenComposerGuides.kGuideBarWidthPx;
                    if (size > 0)
                    {
                        Vector4 overlayOpacityScalar
                            = new Vector4(1f, 1f, 1f, CinemachineSettings.ComposerSettings.OverlayOpacity);
                        GUI.color = Color.black * overlayOpacityScalar;
                        GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                    }
                }
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachinePositionComposer))]
        private static void DrawGroupComposerGizmos(CinemachinePositionComposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.AbstractFollowTargetGroup != null
                && target.GroupFramingMode != CinemachinePositionComposer.FramingModes.None)
            {
                Matrix4x4 m = Gizmos.matrix;
                Bounds b = target.LastBounds;
                Gizmos.matrix = target.LastBoundsMatrix;
                Gizmos.color = Color.yellow;
                if (target.VcamState.Lens.Orthographic)
                    Gizmos.DrawWireCube(b.center, b.size);
                else
                {
                    float z = b.center.z;
                    Vector3 e = b.extents;
                    Gizmos.DrawFrustum(
                        Vector3.zero,
                        Mathf.Atan2(e.y, z) * Mathf.Rad2Deg * 2,
                        z + e.z, z - e.z, e.x / e.y);
                }
                Gizmos.matrix = m;
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
                var targetForward = Target.VirtualCamera.State.FinalOrientation * Vector3.forward;
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
