using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFollow))]
    [CanEditMultipleObjects]
    class CinemachineFollowEditor : UnityEditor.Editor
    {
        CinemachineFollow Target => target as CinemachineFollow;

        void OnEnable()
        {
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
        }
        
        void OnDisable()
        {
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Tracking);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TrackerSettings)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FollowOffset)));
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
