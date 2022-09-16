using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(FoldoutWithEnabledButtonAttribute))]
    class FoldoutWithEnabledButtonPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var a = (FoldoutWithEnabledButtonAttribute)attribute;
            return InspectorUtility.EnabledFoldoutHeight(property, a.EnabledPropertyName);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var a = (FoldoutWithEnabledButtonAttribute)attribute;
            InspectorUtility.EnabledFoldout(rect, property, a.EnabledPropertyName);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = (FoldoutWithEnabledButtonAttribute)attribute;
            var enabledProp = property.FindPropertyRelative(a.EnabledPropertyName);
            if (enabledProp == null)
                return new PropertyField(property);

            var ux = new VisualElement();
            ux.Add(new PropertyField(enabledProp, property.displayName) { tooltip = property.tooltip });
            // GML todo: fix the indenting
            var children = ux.AddChild(new VisualElement() { style = { marginLeft = InspectorUtility.SingleLineHeight }});

            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                if (!SerializedProperty.EqualContents(childProperty, enabledProp))
                    children.Add(new PropertyField(childProperty));
                childProperty.NextVisible(false);
            }

            TrackEnabled(enabledProp);
            ux.TrackPropertyValue(enabledProp, TrackEnabled);

            void TrackEnabled(SerializedProperty p)
            {
                children.SetVisible(p.boolValue);
            }
            return ux;
        }
    }
}
