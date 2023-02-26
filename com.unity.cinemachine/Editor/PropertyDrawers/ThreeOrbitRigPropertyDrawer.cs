using UnityEditor;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(Cinemachine3OrbitRig.Orbit))]
    class ThreeOrbitRigPropertyDrawer : PropertyDrawer
    {
        Cinemachine3OrbitRig.Orbit def = new Cinemachine3OrbitRig.Orbit();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new InspectorUtility.LeftRightRow();
            row.Left.Add(new Label(property.displayName) { tooltip = property.tooltip });
            row.Right.AddChild(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Height)) { style = { flexGrow = 1, paddingLeft = 3 }});
            row.Right.AddChild(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Radius)) { style = { flexGrow = 1, paddingLeft = 5 }});
            return row;
        }
    }
}
