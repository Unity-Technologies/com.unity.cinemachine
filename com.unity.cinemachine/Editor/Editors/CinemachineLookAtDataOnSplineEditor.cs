using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineLookAtDataOnSpline))]
    [CanEditMultipleObjects]
    class CinemachineLookAtDataOnSplineEditor : CinemachineComponentBaseEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);

            var splineData = target as CinemachineLookAtDataOnSpline;
            var invalidHelp = new HelpBox(
                "This component requires a CinemachineSplineDolly component referencing a nonempty Spline", 
                HelpBoxMessageType.Warning);
            ux.Add(invalidHelp);
            ux.TrackAnyUserActivity(() => invalidHelp.SetVisible(splineData != null && !splineData.GetTargets(out _, out _)));

            ux.Add(new Button(() => ToolManager.SetActiveTool(typeof(LookAtDataOnSplineTool))) 
                { text = "Edit Data Points in Scene View" });
            ux.AddSpace();

            var property = serializedObject.FindProperty(() => splineData.LookAtData);
            ux.Add(new PropertyField(property.FindPropertyRelative("m_IndexUnit")) 
                { tooltip = "Defines how to interpret the Index field for each data point.  "
                    + "Knot is the recommended value because it remains robust if the spline points change." });

            ux.Add(new PropertyField(property.FindPropertyRelative("m_DataPoints")) 
                { tooltip = "The list of markup points on the spline.  As the camera approaches these points on the spline, "
                    + "the corresponding LookAt points will come into effect."});

            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
                | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineLookAtDataOnSpline))]
        static void DrawGizmos(CinemachineLookAtDataOnSpline splineData, GizmoType selectionType)
        {
            // For performance reasons, we only draw a gizmo for the current active game object
            if (Selection.activeGameObject == splineData.gameObject && splineData.LookAtData.Count > 0
                && splineData.GetTargets(out var spline, out _) && spline.Spline != null)
            {
                Gizmos.color = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;

                var indexUnit = splineData.LookAtData.PathIndexUnit;
                for (int i = 0; i < splineData.LookAtData.Count; i++)
                {
                    var t = SplineUtility.GetNormalizedInterpolation(spline.Spline, splineData.LookAtData[i].Index, indexUnit);
                    spline.Evaluate(t, out var position, out _, out _);
                    var p = splineData.LookAtData[i].Value.WorldLookAt;
                    Gizmos.DrawLine(position, p);
                    Gizmos.DrawSphere(p, HandleUtility.GetHandleSize(p) * 0.1f);

#if false // Enable this for debugging easing
                    if (i > 0)
                    {
                        var oldColor = Gizmos.color;
                        Gizmos.color = Color.white;
                        var it = new CinemachineLookAtDataOnSpline.LerpItem();
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

    [CustomPropertyDrawer(typeof(DataPoint<CinemachineLookAtDataOnSpline.Item>))]
    class CinemachineLookAtDataOnSplineItemPropertyDrawer : PropertyDrawer
    {
        Transform m_LastLookAtTarget;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            const string indexTooltip = "The position on the Spline at which this data point will take effect.  "
                + "The value is interpreted according to the Index Unit setting.";

            CinemachineLookAtDataOnSpline.Item def = new ();
            var indexProp = property.FindPropertyRelative("m_Index");
            var valueProp = property.FindPropertyRelative("m_Value");

            var overlay = new PropertyField(indexProp, "") { tooltip = indexTooltip, style = { flexGrow = 1, flexBasis = 100 }};
            var overlayLabel = new Label("Index") { tooltip = indexTooltip, style = { alignSelf = Align.Center }};
            overlayLabel.AddDelayedFriendlyPropertyDragger(indexProp, overlay);

            var foldout = new Foldout() { text = "Data Point" };
            foldout.BindProperty(property);
            foldout.Add(new PropertyField(indexProp) { tooltip = indexTooltip });

            var lookAtProp = valueProp.FindPropertyRelative(() => def.LookAtTarget);
            var offsetProp = valueProp.FindPropertyRelative(() => def.Offset);

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

    [EditorTool("LookAt Data On Spline Tool", typeof(CinemachineLookAtDataOnSpline))]
    class LookAtDataOnSplineTool : EditorTool
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;

        bool GetTargets(out CinemachineLookAtDataOnSpline splineDataTarget, out SplineContainer spline, out CinemachineSplineDolly dolly)
        {
            splineDataTarget = target as CinemachineLookAtDataOnSpline;
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
                text = "LookAt Data On Spline Tool",
                tooltip = "Assign LookAt points to points on the spline."
            };
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!GetTargets(out var splineDataTarget, out var spline, out _))
                return;

            Undo.RecordObject(splineDataTarget, "Modifying CinemachineLookAtDataOnSpline values");
            using (new Handles.DrawingScope(Handles.selectedColor))
            {
                DrawDataPoints(splineDataTarget.LookAtData);
                var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);
                nativeSpline.DataPointHandles(splineDataTarget.LookAtData);
            }
        }

        void DrawDataPoints(SplineData<CinemachineLookAtDataOnSpline.Item> splineData)
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
