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

            var toolHelp = ux.AddChild(new HelpBox(
                "Use the Scene View tool to Edit the LookAt targets on the spline", HelpBoxMessageType.Info));
            toolHelp.OnInitialGeometry(() =>
            {
                var icon = toolHelp.Q(className: "unity-help-box__icon");
                if (icon != null)
                {
                    icon.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>(LookAtDataOnSplineTool.IconPath);
                    icon.style.marginRight = 12;
                }
            });
            ux.AddSpace();

            ux.TrackAnyUserActivity(() =>
            {
                var haveSpline = splineData != null && splineData.GetGetSplineAndDolly(out _, out _);
                invalidHelp.SetVisible(!haveSpline);
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
                    var item = new CinemachineSplineDollyLookAtTargets.Item { LookAt = splineData.VirtualCamera.LookAt, Easing = 1 };
                    if (item.LookAt == null)
                    {
                        // No LookAt?  Find a point to look at near the spline
                        dolly.SplineSettings.GetCachedSpline().EvaluateSplineWithRoll(
                            spline.transform, 0, null, out var pos, out var rot);
                        item.WorldLookAt = pos + rot * Vector3.right * 3;
                    }
                    return item;
                }));

            var arrayProp = targetsProp.FindPropertyRelative("m_DataPoints");
            list.makeItem = () => 
            {
                var itemRootName = "ItemRoot";
                var row = new BindableElement() { name = itemRootName, style = { marginRight = 4 }};

                var overlay = new VisualElement () { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                var overlayLabel = overlay.AddChild(new Label("Index"));
                var indexField1 = overlay.AddChild(InspectorUtility.CreateDraggableField(
                    typeof(float), "m_Index", SplineDataInspectorUtility.ItemIndexTooltip, overlayLabel, out var dragger));
                indexField1.style.flexGrow = 1;
                indexField1.style.flexBasis = 50;
                indexField1.SafeSetIsDelayed();
                dragger.OnDragValueChangedFloat = (v) => BringCameraToCustomSplinePoint(splineData, v);
                dragger.OnStartDrag = (d) => list.selectedIndex = GetIndexInList(list, d.DragElement, itemRootName);

                CinemachineSplineDollyLookAtTargets.Item def = new ();
                var lookAtField1 = overlay.AddChild(new ObjectField 
                { 
                    bindingPath = "m_Value." + SerializedPropertyHelper.PropertyName(() => def.LookAt),
                    tooltip = SerializedPropertyHelper.PropertyTooltip(() => def.LookAt),
                    objectType = typeof(Transform),
                    style = { flexGrow = 4, flexBasis = 50, marginLeft = 6 }
                });
                    
                var foldout = new Foldout() { value = false, text = "Target" }; // do not bind to "m_Value" because it will mess up the binding for index
                var indexRow = foldout.AddChild(new InspectorUtility.LabeledRow("Index", SplineDataInspectorUtility.ItemIndexTooltip));
                var indexField2 = indexRow.Contents.AddChild(InspectorUtility.CreateDraggableField(
                    typeof(float), "m_Index", SplineDataInspectorUtility.ItemIndexTooltip, indexRow.Label, out dragger));
                indexField2.style.flexGrow = 1;
                indexField2.style.flexBasis = 50;
                indexField2.SafeSetIsDelayed();
                dragger.OnDragValueChangedFloat = (v) => BringCameraToCustomSplinePoint(splineData, v);
                dragger.OnStartDrag = (d) => list.selectedIndex = GetIndexInList(list, d.DragElement, itemRootName);

                var lookAtField2 = foldout.AddChild(new PropertyField { bindingPath = "m_Value." + SerializedPropertyHelper.PropertyName(() => def.LookAt) });

                foldout.Add(new PropertyField { bindingPath = "m_Value." + SerializedPropertyHelper.PropertyName(() => def.Offset) });
                foldout.Add(new PropertyField { bindingPath = "m_Value." + SerializedPropertyHelper.PropertyName(() => def.Easing) });

                var foldoutWithOverlay = row.AddChild(new InspectorUtility.FoldoutWithOverlay(
                    foldout, overlay, overlayLabel) { style = { marginLeft = 12 }});
                foldoutWithOverlay.OpenFoldout.name = foldoutWithOverlay.ClosedFoldout.name = "ItemFoldout";

                // When the LookAt is changed, we want to do a little processing to fix up the offset
                lookAtField1.RegisterValueChangedCallback((evt) => OnLookAtChanged(GetIndexInList(list, row, itemRootName)));

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
                
                void OnLookAtChanged(int index)
                {
                    // Don't mess with the offset if change was a result of undo/redo
                    if (m_UndoRedoMonitor.IsUndoRedo || index < 0 || index >= arrayProp.arraySize)
                        return;

                    var offsetProp = arrayProp.GetArrayElementAtIndex(index).FindPropertyRelative("m_Value.Offset");
                    var lookAtProp = arrayProp.GetArrayElementAtIndex(index).FindPropertyRelative("m_Value.LookAt");

                    // if lookAt target was set to null, preserve the worldspace location
                    if (GetInspectorStateCache(splineData).GetCachedValue(index, out var previous))
                    {
                        var newData = lookAtProp.objectReferenceValue;
                        if (newData == null && previous.Value.LookAt != null)
                            SetOffset(previous.Value.WorldLookAt);

                        // if lookAt target was changed, zero the offset
                        else if (newData != null && newData != previous.Value.LookAt)
                            SetOffset(Vector3.zero);

                        // local function
                        void SetOffset(Vector3 offset)
                        {
                            offsetProp.vector3Value = offset;
                            lookAtProp.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            };

            list.TrackPropertyWithInitialCallback(arrayProp, (p) => 
            {
                // Fix up the foldout names to reflect the index of the item
                int index = 0;
                list.Query("ItemFoldout").ForEach((e) => 
                {
                    if (e is Foldout f)
                        f.text = $"Target {index++ / 2}"; // because there are 2 foldouts for each item
                });
                // Reset the state cache after all processing is done
                EditorApplication.delayCall += () => GetInspectorStateCache(splineData).Reset(splineData);
            });

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
                    var position = spline.EvaluateSplinePosition(splineContainer.transform, t);
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

        public static string IconPath => $"{CinemachineSceneToolHelpers.IconPath}/CmSplineLookAtTargetsTool@256.png";

        void OnEnable()
        {
            m_IconContent = new ()
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath),
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

        int DrawIndexPointHandles(ISpline spline, CinemachineSplineDollyLookAtTargets splineData)
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

        int DrawDataPointHandles(ISpline spline, CinemachineSplineDollyLookAtTargets splineData)
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

        void DrawTooltip(ISpline spline, CinemachineSplineDollyLookAtTargets splineData, int index, bool useLookAt)
        {
            var dataPoint = splineData.Targets[index];
            var haveLookAt = dataPoint.Value.LookAt != null;
            var targetText = haveLookAt ? dataPoint.Value.LookAt.name : dataPoint.Value.WorldLookAt.ToString();
            if (haveLookAt && dataPoint.Value.Offset != Vector3.zero)
                targetText += $" + {dataPoint.Value.Offset}";
            var text = $"Target {index}\nIndex: {dataPoint.Index}\nLookAt: {targetText}";

            var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.Targets.PathIndexUnit);
            var p0 = spline.EvaluatePosition(t);
            var p1 = dataPoint.Value.WorldLookAt;
            CinemachineSceneToolHelpers.DrawLabel(useLookAt ? p1 : p0, text);

            // Highlight the view line
            Handles.DrawLine(p0, p1, Handles.lineThickness + 2);
        }
    }
}
