#if CINEMACHINE_PHYSICS

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineShotQualityEvaluator))]
    [CanEditMultipleObjects]
    class CinemachineShotQualityEvaluatorEditor : UnityEditor.Editor
    {
        CinemachineShotQualityEvaluator Target => target as CinemachineShotQualityEvaluator;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.OcclusionLayers)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.IgnoreTag)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.MinimumDistanceFromTarget)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraRadius)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DistanceEvaluation)));

            return ux;
        }
    }
}
#endif
