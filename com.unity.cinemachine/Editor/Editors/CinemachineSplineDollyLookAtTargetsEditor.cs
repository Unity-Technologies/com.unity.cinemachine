using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor.UIElements;
using System;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDollyLookAtTargets))]
    class CinemachineLookAtDataOnSplineEditor : CinemachineComponentBaseEditor
    {
        private void OnEnable()
        {
            LookAtDataOnSplineTool.s_OnDataLookAtDragged += (spline, splineData, index) => 
                EditorApplication.delayCall += () => InspectorUtility.RepaintGameView();

            LookAtDataOnSplineTool.s_OnDataIndexDragged += (spline, splineData, index) =>
            {
                // Bring the camera to this point on the spline
                var dolly = splineData.GetComponent<CinemachineSplineDolly>();
                if (dolly != null)
                {
                    var dataPoint = splineData.Targets[index];
                    Undo.RecordObject(dolly, "Modifying CinemachineSplineDollyLookAtTargets values");
                    dolly.CameraPosition = spline.Spline.ConvertIndexUnit(dataPoint.Index, splineData.Targets.PathIndexUnit, dolly.PositionUnits);
                    EditorApplication.delayCall += () => InspectorUtility.RepaintGameView();
                }
            };
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);

            var splineData = target as CinemachineSplineDollyLookAtTargets;
            var invalidHelp = new HelpBox(
                "This component requires a CinemachineSplineDolly component referencing a nonempty Spline", 
                HelpBoxMessageType.Warning);
            ux.Add(invalidHelp);
            ux.TrackAnyUserActivity(() => invalidHelp.SetVisible(splineData != null && !splineData.GetTargets(out _, out _)));

            var targetsProp = serializedObject.FindProperty(() => splineData.Targets);
            ux.Add(new PropertyField(targetsProp.FindPropertyRelative("m_IndexUnit")) 
                { tooltip = "Defines how to interpret the Index field for each data point.  "
                    + "Knot is the recommended value because it remains robust if the spline points change." });

            ux.AddSpace();
            ux.Add(new Button(() => ToolManager.SetActiveTool(typeof(LookAtDataOnSplineTool))) 
                { text = "Edit Targets in Scene View" });

            ux.AddHeader("Targets");
            var dataPointsProp = targetsProp.FindPropertyRelative("m_DataPoints");
            var list = ux.AddChild(new PropertyField(dataPointsProp, "Targets") 
                { tooltip = "The list of LookAt target on the spline.  As the camera approaches these positions on the spline, "
                    + "the camera will look at the corresponding targets."});
            list.OnInitialGeometry(() => 
            {
                var listView = list.Q<ListView>();
                listView.reorderable = false;
                listView.showFoldoutHeader = false;
                listView.showBoundCollectionSize = false;
            });

            ux.TrackPropertyValue(dataPointsProp, (p) => 
            {
                if (p.arraySize > 1)
                {
                    // Hack to set dirty to force a reorder
                    var item = splineData.Targets[0];
                    splineData.Targets[0] = item;
                    splineData.Targets.SortIfNecessary();
                }
            });

            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineSplineDollyLookAtTargets))]
        static void DrawGizmos(CinemachineSplineDollyLookAtTargets splineData, GizmoType selectionType)
        {
            // For performance reasons, we only draw a gizmo for the current active game object
            if (Selection.activeGameObject == splineData.gameObject && splineData.Targets.Count > 0
                && splineData.GetTargets(out var spline, out _) && spline.Spline != null)
            {
                Gizmos.color = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;

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

    [CustomPropertyDrawer(typeof(DataPoint<CinemachineSplineDollyLookAtTargets.Item>))]
    class CinemachineLookAtDataOnSplineItemPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            const string indexTooltip = "The position on the Spline at which this data point will take effect.  "
                + "The value is interpreted according to the Index Unit setting.";

            CinemachineSplineDollyLookAtTargets.Item def = new ();
            var indexProp = property.FindPropertyRelative("m_Index");
            var valueProp = property.FindPropertyRelative("m_Value");
            var lookAtProp = valueProp.FindPropertyRelative(() => def.LookAt);
            var offsetProp = valueProp.FindPropertyRelative(() => def.Offset);

            var overlay = new VisualElement () { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
            var indexField1 = overlay.AddChild(new PropertyField(indexProp, "") { tooltip = indexTooltip, style = { flexGrow = 1, flexBasis = 50 }});
            indexField1.OnInitialGeometry(() => indexField1.SafeSetIsDelayed());

            overlay.Add(new PropertyField(lookAtProp, "") { style = { flexGrow = 4, flexBasis = 50, marginLeft = 3 }});
            var overlayLabel = new Label("Index") { tooltip = indexTooltip, style = { alignSelf = Align.Center }};
            overlayLabel.AddDelayedFriendlyPropertyDragger(indexProp, overlay, false);

            var foldout = new Foldout() { text = "Data Point" };
            foldout.BindProperty(property);
            var indexField2 = foldout.AddChild(new PropertyField(indexProp) { tooltip = indexTooltip });
            indexField2.OnInitialGeometry(() => indexField2.SafeSetIsDelayed());

            var row = foldout.AddChild(InspectorUtility.PropertyRow(lookAtProp, out _));
            row.Contents.Add(new Button(() => 
            {
                var previous = lookAtProp.objectReferenceValue as Transform;
                lookAtProp.objectReferenceValue = null;
                if (previous != null)
                    offsetProp.vector3Value = previous.TransformPoint(offsetProp.vector3Value);
                lookAtProp.serializedObject.ApplyModifiedProperties();
            }) { text = "Clear", tooltip = "Set the Target to None, and convert the offset to world space" });

            foldout.Add(new PropertyField(offsetProp));
            foldout.Add(new PropertyField(valueProp.FindPropertyRelative(() => def.Easing)));

            return new InspectorUtility.FoldoutWithOverlay(foldout, overlay, overlayLabel) { style = { marginLeft = 12 }};
        }
    }

    [EditorTool("Spline Dolly LookAt Targets Tool", typeof(CinemachineSplineDollyLookAtTargets))]
    class LookAtDataOnSplineTool : EditorTool
    {
        GUIContent m_IconContent;

        public static Action<SplineContainer, CinemachineSplineDollyLookAtTargets, int> s_OnDataIndexDragged;
        public static Action<SplineContainer, CinemachineSplineDollyLookAtTargets, int> s_OnDataLookAtDragged;

        public override GUIContent toolbarIcon => m_IconContent;

        bool GetTargets(out CinemachineSplineDollyLookAtTargets splineData, out SplineContainer spline, out CinemachineSplineDolly dolly)
        {
            splineData = target as CinemachineSplineDollyLookAtTargets;
            if (splineData != null && splineData.GetTargets(out spline, out dolly))
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
            if (!GetTargets(out var splineData, out var spline, out _))
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

            int nearestIndex = ControlIdToIndex(anchorId, HandleUtility.nearestControl, splineData.Targets.Count);
            if (nearestIndex >= 0)
                DrawTooltip(spline, splineData, nearestIndex);

            return ControlIdToIndex(anchorId, GUIUtility.hotControl, splineData.Targets.Count);

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
                var newPos = Handles.PositionHandle(dataPoint.Value.WorldLookAt, Quaternion.identity);
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

        void DrawTooltip(SplineContainer spline, CinemachineSplineDollyLookAtTargets splineData, int index)
        {
            var dataPoint = splineData.Targets[index];
            var targetText = dataPoint.Value.LookAt != null ? dataPoint.Value.LookAt.name : dataPoint.Value.WorldLookAt.ToString();
            var text = $"Index: {dataPoint.Index}\nLookAt: {targetText}";

            var t = SplineUtility.GetNormalizedInterpolation(spline.Spline, dataPoint.Index, splineData.Targets.PathIndexUnit);
            spline.Evaluate(t, out var position, out _, out _);
            CinemachineSceneToolHelpers.DrawLabel(position, text);
        }
    }
}
