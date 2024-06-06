﻿using UnityEditor;
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
        readonly UndRedoMonitor m_UndoRedoMonitor = new ();

        /// <summary>
        /// This is needed to keep track of array values so that when they are changed by the user
        /// we can peek at the previous values and decide if we need to update the offset
        /// based on how the LookAt target changed.
        /// We also use it to select the appropriate item in the ListView when the user manipulates
        /// a data point in the scene view.
        /// </summary>
        class InspectorStateCache
        {
            readonly List<DataPoint<CinemachineSplineDollyLookAtTargets.Item>> m_DataPointsCache = new ();

            public int CurrentSelection { get; set; } = -1;

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
                m_DataPointsCache.Clear();
                for (int i = 0; i < splineData.Targets.Count; ++i)
                    m_DataPointsCache.Add(splineData.Targets[i]);
            }
        }

        // We keep the state cache in a static dictionary so that it can be accessed by the gizmo drawer
        static readonly Dictionary<CinemachineSplineDollyLookAtTargets, InspectorStateCache> s_CacheLookup = new ();
        static InspectorStateCache GetInspectorStateCache(CinemachineSplineDollyLookAtTargets splineData)
        {
            if (s_CacheLookup.TryGetValue(splineData, out var value))
                return value;
            value = new ();
            s_CacheLookup.Add(splineData, value);
            return value;
        }

        private void OnDisable() 
        {
            var t = target as CinemachineSplineDollyLookAtTargets;
            if (t != null && s_CacheLookup.ContainsKey(t))
                s_CacheLookup.Remove(t);
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
            var list = ux.AddChild(SplineDataInspectorUtility.CreateDataListField(
                splineData.Targets, targetsProp, 
                () => splineData.GetGetSplineAndDolly(out var spline, out _) ? spline : null,
                () =>
                {
                    // Create a default item for index 0
                    var item = new CinemachineSplineDollyLookAtTargets.Item();
                    item.LookAt = splineData.VirtualCamera.LookAt;
                    if (item.LookAt == null)
                    {
                        // No LookAt?  Find a point to look at near the spline
                        dolly.SplineSettings.GetCachedSpline().EvaluateSplineWithRoll(
                            spline.transform, 0, Quaternion.identity, null, out var pos, out var rot);
                        item.WorldLookAt = pos + rot * Vector3.right * 3;
                    }
                    return item;
                }));
           

            var arrayProp = targetsProp.FindPropertyRelative("m_DataPoints");
            list.makeItem = () => new BindableElement() { style = { marginRight = 4 }};
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
                var overlayLabel = new Label(indexProp.displayName) { tooltip = indexTooltip, style = { alignSelf = Align.Center }};
                overlayLabel.AddDelayedFriendlyPropertyDragger(indexProp, overlay, OnIndexDraggerCreated);
                    
                var foldout = new Foldout() { text = $"Target {index}" };
                foldout.BindProperty(element);
                var row = foldout.AddChild(new InspectorUtility.LabeledRow(indexProp.displayName, indexTooltip));
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
                    dragger.OnDragValueChangedFloat = (v) => BringCameraToCustomSplinePoint(splineData, v);
                }
            };

            list.TrackPropertyValue(arrayProp, (p) => EditorApplication.delayCall += () => GetInspectorStateCache(splineData).Reset(splineData));

            // When the list selection changes, cache the index and put the camera at that point on the dolly track
            list.selectedIndicesChanged += (indices) =>
            {
                var it = indices.GetEnumerator();
                var cache =  GetInspectorStateCache(splineData);
                cache.CurrentSelection = it.MoveNext() ? it.Current : -1;
                BringCameraToSplinePoint(splineData, cache.CurrentSelection);
            };

            LookAtDataOnSplineTool.s_OnDataLookAtDragged += OnToolDragged;
            LookAtDataOnSplineTool.s_OnDataIndexDragged += OnToolDragged;
            void OnToolDragged(CinemachineSplineDollyLookAtTargets data, int index)
            {
                EditorApplication.delayCall += () => 
                {
                    // GML This is a hack to avoid spurious exceptions thrown by uitoolkit!
                    // GML TODO: Remove when they fix it
                    try 
                    {
                        if (data == splineData)
                        {
                            list.selectedIndex = index;
                            BringCameraToSplinePoint(data, index);
                        }
                    }
                    catch {} // Ignore exceptions
                };
            }

            return ux;
        }

        static void BringCameraToSplinePoint(CinemachineSplineDollyLookAtTargets splineData, int index)
        {
            if (splineData != null && index >= 0)
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
                    if (inspectorCache != null && inspectorCache.CurrentSelection == i)
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
        public override GUIContent toolbarIcon => m_IconContent;

        public static Action<CinemachineSplineDollyLookAtTargets, int> s_OnDataIndexDragged;
        public static Action<CinemachineSplineDollyLookAtTargets, int> s_OnDataLookAtDragged;

        void OnEnable()
        {
            m_IconContent = new ()
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/CmSplineLookAtTargetsTool@256.png"),
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

            // Return the index that's being changed, or -1
            return hotIndex;

            // Local function
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
