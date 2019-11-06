using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSameAsFollowTarget))]
    internal sealed class CinemachineSameAsFollowTargetEditor : BaseEditor<CinemachineSameAsFollowTarget>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (Target.FollowTarget == null)
                EditorGUILayout.HelpBox(
                    "Same As Follow Target requires a Follow target.  It will set the virtual camera's rotation to be the same as that of the Follow Target.",
                    MessageType.Warning);
            DrawRemainingPropertiesInInspector();
        }
    }
}
