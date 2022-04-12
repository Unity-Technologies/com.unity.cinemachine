using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(FoldoutWithEnabledButtonAttribute))]
    sealed class FoldoutWithEnabledButtonPropertyDrawer : PropertyDrawer
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
    }
}
