using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDolly))]
    [CanEditMultipleObjects]
    class CinemachineSplineDollyEditor : UnityEditor.Editor
    {
        CinemachineSplineDolly Target => target as CinemachineSplineDolly;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);
            var noSplineHelp = ux.AddChild(new HelpBox("A Spline is required.", HelpBoxMessageType.Warning));

            var splineProp = serializedObject.FindProperty(() => Target.Spline);
            ux.Add(new PropertyField(splineProp));

            var row = ux.AddChild(InspectorUtility.CreatePropertyRow(
                serializedObject.FindProperty(() => Target.CameraPosition), out _));
            row.Right.Add(new PropertyField(serializedObject.FindProperty(() => Target.PositionUnits), "") 
                { style = { flexGrow = 2, flexBasis = 0 }});

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SplineOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraUp)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AutomaticDolly)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));

            TrackSpline(splineProp);
            ux.TrackPropertyValue(splineProp, TrackSpline);
            void TrackSpline(SerializedProperty p)
            {
                bool noSpline = false;
                for (int i = 0; !noSpline && i < targets.Length; ++i)
                    noSpline = targets[i] != null && ((CinemachineSplineDolly)targets[i]).Spline == null;
                noSplineHelp.SetVisible(noSpline);
            }
            m_PipelineUtility.UpdateState();
            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineRoll))]
        static void DrawGizmos(CinemachineSplineRoll splineRoll, GizmoType selectionType)
        {
            if (Selection.activeGameObject != splineRoll.gameObject) return;
            
            DrawSplineGizmo(splineRoll, CinemachineSplineDollyPrefs.SplineRollColor.Value, 
                CinemachineSplineDollyPrefs.SplineWidth.Value, CinemachineSplineDollyPrefs.SplineResolution.Value);
        }

        static void DrawSplineGizmo(CinemachineSplineRoll splineRoll, Color pathColor, float width, int resolution)
        {
            var spline = splineRoll == null ? null : splineRoll.Container;
            if (spline == null || spline.Spline == null || spline.Spline.Count == 0)
                return;

            var numKnots = spline.Spline.Count;
            var numSteps = numKnots * resolution;
            var stepSize = 1.0f / numSteps;
            var halfWidth = width * 0.5f;

            // For efficiency, we create a mesh with the track and draw it in one shot
            spline.EvaluateSplineWithRoll(splineRoll, Quaternion.identity, 0, out var p, out var q);
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
                spline.EvaluateSplineWithRoll(splineRoll, Quaternion.identity, t, out p, out q);
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
