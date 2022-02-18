#if CINEMACHINE_UNITY_SPLINES
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDolly))]
    [CanEditMultipleObjects]
    sealed class CinemachineSplineDollyEditor : BaseEditor<CinemachineSplineDolly>
    {
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_CameraPosition));
            excluded.Add(FieldPath(x => x.m_PositionUnits));
            excluded.Add(FieldPath(x => x.m_SplineOffset));
            excluded.Add(FieldPath(x => x.m_CameraUp));
            excluded.Add(FieldPath(x => x.m_DampingEnabled));
            excluded.Add(FieldPath(x => x.m_Damping));
            excluded.Add(FieldPath(x => x.m_AngularDamping));
            excluded.Add(FieldPath(x => x.m_AutoDolly));
        }
        
        GUIContent m_CameraPositionGUIContent;
        GUIContent m_PositionUnitsGUIContent;
        GUIContent m_SplineOffsetGUIContent;
        GUIContent m_CameraUpGUIContent;
        GUIContent m_DampingEnabledGUIContent;
        GUIContent m_DampingGUIContent;
        GUIContent m_AngularDampingGUIContent;
        GUIContent m_AutoDollyEnabledGUIContent;
        GUIContent m_AutoDollyPositionOffsetGUIContent;
        void OnEnable()
        {
            //Assign it on enable
            m_CameraPositionGUIContent = new GUIContent("Camera Position", serializedObject.FindProperty("m_CameraPosition").tooltip);
            m_PositionUnitsGUIContent = new GUIContent("", serializedObject.FindProperty("m_PositionUnits").tooltip);
            m_SplineOffsetGUIContent = new GUIContent("Offset", serializedObject.FindProperty("m_SplineOffset").tooltip);
            m_CameraUpGUIContent = new GUIContent("Camera Up", serializedObject.FindProperty("m_CameraUp").tooltip);
            m_DampingEnabledGUIContent = new GUIContent("Damping", serializedObject.FindProperty("m_DampingEnabled").tooltip);
            m_DampingGUIContent = new GUIContent("Positional", serializedObject.FindProperty("m_Damping").tooltip);
            m_AngularDampingGUIContent = new GUIContent("Angular", serializedObject.FindProperty("m_AngularDamping").tooltip);
            m_AutoDollyEnabledGUIContent = new GUIContent("Auto Dolly", serializedObject.FindProperty("m_AutoDolly.m_Enabled").tooltip);
            m_AutoDollyPositionOffsetGUIContent = new GUIContent("Position Offset", serializedObject.FindProperty("m_AutoDolly.m_PositionOffset").tooltip);
        }

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

            var splineDolly = Target;
            EditorGUILayout.BeginHorizontal();
            splineDolly.m_CameraPosition = EditorGUILayout.FloatField(m_CameraPositionGUIContent, splineDolly.m_CameraPosition);
            splineDolly.m_PositionUnits = 
                (PathIndexUnit) EditorGUILayout.EnumPopup(m_PositionUnitsGUIContent, splineDolly.m_PositionUnits, GUILayout.MaxWidth(100));
            EditorGUILayout.EndHorizontal();
            splineDolly.m_SplineOffset = EditorGUILayout.Vector3Field(m_SplineOffsetGUIContent, splineDolly.m_SplineOffset);
            splineDolly.m_CameraUp = 
                (CinemachineSplineDolly.CameraUpMode) EditorGUILayout.EnumPopup(m_CameraUpGUIContent, splineDolly.m_CameraUp);
            splineDolly.m_AutoDolly.m_Enabled = EditorGUILayout.Toggle(m_AutoDollyEnabledGUIContent, splineDolly.m_AutoDolly.m_Enabled);
            if (splineDolly.m_AutoDolly.m_Enabled)
            {
                EditorGUI.indentLevel++;
                splineDolly.m_AutoDolly.m_PositionOffset = 
                    EditorGUILayout.FloatField(m_AutoDollyPositionOffsetGUIContent, splineDolly.m_AutoDolly.m_PositionOffset);
                EditorGUI.indentLevel--;
            }
            splineDolly.m_DampingEnabled = EditorGUILayout.Toggle(m_DampingEnabledGUIContent, splineDolly.m_DampingEnabled);
            if (splineDolly.m_DampingEnabled)
            {
                EditorGUI.indentLevel++;
                splineDolly.m_Damping = EditorGUILayout.Vector3Field(m_DampingGUIContent, splineDolly.m_Damping);
                splineDolly.m_AngularDamping = EditorGUILayout.FloatField(m_AngularDampingGUIContent, splineDolly.m_AngularDamping);
                EditorGUI.indentLevel--;
            }
            Undo.RecordObject(splineDolly, "Changed value on SplineDolly in inspector.");
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineRoll))]
        static void DrawGizmos(CinemachineSplineRoll splineRoll, GizmoType selectionType)
        {
            if (Selection.activeGameObject == splineRoll.gameObject)
                DrawSplineGizmo(splineRoll, Color.green, 1, 1000); // GML todo: make this configurable
        }

        static void DrawSplineGizmo(CinemachineSplineRoll splineRoll, Color pathColor, float width, int maxSteps)
        {
            var spline = splineRoll == null ? null : splineRoll.SplineContainer;
            if (spline == null || spline.Spline == null || spline.Spline.Count == 0)
                return;

            var length = spline.CalculateLength();
            var numSteps = Mathf.FloorToInt(Mathf.Clamp(length / width, 3, maxSteps));
            var stepSize = 1.0f / numSteps;
            var halfWidth = width * 0.5f;

            // For efficiency, we create a mesh with the track and draw it in one shot
            spline.EvaluateSplineWithRoll(splineRoll, 0, out var p, out var q);
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
