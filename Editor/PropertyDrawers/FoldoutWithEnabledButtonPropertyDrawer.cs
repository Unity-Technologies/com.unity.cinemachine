using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(FoldoutWithEnabledButtonAttribute))]
    sealed class FoldoutWithEnabledButtonPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var a = attribute as FoldoutWithEnabledButtonAttribute;
            return InspectorUtility.EnabledFoldoutHeight(property, a.EnabledPropertyName);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var a = attribute as FoldoutWithEnabledButtonAttribute;
            InspectorUtility.EnabledFoldout(rect, property, a.EnabledPropertyName);
        }
    }
}
