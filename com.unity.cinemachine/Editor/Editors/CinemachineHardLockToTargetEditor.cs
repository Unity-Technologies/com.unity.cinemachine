using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLockToTarget))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineHardLockToTargetEditor : BaseEditor<CinemachineHardLockToTarget>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineHardLockToTarget).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox(
                    "Hard Lock requires a Tracking Target.  Change Position Control to None if you don't want a Tracking target.",
                    MessageType.Warning);
            DrawRemainingPropertiesInInspector();
        }
    }
}
