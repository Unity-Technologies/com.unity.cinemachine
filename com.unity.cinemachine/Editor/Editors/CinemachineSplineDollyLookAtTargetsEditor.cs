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
        /// <summary>
        /// This is needed to keep track of array values so that when they are changed by the user
        /// we can peek at the previous values and decide if we need to update the offset
        /// based on how the LookAt target changed.
        /// We also use it to select the appropriate item in the ListView when the user manipulates
        /// a data point in the scene view.
        /// </summary>
        class ArrayValuesCache
        {
            public int LastUndoRedoFrame = -1;
            List<DataPoint<CinemachineSplineDollyLookAtTargets.Item>> m_DataPointsCache = new ();

            public ArrayValuesCache() { Undo.undoRedoPerformed += () => LastUndoRedoFrame = Time.frameCount; }

            // Returns index of first changed item, and its previous value
            public int GetFirstChangedItem(
                CinemachineSplineDollyLookAtTargets splineData, 
                out DataPoint<CinemachineSplineDollyLookAtTargets.Item> previousValue)
            {
                int count = Mathf.Min(splineData.Targets.Count, m_DataPointsCache.Count);
                for (int i = 0; i < count; ++i)
                {
                    var dataPoint = splineData.Targets[i];
                    if (m_DataPointsCache[i].Index != dataPoint.Index 
                        || m_DataPointsCache[i].Value.LookAt != dataPoint.Value.LookAt
                        || m_DataPointsCache[i].Value.Offset != dataPoint.Value.Offset)
                    {
                        previousValue = m_DataPointsCache[i];
                        m_DataPointsCache[i] = dataPoint;
                        return i;
                    }
                }
                previousValue = default;
                return -1;
            }

            public void Refresh(CinemachineSplineDollyLookAtTargets splineData)
            {
                m_DataPointsCache.Clear();
                for (int i = 0; i < splineData.Targets.Count; ++i)
                    m_DataPointsCache.Add(splineData.Targets[i]);
            }
        }
        ArrayValuesCache m_ArrayValuesCache = new ();

        private void OnEnable()
        {
            LookAtDataOnSplineTool.s_OnDataLookAtDragged += BringCameraToSplinePoint;
            LookAtDataOnSplineTool.s_OnDataIndexDragged += BringCameraToSplinePoint;
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
                    indexField1.RegisterValueChangeCallback((evt) => BringCameraToSplinePoint(spline, splineData, index)); // GML does nothing!  TODO: Fix this

                    var lookAtField1 = overlay.AddChild(new PropertyField(lookAtProp, "") { style = { flexGrow = 4, flexBasis = 50, marginLeft = 3 }});
                    var overlayLabel = new Label("Index") { tooltip = indexTooltip, style = { alignSelf = Align.Center }};
                    overlayLabel.AddDelayedFriendlyPropertyDragger(indexProp, overlay, false);

                    var foldout = new Foldout() { text = $"Target {index}" };
                    foldout.BindProperty(element);
                    var indexField2 = foldout.AddChild(new PropertyField(indexProp) { tooltip = indexTooltip });
                    indexField2.OnInitialGeometry(() => indexField2.SafeSetIsDelayed());
                    indexField2.RegisterValueChangeCallback((evt) => BringCameraToSplinePoint(spline, splineData, index)); // GML does nothing!  TODO: Fix this
                    var lookAtField2 = foldout.AddChild(new PropertyField(lookAtProp));
                    foldout.Add(new PropertyField(offsetProp));
                    foldout.Add(new PropertyField(valueProp.FindPropertyRelative(() => def.Easing)));

                    ux.Add(new InspectorUtility.FoldoutWithOverlay(foldout, overlay, overlayLabel) { style = { marginLeft = 12 }});

                    ((BindableElement)ux).BindProperty(element); // bind must be done at the end
                };

                list.TrackPropertyValue(arrayProp, (p) => 
                {
                    var selectedIndex = m_ArrayValuesCache.GetFirstChangedItem(splineData, out var previous);
                    if (selectedIndex >= 0)
                    {
                        list.selectedIndex = selectedIndex;
                        list.ScrollToItem(selectedIndex);

                        BringCameraToSplinePoint(spline, splineData, selectedIndex);

                        // Don't mess with the offset if change was a result of undo/redo
                        if (Time.frameCount <= m_ArrayValuesCache.LastUndoRedoFrame + 1)
                            return;

                        var newData = splineData.Targets[selectedIndex];

                        // if lookAt target was set to null, preserve the worldspace location
                        if (newData.Value.LookAt == null && previous.Value.LookAt != null)
                            SetOffset(previous.Value.WorldLookAt);

                        // if lookAt target was changed, zero the offset
                        else if (newData.Value.LookAt != null && newData.Value.LookAt != previous.Value.LookAt)
                            SetOffset(Vector3.zero);

                        // local function
                        void SetOffset(Vector3 offset)
                        {
                            Undo.RecordObject(splineData, "Modifying CinemachineSplineDollyLookAtTargets values");
                            var v = newData.Value;
                            v.Offset = offset;
                            newData.Value = v;
                            splineData.Targets.SetDataPoint(selectedIndex, newData);
                            p.serializedObject.Update();
                        }
                    }
                    EditorApplication.delayCall += () => m_ArrayValuesCache.Refresh(splineData);
                });
            });

            return ux;
        }

        static void BringCameraToSplinePoint(SplineContainer spline, CinemachineSplineDollyLookAtTargets splineData, int index)
        {
            if (splineData != null && splineData.TryGetComponent<CinemachineSplineDolly>(out var dolly))
            {
                var dataPoint = splineData.Targets[index];
                Undo.RecordObject(dolly, "Modifying CinemachineSplineDollyLookAtTargets values");
                dolly.CameraPosition = spline.Spline.ConvertIndexUnit(dataPoint.Index, splineData.Targets.PathIndexUnit, dolly.PositionUnits);
                EditorApplication.delayCall += () => InspectorUtility.RepaintGameView();
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineDollyLookAtTargets))]
        static void DrawGizmos(CinemachineSplineDollyLookAtTargets splineData, GizmoType selectionType)
        {
            // For performance reasons, we only draw a gizmo for the current active game object
            if (Selection.activeGameObject == splineData.gameObject && splineData.Targets.Count > 0
                && splineData.GetGetSplineAndDolly(out var spline, out _) && spline.Spline != null)
            {
                var c = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;
                if (ToolManager.activeToolType != typeof(LookAtDataOnSplineTool))
                    c.a = 0.5f;
                Gizmos.color = c;

                var indexUnit = splineData.Targets.PathIndexUnit;
                for (int i = 0; i < splineData.Targets.Count; i++)
                {
                    var t = SplineUtility.GetNormalizedInterpolation(spline.Spline, splineData.Targets[i].Index, indexUnit);
                    spline.Evaluate(t, out var position, out _, out _);
                    var p = splineData.Targets[i].Value.WorldLookAt;
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

        public static Action<SplineContainer, CinemachineSplineDollyLookAtTargets, int> s_OnDataIndexDragged;
        public static Action<SplineContainer, CinemachineSplineDollyLookAtTargets, int> s_OnDataLookAtDragged;

        public override GUIContent toolbarIcon => m_IconContent;

        bool GetGetSplineAndDolly(out CinemachineSplineDollyLookAtTargets splineData, out SplineContainer spline, out CinemachineSplineDolly dolly)
        {
            splineData = target as CinemachineSplineDollyLookAtTargets;
            if (splineData != null && splineData.GetGetSplineAndDolly(out spline, out dolly))
                return true;
            spline = null;
            dolly = null;
            return false;
        }

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
            if (!GetGetSplineAndDolly(out var splineData, out var spline, out _))
                return;

            Undo.RecordObject(splineData, "Modifying CinemachineSplineDollyLookAtTargets values");
            using (new Handles.DrawingScope(Handles.selectedColor))
            {
                int changed = DrawDataPointHandles(spline, splineData);
                if (changed >= 0)
                    s_OnDataLookAtDragged?.Invoke(spline, splineData, changed);

                changed = DrawIndexPointHandles(spline, splineData);
                if (changed >= 0)
                    s_OnDataIndexDragged?.Invoke(spline, splineData, changed);
            }
        }

        int DrawIndexPointHandles(SplineContainer spline, CinemachineSplineDollyLookAtTargets splineData)
        {
            int anchorId = GUIUtility.GetControlID(FocusType.Passive);
            var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);
            nativeSpline.DataPointHandles(splineData.Targets);

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

        int DrawDataPointHandles(SplineContainer spline, CinemachineSplineDollyLookAtTargets splineData)
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

        void DrawTooltip(SplineContainer spline, CinemachineSplineDollyLookAtTargets splineData, int index, bool useLookAt)
        {
            var dataPoint = splineData.Targets[index];
            var haveLookAt = dataPoint.Value.LookAt != null;
            var targetText = haveLookAt ? dataPoint.Value.LookAt.name : dataPoint.Value.WorldLookAt.ToString();
            if (haveLookAt && dataPoint.Value.Offset != Vector3.zero)
                targetText += $" + {dataPoint.Value.Offset}";
            var text = $"Target {index}\nIndex: {dataPoint.Index}\nLookAt: {targetText}";

            var t = SplineUtility.GetNormalizedInterpolation(spline.Spline, dataPoint.Index, splineData.Targets.PathIndexUnit);
            spline.Evaluate(t, out var p0, out _, out _);
            var p1 = dataPoint.Value.WorldLookAt;
            CinemachineSceneToolHelpers.DrawLabel(useLookAt ? p1 : (Vector3)p0, text);

            // Highlight the view line
            Handles.DrawLine(p0, p1, Handles.lineThickness + 2);
        }
    }
}
