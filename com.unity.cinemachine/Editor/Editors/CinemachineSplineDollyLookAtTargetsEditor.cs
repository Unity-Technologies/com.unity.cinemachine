using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDollyLookAtTargets))]
    [CanEditMultipleObjects]
    class CinemachineLookAtDataOnSplineEditor : CinemachineComponentBaseEditor
    {
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

            ux.Add(new Button(() => ToolManager.SetActiveTool(typeof(LookAtDataOnSplineTool))) 
                { text = "Edit Data Points in Scene View" });
            ux.AddSpace();

            var property = serializedObject.FindProperty(() => splineData.Targets);
            ux.Add(new PropertyField(property.FindPropertyRelative("m_IndexUnit")) 
                { tooltip = "Defines how to interpret the Index field for each data point.  "
                    + "Knot is the recommended value because it remains robust if the spline points change." });

            ux.Add(new PropertyField(property.FindPropertyRelative("m_DataPoints")) 
                { tooltip = "The list of markup points on the spline.  As the camera approaches these points on the spline, "
                    + "the corresponding LookAt points will come into effect."});

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

#if false // Enable this for debugging easing
                    if (i > 0)
                    {
                        var oldColor = Gizmos.color;
                        Gizmos.color = Color.white;
                        var it = new CinemachineSplineDollyLookAtTargets.LerpItem();
                        for (float j = 0; j < 1f; j += 0.05f)
                        {
                            var item = it.Interpolate(splineData.LookAtData[i-1].Value, splineData.LookAtData[i].Value, j);
                            Gizmos.DrawLine(p, item.WorldLookAt);
                            p = item.WorldLookAt;
                            Gizmos.DrawSphere(p, HandleUtility.GetHandleSize(p) * 0.05f);
                        }
                        Gizmos.color = oldColor;
                    }
#endif
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
            overlay.Add(new PropertyField(indexProp, "") { tooltip = indexTooltip, style = { flexGrow = 1, flexBasis = 50 }});
            overlay.Add(new PropertyField(lookAtProp, "") { style = { flexGrow = 4, flexBasis = 50, marginLeft = 3 }});
            var overlayLabel = new Label("Index") { tooltip = indexTooltip, style = { alignSelf = Align.Center }};
            overlayLabel.AddDelayedFriendlyPropertyDragger(indexProp, overlay);

            var foldout = new Foldout() { text = "Target" };
            foldout.BindProperty(property);
            foldout.Add(new PropertyField(indexProp) { tooltip = indexTooltip });

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

            return new InspectorUtility.FoldoutWithOverlay(foldout, overlay, overlayLabel);
        }
    }

    [EditorTool("Spline Dolly LookAt Targets Tool", typeof(CinemachineSplineDollyLookAtTargets))]
    class LookAtDataOnSplineTool : EditorTool
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;

        bool GetTargets(out CinemachineSplineDollyLookAtTargets splineDataTarget, out SplineContainer spline, out CinemachineSplineDolly dolly)
        {
            splineDataTarget = target as CinemachineSplineDollyLookAtTargets;
            if (splineDataTarget != null && splineDataTarget.GetTargets(out spline, out dolly))
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
            if (!GetTargets(out var splineDataTarget, out var spline, out _))
                return;

            Undo.RecordObject(splineDataTarget, "Modifying CinemachineSplineDollyLookAtTargets values");
            using (new Handles.DrawingScope(Handles.selectedColor))
            {
                DrawDataPoints(splineDataTarget.Targets);
                var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);
                nativeSpline.DataPointHandles(splineDataTarget.Targets);
            }
        }

        void DrawDataPoints(SplineData<CinemachineSplineDollyLookAtTargets.Item> splineData)
        {
            for (var r = 0; r < splineData.Count; ++r)
            {
                var dataPoint = splineData[r];
                var newPos = Handles.PositionHandle(dataPoint.Value.WorldLookAt, Quaternion.identity);
                if (newPos != dataPoint.Value.WorldLookAt)
                {
                    var item = dataPoint.Value;
                    item.WorldLookAt = newPos;
                    dataPoint.Value = item;
                    splineData[r] = dataPoint;
                }
            }
        }
    }
}
