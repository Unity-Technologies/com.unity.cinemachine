using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineImpulseSource1D))]
    internal sealed class CinemachineImpulseSource1DEditor 
        : BaseEditor<CinemachineImpulseSource1D>
    {
        float m_TestForce = 1;

        public override void OnInspectorGUI()
        {
            BeginInspector();
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(
                "Connect your impulse-generating event "
                    + "to one of the GenerateImpulse API methods defined in this script.",
                MessageType.Info);
            DrawRemainingPropertiesInInspector();

            GUI.enabled = EditorApplication.isPlaying;
            var labelWidth = EditorGUIUtility.labelWidth;
            var r1 = EditorGUILayout.GetControlRect();
            var r2 = r1;
            r1.width = labelWidth - 2;
            r2.x += labelWidth; r2.width -= labelWidth;
            m_TestForce = EditorGUI.Slider(r2, m_TestForce, 0.1f, 20f);
            if (GUI.Button(r1, "Test"))
                Target.GenerateImpulseWithForce(m_TestForce);
            GUI.enabled = true;
        }
    }
}
