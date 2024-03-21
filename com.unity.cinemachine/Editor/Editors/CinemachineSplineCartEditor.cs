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
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SplineSettings)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UpdateMethod)));

            var autoDollyProp = serializedObject.FindProperty(() => Target.AutomaticDolly);
            ux.Add(new PropertyField(autoDollyProp));
            var targetField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.TrackingTarget)));

            ux.TrackPropertyWithInitialCallback(autoDollyProp, (p) =>
            {
                var autodolly = p.FindPropertyRelative("Method").managedReferenceValue as SplineAutoDolly.ISplineAutoDolly;
                targetField.SetVisible(autodolly != null && autodolly.RequiresTrackingTarget);
            });

            return ux;
        }
    }
}
