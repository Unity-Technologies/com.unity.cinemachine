using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using Cinemachine.Utility;
using System;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineFreeLookModifier))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineFreeLookModifierEditor : UnityEditor.Editor
    {
        CinemachineFreeLookModifier Target => target as CinemachineFreeLookModifier;

        GUIContent m_AddModifierLabel = new GUIContent("Add Modifier");
        GUIContent m_DeleteModifierLabel = new GUIContent("X", "Delete this modifier");

        public override void OnInspectorGUI()
        {
            if (!Target.HasOrbital())
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("An Orbital Follow component is required.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            rect = EditorGUI.PrefixLabel(rect, m_AddModifierLabel);
            int selection = EditorGUI.Popup(rect, 0, s_ModifierNames);
            if (selection > 0)
            {
                Type type = s_AllModifiers[selection];

                // For each inspected object, add selected item if not already present
                for (int i = 0; i < targets.Length; i++)
                {
                    var t = targets[i] as CinemachineFreeLookModifier;
                    if (t == null)
                        continue;
                    bool gotIt = false;
                    for (int m = 0; !gotIt && m < t.Modifiers.Count; ++m)
                        if (t.Modifiers[m].GetType() == type)
                            gotIt = true;
                    if (!gotIt)
                    {
                        Undo.RecordObject(t, "add modofier");
                        var m = (CinemachineFreeLookModifier.Modifier)Activator.CreateInstance(type);
                        m.Reset(Target.VirtualCamera);
                        t.Modifiers.Add(m);
                    }
                }
            }

            int indexToDelete = -1;
            var modifiers = serializedObject.FindProperty(() => Target.Modifiers);
            for (int i = 0; i < modifiers.arraySize; ++i)
            {
                var e = modifiers.GetArrayElementAtIndex(i);
                var v = e.managedReferenceValue;
                if (v == null)
                    continue;
                var r = EditorGUILayout.GetControlRect();
                r.width -= EditorGUIUtility.singleLineHeight; 
                if (e.isExpanded = EditorGUI.Foldout(r, e.isExpanded, GetModifierName(v.GetType()), true))
                {
                    ++EditorGUI.indentLevel;
                    InspectorUtility.DrawChildProperties(
                        EditorGUILayout.GetControlRect(true, InspectorUtility.PropertyHeightOfChidren(e)), e);
                    --EditorGUI.indentLevel;
                }
                r.x += r.width; r.width = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(r, m_DeleteModifierLabel))
                    indexToDelete = i;
            }
            if (indexToDelete != -1)
                modifiers.DeleteArrayElementAtIndex(indexToDelete);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        static GUIContent GetModifierName(Type type)
        {
            for (int j = 0; j < s_AllModifiers.Count; ++j)
                if (s_AllModifiers[j] == type)
                    return s_ModifierNames[j];
            return new GUIContent(type.Name); // should never get here
        }

        static List<Type> s_AllModifiers = new List<Type>();
        static GUIContent[] s_ModifierNames = new GUIContent[0];

        [InitializeOnLoad]
        static class EditorInitialize
        {
            // This code dynamically discovers eligible classes and builds the menu
            // data for the various component pipeline stages.
            static EditorInitialize()
            {
                // Get all ICinemachineComponents
                var allTypes
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                        (Type t) => typeof(CinemachineFreeLookModifier.Modifier).IsAssignableFrom(t) && !t.IsAbstract);

                s_AllModifiers.Clear();
                s_AllModifiers.Add(null);
                s_AllModifiers.AddRange(allTypes);

                s_ModifierNames = new GUIContent[s_AllModifiers.Count];
                s_ModifierNames[0] = new GUIContent("(select)");
                for (int i = 1; i < s_AllModifiers.Count; ++i)
                {
                    var name = s_AllModifiers[i].Name;
                    var index = name.LastIndexOf("Modifier", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                        name = name.Remove(index);
                    s_ModifierNames[i] = new GUIContent(name);
                }
            }
        }

    }
}
