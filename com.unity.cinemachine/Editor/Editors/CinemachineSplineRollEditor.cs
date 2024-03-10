using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineRoll))]
    [CanEditMultipleObjects]
    class CinemachineSplineRollEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);
            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineRoll))]
        static void DrawGizmos(CinemachineSplineRoll splineRoll, GizmoType selectionType)
        {
            // For performance reasons, we only draw a gizmo for the current active game object
            if (Selection.activeGameObject == splineRoll.gameObject)
            {
                DrawSplineGizmo(splineRoll, CinemachineSplineDollyPrefs.SplineRollColor.Value, 
                    CinemachineSplineDollyPrefs.SplineWidth.Value, CinemachineSplineDollyPrefs.SplineResolution.Value);
            }
        }

        static void DrawSplineGizmo(CinemachineSplineRoll splineRoll, Color pathColor, float width, int resolution)
        {
            var spline = splineRoll == null ? null : splineRoll.Container;
            if (spline == null || spline.Spline == null || spline.Spline.Count == 0)
                return;

            // Rebuild the cached mesh if necessary.  This can be expensive!
            if (SplineGizmoCache.Instance == null 
                || SplineGizmoCache.Instance.Mesh == null
                || SplineGizmoCache.Instance.Spline != spline.Spline
                || SplineGizmoCache.Instance.SplineData != splineRoll.Roll
                || SplineGizmoCache.Instance.Width != width
                || SplineGizmoCache.Instance.Resolution != resolution)
            {
                var numKnots = spline.Spline.Count;
                var numSteps = numKnots * resolution;
                var stepSize = 1.0f / numSteps;
                var halfWidth = width * 0.5f;

                // For efficiency, we create a mesh with the track and draw it in one shot
                spline.LocalEvaluateSplineWithRoll(splineRoll, Quaternion.identity, 0, out var p, out var q);
                var w = (q * Vector3.right) * halfWidth;

                var indices = new int[2 * 3 * numSteps];
                numSteps++; // ceil
                var vertices = new Vector3[2 * numSteps];
                var normals = new Vector3[vertices.Length];
                int vIndex = 0;
                vertices[vIndex] = p - w; normals[vIndex++] = Vector3.up;
                vertices[vIndex] = p + w; normals[vIndex++] = Vector3.up;

                int iIndex = 0;

                for (int i = 1; i < numSteps; ++i)
                {
                    var t = i * stepSize;
                    spline.LocalEvaluateSplineWithRoll(splineRoll, Quaternion.identity, t, out p, out q);
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

                var mesh = new Mesh();
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetIndices(indices, MeshTopology.Lines, 0);

                SplineGizmoCache.Instance = new SplineGizmoCache
                {
                    Mesh = mesh,
                    Spline = spline.Spline,
                    SplineData = splineRoll.Roll,
                    Width = width,
                    Resolution = resolution
                };
            }
            // Draw the path
            var colorOld = Gizmos.color;
            var matrixOld = Gizmos.matrix;
            Gizmos.matrix = spline.transform.localToWorldMatrix;
            Gizmos.color = pathColor;
            Gizmos.DrawWireMesh(SplineGizmoCache.Instance.Mesh);
            Gizmos.matrix =matrixOld;
            Gizmos.color = colorOld;
        }

        [InitializeOnLoad]
        class SplineGizmoCache
        {
            public Mesh Mesh;
            public SplineData<float> SplineData;
            public Spline Spline;
            public float Width;
            public int Resolution;

            public static SplineGizmoCache Instance;

            // Invalidate the cache whenever the cached spline's data changes
            static SplineGizmoCache()
            {
                Instance = null;
                EditorSplineUtility.AfterSplineWasModified -= OnSplineChanged;
                EditorSplineUtility.AfterSplineWasModified += OnSplineChanged;
                EditorSplineUtility.UnregisterSplineDataChanged<float>(OnSplineDataChanged);
                EditorSplineUtility.RegisterSplineDataChanged<float>(OnSplineDataChanged);
            }
            static void OnSplineChanged(Spline spline)
            {
                if (Instance != null && spline == Instance.Spline)
                    Instance = null;
            }
            static void OnSplineDataChanged(SplineData<float> data)
            {
                if (Instance != null && data == Instance.SplineData)
                    Instance = null;
            }
        }
    }
}
