using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using Cinemachine.Utility;
using System;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineFreeLookModifier))]
    internal sealed class CinemachineFreeLookModifierEditor : UnityEditor.Editor
    {
        CinemachineFreeLookModifier Target => target as CinemachineFreeLookModifier;

        GUIContent m_AddModifierLabel = new GUIContent("Add Modifier");
        GUIContent m_DeleteModifierLabel = new GUIContent("X", "Delete this modifier");
        GUIContent m_ResetModifierLabel = new GUIContent("R", "Reset this modifier");

        public override void OnInspectorGUI()
        {
            if (!Target.HasValueSource())
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("No appropriate CM components found.  "
                    + $"Must have one of {GetAssignableTypes(typeof(CinemachineFreeLookModifier.IModifierValueSource))}.", 
                    MessageType.Warning);
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
                bool gotIt = false;
                for (int m = 0; !gotIt && m < Target.Modifiers.Count; ++m)
                    if (Target.Modifiers[m].GetType() == type)
                        gotIt = true;
                if (!gotIt)
                {
                    Undo.RecordObject(Target, "add modifier");
                    var m = (CinemachineFreeLookModifier.Modifier)Activator.CreateInstance(type);
                    m.RefreshCache(Target.VirtualCamera);
                    m.Reset(Target.VirtualCamera);
                    Target.Modifiers.Add(m);
                }
            }

            int indexToDelete = -1;
            var modifiers = serializedObject.FindProperty(() => Target.Modifiers);
            for (int i = 0; i < modifiers.arraySize; ++i)
            {
                var e = modifiers.GetArrayElementAtIndex(i);
                var m = e.managedReferenceValue as CinemachineFreeLookModifier.Modifier;
                if (m == null)
                    continue;
                bool needsWarning = !m.HasRequiredComponent;
                var r = EditorGUILayout.GetControlRect();
                r.width -= 2 * EditorGUIUtility.singleLineHeight; 
                if (e.isExpanded = EditorGUI.Foldout(r, e.isExpanded, GetModifierName(m.GetType()), true))
                {
                    ++EditorGUI.indentLevel;
                    if (needsWarning)
                        EditorGUILayout.HelpBox("No appropriate CM components found.  "
                            + $"Must have one of {GetAssignableTypes(m.CachedComponentType)}.", 
                            MessageType.Warning);
                    InspectorUtility.DrawChildProperties(
                        EditorGUILayout.GetControlRect(true, InspectorUtility.PropertyHeightOfChidren(e)), e);
                    --EditorGUI.indentLevel;
                }

                r.x += r.width; r.width = EditorGUIUtility.singleLineHeight;
                if (needsWarning)
                {
                    EditorGUI.LabelField(new Rect(r.x - r.width, r.y, r.width, r.height), 
                        new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml").image));
                }
                if (GUI.Button(r, m_ResetModifierLabel))
                {
                    Undo.RecordObject(Target, "reset modifier");
                    m.Reset(Target.VirtualCamera);
                }
                r.x += r.width;
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
        static GUIContent[] s_ModifierNames = Array.Empty<GUIContent>();
        static Dictionary<Type, string> s_AsignableTypes = new Dictionary<Type, string>();

        static string GetAssignableTypes(Type inputType)
        {
            if (!s_AsignableTypes.ContainsKey(inputType))
            {
                var allSources
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                        (Type t) => inputType.IsAssignableFrom(t) && !t.IsAbstract);
                var s = string.Empty;
                foreach (var t in allSources)
                {
                    var sep = (s.Length == 0) ? string.Empty : ", ";
                    s += sep + t.Name;
                }
                if (s.Length == 0)
                    s = "(none)";
                s_AsignableTypes[inputType] = s;
            }
            return s_AsignableTypes[inputType];
        }

        [InitializeOnLoad]
        static class EditorInitialize
        {
            // This code dynamically discovers eligible classes and builds the menu
            // data for the various component pipeline stages.
            static EditorInitialize()
            {
                // Get all Modifiers
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
