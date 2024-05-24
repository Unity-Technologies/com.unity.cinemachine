using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDollyLookAtTargets))]
    class CinemachineLookAtDataOnSplineEditor : CinemachineComponentBaseEditor
    {
        class UndRedoMonitor
        {
            int m_LastUndoRedoFrame;
            public UndRedoMonitor() { Undo.undoRedoPerformed += () => m_LastUndoRedoFrame = Time.frameCount; }
            public bool IsUndoRedo => Time.frameCount <= m_LastUndoRedoFrame + 1;
        }
        UndRedoMonitor m_UndoRedoMonitor = new ();

        /// <summary>
        /// This is needed to keep track of array values so that when they are changed by the user
        /// we can peek at the previous values and decide if we need to update the offset
        /// based on how the LookAt target changed.
        /// We also use it to select the appropriate item in the ListView when the user manipulates
        /// a data point in the scene view.
        /// </summary>
        class InspectorStateCache
        {
            bool m_WasDragging = false;
            DataPoint<CinemachineSplineDollyLookAtTargets.Item> m_DraggedValue;
            List<DataPoint<CinemachineSplineDollyLookAtTargets.Item>> m_DataPointsCache = new ();

            public int CurrentSelection { get; set; } = -1;

            // Returns index of first changed item, and its previous value
            public int GetFirstChangedItem(
                CinemachineSplineDollyLookAtTargets splineData, 
                out DataPoint<CinemachineSplineDollyLookAtTargets.Item> previousValue)
            {
                if (m_WasDragging)
                {
                    previousValue = m_DraggedValue;
                    for (int i = 0; i < splineData.Targets.Count; ++i)
                        if (Equals(m_DraggedValue, splineData.Targets[i]))
                            return i;
                    return -1;
                }
                int count = Mathf.Min(splineData.Targets.Count, m_DataPointsCache.Count);
                for (int i = 0; i < count; ++i)
                {
                    if (!Equals(m_DataPointsCache[i], splineData.Targets[i]))
                    {
                        previousValue = m_DataPointsCache[i];
                        return i;
                    }
                }
                previousValue = default;
                return -1;

                static bool Equals(
                    DataPoint<CinemachineSplineDollyLookAtTargets.Item> a, 
                    DataPoint<CinemachineSplineDollyLookAtTargets.Item> b)
                {
                    return a.Index == b.Index && a.Value.LookAt == b.Value.LookAt 
                        && a.Value.Offset == b.Value.Offset && a.Value.Easing == b.Value.Easing;
                }
            }

            public bool GetCachedValue(int index, out DataPoint<CinemachineSplineDollyLookAtTargets.Item> value)
            {
                if (index >= 0 && index < m_DataPointsCache.Count)
                {
                    value = m_DataPointsCache[index];
                    return true;
                }
                value = default;
                return false;
            }

            public void Reset(CinemachineSplineDollyLookAtTargets splineData)
            {
                m_WasDragging = false;
                m_DataPointsCache.Clear();
                for (int i = 0; i < splineData.Targets.Count; ++i)
                    m_DataPointsCache.Add(splineData.Targets[i]);
            }

            public void SnapshotIndexDrag(CinemachineSplineDollyLookAtTargets splineData, int arrayIndex, float value)
            {
                if (arrayIndex >= 0 && arrayIndex < m_DataPointsCache.Count
                    && splineData != null && splineData.GetGetSplineAndDolly(out var spline, out var dolly))
                {
                    m_WasDragging = true;
                    m_DraggedValue = m_DataPointsCache[arrayIndex];
                    m_DraggedValue.Index = dolly.SplineSettings.GetCachedSpline().StandardizePosition(value, splineData.Targets.PathIndexUnit, out _);
                }
            }
        }

        static Dictionary<CinemachineSplineDollyLookAtTargets, InspectorStateCache> m_CacheLookup = new ();
        static InspectorStateCache GetInspectorStateCache(CinemachineSplineDollyLookAtTargets splineData) => m_CacheLookup[splineData];

        private void OnEnable()
        {
            LookAtDataOnSplineTool.s_OnDataLookAtDragged += BringCameraToSplinePoint;
            LookAtDataOnSplineTool.s_OnDataIndexDragged += BringCameraToSplinePoint;
            m_CacheLookup.Add(target as CinemachineSplineDollyLookAtTargets, new InspectorStateCache());
        }

        private void OnDisable()
        {
            LookAtDataOnSplineTool.s_OnDataLookAtDragged -= BringCameraToSplinePoint;
            LookAtDataOnSplineTool.s_OnDataIndexDragged -= BringCameraToSplinePoint;
            m_CacheLookup.Remove(target as CinemachineSplineDollyLookAtTargets);
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            var splineData = target as CinemachineSplineDollyLookAtTargets;
            splineData.GetGetSplineAndDolly(out var spline, out var dolly);
            this.AddMissingCmCameraHelpBox(ux);

            var invalidHelp = new HelpBox(
                "This component requires a CinemachineSplineDolly component referencing a nonempty Spline", 
                HelpBoxMessageType.Warning);
            ux.Add(invalidHelp);
            var toolButton = ux.AddChild(new Button(() => ToolManager.SetActiveTool(typeof(LookAtDataOnSplineTool))) 
                { text = "Edit Targets in Scene View" });
            ux.TrackAnyUserActivity(() =>
            {
                var haveSpline = splineData != null && splineData.GetGetSplineAndDolly(out _, out _);
                invalidHelp.SetVisible(!haveSpline);
                toolButton.SetEnabled(haveSpline);
            });

            var targetsProp = serializedObject.FindProperty(() => splineData.Targets);
            ux.Add(SplineDataInspectorUtility.CreatePathUnitField(targetsProp, () 
                => splineData.GetGetSplineAndDolly(out var spline, out _) ? spline : null));

            ux.AddHeader("Data Points");
            var listField = ux.AddChild(SplineDataInspectorUtility.CreateDataListField(
                splineData.Targets, targetsProp, () => splineData.GetGetSplineAndDolly(out var spline, out _) ? spline : null));

            var arrayProp = targetsProp.FindPropertyRelative("m_DataPoints");
            listField.OnInitialGeometry(() => 
            {
                var list = listField.Q<ListView>();
                list.makeItem = () => new BindableElement();
                list.bindItem = (ux, index) =>
                {
                    // Remove children - items get recycled
                    for (int i = ux.childCount - 1; i >= 0; --i)
                        ux.RemoveAt(i);

                    const string indexTooltip = "The position on the Spline at which this data point will take effect.  "
                        + "The value is interpreted according to the Index Unit setting.";

                    var element = index < arrayProp.arraySize ? arrayProp.GetArrayElementAtIndex(index) : null;
                    CinemachineSplineDollyLookAtTargets.Item def = new ();
                    var indexProp = element.FindPropertyRelative("m_Index");
                    var valueProp = element.FindPropertyRelative("m_Value");
                    var lookAtProp = valueProp.FindPropertyRelative(() => def.LookAt);
                    var offsetProp = valueProp.FindPropertyRelative(() => def.Offset);

                    var overlay = new VisualElement () { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                    var indexField1 = overlay.AddChild(new PropertyField(indexProp, "") { tooltip = indexTooltip, style = { flexGrow = 1, flexBasis = 50 }});
                    indexField1.OnInitialGeometry(() => indexField1.SafeSetIsDelayed());

                    var lookAtField1 = overlay.AddChild(new PropertyField(lookAtProp, "") { style = { flexGrow = 4, flexBasis = 50, marginLeft = 3 }});
                    var overlayLabel = new Label("Index") { tooltip = indexTooltip, style = { alignSelf = Align.Center }};
                    overlayLabel.AddDelayedFriendlyPropertyDragger(indexProp, overlay, OnIndexDraggerCreated);
                    
                    var foldout = new Foldout() { text = $"Target {index}" };
                    foldout.BindProperty(element);
                    var row = foldout.AddChild(new InspectorUtility.LabeledRow("Index", indexTooltip));
                    var indexField2 = row.Contents.AddChild(new PropertyField(indexProp, "") { style = { flexGrow = 1 }});
                    indexField2.OnInitialGeometry(() => indexField2.SafeSetIsDelayed());
                    row.Label.AddDelayedFriendlyPropertyDragger(indexProp, indexField2, OnIndexDraggerCreated);

                    var lookAtField2 = foldout.AddChild(new PropertyField(lookAtProp));
                    foldout.Add(new PropertyField(offsetProp));
                    foldout.Add(new PropertyField(valueProp.FindPropertyRelative(() => def.Easing)));

                    ux.Add(new InspectorUtility.FoldoutWithOverlay(foldout, overlay, overlayLabel) { style = { marginLeft = 12 }});

                    ux.TrackPropertyValue(lookAtProp, (p) => 
                    {
                        // Don't mess with the offset if change was a result of undo/redo
                        if (m_UndoRedoMonitor.IsUndoRedo)
                            return;

                        // if lookAt target was set to null, preserve the worldspace location
                        if (GetInspectorStateCache(splineData).GetCachedValue(index, out var previous))
                        {
                            var newData = p.objectReferenceValue;
                            if (newData == null && previous.Value.LookAt != null)
                                SetOffset(previous.Value.WorldLookAt);

                            // if lookAt target was changed, zero the offset
                            else if (newData != null && newData != previous.Value.LookAt)
                                SetOffset(Vector3.zero);

                            // local function
                            void SetOffset(Vector3 offset)
                            {
                                offsetProp.vector3Value = offset;
                                p.serializedObject.ApplyModifiedProperties();
                            }
                        }
                    });

                    ((BindableElement)ux).BindProperty(element); // bind must be done at the end

                    // local function
                    void OnIndexDraggerCreated(IDelayedFriendlyDragger dragger)
                    {
                        dragger.OnStartDrag = () => list.selectedIndex = index;
                        dragger.OnDragValueChangedFloat = (v) => 
                        {
                            GetInspectorStateCache(splineData).SnapshotIndexDrag(splineData, index, v);
                            BringCameraToCustomSplinePoint(splineData, v);
                        };
                    }
                };

                list.TrackPropertyValue(arrayProp, (p) => 
                {
                    var selectedIndex = GetInspectorStateCache(splineData).GetFirstChangedItem(splineData, out var previous);
                    if (selectedIndex >= 0)
                    {
                        list.selectedIndex = selectedIndex;
                        EditorApplication.delayCall += () => list.ScrollToItem(selectedIndex);
                    }
                    EditorApplication.delayCall += () => GetInspectorStateCache(splineData).Reset(splineData);
                });

                list.selectedIndicesChanged += (indices) =>
                {
                    var it = indices.GetEnumerator();
                    if (it.MoveNext())
                    {
                        GetInspectorStateCache(splineData).CurrentSelection = it.Current;
                        BringCameraToSplinePoint(splineData, it.Current);
                    }
                    else
                    {
                        GetInspectorStateCache(splineData).CurrentSelection = -1;
                    }
                };
            });

            return ux;
        }

        static void BringCameraToSplinePoint(CinemachineSplineDollyLookAtTargets splineData, int index)
        {
            if (splineData != null)
                BringCameraToCustomSplinePoint(splineData, splineData.Targets[index].Index);
        }

        static void BringCameraToCustomSplinePoint(CinemachineSplineDollyLookAtTargets splineData, float splineIndex)
        {
            if (splineData != null && splineData.GetGetSplineAndDolly(out var spline, out var dolly))
            {
                Undo.RecordObject(dolly, "Modifying CinemachineSplineDollyLookAtTargets values");
                dolly.CameraPosition = dolly.SplineSettings.GetCachedSpline().ConvertIndexUnit(
                    splineIndex, splineData.Targets.PathIndexUnit, dolly.PositionUnits);
                EditorApplication.delayCall += () => InspectorUtility.RepaintGameView();
            }
        }


        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineDollyLookAtTargets))]
        static void DrawGizmos(CinemachineSplineDollyLookAtTargets splineData, GizmoType selectionType)
        {
            // For performance reasons, we only draw a gizmo for the current active game object
            if (Selection.activeGameObject == splineData.gameObject && splineData.Targets.Count > 0
                && splineData.GetGetSplineAndDolly(out var splineContainer, out var dolly))
            {
                var spline = dolly.SplineSettings.GetCachedSpline();
                var c = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;
                if (ToolManager.activeToolType != typeof(LookAtDataOnSplineTool))
                    c.a = 0.5f;

                var inspectorCache = GetInspectorStateCache(splineData);
                var indexUnit = splineData.Targets.PathIndexUnit;
                for (int i = 0; i < splineData.Targets.Count; i++)
                {
                    var t = SplineUtility.GetNormalizedInterpolation(spline, splineData.Targets[i].Index, indexUnit);
                    spline.EvaluateSplinePosition(splineContainer.transform, t, out var position);
                    var p = splineData.Targets[i].Value.WorldLookAt;
                    if (inspectorCache.CurrentSelection == i)
                        Gizmos.color = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;
                    else
                        Gizmos.color = c;
                    Gizmos.DrawLine(position, p);
                    Gizmos.DrawSphere(p, HandleUtility.GetHandleSize(p) * 0.1f);
                }
            }
        }
    }

    [EditorTool("Spline Dolly LookAt Targets Tool", typeof(CinemachineSplineDollyLookAtTargets))]
    class LookAtDataOnSplineTool : EditorTool
    {
        GUIContent m_IconContent;

        public static Action<CinemachineSplineDollyLookAtTargets, int> s_OnDataIndexDragged;
        public static Action<CinemachineSplineDollyLookAtTargets, int> s_OnDataLookAtDragged;

        public override GUIContent toolbarIcon => m_IconContent;

        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmTrackLookAt@256.png"),
                text = "Spline Dolly LookAt Targets Tool",
                tooltip = "Assign LookAt targets to positions on the spline."
            };
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var splineData = target as CinemachineSplineDollyLookAtTargets;
            if (splineData == null || !splineData.GetGetSplineAndDolly(out var splineContainer, out var dolly))
                return;

            Undo.RecordObject(splineData, "Modifying CinemachineSplineDollyLookAtTargets values");
            using (new Handles.DrawingScope(Handles.selectedColor))
            {
                var spline = new NativeSpline(splineContainer.Spline, splineContainer.transform.localToWorldMatrix);
                int changedIndex = DrawDataPointHandles(spline, splineData);
                if (changedIndex >= 0)
                    s_OnDataLookAtDragged?.Invoke(splineData, changedIndex);

                changedIndex = DrawIndexPointHandles(spline, splineData);
                if (changedIndex >= 0)
                    s_OnDataIndexDragged?.Invoke(splineData, changedIndex);
            }
        }

        int DrawIndexPointHandles(NativeSpline spline, CinemachineSplineDollyLookAtTargets splineData)
        {
            int anchorId = GUIUtility.GetControlID(FocusType.Passive);

            spline.DataPointHandles(splineData.Targets);

            var nearestIndex = ControlIdToIndex(anchorId, HandleUtility.nearestControl, splineData.Targets.Count);
            var hotIndex = ControlIdToIndex(anchorId, GUIUtility.hotControl, splineData.Targets.Count);
            var tooltipIndex = hotIndex >= 0 ? hotIndex : nearestIndex;
            if (tooltipIndex >= 0)
                DrawTooltip(spline, splineData, tooltipIndex, false);

            return hotIndex;

            static int ControlIdToIndex(int anchorId, int controlId, int targetCount)
            {
                int index = controlId - anchorId - 2;
                return index >= 0 && index < targetCount ? index : -1;
            }
        }

        int DrawDataPointHandles(NativeSpline spline, CinemachineSplineDollyLookAtTargets splineData)
        {
            int changed = -1;
            for (var i = 0; i < splineData.Targets.Count; ++i)
            {
                var dataPoint = splineData.Targets[i];

                int anchorId0 = GUIUtility.GetControlID(FocusType.Passive);
                var newPos = Handles.PositionHandle(dataPoint.Value.WorldLookAt, Quaternion.identity);
                int anchorId1 = GUIUtility.GetControlID(FocusType.Passive);

                var nearestIndex = HandleUtility.nearestControl > anchorId0 && HandleUtility.nearestControl < anchorId1 ? i : -1;
                var hotIndex = GUIUtility.hotControl > anchorId0 && GUIUtility.hotControl < anchorId1 ? i : -1;
                var tooltipIndex = hotIndex >= 0 ? hotIndex : nearestIndex;
                if (tooltipIndex >= 0)
                    DrawTooltip(spline, splineData, tooltipIndex, true);

                if (newPos != dataPoint.Value.WorldLookAt)
                {
                    var item = dataPoint.Value;
                    item.WorldLookAt = newPos;
                    dataPoint.Value = item;
                    splineData.Targets[i] = dataPoint;
                    changed = i;
                }
            }
            return changed;
        }

        void DrawTooltip(NativeSpline spline, CinemachineSplineDollyLookAtTargets splineData, int index, bool useLookAt)
        {
            var dataPoint = splineData.Targets[index];
            var haveLookAt = dataPoint.Value.LookAt != null;
            var targetText = haveLookAt ? dataPoint.Value.LookAt.name : dataPoint.Value.WorldLookAt.ToString();
            if (haveLookAt && dataPoint.Value.Offset != Vector3.zero)
                targetText += $" + {dataPoint.Value.Offset}";
            var text = $"Target {index}\nIndex: {dataPoint.Index}\nLookAt: {targetText}";

            var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.Targets.PathIndexUnit);
            spline.Evaluate(t, out var p0, out _, out _);
            var p1 = dataPoint.Value.WorldLookAt;
            CinemachineSceneToolHelpers.DrawLabel(useLookAt ? p1 : p0, text);

            // Highlight the view line
            Handles.DrawLine(p0, p1, Handles.lineThickness + 2);
        }
    }
}
