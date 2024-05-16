using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineRoll))]
    class CinemachineSplineRollEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            ux.Add(new Button(() => ToolManager.SetActiveTool(typeof(SplineRollTool))) 
                { text = "Edit Data Points in Scene View" });
            ux.AddSpace();

            var splineData = target as CinemachineSplineRoll;
            var rollProp = serializedObject.FindProperty(() => splineData.Roll);
            ux.Add(new PropertyField(rollProp.FindPropertyRelative("m_IndexUnit")) 
                { tooltip = "Defines how to interpret the Index field for each data point.  "
                    + "Knot is the recommended value because it remains robust if the spline points change." });

            ux.AddHeader("Data Points");
            var dataPointsProp = rollProp.FindPropertyRelative("m_DataPoints");
            var list = ux.AddChild(new PropertyField(dataPointsProp));
            list.OnInitialGeometry(() => 
            {
                var listView = list.Q<ListView>();
                listView.reorderable = false;
                listView.showFoldoutHeader = false;
                listView.showBoundCollectionSize = false;
            });

            ux.TrackPropertyValue(dataPointsProp, (p) => 
            {
                // Invalidate the mesh cache when the property changes (SetDirty() not always called!)
                SplineGizmoCache.Instance = null; 
                EditorApplication.delayCall += () => InspectorUtility.RepaintGameView();

                if (p.arraySize > 1)
                {
                    // Hack to set dirty to force a reorder
                    var item = splineData.Roll[0];
                    splineData.Roll[0] = item;
                    splineData.Roll.SortIfNecessary();
                }
            });

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
            var spline = splineRoll == null ? null : splineRoll.Spline;
            if (spline == null || spline.Spline == null || spline.Spline.Count == 0)
                return;

            // Rebuild the cached mesh if necessary.  This can be expensive!
            if (SplineGizmoCache.Instance == null 
                || SplineGizmoCache.Instance.Mesh == null
                || SplineGizmoCache.Instance.Spline != spline.Spline
                || SplineGizmoCache.Instance.RollData != splineRoll.Roll
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
                    RollData = splineRoll.Roll,
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
            public SplineData<CinemachineSplineRoll.RollData> RollData;
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
                EditorSplineUtility.UnregisterSplineDataChanged<CinemachineSplineRoll.RollData>(OnSplineDataChanged);
                EditorSplineUtility.RegisterSplineDataChanged<CinemachineSplineRoll.RollData>(OnSplineDataChanged);
            }
            static void OnSplineChanged(Spline spline)
            {
                if (Instance != null && spline == Instance.Spline)
                    Instance = null;
            }
            static void OnSplineDataChanged(SplineData<CinemachineSplineRoll.RollData> data)
            {
                if (Instance != null && data == Instance.RollData)
                    Instance = null;
            }
        }
    }

    [CustomPropertyDrawer(typeof(DataPoint<CinemachineSplineRoll.RollData>))]
    class CinemachineLookAtDataOnSplineDataPointPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            const string indexTooltip = "The position on the Spline at which this data point will take effect.  "
                + "The value is interpreted according to the Index Unit setting.";

            var def = new CinemachineSplineRoll.RollData();
            var indexProp = property.FindPropertyRelative("m_Index");
            var valueProp = property.FindPropertyRelative("m_Value");

            var ux = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginLeft = 3 }};

            var label = ux.AddChild(new Label(indexProp.displayName) { tooltip = indexTooltip, style = { alignSelf = Align.Center }});
            var indexField = ux.AddChild(new PropertyField(indexProp, "") { style = { flexGrow = 1, flexBasis = 20 } });
            indexField.OnInitialGeometry(() => indexField.SafeSetIsDelayed());
            InspectorUtility.AddDelayedFriendlyPropertyDragger(label, indexProp, indexField, false);

            ux.Add(new InspectorUtility.CompactPropertyField(valueProp.FindPropertyRelative(() => def.Value)) { style = { marginLeft = 6 }});
            return ux;
        }
    }

    [EditorTool("Spline Roll Tool", typeof(CinemachineSplineRoll))]
    sealed class SplineRollTool : EditorTool, IDrawSelectedHandles
    {
        GUIContent m_IconContent;
        bool m_RollInUse;
        bool Active => ToolManager.IsActiveTool(this);

        public override GUIContent toolbarIcon => m_IconContent;

        bool GetTargets(out CinemachineSplineRoll splineData, out SplineContainer spline)
        {
            splineData = target as CinemachineSplineRoll;
            if (splineData != null)
            {
                spline = splineData.Spline;
                return spline != null && spline.Spline != null;
            }
            spline = null;
            return false;
        }

        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmTrack@256.png"),
                text = "Roll Tool",
                tooltip = "Adjust the roll data points along the spline."
            };
        }

        const float k_UnselectedAlpha = 0.3f;
        public override void OnToolGUI(EditorWindow window)
        {
            if (!GetTargets(out var splineData, out var spline))
                return;

            Undo.RecordObject(splineData, "Modifying Roll RollData");
            var color = Handles.selectedColor;
            if (!Active) 
                color.a = k_UnselectedAlpha;
            using (new Handles.DrawingScope(color))
            {
                var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);
                m_RollInUse = DrawRollDataPoints(nativeSpline, splineData.Roll, true);
                nativeSpline.DataPointHandles(splineData.Roll);
            }
        }

        public void OnDrawHandles()
        {
            if (Event.current.type != EventType.Repaint || Active || !GetTargets(out var splineData, out var spline))
                return;

            var color = Handles.selectedColor;
            color.a = k_UnselectedAlpha;
            using (new Handles.DrawingScope(color))
            {
                var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);
                DrawRollDataPoints(nativeSpline, splineData.Roll, false);
            }
        }
        
        // inverse pre-calculation optimization
        readonly Quaternion m_DefaultHandleOrientation = Quaternion.Euler(270, 0, 0);
        readonly Quaternion m_DefaultHandleOrientationInverse = Quaternion.Euler(90, 0, 0);
        bool DrawRollDataPoints(NativeSpline spline, SplineData<CinemachineSplineRoll.RollData> splineData, bool enabled)
        {
            var inUse = false;
            for (var r = 0; r < splineData.Count; ++r)
            {
                var dataPoint = splineData[r];
                var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
                spline.Evaluate(t, out var position, out var tangent, out var up);

                var id = enabled ? GUIUtility.GetControlID(FocusType.Passive) : -1;
                if (DrawDataPoint(id, position, tangent, up, dataPoint.Value, out var result))
                {
                    dataPoint.Value = result;
                    splineData.SetDataPoint(r, dataPoint);
                    inUse = true;
                }
            }
            return inUse;

            // local function
            bool DrawDataPoint(int controlID, Vector3 position, Vector3 tangent, Vector3 up, float rollData, out float result)
            {
                result = 0;
                if (tangent == Vector3.zero)
                    return false;

                var drawMatrix = Handles.matrix * Matrix4x4.TRS(position, Quaternion.LookRotation(tangent, up), Vector3.one);
                using (new Handles.DrawingScope(drawMatrix)) // use draw matrix, so we work in local space
                {
                    var localRot = Quaternion.Euler(0, rollData, 0);
                    var globalRot = m_DefaultHandleOrientation * localRot;

                    var handleSize = Mathf.Max(HandleUtility.GetHandleSize(Vector3.zero) / 2f, CinemachineSplineDollyPrefs.SplineWidth.Value);
                    if (Event.current.type == EventType.Repaint) 
                    {
                        using (new Handles.DrawingScope(m_RollInUse ? Handles.selectedColor : Handles.color)) 
                            Handles.ArrowHandleCap(-1, Vector3.zero, globalRot, handleSize, EventType.Repaint);
                    }
                    
                    var newGlobalRot = Handles.Disc(controlID, globalRot, Vector3.zero, Vector3.forward, handleSize, false, 0);
                    if (GUIUtility.hotControl == controlID)
                    {
                        // Handles.Disc returns roll values in the [0, 360] range. Therefore, it works only in fixed ranges
                        // For example, within any of these ..., [-720, -360], [-360, 0], [0, 360], [360, 720], ...
                        // But we want to be able to rotate through these ranges, and not get stuck. We can detect when to
                        // move between ranges: when the roll delta is big. e.g. 359 -> 1 (358), instead of 1 -> 2 (1)
                        var newLocalRot = m_DefaultHandleOrientationInverse * newGlobalRot;
                        var deltaRoll = newLocalRot.eulerAngles.y - localRot.eulerAngles.y;
                        if (deltaRoll > 180)
                            deltaRoll -= 360; // Roll down one range
                        else if (deltaRoll < -180) 
                            deltaRoll += 360; // Roll up one range

                        rollData += deltaRoll;
                        result = rollData;
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
