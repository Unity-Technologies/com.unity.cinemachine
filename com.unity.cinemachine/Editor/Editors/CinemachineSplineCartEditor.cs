using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineCart))]
    [CanEditMultipleObjects]
    class CinemachineSplineCartEditor : UnityEditor.Editor
    {
        CinemachineSplineCart Target => target as CinemachineSplineCart;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Spline)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UpdateMethod)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PositionUnits)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SplinePosition)));

            var autoDollyProp = serializedObject.FindProperty(() => Target.AutomaticDolly);
            ux.Add(new PropertyField(autoDollyProp));
            var targetField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.TrackingTarget)));

            TrackAutoDolly(autoDollyProp);
            ux.TrackPropertyValue(autoDollyProp, TrackAutoDolly);
            void TrackAutoDolly(SerializedProperty p) 
            {
                targetField.SetVisible(p.FindPropertyRelative("Implementation").managedReferenceValue != null);
            }

            return ux;
        }
    }
}
