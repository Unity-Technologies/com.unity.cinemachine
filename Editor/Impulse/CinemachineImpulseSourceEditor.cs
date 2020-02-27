using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineImpulseSource))]
    internal sealed class CinemachineImpulseSourceEditor 
        : BaseEditor<CinemachineImpulseSource>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(
                "First set up the Signal and Range, then connect your impulse-generating event "
                    + "to one of the GenerateImpulse API methods defined in this script.",
                MessageType.Info);
            DrawRemainingPropertiesInInspector();
        }
    }
}
