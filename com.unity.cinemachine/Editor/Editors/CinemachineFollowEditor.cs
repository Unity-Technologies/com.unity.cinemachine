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

        VisualElement m_NoFollowHelp;

        void OnEnable()
        {
            EditorApplication.update += UpdateHelpBoxes;
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
        }
        
        void OnDisable()
        {
            EditorApplication.update -= UpdateHelpBoxes;
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_NoFollowHelp = ux.AddChild(new HelpBox("Follow requires a Tracking Target in the CmCamera.", HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TrackerSettings)));
            ux.AddSpace();
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FollowOffset)));

            UpdateHelpBoxes();
            return ux;
        }

        void UpdateHelpBoxes()
        {
            if (target == null)
                return;  // target was deleted
            bool noFollow = false;
            for (int i = 0; i < targets.Length; ++i)
            {
                var t = targets[i] as CinemachineFollow;
                noFollow |= t.FollowTarget == null;
            }
            if (m_NoFollowHelp != null)
                m_NoFollowHelp.SetVisible(noFollow);
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
