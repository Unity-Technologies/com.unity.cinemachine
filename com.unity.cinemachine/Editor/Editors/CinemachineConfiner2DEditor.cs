#if CINEMACHINE_PHYSICS_2D

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineConfiner2D))]
    [CanEditMultipleObjects]
    class CinemachineConfiner2DEditor : BaseEditor<CinemachineConfiner2D>
    {
        SerializedProperty m_MaxWindowSizeProperty;
        GUIContent m_ComputeSkeletonLabel = new(
            "Oversize Window", "If enabled, the confiner will compute a skeleton polygon to "
                + "support cases where camera window size is bigger than some regions of the "
                + "confining polygon.  Enable only if needed, because it's costly.");
        GUIContent m_MaxWindowSizeLabel;
        GUIContent m_InvalidateLensCacheLabel = new(
            "Invalidate Lens Cache", 
            "Invalidates the lens cache, so a new one is computed next frame.\n" +
            "Call this when when the Field of View or Orthographic Size changes.");
        GUIContent m_InvalidateBoundingShapeCacheLabel = new(
            "Invalidate Bounding Shape Cache", 
            "Forces a re-computation of the whole confiner2D cache next frame.  This recomputes:\n" +
            "- the bounding shape cache, and \n"+
            "- the lens cache.\n" +
            "Call this when the input bounding shape changes " +
            "(non-uniform scale, rotation, or points are moved, added or deleted).");

        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.MaxWindowSize));
        }

        void OnEnable()
        {
            m_MaxWindowSizeProperty = FindProperty(x => x.MaxWindowSize);
            m_MaxWindowSizeLabel = new GUIContent(
                m_MaxWindowSizeProperty.displayName, 
                "To optimize computation and memory costs, set this to the largest view size that the "
                + "camera is expected to have.  The confiner will not compute a polygon cache for frustum "
                + "sizes larger than this.  This refers to the size in world units of the frustum at the "
                + "confiner plane (for orthographic cameras, this is just the orthographic size).  If set "
                + "to 0, then this parameter is ignored and a polygon cache will be calculated for all "
                + "potential window sizes.");
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();

            CmPipelineComponentInspectorUtility.IMGUI_DrawMissingCmCameraHelpBox(this);

            if (Target.BoundingShape2D == null)
                EditorGUILayout.HelpBox("A Bounding Shape is required.", MessageType.Warning);
            else if (Target.BoundingShape2D.GetType() != typeof(PolygonCollider2D)
                && Target.BoundingShape2D.GetType() != typeof(CompositeCollider2D)
                && Target.BoundingShape2D.GetType() != typeof(BoxCollider2D))
            {
                EditorGUILayout.HelpBox(
                    "Must be a PolygonCollider2D, BoxCollider2D, or CompositeCollider2D.",
                    MessageType.Warning);
            }
            else if (Target.BoundingShape2D is CompositeCollider2D compositeCollider2D)
            {
                if (compositeCollider2D.geometryType != CompositeCollider2D.GeometryType.Polygons)
                {
                    EditorGUILayout.HelpBox(
                        "CompositeCollider2D geometry type must be Polygons",
                        MessageType.Warning);
                }
            }

#if false
            // Debugging info
            if (Target.GetGizmoPaths(out var originalPath, ref s_currentPathCache, out var pathLocalToWorld))
            {
                int pointCount0 = 0;
                foreach (var path in originalPath )
                    pointCount0 += path.Count;

                int pointCount1 = 1;
                foreach (var path in s_currentPathCache)
                    pointCount1 += path.Count;

                EditorGUILayout.HelpBox(
                    $"Original Path: {pointCount0} points in {originalPath.Count} paths\n"
                    + $"Confiner Path: {pointCount1} points in {s_currentPathCache.Count} paths",
                    MessageType.Info);
            }
#endif

            DrawRemainingPropertiesInInspector();

            float vSpace = EditorGUIUtility.standardVerticalSpacing;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float maxSize = m_MaxWindowSizeProperty.floatValue;
            bool computeSkeleton = maxSize >= 0;
            var rect = EditorGUILayout.GetControlRect(true, (lineHeight + vSpace) * (computeSkeleton ? 2 : 1));
            EditorGUI.BeginProperty(rect, m_ComputeSkeletonLabel, m_MaxWindowSizeProperty);
            {
                var r = rect; r.height = lineHeight;
                computeSkeleton = EditorGUI.Toggle(r, m_ComputeSkeletonLabel, maxSize >= 0);
                if (!computeSkeleton)
                    maxSize = -1;
                else
                {
                    r.y += lineHeight + vSpace;
                    maxSize = Mathf.Max(0, EditorGUI.FloatField(
                        r, m_MaxWindowSizeLabel, Mathf.Max(0, maxSize)));
                }
                m_MaxWindowSizeProperty.floatValue = maxSize;
                m_MaxWindowSizeProperty.serializedObject.ApplyModifiedProperties();
                EditorGUI.EndProperty();
            }

            rect = EditorGUILayout.GetControlRect(true);
            if (GUI.Button(rect, m_InvalidateLensCacheLabel))
            {
                Target.InvalidateLensCache();
                EditorUtility.SetDirty(Target);
            }
            rect = EditorGUILayout.GetControlRect(true);
            if (GUI.Button(rect, m_InvalidateBoundingShapeCacheLabel))
            {
                Target.InvalidateBoundingShapeCache();
                EditorUtility.SetDirty(Target);
            }

            bool timedOut = Target.ConfinerOvenTimedOut();
            if (computeSkeleton)
            {
                var progress = Target.BakeProgress();
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, 
                    timedOut ? "Timed out" : progress == 0 ? "" : progress < 1f ? "Baking" : "Baked");
                if (progress > 0 && progress < 1 && Event.current.type == EventType.Repaint)
                    EditorUtility.SetDirty(target);
            }
            
            if (timedOut)
            {
                EditorGUILayout.HelpBox(
                    "Polygon skeleton computation timed out.  Confiner result might be incomplete."
                    + "\n\nTo fix this, reduce the number of points in the confining shape, "
                    + "or set the MaxWindowSize parameter to limit skeleton computation.",
                    MessageType.Warning);
            }
        }

        static List<List<Vector2>> s_CurrentPathCache = new();

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineConfiner2D))]
        static void DrawConfinerGizmos(CinemachineConfiner2D confiner2D, GizmoType type)
        {
            if (!confiner2D.GetGizmoPaths(out var originalPath, ref s_CurrentPathCache, out var pathLocalToWorld))
                return;

            Color color = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;
            Color colorDimmed = new Color(color.r, color.g, color.b, color.a / 2f);
            
            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = pathLocalToWorld;

            // Draw input confiner
            Gizmos.color = color;
            foreach (var path in originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            // Draw confiner for current camera size
            Gizmos.color = colorDimmed;
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
