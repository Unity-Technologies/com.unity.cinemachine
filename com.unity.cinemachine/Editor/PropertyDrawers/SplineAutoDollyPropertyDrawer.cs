using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(AutoDollySelectorAttribute))]
    class SplineAutoDollyPropertyDrawer : PropertyDrawer
    {
#if false // Has refresh problems in 2023.1.0a13
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ux = new VisualElement();

            var value = property.managedReferenceValue;
            var index = AutoDollyMenuItems.GetTypeIndex(value == null ? null : value.GetType());
            var dropdown = ux.AddChild(new DropdownField
            {
                label = property.name,
                tooltip = property.tooltip,
                choices = AutoDollyMenuItems.s_ItemNames,
                index = index,
                style = { flexGrow = 1 }
            });
            dropdown.AddToClassList(InspectorUtility.kAlignFieldClass);
            dropdown.RegisterValueChangedCallback((evt) => 
            {
                var value = property.managedReferenceValue;
                var oldIndex = AutoDollyMenuItems.GetTypeIndex(value == null ? null : value.GetType());
                index = AutoDollyMenuItems.GetTypeIndex(evt.newValue);
                if (oldIndex != index)
                {
                    property.managedReferenceValue = (index == 0) 
                        ? null : Activator.CreateInstance(AutoDollyMenuItems.s_AllItems[index]);
                    property.serializedObject.ApplyModifiedProperties();
                    UpdateChildren();
                }
            });
            UpdateChildren();

            void UpdateChildren()
            {
                // First delete the existing element
                const string kElementName = "ActiveAutoDollyContainer";
                var old = ux.Q(kElementName);
                if (old != null)
                    old.RemoveFromHierarchy();

                // Create a new one
                var type = AutoDollyMenuItems.s_AllItems[index];
                if (type != null)
                {
                    // GML todo: fix indenting hack
                    var children = ux.AddChild(new VisualElement() 
                        { name = kElementName, style = { marginLeft = InspectorUtility.SingleLineHeight } });
                    var childProperty = property.Copy();
                    var endProperty = childProperty.GetEndProperty();
                    childProperty.NextVisible(true);
                    while (!SerializedProperty.EqualContents(childProperty, endProperty))
                    {
                        children.Add(new PropertyField(childProperty));
                        childProperty.NextVisible(false);
                    }
                }
            }

            return ux;
        }
#endif

        [InitializeOnLoad]
        static class AutoDollyMenuItems
        {
            public static int GetTypeIndex(Type type)
            {
                for (int i = 0; i < s_AllItems.Count; ++i)
                    if (s_AllItems[i] == type)
                        return i;
                return 0;
            }
            public static int GetTypeIndex(string typeName)
            {
                for (int i = 0; i < s_ItemNames.Count; ++i)
                    if (s_ItemNames[i] == typeName)
                        return i;
                return 0;
            }
        
            // These lists are synchronized
            public static List<Type> s_AllItems = new ();
            public static List<string> s_ItemNames = new ();
            public static GUIContent[] s_ItemContents = Array.Empty<GUIContent>(); // GML todo: delete me

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

                s_ItemNames.Clear();
                s_ItemNames.Add("None");
                for (int i = 1; i < s_AllItems.Count; ++i)
                    s_ItemNames.Add(InspectorUtility.NicifyClassName(s_AllItems[i]));

                s_ItemContents = new GUIContent[s_AllItems.Count];
                s_ItemContents[0] = new GUIContent("None");
                for (int i = 1; i < s_AllItems.Count; ++i)
                    s_ItemContents[i] = new GUIContent(InspectorUtility.NicifyClassName(s_AllItems[i]));
            }
        }


        // IMGUI implementation (to be removed)
        readonly float vSpace = EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var value = property.managedReferenceValue;
            Type type = value == null ? null : value.GetType();

            EditorGUI.BeginProperty(rect, label, property);
            var r = rect; r.height = EditorGUIUtility.singleLineHeight;
            r = EditorGUI.PrefixLabel(r, label);
            int selection = EditorGUI.Popup(r, AutoDollyMenuItems.GetTypeIndex(type), AutoDollyMenuItems.s_ItemContents);
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
    }
}

