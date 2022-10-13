using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(EnumMaskPropertyAttribute))]
    class EnumMaskPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var a = (uint)(EditorGUI.MaskField(rect, label, property.intValue, property.enumNames));
            if (EditorGUI.EndChangeCheck())
                property.intValue = (int)a;
        }
    }
}
