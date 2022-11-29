using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDolly))]
    [CanEditMultipleObjects]
    class CinemachineSplineDollyEditor : UnityEditor.Editor
    {
        CinemachineSplineDolly Target => target as CinemachineSplineDolly;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);
            var noSplineHelp = ux.AddChild(new HelpBox("A Spline is required.", HelpBoxMessageType.Warning));

            var splineProp = serializedObject.FindProperty(() => Target.Spline);
            ux.Add(new PropertyField(splineProp));

            var row = ux.AddChild(InspectorUtility.CreatePropertyRow(
                serializedObject.FindProperty(() => Target.CameraPosition), out _));
            row.Right.Add(new PropertyField(serializedObject.FindProperty(() => Target.PositionUnits), "") 
                { style = { flexGrow = 2, flexBasis = 0 }});

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SplineOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraUp)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AutomaticDolly)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));

            TrackSpline(splineProp);
            ux.TrackPropertyValue(splineProp, TrackSpline);
            void TrackSpline(SerializedProperty p)
            {
                bool noSpline = false;
                for (int i = 0; !noSpline && i < targets.Length; ++i)
                    noSpline = targets[i] != null && ((CinemachineSplineDolly)targets[i]).Spline == null;
                noSplineHelp.SetVisible(noSpline);
            }
            m_PipelineUtility.UpdateState();
            return ux;
        }
    }
}
