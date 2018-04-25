using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLockToTarget))]
    internal sealed class CinemachineHardLockToTargetEditor : BaseEditor<CinemachineHardLockToTarget>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (Target.FollowTarget == null)
                EditorGUILayout.HelpBox(
                    "Hard Lock requires a Follow Target.  Change Body to Do Nothing if you don't want a Follow target.",
                    MessageType.Warning);
            EditorGUI.BeginChangeCheck();
            GUI.enabled = false;
            EditorGUILayout.LabelField(" ", "No additional settings", EditorStyles.miniLabel);
            GUI.enabled = true;
            DrawRemainingPropertiesInInspector();
        }
    }
}
