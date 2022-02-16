#if CINEMACHINE_UNITY_SPLINES
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineRoll))]
    [CanEditMultipleObjects]
    sealed class CinemachineSplineRollEditor : BaseEditor<CinemachineSplineRoll>
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineRoll))]
        static void DrawGizmos(CinemachineSplineRoll splineRoll, GizmoType selectionType)
        {
            if (Selection.activeGameObject == splineRoll.gameObject)
                DrawSplineGizmo(splineRoll, Color.green, 1, 1000); // GML todo: make this configurable
        }

        static void DrawSplineGizmo(CinemachineSplineRoll splineRoll, Color pathColor, float width, int maxSteps)
        {
            var spline = splineRoll.SplineContainer;
            if (spline == null || spline.Spline == null || spline.Spline.Count == 0)
                return;

            var length = spline.CalculateLength();
            var numSteps = Mathf.RoundToInt(Mathf.Clamp(length / width, 3, maxSteps));
            var stepSize = 1.0f / numSteps;
            var halfWidth = width * 0.5f;

            // For efficiency, we create a mesh with the track and draw it in one shot
            spline.EvaluateSplineWithRoll(splineRoll, 0, out var p, out var q);
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
                spline.EvaluateSplineWithRoll(splineRoll, t, out p, out q);
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
