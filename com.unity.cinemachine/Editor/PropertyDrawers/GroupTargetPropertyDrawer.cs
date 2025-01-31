using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineTargetGroup.Target))]
    partial class GroupTargetPropertyDrawer : PropertyDrawer
    {
        CinemachineTargetGroup.Target def = new();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 3.5f;
            var ux = new VisualElement() { style = { flexDirection = FlexDirection.Row }};

            ux.Add(new PropertyField(property.FindPropertyRelative(() => def.Object), string.Empty)
                { style = { flexGrow = 1, flexBasis = floatFieldWidth }});
            ux.Add(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Radius), "R")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth, marginLeft = 5 }});
            ux.Add(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Weight), "W")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth, marginLeft = 5 }});

            return ux;
        }
    }
}
