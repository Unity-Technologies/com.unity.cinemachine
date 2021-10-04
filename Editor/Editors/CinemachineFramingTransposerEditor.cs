using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFramingTransposer))]
    [CanEditMultipleObjects]
    internal class CinemachineFramingTransposerEditor : BaseEditor<CinemachineFramingTransposer>
    {
        CinemachineScreenComposerGuides m_ScreenGuideEditor;
        GameViewEventCatcher m_GameViewEventCatcher;

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
            ICinemachineTargetGroup group = Target.AbstractFollowTargetGroup;
            if (group == null || Target.m_GroupFramingMode == CinemachineFramingTransposer.FramingMode.None)
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
                if (group == null)
                    excluded.Add(FieldPath(x => x.m_GroupFramingMode));
            }
            else
            {
                CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
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

            // First snapshot some settings
            Rect oldHard = Target.HardGuideRect;
            Rect oldSoft = Target.SoftGuideRect;

            // Draw the properties
            DrawRemainingPropertiesInInspector();
            m_ScreenGuideEditor.SetNewBounds(oldHard, oldSoft, Target.HardGuideRect, Target.SoftGuideRect);
        }


        /// Process a position drag from the user.
        /// Called "magically" by the vcam editor, so don't change the signature.
        public void OnVcamPositionDragged(Vector3 delta)
        {
            if (Target.FollowTarget != null)
            {
                Undo.RegisterCompleteObjectUndo(Target, "Camera drag");
                var fwd = Target.transform.forward;
                var zComponent = Vector3.Dot(fwd, delta);
                delta -= fwd * zComponent;
                Vector3 localOffset = Quaternion.Inverse(Target.FollowTarget.rotation) * delta;
                Target.m_TrackedObjectOffset += localOffset;
                Target.m_CameraDistance  = Mathf.Max(0.01f, Target.m_CameraDistance - zComponent);
            }
        }
        
        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (Target == null || !CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides)
                return;

            // If inspector is collapsed in the vcam editor, don't draw the guides
            if (!VcamStageEditor.ActiveEditorRegistry.IsActiveEditor(this))
                return;

            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
            if (brain == null || (brain.OutputCamera.activeTexture != null && CinemachineCore.Instance.BrainCount > 1))
                return;

            bool isLive = targets.Length <= 1 && brain.IsLive(Target.VirtualCamera, true);

            // Screen guides
            m_ScreenGuideEditor.OnGUI_DrawGuides(isLive, brain.OutputCamera, Target.VcamState.Lens, !Target.m_UnlimitedSoftZone);

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

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineFramingTransposer))]
        private static void DrawGroupComposerGizmos(CinemachineFramingTransposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.AbstractFollowTargetGroup != null
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

        bool m_SoloSetByTools;
        protected override void DrawSceneTools()
        {
            var framingTransposer = Target;
            if (!framingTransposer.IsValid)
            {
                return;
            }

            var originalColor = Handles.color;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(TrackedObjectOffsetTool)))
            {
                var followTargetPosition = framingTransposer.FollowTargetPosition;
                var followTargetRotation = framingTransposer.FollowTargetRotation;
                var trackedObjectPosition = 
                    followTargetPosition + followTargetRotation * framingTransposer.m_TrackedObjectOffset;

                EditorGUI.BeginChangeCheck();
                var tooHandleMinId = GUIUtility.GetControlID(FocusType.Passive);
                var newPos = Handles.PositionHandle(trackedObjectPosition, followTargetRotation);
                var tooHandleMaxId = GUIUtility.GetControlID(FocusType.Passive);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(framingTransposer, 
                        "Change Tracked Object Offset using handle in Scene View.");
                    
                    // calculate delta and discard imprecision, then update offset
                    var delta = Quaternion.Inverse(followTargetRotation) * (newPos - trackedObjectPosition);
                    delta = new Vector3(
                        Mathf.Abs(delta.x) < UnityVectorExtensions.Epsilon ? 0 : delta.x,
                        Mathf.Abs(delta.y) < UnityVectorExtensions.Epsilon ? 0 : delta.y,
                        Mathf.Abs(delta.z) < UnityVectorExtensions.Epsilon ? 0 : delta.z);
                    framingTransposer.m_TrackedObjectOffset += delta;
                    
                    InspectorUtility.RepaintGameView();
                }

                var trackedObjectOffsetHandleIsDragged = 
                    tooHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < tooHandleMaxId;
                var trackedObjectOffsetHandleIsUsedOrHovered = trackedObjectOffsetHandleIsDragged || 
                    tooHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < tooHandleMaxId;
                if (trackedObjectOffsetHandleIsUsedOrHovered)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(trackedObjectPosition, "Tracked Object Offset " + 
                        framingTransposer.m_TrackedObjectOffset.ToString("F1"), labelStyle);
                }
                Handles.color = trackedObjectOffsetHandleIsUsedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(followTargetPosition, trackedObjectPosition, 5f);
                Handles.DrawLine(trackedObjectPosition, framingTransposer.VcamState.FinalPosition);
                
                // solo this vcam when dragging
                if (trackedObjectOffsetHandleIsDragged)
                {
                    // if solo was activated by the user, then it was not the tool who set it to solo.
                    m_SoloSetByTools = m_SoloSetByTools || 
                        CinemachineBrain.SoloCamera != (ICinemachineCamera) framingTransposer.VirtualCamera;
                    CinemachineBrain.SoloCamera = framingTransposer.VirtualCamera;
                    InspectorUtility.RepaintGameView();
                }
                else if (m_SoloSetByTools && tooHandleMaxId != -1) // TODO-KGB: -1: there was an error in handles -> ignore frame
                {
                    CinemachineBrain.SoloCamera = null;
                    m_SoloSetByTools = false;
                    InspectorUtility.RepaintGameView();
                }
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var cameraPosition = framingTransposer.VcamState.RawPosition;
                var targetForward = framingTransposer.VirtualCamera.State.FinalOrientation * Vector3.forward;
                EditorGUI.BeginChangeCheck();
                Handles.color = Color.magenta;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newHandlePosition = Handles.Slider(cdHandleId, cameraPosition, targetForward,
                    HandleUtility.GetHandleSize(cameraPosition), Handles.ArrowHandleCap, 0.1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(framingTransposer, 
                        "Changed FramingTransposer distance using handle in Scene View.");
                    var diffHandlePosition = newHandlePosition - cameraPosition;
                    var sameDirection = Vector3.Dot(diffHandlePosition.normalized, targetForward) > 0;
                    framingTransposer.m_CameraDistance -= (sameDirection ? 1f : -1f) * diffHandlePosition.magnitude;
                    InspectorUtility.RepaintGameView();
                }

                var cameraDistanceHandleIsDragged = GUIUtility.hotControl == cdHandleId;
                var cameraDistanceHandleIsUsedOrHovered = cameraDistanceHandleIsDragged || 
                    HandleUtility.nearestControl == cdHandleId;
                if (cameraDistanceHandleIsUsedOrHovered)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(cameraPosition, "Camera Distance (" + 
                        framingTransposer.m_CameraDistance.ToString("F1") + ")", labelStyle);
                }
                
                // solo this vcam when dragging or hovering
                if (cameraDistanceHandleIsDragged)
                {
                    // if solo was activated by the user, then it was not the tool who set it to solo.
                    m_SoloSetByTools = m_SoloSetByTools || 
                        CinemachineBrain.SoloCamera != (ICinemachineCamera) framingTransposer.VirtualCamera;
                    CinemachineBrain.SoloCamera = framingTransposer.VirtualCamera;
                    InspectorUtility.RepaintGameView();
                }
                else if (m_SoloSetByTools)
                {
                    CinemachineBrain.SoloCamera = null;
                    m_SoloSetByTools = false;
                    InspectorUtility.RepaintGameView();
                }
            }
            Handles.color = originalColor;
        }
    }
}
