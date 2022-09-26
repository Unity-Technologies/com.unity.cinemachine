using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineRecomposer))]
    [CanEditMultipleObjects]
    class CinemachineRecomposerEditor : UnityEditor.Editor
    {
        CinemachineRecomposer Target => target as CinemachineRecomposer;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ApplyAfter)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Tilt)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Pan)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Dutch)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ZoomScale)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FollowAttachment)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.LookAtAttachment)));

            m_PipelineUtility.UpdateState();
            return ux;
        }
    }
}
