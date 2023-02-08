using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineGroupFraming))]
    [CanEditMultipleObjects]
    class CinemachineGroupFramingEditor : UnityEditor.Editor
    {
        CinemachineGroupFraming Target => target as CinemachineGroupFraming;

        CmPipelineComponentInspectorUtility m_PipelineUtility;
        VisualElement m_GroupSizeIsZeroHelp;
        VisualElement m_PerspectiveControls;
        VisualElement m_OrthoControls;

        void OnEnable() 
        {
            m_PipelineUtility = new(this);
            EditorApplication.update += UpdateVisibility;

            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
        }
        void OnDisable() 
        {
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();

            EditorApplication.update -= UpdateVisibility;
            m_PipelineUtility.OnDisable();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var serializedTarget = new SerializedObject(Target);
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.FollowGroup);
            m_GroupSizeIsZeroHelp = ux.AddChild(new HelpBox("Group size is zero, cannot frame.", HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.FramingMode)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.FramingSize)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Damping)));

            m_PerspectiveControls = ux.AddChild(new VisualElement());

            var sizeAdjustmentProperty = serializedTarget.FindProperty(() => Target.SizeAdjustment);
            m_PerspectiveControls.Add(new PropertyField(sizeAdjustmentProperty));
            m_PerspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.LateralAdjustment)));
            var fovRange = m_PerspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.FovRange)));
            var dollyRange = m_PerspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.DollyRange)));

            m_OrthoControls = ux.AddChild(new VisualElement());
            m_OrthoControls.Add(new PropertyField(serializedTarget.FindProperty(() => Target.OrthoSizeRange)));

            ux.TrackPropertyValue(sizeAdjustmentProperty, (prop) =>
            {
                bool haveDolly = prop.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.ZoomOnly;
                bool haveZoom = prop.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;

                fovRange.SetVisible(haveZoom);
                dollyRange.SetVisible(haveDolly);
            });
            
            m_PipelineUtility.UpdateState();
            UpdateVisibility();
            return ux;
        }

        void UpdateVisibility()
        {
            if (target == null || m_GroupSizeIsZeroHelp == null)
                return; 

            ICinemachineTargetGroup group = null;
            for (int i = 0; group == null && i < targets.Length; ++i)
            {
                var vcam = (targets[i] as CinemachineGroupFraming).ComponentOwner;
                if (vcam != null)
                    group = vcam.FollowTargetAsGroup;
            }
            m_GroupSizeIsZeroHelp.SetVisible(group != null && group.Sphere.radius < 0.01f);

            bool ortho = Target.ComponentOwner != null && Target.ComponentOwner.State.Lens.Orthographic;
            m_PerspectiveControls.SetVisible(!ortho);
            m_OrthoControls.SetVisible(ortho);
        }

        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
                return;

            if (brain == null || (brain.OutputCamera.activeTexture != null && CinemachineCore.Instance.BrainCount > 1))
                return;

            var vcam = Target.ComponentOwner;
            if (!brain.IsValidChannel(vcam))
                return;

            var group = vcam.LookAtTargetAsGroup;
            if (group == null)
                return;

            CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                group, group.Sphere.position, 
                vcam.State.GetFinalOrientation(), brain.OutputCamera);
        }
        
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupFraming))]
        static void DrawGroupComposerGizmos(CinemachineGroupFraming target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.enabled && target.ComponentOwner != null && target.ComponentOwner.FollowTargetAsGroup != null)
            {
                var oldM = Gizmos.matrix;
                var oldC = Gizmos.color;

                Gizmos.matrix = target.GroupBoundsMatrix;
                Bounds b = target.GroupBounds;
                Gizmos.color = Color.yellow;
                if (target.ComponentOwner.State.Lens.Orthographic)
                    Gizmos.DrawWireCube(b.center, b.size);
                else
                {
                    var e = b.extents;
                    var z = b.center.z - e.z; // near face
                    var d = b.size.z;
                    var a = Mathf.Atan2(e.y, z) * Mathf.Rad2Deg * 2;
                    Gizmos.DrawFrustum(Vector3.zero, a, z + d, z, e.x / e.y);
                }
                Gizmos.matrix = oldM;
                Gizmos.color = oldC;
            }
        }
    }
}
