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

            if (Target.IsOverCachedMaxFrustumHeight())
            {
                EditorGUILayout.HelpBox(
                    "Camera window size is bigger than the maximum confinable size.  "
                    + "\n\nTo be fully confined, the camera window must be smaller "
                    + "than the value of Max Window Size (if nonzero), and also must fit completely "
                    + "inside the smallest region of the confining shape.",
                    MessageType.Warning);
            }
            
            if (GUILayout.Button("Invalidate Cache"))
            {
                Target.InvalidateCache();
                EditorUtility.SetDirty(Target);
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

            // Draw confiner for current camera size
            Gizmos.color = colorDimmed;
            foreach (var path in s_currentPathCache)
            {
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            // Draw input confiner
            Gizmos.color = color;
            foreach (var path in originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                    Gizmos.DrawLine(path[index], path[(index + 1) % path.Count]);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
#endif
}
