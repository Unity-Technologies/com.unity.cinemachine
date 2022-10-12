using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineExternalCamera))]
    [CanEditMultipleObjects]
    class CinemachineExternalCameraEditor : UnityEditor.Editor
    {
        CinemachineExternalCamera Target => target as CinemachineExternalCamera;
        CmCameraInspectorUtility m_CameraUtility = new();

        void OnEnable()
        {
            m_CameraUtility.OnEnable(targets);
        }

        void OnDisable()
        {
            m_CameraUtility.OnDisable();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_CameraUtility.AddCameraStatus(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PriorityAndChannel)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.StandbyUpdate)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TransitionHint)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.LookAtTarget)));

            return ux;
        }
    }
}
