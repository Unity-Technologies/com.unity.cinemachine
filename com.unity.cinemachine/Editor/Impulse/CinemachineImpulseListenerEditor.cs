using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineImpulseListener))]
    [CanEditMultipleObjects]
    class CinemachineImpulseListenerEditor : UnityEditor.Editor
    {
        CinemachineImpulseListener Target => target as CinemachineImpulseListener;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);
            ux.Add(new HelpBox("The Impulse Listener will respond to signals broadcast by any CinemachineImpulseSource.", HelpBoxMessageType.Info));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ApplyAfter)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ChannelMask)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Gain)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Use2DDistance)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UseCameraSpace)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ReactionSettings)));

            m_PipelineUtility.UpdateState();
            return ux;
        }
    }
}
