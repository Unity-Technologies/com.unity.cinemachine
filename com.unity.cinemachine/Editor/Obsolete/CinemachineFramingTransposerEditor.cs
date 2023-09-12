#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineFramingTransposer))]
    [CanEditMultipleObjects]
    class CinemachineFramingTransposerEditor : BaseEditor<CinemachineFramingTransposer>
    {
        GameViewComposerGuides m_GameViewGuides = new();

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (Target.m_UnlimitedSoftZone)
            {
                excluded.Add(FieldPath(x => x.m_SoftZoneWidth));
                excluded.Add(FieldPath(x => x.m_SoftZoneHeight));
                excluded.Add(FieldPath(x => x.m_BiasX));
                excluded.Add(FieldPath(x => x.m_BiasY));
            }
            ICinemachineTargetGroup group = Target.FollowTargetAsGroup;
            if (group == null || !group.IsValid || Target.m_GroupFramingMode == CinemachineFramingTransposer.FramingMode.None)
            {
                excluded.Add(FieldPath(x => x.m_GroupFramingSize));
                excluded.Add(FieldPath(x => x.m_AdjustmentMode));
                excluded.Add(FieldPath(x => x.m_MaxDollyIn));
                excluded.Add(FieldPath(x => x.m_MaxDollyOut));
                excluded.Add(FieldPath(x => x.m_MinimumDistance));
                excluded.Add(FieldPath(x => x.m_MaximumDistance));
                excluded.Add(FieldPath(x => x.m_MinimumFOV));
                excluded.Add(FieldPath(x => x.m_MaximumFOV));
                excluded.Add(FieldPath(x => x.m_MinimumOrthoSize));
                excluded.Add(FieldPath(x => x.m_MaximumOrthoSize));
                if (group == null || !group.IsValid)
                    excluded.Add(FieldPath(x => x.m_GroupFramingMode));
            }
            else
            {
                CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(Target.VirtualCamera);
                bool ortho = brain != null ? brain.OutputCamera.orthographic : false;
                if (ortho)
                {
                    excluded.Add(FieldPath(x => x.m_AdjustmentMode));
                    excluded.Add(FieldPath(x => x.m_MaxDollyIn));
                    excluded.Add(FieldPath(x => x.m_MaxDollyOut));
                    excluded.Add(FieldPath(x => x.m_MinimumDistance));
                    excluded.Add(FieldPath(x => x.m_MaximumDistance));
                    excluded.Add(FieldPath(x => x.m_MinimumFOV));
                    excluded.Add(FieldPath(x => x.m_MaximumFOV));
                }
                else
                {
                    excluded.Add(FieldPath(x => x.m_MinimumOrthoSize));
                    excluded.Add(FieldPath(x => x.m_MaximumOrthoSize));
                    switch (Target.m_AdjustmentMode)
                    {
                    case CinemachineFramingTransposer.AdjustmentMode.DollyOnly:
                        excluded.Add(FieldPath(x => x.m_MinimumFOV));
                        excluded.Add(FieldPath(x => x.m_MaximumFOV));
                        break;
                    case CinemachineFramingTransposer.AdjustmentMode.ZoomOnly:
                        excluded.Add(FieldPath(x => x.m_MaxDollyIn));
                        excluded.Add(FieldPath(x => x.m_MaxDollyOut));
                        excluded.Add(FieldPath(x => x.m_MinimumDistance));
                        excluded.Add(FieldPath(x => x.m_MaximumDistance));
                        break;
                    default:
                        break;
                    }
                }
            }
        }

        protected virtual void OnEnable()
        {
            m_GameViewGuides.GetComposition = () => Target.Composition;
            m_GameViewGuides.SetComposition = (s) => Target.Composition = s;
            m_GameViewGuides.Target = () => { return serializedObject; };
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

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineFramingTransposer).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox(
                    "Framing Transposer requires a Follow target.  "
                        + "Change Body to Do Nothing if you don't want a Follow target.",
                    MessageType.Warning);

            // Draw the properties
            DrawRemainingPropertiesInInspector();
        }
        
        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            // Draw the camera guides
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value)
                return;

            // If inspector is collapsed in the vcam editor, don't draw the guides
            if (!VcamStageEditor.ActiveEditorRegistry.IsActiveEditor(this))
                return;

            if (brain == null || (brain.OutputCamera.activeTexture != null && CinemachineBrain.ActiveBrainCount > 1))
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

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineFramingTransposer))]
        private static void DrawGroupComposerGizmos(CinemachineFramingTransposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.FollowTargetAsGroup != null && target.FollowTargetAsGroup.IsValid
                && target.m_GroupFramingMode != CinemachineFramingTransposer.FramingMode.None)
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
            var framingTransposer = Target;
            if (framingTransposer == null || !framingTransposer.IsValid)
            {
                return;
            }
            
            if (CinemachineSceneToolUtility.IsToolActive(typeof(TrackedObjectOffsetTool)))
            {
                CinemachineSceneToolHelpers.TrackedObjectOffsetTool(
                    Target.VirtualCamera, 
                    new SerializedObject(Target).FindProperty(() => Target.m_TrackedObjectOffset),
                    CinemachineCore.Stage.Body);
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var originalColor = Handles.color;
                var camPos = framingTransposer.VcamState.RawPosition;
                var targetForward = framingTransposer.VirtualCamera.State.GetFinalOrientation() * Vector3.forward;
                EditorGUI.BeginChangeCheck();
                Handles.color = CinemachineSceneToolHelpers.HelperLineDefaultColor;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newHandlePosition = Handles.Slider(cdHandleId, camPos, targetForward,
                    CinemachineSceneToolHelpers.CubeHandleCapSize(camPos), Handles.CubeHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    // Modify via SerializedProperty for OnValidate to get called automatically, and scene repainting too
                    var so = new SerializedObject(framingTransposer);
                    var prop = so.FindProperty(() => framingTransposer.m_CameraDistance);
                    prop.floatValue -= CinemachineSceneToolHelpers.SliderHandleDelta(newHandlePosition, camPos, targetForward);
                    so.ApplyModifiedProperties();
                }

                var cameraDistanceHandleIsDragged = GUIUtility.hotControl == cdHandleId;
                var cameraDistanceHandleIsUsedOrHovered = cameraDistanceHandleIsDragged || 
                    HandleUtility.nearestControl == cdHandleId;
                if (cameraDistanceHandleIsUsedOrHovered)
                {
                    CinemachineSceneToolHelpers.DrawLabel(camPos, 
                        "Camera Distance (" + framingTransposer.m_CameraDistance.ToString("F1") + ")");
                }
                
                Handles.color = cameraDistanceHandleIsUsedOrHovered ? 
                    Handles.selectedColor : CinemachineSceneToolHelpers.HelperLineDefaultColor;
                Handles.DrawLine(camPos, 
                    framingTransposer.FollowTarget.position + framingTransposer.m_TrackedObjectOffset);

                CinemachineSceneToolHelpers.SoloOnDrag(cameraDistanceHandleIsDragged, framingTransposer.VirtualCamera,
                    cdHandleId);
                
                Handles.color = originalColor;
            }
        }
    }
}
#endif
