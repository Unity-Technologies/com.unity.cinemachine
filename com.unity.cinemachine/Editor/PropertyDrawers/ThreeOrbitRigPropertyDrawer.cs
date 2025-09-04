using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(Cinemachine3OrbitRig.Orbit))]
    class ThreeOrbitRigPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Cinemachine3OrbitRig.Orbit def = new ();
            var row = new InspectorUtility.LeftRightRow(new Label(property.displayName) { tooltip = property.tooltip });
            row.Right.AddChild(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Height)) { style = { flexGrow = 1, paddingLeft = 3 }});
            row.Right.AddChild(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Radius)) { style = { flexGrow = 1, paddingLeft = 5 }});
            return row;
        }
    }
}
