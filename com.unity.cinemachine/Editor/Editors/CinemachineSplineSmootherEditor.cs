using UnityEditor;
using UnityEditor.Splines;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineSmoother))]
    class CinemachineSplineSmootherEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            ux.Add(new HelpBox("Spline Smoother adjusts the spline's knot settings to maintain smoothness "
            + "suitable for camera paths.  Do not adjust the tangents manually; they will be overwritten by the smoother.", 
            HelpBoxMessageType.Info));
            ux.Add(new PropertyField(serializedObject.FindProperty("AutoSmooth")));
            ux.Add(new Button(() => (target as CinemachineSplineSmoother).SmoothSplineNow()) { text = "Smooth Spline Now" });
            return ux;
        }

        [InitializeOnLoad]
        static class AutoSmoothHookup
        {
            static AutoSmoothHookup()
            {
                CinemachineSplineSmoother.OnEnableCallback += OnSmootherEnable;
                CinemachineSplineSmoother.OnDisableCallback += OnSmootherDisable;
            }

            static void OnSmootherEnable(CinemachineSplineSmoother smoother) => EditorSplineUtility.AfterSplineWasModified += smoother.OnSplineModified;
            static void OnSmootherDisable(CinemachineSplineSmoother smoother) => EditorSplineUtility.AfterSplineWasModified -= smoother.OnSplineModified;
        }
    }
}
