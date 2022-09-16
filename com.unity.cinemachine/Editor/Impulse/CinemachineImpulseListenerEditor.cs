using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineImpulseListener))]
    [CanEditMultipleObjects]
    class CinemachineImpulseListenerEditor : BaseEditor<CinemachineImpulseListener>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            EditorGUILayout.HelpBox(
                "The Impulse Listener will respond to signals broadcast by any CinemachineImpulseSource.",
                MessageType.Info);
            DrawRemainingPropertiesInInspector();
        }
    }
}
