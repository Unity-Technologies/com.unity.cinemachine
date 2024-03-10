using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFollow))]
    [CanEditMultipleObjects]
    class CinemachineFollowEditor : CinemachineComponentBaseEditor
    {
        void OnEnable() => CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
        void OnDisable() => CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));

        void OnSceneGUI()
        {
            var follow = target as CinemachineFollow;
            if (follow == null || !follow.IsValid)
                return;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var property = new SerializedObject(follow).FindProperty(() => follow.FollowOffset);
                var up = follow.VirtualCamera.State.ReferenceUp;
                CinemachineSceneToolHelpers.FollowOffsetTool(
                    follow.VirtualCamera, property, follow.GetDesiredCameraPosition(up),
                    follow.FollowTargetPosition, follow.GetReferenceOrientation(up), () =>
                    {
                        // Sanitize the offset
                        property.vector3Value = follow.EffectiveOffset;
                        property.serializedObject.ApplyModifiedProperties();
                    });
            }
        }
    }
}
