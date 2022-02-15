#if CINEMACHINE_UNITY_SPLINES
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDolly))]
    [CanEditMultipleObjects]
    sealed class CinemachineSplineDollyEditor : BaseEditor<CinemachineSplineDolly>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineSplineDolly).m_Spline == null;
            if (needWarning)
                EditorGUILayout.HelpBox("A Path is required", MessageType.Warning);

            needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineSplineDolly).m_AutoDolly.m_Enabled 
                    && (targets[i] as CinemachineSplineDolly).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox("AutoDolly requires a Follow Target", MessageType.Warning);

            DrawRemainingPropertiesInInspector();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineDolly))]
        static void DrawGizmos(CinemachineSplineDolly dolly, GizmoType selectionType)
        {
            if (Selection.activeGameObject == dolly.VirtualCamera.gameObject)
                DrawDollyGizmo(dolly, Color.green, 1); // GML todo: make this configurable
        }

        static void DrawDollyGizmo(CinemachineSplineDolly dolly, Color pathColor, float width)
        {
            var spline = dolly.m_Spline;
            if (dolly.m_Spline == null || spline.Spline == null || spline.Spline.Count == 0)
                return;

            var length = spline.CalculateLength();
            var numSteps = (int)Mathf.Round(Mathf.Clamp(length / width, 3, 1000));
            var stepSize = 1.0f / numSteps;
            var halfWidth = width * 0.5f;

            // For efficiency, we create a mesh with the track and draw it in one shot
            dolly.EvaluateSpline(0, out var p, out var q);
            var w = (q * Vector3.right) * halfWidth;

            var vertices = new Vector3[2 * numSteps];
            var normals = new Vector3[vertices.Length];
            int vIndex = 0;
            vertices[vIndex] = p - w; normals[vIndex++] = Vector3.up;
            vertices[vIndex] = p + w; normals[vIndex++] = Vector3.up;

            var indices = new int[2 * 3 * numSteps];
            int iIndex = 0;

            for (int i = 1; i < numSteps; ++i)
            {
                var t = i * stepSize;
                dolly.EvaluateSpline(t, out p, out q);
                w = (q * Vector3.right) * halfWidth;

                indices[iIndex++] = vIndex - 2;
                indices[iIndex++] = vIndex - 1;

                vertices[vIndex] = p - w; normals[vIndex++] = Vector3.up;
                vertices[vIndex] = p + w; normals[vIndex++] = Vector3.up;

                indices[iIndex++] = vIndex - 4;
                indices[iIndex++] = vIndex - 2;
                indices[iIndex++] = vIndex - 3;
                indices[iIndex++] = vIndex - 1;
            }

            // Draw the path
            Color colorOld = Gizmos.color;
            Gizmos.color = pathColor;

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            Gizmos.DrawWireMesh(mesh);

            Gizmos.color = colorOld;
        }
    }
}
#endif
