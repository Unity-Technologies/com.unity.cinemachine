#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
    [CustomEditor(typeof(CinemachineConfiner2D))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineConfiner2DEditor : BaseEditor<CinemachineConfiner2D>
    {
        SerializedProperty m_MaxWindowSizeProperty;
        GUIContent m_ComputeSkeletonLabel = new GUIContent(
            "Oversize Window", "If enabled, confiner will compute a skeleton polygon to "
                + "support cases where camera window size is bigger than some regions of the "
                + "confining polygon.  Enable only if needed, because it's costly");
        GUIContent m_MaxWindowSizeLabel;
        GUIContent m_InvalidateCacheLabel = new GUIContent(
            "Invalidate Cache", "Force a recomputation of the oversize window cache");

        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_MaxWindowSize));
        }

        private void OnEnable()
        {
            m_MaxWindowSizeProperty = FindProperty(x => x.m_MaxWindowSize);
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

            if (computeSkeleton)
            {
                rect = EditorGUILayout.GetControlRect(true);
                if (GUI.Button(rect, m_InvalidateCacheLabel))
                {
                    Target.InvalidateCache();
                    EditorUtility.SetDirty(Target);
                }
            }
            if (Target.ConfinerOvenTimedOut())
            {
                EditorGUILayout.HelpBox(
                    "Polygon skeleton computation timed out.  Confiner result might be incomplete."
                    + "\n\nTo fix this, reduce the number of points in the confining shape, "
                    + "or set the MaxWindowSize parameter to limit skeleton computation.",
                    MessageType.Warning);
            }
        }

        private static List<List<Vector2>> s_currentPathCache = new List<List<Vector2>>();

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineConfiner2D))]
        private static void DrawConfinerGizmos(CinemachineConfiner2D confiner2D, GizmoType type)
        {
            if (!confiner2D.GetGizmoPaths(out var originalPath, ref s_currentPathCache, out var pathLocalToWorld))
                return;

            Color color = CinemachineSettings.CinemachineCoreSettings.BoundaryObjectGizmoColour;
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
            foreach (var path in s_currentPathCache)
            {
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
#endif
}
