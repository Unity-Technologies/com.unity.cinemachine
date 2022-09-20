using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSameAsFollowTarget))]
    [CanEditMultipleObjects]
    class CinemachineSameAsFollowTargetEditor : BaseEditor<CinemachineSameAsFollowTarget>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineSameAsFollowTarget).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox(
                    "Same As Follow Target requires a Tracking Target in the CmCamera.  It will set the camera's "
                        + "rotation to be the same as that of the Tracking Target.",
                    MessageType.Warning);
            DrawRemainingPropertiesInInspector();
        }
    }
}
