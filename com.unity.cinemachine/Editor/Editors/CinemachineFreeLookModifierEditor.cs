using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineFreeLookModifier))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineFreeLookModifierEditor : UnityEditor.Editor
    {
        CinemachineFreeLookModifier Target => target as CinemachineFreeLookModifier;

        public override void OnInspectorGUI()
        {
            var def = Target;

            if (!def.HasOrbital())
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("An Orbital Follow component is required.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Tilt));
            if (def.HasNoise())
                EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Noise));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Lens));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
