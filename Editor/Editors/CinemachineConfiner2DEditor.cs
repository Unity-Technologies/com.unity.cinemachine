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
            if (Target.IsOverMaxOrthosize())
            {
                EditorGUILayout.HelpBox(
                    "Camera window size is bigger than the maximum window size calculated by Confiner2D!",
                    MessageType.Warning);
            }
            
            DrawRemainingPropertiesInInspector();
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
