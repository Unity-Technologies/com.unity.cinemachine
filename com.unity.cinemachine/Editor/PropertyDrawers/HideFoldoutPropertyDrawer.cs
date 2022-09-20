using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(HideFoldoutAttribute))]
    class HideFoldoutPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return InspectorUtility.PropertyHeightOfChidren(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InspectorUtility.DrawChildProperties(position, property);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ux = new VisualElement();

            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                ux.Add(new PropertyField(childProperty));
                childProperty.NextVisible(false);
            }
            return ux;
        }
    }
}
