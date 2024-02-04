using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.Splines;

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
            
            SerializedProperty positionUnitsProp  = serializedObject.FindProperty(() => Target.positionUnitsBackingField);
            PropertyField      positionUnitsField = new (positionUnitsProp, label: "") { style = { flexGrow = 2, flexBasis = 0 } };
            row.Contents.Add(positionUnitsField);
            
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SplineOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraRotation)));
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
            
            // Store the initial value
            PathIndexUnit previousUnits = Target.positionUnitsBackingField;
            ux.TrackPropertyValue(positionUnitsProp, TrackPositionUnits);
            void TrackPositionUnits(SerializedProperty p)
            {
                PathIndexUnit newUnits = (PathIndexUnit) p.enumValueIndex;
                
                Target.UpdateDistanceForPositionUnits(previousUnits, newUnits);
                // Update previous value for the next change
                previousUnits = newUnits;
            }

            return ux;
        }
    }
}
