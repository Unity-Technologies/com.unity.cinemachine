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

            var splineIsChildHelp = ux.AddChild(new HelpBox("Spline should not be a child of this object.", HelpBoxMessageType.Error));
            ux.Add(new PropertyField(serializedObject.FindProperty("m_SplineSettings")));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UpdateMethod)));

            var autoDollyProp = serializedObject.FindProperty(() => Target.AutomaticDolly);
            ux.Add(new PropertyField(autoDollyProp));
            var targetField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.TrackingTarget)));

            ux.TrackPropertyWithInitialCallback(autoDollyProp, (p) =>
            {
                var autodolly = p.FindPropertyRelative("Method").managedReferenceValue as SplineAutoDolly.ISplineAutoDolly;
                targetField.SetVisible(autodolly != null && autodolly.RequiresTrackingTarget);
            });

            ux.TrackAnyUserActivity(() =>
            {
                bool isChild = false;
                for (int i = 0; !isChild && i < targets.Length; ++i)
                {
                    var t = targets[i] as CinemachineSplineCart;
                    if (t != null && t.Spline != null)
                        isChild = t.transform.IsAncestorOf(t.Spline.transform);
                }
                splineIsChildHelp.SetVisible(isChild);
            });

            return ux;
        }
    }
}
