using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Splines;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(SplineSettings))]
    class SplineSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SplineSettings def = new ();
            var splineProp = property.FindPropertyRelative(() => def.Spline);
            var positionProp = property.FindPropertyRelative(() => def.Position);
            var unitsProp = property.FindPropertyRelative(() => def.Units);

            var ux = new VisualElement();
            ux.Add(new PropertyField(splineProp));
            var row = ux.AddChild(InspectorUtility.PropertyRow(positionProp, out var posField));
            posField.style.flexGrow = 1;
            posField.style.flexBasis = 0;

            var initialUnits = (PathIndexUnit)unitsProp.enumValueIndex;
            var targets = property.serializedObject.targetObjects;
            for (int t = 0; t < targets.Length && initialUnits == (PathIndexUnit)unitsProp.enumValueIndex; ++t)
                if (new SerializedObject(targets[t]).FindProperty(unitsProp.propertyPath).enumValueIndex != (int)initialUnits)
                    initialUnits = (PathIndexUnit)(-1); // mixed values

            var unitsField = row.Contents.AddChild(new EnumField(
                initialUnits) { style = { flexGrow = 2, flexBasis = 0 } });
            unitsField.RegisterValueChangedCallback((evt) => 
            {
                var newUnits = (PathIndexUnit)evt.newValue;
                for (int i = 0; i < targets.Length; ++i)
                {
                    var o = new SerializedObject(targets[i]);
                    var pU = o.FindProperty(unitsProp.propertyPath);
                    var oldUnits = (PathIndexUnit)pU.enumValueIndex;
                    if (oldUnits != newUnits)
                    {
                        var splineReferencer = targets[i] as ISplineReferencer;
                        var spline = splineReferencer?.SplineSettings.GetCachedSpline();
                        if (spline != null)
                        {
                            // Convert the position to the new units
                            var pP = o.FindProperty(positionProp.propertyPath);
                            pP.floatValue = spline.ConvertIndexUnit(pP.floatValue, oldUnits, newUnits);
                        }
                        pU.enumValueIndex = (int)newUnits;
                        o.ApplyModifiedProperties();
                    }
                }
            });
            return ux;
        }
    }
}
