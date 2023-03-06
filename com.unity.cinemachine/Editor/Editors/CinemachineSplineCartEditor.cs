using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
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
                var autodolly = p.FindPropertyRelative("Method").managedReferenceValue as SplineAutoDolly.ISplineAutoDolly;
                targetField.SetVisible(autodolly != null && autodolly.RequiresTrackingTarget);
            }

            return ux;
        }
    }
}
