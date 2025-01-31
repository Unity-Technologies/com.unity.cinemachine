using UnityEditor;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
    [CustomEditor(typeof(CinemachineCollisionImpulseSource))]
    class CinemachineCollisionImpulseSourceEditor : UnityEditor.Editor
    {
        CinemachineCollisionImpulseSource Target => target as CinemachineCollisionImpulseSource;

        float m_TestForce = 1;
        GUIContent m_TestButton = new (
            "Invoke", "Play-mode only: Generate an impulse with the default velocity scaled by this amount");
        GUIContent m_TestLabel = new (
            "Test with Force", "Generate an impulse with the default velocity scaled by an amount");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Separator();
            Target.TryGetComponent<Collider>(out var collider);
            Target.TryGetComponent<Collider2D>(out var collider2D);
            if ((collider == null || !collider.enabled) && (collider2D == null || !collider2D.enabled))
                EditorGUILayout.HelpBox(
                    "An active Collider or Collider2D component is required in order to detect "
                        + "collisions and generate Impulse events",
                    MessageType.Warning);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.ImpulseDefinition));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.DefaultVelocity));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.LayerMask));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.IgnoreTag));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.UseImpactDirection));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.ScaleImpactWithMass));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.ScaleImpactWithSpeed));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            GUI.enabled = EditorApplication.isPlaying;
            {
                var r1 = EditorGUILayout.GetControlRect();
                r1 = EditorGUI.PrefixLabel(r1, m_TestLabel);
                var testButtonWidth = GUI.skin.button.CalcSize(m_TestButton).x;
                var r2 = r1;
                r1.width = testButtonWidth;
                r2.x += testButtonWidth + 3; r2.width -= testButtonWidth + 3;

                m_TestForce = EditorGUI.Slider(r2, m_TestForce, 0.1f, 20f);
                if (GUI.Button(r1, m_TestButton))
                    Target.GenerateImpulseWithForce(m_TestForce);
            }
            GUI.enabled = true;
        }
    }
#endif
}
