using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using System;
using Cinemachine.Utility;
using UnityEngine.Assertions;
using System.Diagnostics;

namespace Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineSmoothPath))]
    internal sealed class CinemachineSmoothPathEditor : BaseEditor<CinemachineSmoothPath>
    {
        private ReorderableList mWaypointList;

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_Waypoints));
        }

        void OnEnable()
        {
            mWaypointList = null;
        }


        // ReSharper disable once UnusedMember.Global - magic method called when doing Frame Selected
        public bool HasFrameBounds()
        {
            return Target.m_Waypoints != null && Target.m_Waypoints.Length > 0;
        }

        // ReSharper disable once UnusedMember.Global - magic method called when doing Frame Selected
        public Bounds OnGetFrameBounds()
        {
            Vector3[] wp;
            int selected = mWaypointList == null ? -1 : mWaypointList.index;
            if (selected >= 0 && selected < Target.m_Waypoints.Length)
                wp = new Vector3[1] { Target.m_Waypoints[selected].position };
            else
                wp = Target.m_Waypoints.Select(p => p.position).ToArray();
            return GeometryUtility.CalculateBounds(wp, Target.transform.localToWorldMatrix);
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (mWaypointList == null)
                SetupWaypointList();

            if (mWaypointList.index >= mWaypointList.count)
                mWaypointList.index = mWaypointList.count - 1;

            // Ordinary properties
            DrawRemainingPropertiesInInspector();

            // Path length
            EditorGUILayout.LabelField("Path Length", Target.PathLength.ToString());

            // Waypoints
            EditorGUI.BeginChangeCheck();
            mWaypointList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void SetupWaypointList()
        {
            mWaypointList = new ReorderableList(
                    serializedObject, FindProperty(x => x.m_Waypoints),
                    true, true, true, true);

            mWaypointList.drawHeaderCallback = (Rect rect) =>
                { EditorGUI.LabelField(rect, "Waypoints"); };

            mWaypointList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                { DrawWaypointEditor(rect, index); };

            mWaypointList.onAddCallback = (ReorderableList l) =>
                { InsertWaypointAtIndex(l.index); };
        }

        void DrawWaypointEditor(Rect rect, int index)
        {
            // Needed for accessing string names of fields
            CinemachineSmoothPath.Waypoint def = new CinemachineSmoothPath.Waypoint();
            SerializedProperty element = mWaypointList.serializedProperty.GetArrayElementAtIndex(index);

            float hSpace = 3;
            rect.width -= hSpace; rect.y += 1;
            Vector2 numberDimension = GUI.skin.label.CalcSize(new GUIContent("999"));
            Rect r = new Rect(rect.position, numberDimension);
            if (GUI.Button(r, new GUIContent(index.ToString(), "Go to the waypoint in the scene view")))
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    mWaypointList.index = index;
                    SceneView.lastActiveSceneView.pivot = Target.EvaluatePosition(index);
                    SceneView.lastActiveSceneView.size = 4;
                    SceneView.lastActiveSceneView.Repaint();
                }
            }

            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2f;
            GUIContent rollLabel = new GUIContent("Roll");
            Vector2 labelDimension = GUI.skin.label.CalcSize(rollLabel);
            float rollWidth = labelDimension.x + floatFieldWidth;
            r.x += r.width + hSpace; r.width = rect.width - (r.width + hSpace + rollWidth) - (r.height + hSpace);
            EditorGUI.PropertyField(r, element.FindPropertyRelative(() => def.position), GUIContent.none);

            r.x += r.width + hSpace; r.width = rollWidth;
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelDimension.x;

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(r, element.FindPropertyRelative(() => def.roll), rollLabel);
            EditorGUIUtility.labelWidth = oldWidth;
            EditorGUI.indentLevel = indent;

            r.x += r.width + hSpace; r.height += 1; r.width = r.height;
            GUIContent setButtonContent = EditorGUIUtility.IconContent("d_RectTransform Icon");
            setButtonContent.tooltip = "Set to scene-view camera position";
            if (GUI.Button(r, setButtonContent, GUI.skin.label) && SceneView.lastActiveSceneView != null)
            {
                Undo.RecordObject(Target, "Set waypoint");
                CinemachineSmoothPath.Waypoint wp = Target.m_Waypoints[index];
                Vector3 pos = SceneView.lastActiveSceneView.camera.transform.position;
                wp.position = Target.transform.InverseTransformPoint(pos);
                Target.m_Waypoints[index] = wp;
            }
        }

        void InsertWaypointAtIndex(int indexA)
        {
            Vector3 pos = Vector3.right;
            float roll = 0;

            // Get new values from the current indexA (if any)
            int numWaypoints = Target.m_Waypoints.Length;
            if (indexA < 0)
                indexA = numWaypoints - 1;
            if (indexA >= 0)
            {
                int indexB = indexA + 1;
                if (Target.m_Looped && indexB >= numWaypoints)
                    indexB = 0;
                if (indexB >= numWaypoints)
                {
                    Vector3 delta = Vector3.right;
                    if (indexA > 0)
                        delta = Target.m_Waypoints[indexA].position - Target.m_Waypoints[indexA - 1].position;
                    pos = Target.m_Waypoints[indexA].position + delta;
                    roll = Target.m_Waypoints[indexA].roll;
                }
                else
                {
                    // Interpolate
                    pos = Target.transform.InverseTransformPoint(Target.EvaluatePosition(0.5f + indexA));
                    roll = Mathf.Lerp(Target.m_Waypoints[indexA].roll, Target.m_Waypoints[indexB].roll, 0.5f);
                }
            }
            Undo.RecordObject(Target, "Add waypoint");
            var wp = new CinemachineSmoothPath.Waypoint();
            wp.position = pos;
            wp.roll = roll;
            var list = new List<CinemachineSmoothPath.Waypoint>(Target.m_Waypoints);
            list.Insert(indexA + 1, wp);
            Target.m_Waypoints = list.ToArray();
            Target.InvalidateDistanceCache();
            InspectorUtility.RepaintGameView();
            mWaypointList.index = indexA + 1; // select it
        }

        void OnSceneGUI()
        {
            if (mWaypointList == null)
                SetupWaypointList();

            if (Tools.current == Tool.Move)
            {
                Color colorOld = Handles.color;
                var localToWorld = Target.transform.localToWorldMatrix;
                for (int i = 0; i < Target.m_Waypoints.Length; ++i)
                {
                    DrawSelectionHandle(i, localToWorld);
                    if (mWaypointList.index == i)
                        DrawPositionControl(i, localToWorld, Target.transform.rotation); // Waypoint is selected
                }
                Handles.color = colorOld;
            }
        }

        void DrawSelectionHandle(int i, Matrix4x4 localToWorld)
        {
            if (Event.current.button != 1)
            {
                Vector3 pos = localToWorld.MultiplyPoint(Target.m_Waypoints[i].position);
                float size = HandleUtility.GetHandleSize(pos) * 0.2f;
                Handles.color = Color.white;
                if (Handles.Button(pos, Quaternion.identity, size, size, Handles.SphereHandleCap)
                    && mWaypointList.index != i)
                {
                    mWaypointList.index = i;
                    InspectorUtility.RepaintGameView();
                }
                // Label it
                Handles.BeginGUI();
                Vector2 labelSize = new Vector2(
                        EditorGUIUtility.singleLineHeight * 2, EditorGUIUtility.singleLineHeight);
                Vector2 labelPos = HandleUtility.WorldToGUIPoint(pos);
                labelPos.y -= labelSize.y / 2;
                labelPos.x -= labelSize.x / 2;
                GUILayout.BeginArea(new Rect(labelPos, labelSize));
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.black;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label(new GUIContent(i.ToString(), "Waypoint " + i), style);
                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }

        void DrawPositionControl(int i, Matrix4x4 localToWorld, Quaternion localRotation)
        {
            CinemachineSmoothPath.Waypoint wp = Target.m_Waypoints[i];
            Vector3 pos = localToWorld.MultiplyPoint(wp.position);
            EditorGUI.BeginChangeCheck();
            Handles.color = Target.m_Appearance.pathColor;
            Quaternion rotation = (Tools.pivotRotation == PivotRotation.Local)
                ? localRotation : Quaternion.identity;
            float size = HandleUtility.GetHandleSize(pos) * 0.1f;
            Handles.SphereHandleCap(0, pos, rotation, size, EventType.Repaint);
            pos = Handles.PositionHandle(pos, rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Move Waypoint");
                wp.position = Matrix4x4.Inverse(localToWorld).MultiplyPoint(pos);
                Target.m_Waypoints[i] = wp;
                Target.InvalidateDistanceCache();
                InspectorUtility.RepaintGameView();
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
             | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSmoothPath))]
        static void DrawGizmos(CinemachineSmoothPath path, GizmoType selectionType)
        {
			int numSegments = path.m_Waypoints.Length - (path.Looped ? 0 : 1);

			// *******************************************************************************************************************************************
			SessionState.SetInt("CinemachineSplineSegmentCount", SessionState.GetInt("CinemachineSplineSegmentCount", 0) + numSegments);
			SessionState.SetInt("CinemachineSplinePointCount", SessionState.GetInt("CinemachineSplinePointCount", 0) + (numSegments * path.m_Resolution));
			Stopwatch stopwatch = Stopwatch.StartNew();
			// *******************************************************************************************************************************************

			var isActive = Selection.activeGameObject == path.gameObject;

			// CinemachinePathEditor.DrawPathGizmo(path, isActive ? path.m_Appearance.pathColor : path.m_Appearance.inactivePathColor, isActive);

			path.UpdateControlPoints();

			// Pre-calculate the bezier weights for each step along a segment
			float step = 1f / path.m_Resolution;
            Vector4[] bezierWeights = new Vector4[path.m_Resolution + 1];
            for(int stepNum = 0; stepNum <= path.m_Resolution; stepNum++)
			{
                float t = ((float)stepNum) / path.m_Resolution;
                float d = 1.0f - t;
                bezierWeights[stepNum] = new Vector4(d * d * d, 3f * d * d * t, 3f * d * t * t, t * t * t);
			}


            Vector3[] stepPoints = new Vector3[(path.m_Resolution * numSegments) + 1];

            // Process each segment of the path
            int stepPointIndex = 0;
            for (int startWaypointIndex = 0; startWaypointIndex < numSegments; startWaypointIndex++)
            {
                int nextWaypointIndex = (startWaypointIndex + 1) % path.m_Waypoints.Length;

                for (int stepNum = 0; stepNum < path.m_Resolution; stepNum++)
                {
                    stepPoints[stepPointIndex++] = (bezierWeights[stepNum].x * path.m_Waypoints[startWaypointIndex].position) +
                        (bezierWeights[stepNum].y * path.m_ControlPoints1[startWaypointIndex].position) +
                        (bezierWeights[stepNum].z * path.m_ControlPoints2[startWaypointIndex].position) +
                        (bezierWeights[stepNum].w * path.m_Waypoints[nextWaypointIndex].position);

                }
            }

            stepPoints[stepPointIndex] = path.Looped ? stepPoints[0] : path.m_Waypoints[path.m_Waypoints.Length - 1].position;

            // Put into world space
            path.transform.TransformPoints(new Span<Vector3>(stepPoints));

			Color colorOld = Gizmos.color;
			Gizmos.color = isActive ? path.m_Appearance.pathColor : path.m_Appearance.inactivePathColor;

			if (!isActive || path.m_Appearance.width == 0)
            {
				SessionState.SetInt("CinemachineSplineNumInactiveRenders", SessionState.GetInt("CinemachineSplineNumInactiveRenders", 0) + 1);  // ******************

                Gizmos.DrawLineStrip(new Span<Vector3>(stepPoints), false);

            }
            else
            {
				SessionState.SetInt("CinemachineSplineNumActiveRenders", SessionState.GetInt("CinemachineSplineNumActiveRenders", 0) + 1);  // ******************

                Quaternion transformRot = path.transform.rotation;
				Vector3 transformUp = transformRot * Vector3.up;

                float halfWidth = path.m_Appearance.width * 0.5f;
                Vector3 halfRight = Vector3.right * halfWidth;

				Vector3[] leftRailPoints = new Vector3[(path.m_Resolution * numSegments) + (path.Looped ? 0 : 1)];
				Vector3[] rightRailPoints = new Vector3[(path.m_Resolution * numSegments) + (path.Looped ? 0 : 1)];
				Vector3[] sleeperPoints = new Vector3[(path.m_Resolution * numSegments * 2) + (path.Looped ? 0 : 2)];

				stepPointIndex = 0;
                int sleeperPointIndex = 0;
				for (int startWaypointIndex = 0; startWaypointIndex < numSegments; startWaypointIndex++)
                {
                    int nextWaypointIndex = (startWaypointIndex + 1) % path.m_Waypoints.Length;

                    float roll = path.m_Waypoints[startWaypointIndex].roll;
                    Vector3 fwd = path.transform.TransformDirection(- 3.0f * path.m_Waypoints[startWaypointIndex].position + 3.0f * path.m_ControlPoints1[startWaypointIndex].position);
                    Vector3 sideVector = ((!fwd.AlmostZero()) ? Quaternion.LookRotation(fwd, transformUp) * CinemachineSmoothPath.RollAroundForward(roll) : transformRot) * halfRight;

                    // bool rollAlwaysZero = (path.m_Waypoints[startWaypointIndex].roll == 0.0f) && (path.m_ControlPoints1[startWaypointIndex].roll == 0.0f) && (path.m_ControlPoints2[startWaypointIndex].roll == 0.0f) && (path.m_Waypoints[nextWaypointIndex].roll == 0.0f);

                    int numSteps = (path.Looped ? path.m_Resolution : ((startWaypointIndex < numSegments - 1) ? path.m_Resolution : (path.m_Resolution + 1)));
                    for (int stepNum = 0; stepNum < numSteps; stepNum++)
                    {
                        Vector3 pointOnCurve = stepPoints[stepPointIndex];

                        roll = (bezierWeights[stepNum].x * path.m_Waypoints[startWaypointIndex].roll) +
					        (bezierWeights[stepNum].y * path.m_ControlPoints1[startWaypointIndex].roll) +
						    (bezierWeights[stepNum].z * path.m_ControlPoints2[startWaypointIndex].roll) +
						    (bezierWeights[stepNum].w * path.m_Waypoints[nextWaypointIndex].roll);

						float t = ((float)stepNum) / path.m_Resolution;

						fwd = SplineHelpers.BezierTangent3(t,
					        path.m_Waypoints[startWaypointIndex].position, path.m_ControlPoints1[startWaypointIndex].position,
                             path.m_ControlPoints2[startWaypointIndex].position, path.m_Waypoints[nextWaypointIndex].position);
                        fwd = path.transform.TransformDirection(fwd);

                        sideVector = ((!fwd.AlmostZero()) ? Quaternion.LookRotation(fwd, transformUp) * CinemachineSmoothPath.RollAroundForward(roll) : transformRot) * halfRight;

						leftRailPoints[stepPointIndex] = pointOnCurve - sideVector;
						rightRailPoints[stepPointIndex] = pointOnCurve + sideVector;
						stepPointIndex++;

						Vector3 elongatedSideVector = sideVector * 1.2f;
                        sleeperPoints[sleeperPointIndex] = pointOnCurve - elongatedSideVector;
                        sleeperPoints[sleeperPointIndex + 1] = pointOnCurve + elongatedSideVector;
                        sleeperPointIndex += 2;
                    }
				}
                Assert.AreEqual(sleeperPointIndex, sleeperPoints.Length);
				Assert.AreEqual(stepPointIndex, leftRailPoints.Length);

				Gizmos.DrawLineStrip(new Span<Vector3>(leftRailPoints), false);
				Gizmos.DrawLineStrip(new Span<Vector3>(rightRailPoints), false);
                Gizmos.DrawLineList(new Span<Vector3>(sleeperPoints));
            }

            int us10Taken = (int)(((double)stopwatch.ElapsedTicks) / Stopwatch.Frequency * 1000.0 * 100.0);
            SessionState.SetInt("CinemachineSplineRenderUs10", SessionState.GetInt("CinemachineSplineRenderUs10", 0) + us10Taken);

			Gizmos.color = colorOld;
		}
	}
}
