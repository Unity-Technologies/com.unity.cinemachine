using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(HideFoldoutAttribute))]
    sealed class HideFoldoutPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return InspectorUtility.PropertyHeightOfChidren(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InspectorUtility.DrawChildProperties(position, property);
        }
    }
}
