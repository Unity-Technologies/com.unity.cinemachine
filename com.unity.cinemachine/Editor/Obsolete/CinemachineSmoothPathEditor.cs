#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineSmoothPath))]
    class CinemachineSmoothPathEditor : BaseEditor<CinemachineSmoothPath>
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
            UpgradeManagerInspectorHelpers.IMGUI_DrawUpgradeControls(this, "Spline");

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
                        delta = Target.m_Waypoints[indexA].position - Target.m_Waypoints[indexA-1].position;
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
            var isActive = Selection.activeGameObject == path.gameObject;
            CinemachinePathEditor.DrawPathGizmo(
                path, isActive ? path.m_Appearance.pathColor : path.m_Appearance.inactivePathColor,
                isActive, LocalSpaceSamplePath);
        }

        static Vector4[] s_BezierWeightsCache;

        // Optimizer for gizmo drawing
        static int LocalSpaceSamplePath(CinemachinePathBase pathBase, int samplesPerSegment, Vector3[] positions, Quaternion[] rotations)
        {
            CinemachineSmoothPath path = pathBase as CinemachineSmoothPath;

            path.UpdateControlPoints();

            int numSegments = path.m_Waypoints.Length - (path.Looped ? 0 : 1);
            if (numSegments == 0)
                return 0;

            // Compute the bezier weights only once - this is shared by all segments
            if (s_BezierWeightsCache == null || s_BezierWeightsCache.Length < samplesPerSegment)
                s_BezierWeightsCache = new Vector4[samplesPerSegment];
            for (int i = 0; i < samplesPerSegment; ++i)
            {
                float t = ((float)i) / samplesPerSegment;
                float d = 1.0f - t;
                s_BezierWeightsCache[i] = new Vector4(d * d * d, 3f * d * d * t, 3f * d * t * t, t * t * t);
            }

            // Process the positions
            int numSamples = 0;
            for (int wp = 0; wp < numSegments && numSamples < positions.Length; ++wp)
            {
                int nextWp = (wp + 1) % path.m_Waypoints.Length;
                for (int i = 0; i < samplesPerSegment; ++i)
                {
                    if (numSamples >= positions.Length)
                        break;
                    positions[numSamples++]
                        = (s_BezierWeightsCache[i].x * path.m_Waypoints[wp].position)
                        + (s_BezierWeightsCache[i].y * path.m_ControlPoints1[wp].position)
                        + (s_BezierWeightsCache[i].z * path.m_ControlPoints2[wp].position)
                        + (s_BezierWeightsCache[i].w * path.m_Waypoints[nextWp].position);
                }
            }
            if (numSamples < positions.Length)
                positions[numSamples++] = path.Looped ? positions[0] : path.m_Waypoints[path.m_Waypoints.Length - 1].position;

            // Process rotations
            if (rotations != null && rotations.Length >= numSamples)
            {
                int numRotSamples = 0;
                for (int wp = 0; wp < numSegments && numRotSamples < numSamples; ++wp)
                {
                    int nextWp = (wp + 1) % path.m_Waypoints.Length;
                    SplineHelpers.BezierTangentWeights3(
                        path.m_Waypoints[wp].position, path.m_ControlPoints1[wp].position,
                        path.m_ControlPoints2[wp].position, path.m_Waypoints[nextWp].position,
                        out var w0, out var w1, out var w2);

                    for (int i = 0; i < samplesPerSegment; ++i)
                    {
                        if (numRotSamples >= numSamples)
                            break;
                        float roll
                            = (s_BezierWeightsCache[i].x * path.m_Waypoints[wp].roll)
                            + (s_BezierWeightsCache[i].y * path.m_ControlPoints1[wp].roll)
                            + (s_BezierWeightsCache[i].z * path.m_ControlPoints2[wp].roll)
                            + (s_BezierWeightsCache[i].w * path.m_Waypoints[nextWp].roll);

                        float t = ((float)i) / samplesPerSegment;

                        // From SplineHelpers.BezierTangent3()
                        var fwd = (w0 * (t * t)) + (w1 * t) + w2;
                        rotations[numRotSamples++] = Quaternion.LookRotation(fwd) * RollAroundForward(roll);
                    }
                }
                if (numRotSamples < numSamples)
                    rotations[numRotSamples++] = path.Looped ? rotations[0] : path.EvaluateLocalOrientation(path.MaxPos);
            }

            return numSamples;
        }

        // same as Quaternion.AngleAxis(roll, Vector3.forward), just simplified
        static Quaternion RollAroundForward(float angle)
        {
            float halfAngle = angle * 0.5F * Mathf.Deg2Rad;
            return new Quaternion(0, 0, Mathf.Sin(halfAngle), Mathf.Cos(halfAngle));
        }
    }
}
#endif
