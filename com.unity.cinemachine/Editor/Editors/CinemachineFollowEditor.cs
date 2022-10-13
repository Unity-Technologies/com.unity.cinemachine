using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFollow))]
    [CanEditMultipleObjects]
    class CinemachineFollowEditor : UnityEditor.Editor
    {
        CinemachineFollow Target => target as CinemachineFollow;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable()
        {
            m_PipelineUtility = new (this);
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
        }
        
        void OnDisable()
        {
            m_PipelineUtility.OnDisable();
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Follow);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TrackerSettings)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FollowOffset)));

            m_PipelineUtility.UpdateState();
            return ux;
        }

        void OnSceneGUI()
        {
            if (Target == null || !Target.IsValid)
                return;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var property = new SerializedObject(Target).FindProperty(() => Target.FollowOffset);
                var up = Target.VirtualCamera.State.ReferenceUp;
                CinemachineSceneToolHelpers.FollowOffsetTool(
                    Target.VirtualCamera, property, Target.GetDesiredCameraPosition(up),
                    Target.FollowTargetPosition, Target.GetReferenceOrientation(up), () =>
                    {
                        // Sanitize the offset
                        property.vector3Value = Target.EffectiveOffset;
                        property.serializedObject.ApplyModifiedProperties();
                    });
            }
        }
    }
}
