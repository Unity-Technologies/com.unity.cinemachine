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
            var splineIsChildHelp = ux.AddChild(new HelpBox("Spline should not be a child of this object.", HelpBoxMessageType.Error));

            var splineProp = serializedObject.FindProperty("m_SplineSettings");
            ux.Add(new PropertyField(splineProp));
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

            ux.TrackAnyUserActivity(() =>
            {
                bool isChild = false;
                for (int i = 0; !isChild && i < targets.Length; ++i)
                {
                    var t = targets[i] as CinemachineSplineDolly;
                    if (t != null && t.Spline != null)
                        isChild = t.transform.IsAncestorOf(t.Spline.transform);
                }
                splineIsChildHelp.SetVisible(isChild);
            });

            return ux;
        }
    }
}
