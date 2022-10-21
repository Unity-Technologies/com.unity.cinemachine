using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(AutoDollySelectorAttribute))]
    class SplineAutoDollyPropertyDrawer : PropertyDrawer
    {
        readonly float vSpace = EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var value = property.managedReferenceValue;
            Type type = value == null ? null : value.GetType();

            EditorGUI.BeginProperty(rect, label, property);
            var r = rect; r.height = EditorGUIUtility.singleLineHeight;
            r = EditorGUI.PrefixLabel(r, label);
            int selection = EditorGUI.Popup(r, AutoDollyMenuItems.GetTypeIndex(type), AutoDollyMenuItems.s_ItemNames);
            if (selection >= 0)
            {
                Type newType = AutoDollyMenuItems.s_AllItems[selection];
                if (type != newType)
                {
                    property.managedReferenceValue = (newType == null) ? null : Activator.CreateInstance(newType);
                    property.serializedObject.ApplyModifiedProperties();
                    type = newType;
                }
            }
            if (type != null)
            {
                rect.y += r.height + vSpace; rect.height -= r.height - vSpace;
                ++EditorGUI.indentLevel;
                InspectorUtility.DrawChildProperties(rect, property);
                --EditorGUI.indentLevel;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (property.managedReferenceValue != null)
                height += InspectorUtility.PropertyHeightOfChidren(property) + vSpace;
            return height;
        }

        [InitializeOnLoad]
        static class AutoDollyMenuItems
        {
            public static int GetTypeIndex(Type type)
            {
                for (int j = 0; j < s_AllItems.Count; ++j)
                    if (s_AllItems[j] == type)
                        return j;
                return -1;
            }
        
            // These lists are synchronized
            public static List<Type> s_AllItems = new List<Type>();
            public static GUIContent[] s_ItemNames = Array.Empty<GUIContent>();

            // This code dynamically discovers eligible classes and builds the menu data 
            static AutoDollyMenuItems()
            {
                // Get all eligible types
                var allTypes
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                        (Type t) => typeof(SplineAutoDolly.ISplineAutoDolly).IsAssignableFrom(t) && !t.IsAbstract);

                s_AllItems.Clear();
                s_AllItems.Add(null);
                s_AllItems.AddRange(allTypes);

                s_ItemNames = new GUIContent[s_AllItems.Count];
                s_ItemNames[0] = new GUIContent("None");
                for (int i = 1; i < s_AllItems.Count; ++i)
                    s_ItemNames[i] = new GUIContent(InspectorUtility.NicifyClassName(s_AllItems[i]));
            }
        }
    }
}

