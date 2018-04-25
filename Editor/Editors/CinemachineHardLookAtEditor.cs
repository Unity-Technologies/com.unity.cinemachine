using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLookAt))]
    internal sealed class CinemachineHardLookAtEditor : BaseEditor<CinemachineHardLookAt>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (Target.LookAtTarget == null)
                EditorGUILayout.HelpBox(
                    "Hard Look At requires a LookAt target.  Change Aim to Do Nothing if you don't want a LookAt target.", 
                    MessageType.Warning);
            EditorGUI.BeginChangeCheck();
            GUI.enabled = false;
            EditorGUILayout.LabelField(" ", "No additional settings", EditorStyles.miniLabel);
            GUI.enabled = true;
            DrawRemainingPropertiesInInspector();
        }
    }
}
