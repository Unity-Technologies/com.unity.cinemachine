using UnityEditor;
using UnityEditor.Splines;
using UnityEditor.UIElements;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineSmoother))]
    class CinemachineSplineSmootherEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            ux.Add(new HelpBox("The Spline Smoother adjusts the spline's knot settings to maintain smoothness "
                + "suitable for camera paths.  Do not adjust the tangents or knot modes manually; they will be overwritten by the smoother.\n"
                + "To adjust tangents and knot settings manually, disable or remove this Behaviour.",
            HelpBoxMessageType.Info));

            var autoSmoothProp = serializedObject.FindProperty(nameof(CinemachineSplineSmoother.AutoSmooth));
            ux.Add(new PropertyField(autoSmoothProp));
            ux.TrackPropertyValue(autoSmoothProp, (p) =>
            {
                if (p.boolValue)
                {
                    var smoother = target as CinemachineSplineSmoother;
                    if (smoother != null && smoother.enabled)
                        SmoothSplineNow(smoother);
                }
            });

            ux.Add(new Button(() => SmoothSplineNow(target as CinemachineSplineSmoother)) { text = "Smooth Spline Now" });
            return ux;
        }

        static void SmoothSplineNow(CinemachineSplineSmoother smoother)
        {
            if (smoother != null && smoother.TryGetComponent(out SplineContainer container))
            {
                Undo.RecordObject(container, "Smooth Spline");
                smoother.SmoothSplineNow();
            }
        }

        [InitializeOnLoad]
        static class AutoSmoothHookup
        {
            static AutoSmoothHookup()
            {
                CinemachineSplineSmoother.OnEnableCallback += OnSmootherEnable;
                CinemachineSplineSmoother.OnDisableCallback += OnSmootherDisable;
            }

            static void OnSmootherEnable(CinemachineSplineSmoother smoother)
            {
                EditorSplineUtility.AfterSplineWasModified += smoother.OnSplineModified;
                if (smoother.AutoSmooth)
                    SmoothSplineNow(smoother);
            }
            static void OnSmootherDisable(CinemachineSplineSmoother smoother) => EditorSplineUtility.AfterSplineWasModified -= smoother.OnSplineModified;
        }
    }
}
