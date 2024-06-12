using System;
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
            var splineData = target as CinemachineSplineRoll;

            var invalidHelp = new HelpBox(
                "This component should be associated with a non-empty spline", 
                HelpBoxMessageType.Warning);
            ux.Add(invalidHelp);
            var toolButton = ux.AddChild(new Button(() => ToolManager.SetActiveTool(typeof(SplineRollTool))) 
                { text = "Edit Data Points in Scene View" });

            ux.TrackAnyUserActivity(() =>
            {
                var haveSpline = splineData != null && splineData.SplineContainer != null;
                invalidHelp.SetVisible(!haveSpline);
                toolButton.SetEnabled(haveSpline);
            });

            var rollProp = serializedObject.FindProperty(() => splineData.Roll);
            ux.Add(SplineDataInspectorUtility.CreatePathUnitField(rollProp, () => splineData == null ? null : splineData.SplineContainer));

            ux.AddHeader("Data Points");
            var list = ux.AddChild(SplineDataInspectorUtility.CreateDataListField(splineData.Roll, rollProp, () => splineData?.SplineContainer));

            var arrayProp = rollProp.FindPropertyRelative("m_DataPoints");

            list.makeItem = () => 
            {
                var itemRootName = "ItemRoot";
                var row = new BindableElement() { name = itemRootName, style = { flexDirection = FlexDirection.Row, marginRight = 4 }};

                row.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { flexBasis = 12 }}); // pass-through for selecting row in list
                var indexField = row.AddChild(InspectorUtility.CreateDraggableField(
                    typeof(float), "m_Index", SplineDataInspectorUtility.ItemIndexTooltip, 
                        row.AddChild(new Label("Index")), out var dragger));
                indexField.style.flexGrow = 1;
                indexField.style.flexBasis = 50;
                indexField.SafeSetIsDelayed();
                dragger.OnStartDrag = (d) => list.selectedIndex = GetIndexInList(list, d.DragElement, itemRootName);

                var def = new CinemachineSplineRoll.RollData();
                var rollTooltip = SerializedPropertyHelper.PropertyTooltip(() => def.Value);
                row.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { flexBasis = 12 }}); // pass-through for selecting row in list
                var rollField = row.AddChild(InspectorUtility.CreateDraggableField(
                    typeof(float), "m_Value.Value", rollTooltip, row.AddChild(new Label("Roll")), out dragger));
                rollField.style.flexGrow = 1;
                rollField.style.flexBasis = 50;
                dragger.OnStartDrag = (d) => list.selectedIndex = GetIndexInList(list, d.DragElement, itemRootName);

                return row;

                // Sneaky way to find out which list element we are
                static int  GetIndexInList(ListView list, VisualElement element, string itemRootName)
                {
                    var container = list.Q("unity-content-container");
                    if (container != null)
                    {
                        while (element != null && element.name != itemRootName)
                            element = element.parent;
                        if (element != null)
                            return container.IndexOf(element);
                    }
                    return - 1;
                }

            };

            SplineRollTool.s_OnDataLookAtDragged += OnToolDragged;
            SplineRollTool.s_OnDataIndexDragged += OnToolDragged;
            void OnToolDragged(CinemachineSplineRoll data, int index)
            {
                EditorApplication.delayCall += () => 
                {
                    // This is a hack to avoid spurious exceptions thrown by uitoolkit!
                    // GML TODO: Remove when they fix it
                    try 
                    {
                        if (data == splineData)
                            list.selectedIndex = index;
                    }
                    catch {} // Ignore exceptions
                };
            }

            ux.TrackPropertyValue(rollProp, (p) => 
            {
                // Invalidate the mesh cache when the property changes (SetDirty() not always called!)
                SplineGizmoCache.Instance = null; 
                EditorApplication.delayCall += () => InspectorUtility.RepaintGameView();
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
            var splineContainer = splineRoll == null ? null : splineRoll.SplineContainer as SplineContainer;
            if (!splineContainer.IsValid())
                return;

            var transform = splineContainer.transform;
            var scale = transform.lossyScale;
            width = width * 3 / (scale.x + scale.y + scale.z);

            // Rebuild the cached mesh if necessary.  This can be expensive!
            if (SplineGizmoCache.Instance == null 
                || SplineGizmoCache.Instance.Mesh == null
                || SplineGizmoCache.Instance.Spline != splineContainer.Spline
                || SplineGizmoCache.Instance.RollData != splineRoll.Roll
                || SplineGizmoCache.Instance.Width != width
                || SplineGizmoCache.Instance.Resolution != resolution
                || SplineGizmoCache.Instance.Enabled != splineRoll.enabled)
            {
                var numKnots = splineContainer.Spline.Count;
                var numSteps = numKnots * resolution;
                var stepSize = 1.0f / numSteps;
                var halfWidth = width * 0.5f;

                // For efficiency, we create a mesh with the track and draw it in one shot
                var scaledSpline = new CachedScaledSpline(splineContainer.Spline, transform, Collections.Allocator.Temp);
                scaledSpline.LocalEvaluateSplineWithRoll(0, Quaternion.identity, splineRoll, out var p, out var q);
                var w = q * Vector3.right * halfWidth;

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
                    scaledSpline.LocalEvaluateSplineWithRoll(t, Quaternion.identity, splineRoll, out p, out q);
                    w = q * Vector3.right * halfWidth;

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
                    Spline = splineContainer.Spline,
                    RollData = splineRoll.Roll,
                    Width = width,
                    Resolution = resolution,
                    Enabled = splineRoll.enabled
                };
            }
            // Draw the path
            var colorOld = Gizmos.color;
            var matrixOld = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
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
            public bool Enabled;

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

    [EditorTool("Spline Roll Tool", typeof(CinemachineSplineRoll))]
    sealed class SplineRollTool : EditorTool
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;

        public static Action<CinemachineSplineRoll, int> s_OnDataIndexDragged;
        public static Action<CinemachineSplineRoll, int> s_OnDataLookAtDragged;

        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/CmSplineRollTool@256.png"),
                tooltip = "Adjust the roll data points along the spline"
            };
        }

        bool GetTargets(out CinemachineSplineRoll splineData, out SplineContainer spline)
        {
            splineData = target as CinemachineSplineRoll;
            if (splineData != null)
            {
                spline = splineData.SplineContainer as SplineContainer;
                return spline != null && spline.Spline != null;
            }
            spline = null;
            return false;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!GetTargets(out var splineData, out var spline))
                return;

            Undo.RecordObject(splineData, "Modifying Roll RollData");
            var color = Handles.selectedColor;
            using (new Handles.DrawingScope(color))
            {
                var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);

                int changedIndex = DrawIndexPointHandles(nativeSpline, splineData.Roll);
                if (changedIndex >= 0)
                    s_OnDataIndexDragged?.Invoke(splineData, changedIndex);
                changedIndex = DrawDataPointHandles(nativeSpline, splineData.Roll);
                if (changedIndex >= 0)
                    s_OnDataLookAtDragged?.Invoke(splineData, changedIndex);
            }
        }

        int DrawIndexPointHandles(NativeSpline spline, SplineData<CinemachineSplineRoll.RollData> splineData)
        {
            int anchorId = GUIUtility.GetControlID(FocusType.Passive);
            spline.DataPointHandles(splineData);
            int nearestIndex = ControlIdToIndex(anchorId, HandleUtility.nearestControl, splineData.Count);
            var hotIndex = ControlIdToIndex(anchorId, GUIUtility.hotControl, splineData.Count);
            var tooltipIndex = hotIndex >= 0 ? hotIndex : nearestIndex;
            if (tooltipIndex >= 0)
                DrawTooltip(spline, splineData, tooltipIndex);

            // Return the index that's being changed, or -1
            return hotIndex;

            // Local function
            static int ControlIdToIndex(int anchorId, int controlId, int targetCount)
            {
                int index = controlId - anchorId - 2;
                return index >= 0 && index < targetCount ? index : -1;
            }
        }
        
        // inverse pre-calculation optimization
        readonly Quaternion m_DefaultHandleOrientation = Quaternion.Euler(270, 0, 0);
        readonly Quaternion m_DefaultHandleOrientationInverse = Quaternion.Euler(90, 0, 0);

        int DrawDataPointHandles(NativeSpline spline, SplineData<CinemachineSplineRoll.RollData> splineData)
        {
            int changed = -1;
            int tooltipIndex = -1;
            for (var i = 0; i < splineData.Count; ++i)
            {
                var dataPoint = splineData[i];
                var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
                spline.Evaluate(t, out var position, out var tangent, out var up);

                var id = GUIUtility.GetControlID(FocusType.Passive);
                if (DrawDataPoint(id, position, tangent, up, dataPoint.Value, out var result))
                {
                    dataPoint.Value = result;
                    splineData.SetDataPoint(i, dataPoint);
                    changed = i;
                }
                if (tooltipIndex < 0 && id == HandleUtility.nearestControl || id == GUIUtility.hotControl)
                    tooltipIndex = i;
            }
            if (tooltipIndex >= 0)
                DrawTooltip(spline, splineData, tooltipIndex);
            return changed;

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

                    var handleSize = HandleUtility.GetHandleSize(Vector3.zero) / 2f;
                    if (Event.current.type == EventType.Repaint) 
                        Handles.ArrowHandleCap(-1, Vector3.zero, globalRot, handleSize, EventType.Repaint);
                    
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

        void DrawTooltip(NativeSpline spline, SplineData<CinemachineSplineRoll.RollData> splineData, int index)
        {
            var dataPoint = splineData[index];
            var text = $"Index: {dataPoint.Index}\nRoll: {dataPoint.Value.Value}";

            var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
            spline.Evaluate(t, out var position, out _, out _);
            CinemachineSceneToolHelpers.DrawLabel(position, text);
        }
    }
}
