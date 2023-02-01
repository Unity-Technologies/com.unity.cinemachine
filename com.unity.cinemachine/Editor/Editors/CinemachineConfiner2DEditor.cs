#if CINEMACHINE_PHYSICS_2D

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineConfiner2D))]
    [CanEditMultipleObjects]
    class CinemachineConfiner2DEditor : UnityEditor.Editor
    {
        CinemachineConfiner2D Target => target as CinemachineConfiner2D;
        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);

            var boundsHelp = ux.AddChild(new HelpBox(
                "Bounding Shape must be a PolygonCollider2D, BoxCollider2D, or CompositeCollider2D.", 
                HelpBoxMessageType.Warning));
            var polygonsHelp = ux.AddChild(new HelpBox(
                "CompositeCollider2D geometry type must be Polygons.", 
                HelpBoxMessageType.Warning));

            var volumeProp = serializedObject.FindProperty(() => Target.BoundingShape2D);
            ux.Add(new PropertyField(volumeProp));
            TrackVolume(volumeProp);
            ux.TrackPropertyValue(volumeProp, TrackVolume);
            void TrackVolume(SerializedProperty p)
            {
                var c = p.objectReferenceValue;
                boundsHelp.SetVisible(!(c is PolygonCollider2D || c is BoxCollider2D || c is CompositeCollider2D));

                var cc = c as CompositeCollider2D;
                polygonsHelp.SetVisible(cc != null && cc.geometryType != CompositeCollider2D.GeometryType.Polygons);
            }
            
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SlowingDistance)));
            
            var oversizedCameraHelp = ux.AddChild(new HelpBox(
                "The camera window is too big for the confiner. Enable the Oversize Window option.",
               HelpBoxMessageType.Info));

            UpdateOversizedCameraHelpVisibility();
            ux.schedule.Execute(UpdateOversizedCameraHelpVisibility).Every(100);
            void UpdateOversizedCameraHelpVisibility() 
            {
                oversizedCameraHelp.SetVisible(false);
                if (Target == null)
                    return; // target deleted
                
                if (!Target.OversizeWindow.Enabled) 
                    oversizedCameraHelp.SetVisible(Target.IsCameraTooBigForTheConfiner(Target.VirtualCamera));
            }
            
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

            m_PipelineUtility.UpdateState();

            return ux;
        }

        static List<List<Vector2>> s_CurrentPathCache = new();

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineConfiner2D))]
        static void DrawConfinerGizmos(CinemachineConfiner2D confiner2D, GizmoType type)
        {
            if (!confiner2D.GetGizmoPaths(out var originalPath, ref s_CurrentPathCache, out var pathLocalToWorld))
                return;

            var inputColliderColor = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;
            var calculatedConfinerColor = 
                new Color(inputColliderColor.r, inputColliderColor.g, inputColliderColor.b, inputColliderColor.a / 2f);
            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = pathLocalToWorld;

            // Draw input confiner
            Gizmos.color = inputColliderColor;
            foreach (var path in originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            // Draw confiner for current camera size
            Gizmos.color = calculatedConfinerColor;
            foreach (var path in s_CurrentPathCache)
            {
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
}
#endif
