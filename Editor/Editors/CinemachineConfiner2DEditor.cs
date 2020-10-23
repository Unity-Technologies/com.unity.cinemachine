#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;

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
            DrawRemainingPropertiesInInspector();
            if (GUILayout.Button("InvalidateCache"))
            {
                Target.InvalidatePathCache();
                EditorUtility.SetDirty(Target);
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineConfiner2D))]
        private static void DrawConfinerGizmos(CinemachineConfiner2D confiner2D, GizmoType type)
        {
            List<List<Vector2>> currentPath = new List<List<Vector2>>();
            confiner2D.GetCurrentPath(ref currentPath);
            List<List<Vector2>> originalPath = new List<List<Vector2>>();
            confiner2D.GetOriginalPath(ref originalPath);
            
            if (currentPath == null || originalPath == null) { return; }

            Quaternion rotation = Quaternion.identity;
            Vector3 offset = Vector3.zero, translation = Vector3.zero, scale = Vector3.one;
            confiner2D.GetPathDeltaTransformation(ref scale, ref rotation, ref translation, ref offset);

            Color color = CinemachineSettings.CinemachineCoreSettings.BoundaryObjectGizmoColour;
            Color colorDimmed = new Color(color.r, color.g, color.b, color.a / 2f);
            
            // Draw confiner for current camera size
            Gizmos.color = color;
            foreach (var path in currentPath)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        UnityVectorExtensions.ApplyTransformation(path[index], 
                            scale, rotation, translation) + offset,
                        UnityVectorExtensions.ApplyTransformation(path[(index + 1) % path.Count], 
                            scale, rotation, translation) + offset);
                }
            }

            // Draw input confiner
            Gizmos.color = colorDimmed;
            foreach (var path in originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                { 
                    Gizmos.DrawLine(
                        UnityVectorExtensions.ApplyTransformation(path[index], 
                            scale, rotation, translation) + offset,
                        UnityVectorExtensions.ApplyTransformation(path[(index + 1) % path.Count], 
                            scale, rotation, translation) + offset);
                }
            }
        }
    }
#endif
}
