#if CINEMACHINE_PHYSICS_2D

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineConfiner2D))]
    [CanEditMultipleObjects]
    class CinemachineConfiner2DEditor : UnityEditor.Editor
    {
        CinemachineConfiner2D Target => target as CinemachineConfiner2D;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);

            var boundsHelp = ux.AddChild(new HelpBox(
                "Bounding Shape must be a PolygonCollider2D, BoxCollider2D, or CompositeCollider2D.", 
                HelpBoxMessageType.Warning));
            var polygonsHelp = ux.AddChild(new HelpBox(
                "CompositeCollider2D geometry type must be Polygons.", 
                HelpBoxMessageType.Warning));
            var invalidCollider2D = ux.AddChild(new HelpBox(
                "The input Collider2D is not valid; it has no points.", 
                HelpBoxMessageType.Warning));

            var volumeProp = serializedObject.FindProperty(() => Target.BoundingShape2D);
            ux.Add(new PropertyField(volumeProp));
            ux.TrackAnyUserActivity(() =>
            {
                var c = volumeProp.objectReferenceValue;
                boundsHelp.SetVisible(c != null && c is not (PolygonCollider2D or BoxCollider2D or CompositeCollider2D));
                polygonsHelp.SetVisible(c is CompositeCollider2D cc && cc.geometryType != CompositeCollider2D.GeometryType.Polygons);
                invalidCollider2D.SetVisible(c != null && Target.IsConfinerOvenNull());
            });
            
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SlowingDistance)));
            
            var oversizedCameraHelp = ux.AddChild(new HelpBox(
                "The camera window is too big for the confiner. Enable the Oversize Window option.",
                HelpBoxMessageType.Info));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.OversizeWindow)));

            var bakeProgress = ux.AddChild(new ProgressBar { lowValue = 0, highValue = 100 });
            var bakeTimeout = ux.AddChild(new HelpBox(
                "Polygon skeleton computation timed out.  Confiner result might be incomplete."
                + "\n\nTo fix this, reduce the number of points in the confining shape, "
                + "or set the MaxWindowSize parameter to limit skeleton computation.",
                HelpBoxMessageType.Warning));
            
            UpdateBakingProgress();
            ux.schedule.Execute(UpdateBakingProgress).Every(250); // GML todo: is there a better way to do this?
            void UpdateBakingProgress() 
            {
                if (Target == null)
                    return; // target deleted

                oversizedCameraHelp.SetVisible(!Target.OversizeWindow.Enabled && Target.IsCameraLensOversized());
                if (!Target.OversizeWindow.Enabled)
                {
                    bakeTimeout.SetVisible(false);
                    bakeProgress.SetVisible(false);
                    return;
                }
                var progress = Target.BakeProgress();
                bool timedOut = Target.ConfinerOvenTimedOut();
                bakeProgress.value = progress * 100;
                bakeProgress.title = timedOut ? "Timed out" : progress == 0 ? "" : progress < 1f ? "Baking" : "Baked";
                bakeProgress.SetVisible(true);
                bakeTimeout.SetVisible(timedOut);
            }

            ux.Add(new Button(() => 
            {
                Target.InvalidateBoundingShapeCache();
                EditorUtility.SetDirty(Target);
            })
            { 
                text = "Invalidate Bounding Shape Cache",
                tooltip = "Invalidates confiner2D cache, so a new one is computed next frame.\n" 
                    + "Call this when the input bounding shape changes " 
                    + "(non-uniform scale, rotation, or points are moved, added or deleted)."
            });

            return ux;
        }

        static List<List<Vector2>> s_CurrentPathCache = new();

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineConfiner2D))]
        static void DrawConfinerGizmos(CinemachineConfiner2D confiner2D, GizmoType type)
        {
            if (!confiner2D.GetGizmoPaths(out var originalPath, ref s_CurrentPathCache, out var pathLocalToWorld))
                return;

            var color = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;
            var colorDimmed = new Color(color.r, color.g, color.b, color.a / 2f);
            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = pathLocalToWorld;

            // Draw input confiner
            Gizmos.color = color;
            for (int i = 0; i < originalPath.Count; ++i)
            {
                var path = originalPath[i];
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            // Draw confiner for current camera size
            Gizmos.color = colorDimmed;
            for (int i = 0; i < s_CurrentPathCache.Count; ++i)
            {
                var path = s_CurrentPathCache[i];
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
}
#endif
