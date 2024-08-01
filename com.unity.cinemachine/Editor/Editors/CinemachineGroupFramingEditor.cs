using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineGroupFraming))]
    [CanEditMultipleObjects]
    class CinemachineGroupFramingEditor : UnityEditor.Editor
    {
        CinemachineGroupFraming Target => target as CinemachineGroupFraming;

        void OnEnable() 
        {
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
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);
            var groupSizeIsZeroHelp = ux.AddChild(new HelpBox("Group size is zero, cannot frame.", HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FramingMode)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FramingSize)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CenterOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));

            var perspectiveControls = ux.AddChild(new VisualElement());

            var sizeAdjustmentProperty = serializedObject.FindProperty(() => Target.SizeAdjustment);
            perspectiveControls.Add(new PropertyField(sizeAdjustmentProperty));
            perspectiveControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.LateralAdjustment)));
            var fovRange = perspectiveControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.FovRange)));
            var dollyRange = perspectiveControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.DollyRange)));

            var orthoControls = ux.AddChild(new VisualElement());
            orthoControls.Add(new PropertyField(serializedObject.FindProperty(() => Target.OrthoSizeRange)));

            ux.TrackPropertyWithInitialCallback(sizeAdjustmentProperty, (prop) =>
            {
                bool haveDolly = sizeAdjustmentProperty.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.ZoomOnly;
                bool haveZoom = sizeAdjustmentProperty.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;

                fovRange.SetVisible(haveZoom);
                dollyRange.SetVisible(haveDolly);
            });
            
            ux.TrackAnyUserActivity(() =>
            {
                if (target == null || groupSizeIsZeroHelp == null)
                    return; 

                ICinemachineTargetGroup group = null;
                for (int i = 0; group == null && i < targets.Length; ++i)
                {
                    var vcam = (targets[i] as CinemachineGroupFraming).ComponentOwner;
                    if (vcam != null)
                    {
                        group = vcam.FollowTargetAsGroup;
                        if (group != null && !group.IsValid)
                            group = null;
                    }
                }
                groupSizeIsZeroHelp.SetVisible(group != null && group.Sphere.radius < 0.01f);

                bool ortho = Target.ComponentOwner != null && Target.ComponentOwner.State.Lens.Orthographic;
                perspectiveControls.SetVisible(!ortho);
                orthoControls.SetVisible(ortho);
            });

            return ux;
        }

        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
                return;

            var vcam = Target.ComponentOwner;
            if (brain == null || brain != CinemachineCore.FindPotentialTargetBrain(vcam)
                || !brain.IsLiveChild(vcam) || (brain.OutputCamera.activeTexture != null && CinemachineBrain.ActiveBrainCount > 1))
                return;

            var group = vcam.LookAtTargetAsGroup;
            group ??= vcam.FollowTargetAsGroup;
            if (group == null || !group.IsValid)
                return;

            CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                group.Sphere.position, brain.OutputCamera);
            CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenGroupSizeMarker(
                Target.GroupBounds, Target.GroupBoundsMatrix, brain.OutputCamera);
        }
        
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupFraming))]
        static void DrawGroupComposerGizmos(CinemachineGroupFraming target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            var vcam = target.ComponentOwner;
            if (!target.enabled && vcam != null 
                && (vcam.FollowTargetAsGroup != null && vcam.FollowTargetAsGroup.IsValid)
                || (vcam.LookAtTargetAsGroup != null && vcam.LookAtTargetAsGroup.IsValid))
            {
                var oldM = Gizmos.matrix;
                var oldC = Gizmos.color;

                Gizmos.matrix = target.GroupBoundsMatrix;
                Bounds b = target.GroupBounds;
                Gizmos.color = Color.yellow;
                if (vcam.State.Lens.Orthographic)
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
