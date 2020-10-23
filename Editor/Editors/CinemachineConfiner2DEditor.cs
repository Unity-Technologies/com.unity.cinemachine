#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

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
            var currentPath = confiner2D.GetCurrentPath();
            if (currentPath == null || 
                confiner2D.m_shapeCache.m_originalPath == null)
            {
                return;
            }

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
                            confiner2D.m_shapeCache.m_scaleDelta, confiner2D.m_shapeCache.m_rotationDelta, 
                            confiner2D.m_shapeCache.m_positionDelta) + confiner2D.m_shapeCache.m_offset,
                        UnityVectorExtensions.ApplyTransformation(path[(index + 1) % path.Count],
                            confiner2D.m_shapeCache.m_scaleDelta, confiner2D.m_shapeCache.m_rotationDelta, 
                            confiner2D.m_shapeCache.m_positionDelta) + confiner2D.m_shapeCache.m_offset);
                }
            }

            // Draw input confiner
            Gizmos.color = colorDimmed;
            foreach (var path in confiner2D.m_shapeCache.m_originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                { 
                    Gizmos.DrawLine(
                        UnityVectorExtensions.ApplyTransformation(path[index], 
                            confiner2D.m_shapeCache.m_scaleDelta, confiner2D.m_shapeCache.m_rotationDelta, 
                            confiner2D.m_shapeCache.m_positionDelta) + confiner2D.m_shapeCache.m_offset,
                        UnityVectorExtensions.ApplyTransformation(path[(index + 1) % path.Count],
                            confiner2D.m_shapeCache.m_scaleDelta, confiner2D.m_shapeCache.m_rotationDelta, 
                            confiner2D.m_shapeCache.m_positionDelta) + confiner2D.m_shapeCache.m_offset);
                }
            }
        }
    }
#endif
}
