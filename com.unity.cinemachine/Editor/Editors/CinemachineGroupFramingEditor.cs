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
            var serializedTarget = new SerializedObject(Target);
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Group);
            var groupSizeIsZeroHelp = ux.AddChild(new HelpBox("Group size is zero, cannot frame.", HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.FramingMode)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.FramingSize)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Damping)));

            var perspectiveControls = ux.AddChild(new VisualElement());

            var sizeAdjustmentProperty = serializedTarget.FindProperty(() => Target.SizeAdjustment);
            perspectiveControls.Add(new PropertyField(sizeAdjustmentProperty));
            perspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.LateralAdjustment)));
            var fovRange = perspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.FovRange)));
            var dollyRange = perspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.DollyRange)));

            var orthoControls = ux.AddChild(new VisualElement());
            orthoControls.Add(new PropertyField(serializedTarget.FindProperty(() => Target.OrthoSizeRange)));

            ux.TrackPropertyValue(sizeAdjustmentProperty, (prop) =>
            {
                bool haveDolly = prop.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.ZoomOnly;
                bool haveZoom = prop.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;

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
                        group = vcam.FollowTargetAsGroup;
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
