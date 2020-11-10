using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplinePath))]
    internal sealed class CinemachineSplinePathEditor : BaseEditor<CinemachineSplinePath>
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
             | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplinePath))]
        static void DrawGizmos(CinemachineSplinePath path, GizmoType selectionType)
        {
            var isActive = Selection.activeGameObject == path.gameObject;
            if (isActive && path.m_RollAppearance)
            {
                Keyframe[] keys = path.m_Roll.keys;

                Gizmos.color = Color.red;
                foreach (var key in keys)
                {
                    Gizmos.DrawSphere(path.EvaluatePosition(key.time), 0.1f);
                }
            }

            CinemachinePathEditor.DrawPathGizmo(path,
                isActive && !path.m_RollAppearance ? path.m_Appearance.pathColor : path.m_Appearance.inactivePathColor, isActive, path.m_RollAppearance);
        }
        
    }
}
