using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineImpulseListener))]
    internal sealed class CinemachineImpulseListenerEditor 
        : BaseEditor<CinemachineImpulseListener>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            EditorGUILayout.HelpBox(
                "The Impulse Listener will respond to signals broadcasted by any CinemachineImpulseSource.",
                MessageType.Info);
            DrawRemainingPropertiesInInspector();
        }
    }
}
