using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFollow))]
    [CanEditMultipleObjects]
    class CinemachineFollowEditor : CinemachineComponentBaseEditor
    {
        [EditorTool("Follow Offset Tool", typeof(CinemachineFollow))]
        class FollowOffsetTool : EditorTool
        {
            GUIContent m_IconContent;
            public override GUIContent toolbarIcon => m_IconContent;
            void OnEnable()
            {
                m_IconContent = new GUIContent
                {
                    image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/FollowOffset.png"),
                    tooltip = "Adjust the Follow Offset",
                };
            }

            public override void OnToolGUI(EditorWindow window)
            {
                var follow = target as CinemachineFollow;
                if (follow == null || !follow.IsValid)
                    return;

                var property = new SerializedObject(follow).FindProperty(() => follow.FollowOffset);
                var up = follow.VirtualCamera.State.ReferenceUp;
                CinemachineSceneToolHelpers.DoFollowOffsetTool(
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
