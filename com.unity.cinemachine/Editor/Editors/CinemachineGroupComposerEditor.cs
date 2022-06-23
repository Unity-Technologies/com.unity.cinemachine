using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineGroupComposer))]
    internal class CinemachineGroupComposerEditor : CinemachineRotationComposerEditor
    {
        CinemachineGroupComposer Target => target as CinemachineGroupComposer;

        public override VisualElement CreateInspectorGUI()
        {
            var serializedTarget = new SerializedObject(Target);
            var ux = base.CreateInspectorGUI();

            InspectorUtility.AddSpace(ux);
            var notGroupHelp = ux.AddChild(new HelpBox(
                "The Framing settings will be ignored because the LookAt target is not a kind of ICinemachineTargetGroup.", 
                HelpBoxMessageType.Info));

            var framingMode = ux.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_FramingMode)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_GroupFramingSize)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_FrameDamping)));

            var nonOrthoControls = ux.AddChild(new VisualElement());
            var adjustmentModeProperty = serializedTarget.FindProperty(() => Target.m_AdjustmentMode);
            var adjustmentMode = nonOrthoControls.AddChild(new PropertyField(adjustmentModeProperty));
            var maxDollyIn = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MaxDollyIn)));
            var maxDollyOut = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MaxDollyOut)));
            var minDistance = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MinimumDistance)));
            var maxDistance = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MaximumDistance)));
            var minFov = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MinimumFOV)));
            var maxFov = nonOrthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MaximumFOV)));

            var orthoControls = ux.AddChild(new VisualElement());
            var minOrtho = orthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MinimumOrthoSize)));
            var maxOrtho = orthoControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.m_MaximumOrthoSize)));

            // GML: This is rather evil.  Is there a better (event-driven) way?
            UpdateUX();
            ux.schedule.Execute(UpdateUX).Every(250);

            void UpdateUX()
            {
                CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
                bool ortho = brain != null ? brain.OutputCamera.orthographic : false;
                nonOrthoControls.SetVisible(!ortho);
                orthoControls.SetVisible(ortho);

                bool noTarget = false;
                for (int i = 0; i < targets.Length; ++i)
                    noTarget |= targets[i] != null && (targets[i] as CinemachineGroupComposer).AbstractLookAtTargetGroup == null;
                if (notGroupHelp != null)
                    notGroupHelp.SetVisible(noTarget);
            }

            nonOrthoControls.TrackPropertyValue(adjustmentModeProperty, (prop) =>
            {
                bool haveDolly = prop.intValue != (int)CinemachineGroupComposer.AdjustmentMode.ZoomOnly;
                bool haveZoom = prop.intValue != (int)CinemachineGroupComposer.AdjustmentMode.DollyOnly;

                minFov.SetVisible(haveZoom);
                maxFov.SetVisible(haveZoom);
                maxDollyIn.SetVisible(haveDolly);
                maxDollyOut.SetVisible(haveDolly);
                minDistance.SetVisible(haveDolly);
                maxDistance.SetVisible(haveDolly);
            });

            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupComposer))]
        private static void DrawGroupComposerGizmos(CinemachineGroupComposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.AbstractLookAtTargetGroup != null)
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
    }
}
