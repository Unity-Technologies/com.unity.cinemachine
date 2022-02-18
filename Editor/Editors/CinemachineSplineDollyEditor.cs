#if CINEMACHINE_UNITY_SPLINES
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        SerializedProperty m_CameraPosition, m_PositionUnits, m_SplineOffset, 
            m_CameraUp, m_DampingEnabled, m_Damping, 
            m_AngularDamping, m_AutoDollyEnabled, m_AutoDollyPositionOffset;
        GUIContent m_CameraPositionGUIContent, m_PositionUnitsGUIContent, m_SplineOffsetGUIContent, 
            m_CameraUpGUIContent, m_DampingEnabledGUIContent, m_DampingGUIContent, 
            m_AngularDampingGUIContent, m_AutoDollyEnabledGUIContent, m_AutoDollyPositionOffsetGUIContent;
        void OnEnable()
        {
            m_CameraPositionGUIContent = new GUIContent("Camera Position",
                (m_CameraPosition = serializedObject.FindProperty("m_CameraPosition")).tooltip);
            m_PositionUnitsGUIContent = new GUIContent("",
                (m_PositionUnits = serializedObject.FindProperty("m_PositionUnits")).tooltip);
            m_SplineOffsetGUIContent = new GUIContent("Offset", 
                (m_SplineOffset = serializedObject.FindProperty("m_SplineOffset")).tooltip);
            m_CameraUpGUIContent = new GUIContent("Camera Up", 
                (m_CameraUp = serializedObject.FindProperty("m_CameraUp")).tooltip);
            m_DampingEnabledGUIContent = new GUIContent("Damping", 
                (m_DampingEnabled = serializedObject.FindProperty("m_DampingEnabled")).tooltip);
            m_DampingGUIContent = new GUIContent("Positional", 
                (m_Damping = serializedObject.FindProperty("m_Damping")).tooltip);
            m_AngularDampingGUIContent = new GUIContent("Angular", 
                (m_AngularDamping = serializedObject.FindProperty("m_AngularDamping")).tooltip);
            m_AutoDollyEnabledGUIContent = new GUIContent("Auto Dolly", 
                (m_AutoDollyEnabled = serializedObject.FindProperty("m_AutoDolly.m_Enabled")).tooltip);
            m_AutoDollyPositionOffsetGUIContent = new GUIContent("Position Offset", 
                (m_AutoDollyPositionOffset = serializedObject.FindProperty("m_AutoDolly.m_PositionOffset")).tooltip);
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineSplineDolly).m_Spline == null;
            if (needWarning)
                EditorGUILayout.HelpBox("A Spline is required", MessageType.Warning);

            needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineSplineDolly).m_AutoDolly.m_Enabled 
                    && (targets[i] as CinemachineSplineDolly).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox("AutoDolly requires a Follow Target", MessageType.Warning);

            DrawRemainingPropertiesInInspector();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(m_CameraPosition, m_CameraPositionGUIContent);
                EditorGUILayout.PropertyField(m_PositionUnits, m_PositionUnitsGUIContent, GUILayout.MaxWidth(100));
            } 
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(m_SplineOffset, m_SplineOffsetGUIContent);
            EditorGUILayout.PropertyField(m_CameraUp, m_CameraUpGUIContent);
            
            EditorGUILayout.PropertyField(m_AutoDollyEnabled, m_AutoDollyEnabledGUIContent);
            if (m_AutoDollyEnabled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AutoDollyPositionOffset, m_AutoDollyPositionOffsetGUIContent);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(m_DampingEnabled, m_DampingEnabledGUIContent);
            if (m_DampingEnabled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Damping, m_DampingGUIContent);
                EditorGUILayout.PropertyField(m_AngularDamping, m_AngularDampingGUIContent);
                EditorGUI.indentLevel--;
            }
            
            serializedObject.ApplyModifiedProperties();
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
