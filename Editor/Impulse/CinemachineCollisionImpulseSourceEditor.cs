#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
    [CustomEditor(typeof(CinemachineCollisionImpulseSource))]
    internal sealed class CinemachineCollisionImpulseSourceEditor
        : BaseEditor<CinemachineCollisionImpulseSource>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();

            EditorGUILayout.Separator();
            var collider = Target.GetComponent<Collider>();
            var collider2D = Target.GetComponent<Collider2D>();
            if ((collider == null || !collider.enabled) && (collider2D == null || !collider2D.enabled))
                EditorGUILayout.HelpBox(
                    "An active Collider or Collider2D component is required in order to detect collisions and generate Impulse events",
                    MessageType.Warning);

            DrawRemainingPropertiesInInspector();
        }
    }
#endif
}
