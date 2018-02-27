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
                "First set up the Impulse Definition, then connect your Impact Event to the OnImpact method defined here.",
                MessageType.Info);
            DrawRemainingPropertiesInInspector();
        }
    }
}
