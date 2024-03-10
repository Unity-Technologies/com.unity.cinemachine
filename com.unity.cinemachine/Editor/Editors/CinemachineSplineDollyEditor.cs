using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDolly))]
    [CanEditMultipleObjects]
    class CinemachineSplineDollyEditor : UnityEditor.Editor
    {
        CinemachineSplineDolly Target => target as CinemachineSplineDolly;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            var noSplineHelp = ux.AddChild(new HelpBox("A Spline is required.", HelpBoxMessageType.Warning));

            var splineProp = serializedObject.FindProperty(() => Target.Spline);
            ux.Add(new PropertyField(splineProp));

            var row = ux.AddChild(InspectorUtility.PropertyRow(
                serializedObject.FindProperty(() => Target.CameraPosition), out _));
            row.Contents.Add(new PropertyField(serializedObject.FindProperty(() => Target.PositionUnits), "") 
                { style = { flexGrow = 2, flexBasis = 0 }});

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SplineOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraRotation)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AutomaticDolly)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));

            ux.TrackPropertyWithInitialCallback(splineProp, (p) =>
            {
                bool noSpline = false;
                for (int i = 0; !noSpline && i < targets.Length; ++i)
                    noSpline = targets[i] != null && ((CinemachineSplineDolly)targets[i]).Spline == null;
                noSplineHelp.SetVisible(noSpline);
            });

            return ux;
        }
    }
}
