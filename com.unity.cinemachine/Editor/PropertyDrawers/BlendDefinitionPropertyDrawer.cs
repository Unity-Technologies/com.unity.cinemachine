using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineBlendDefinition))]
    partial class BlendDefinitionPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            CinemachineBlendDefinition def = new(); // to access name strings
            var floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            VisualElement ux, contents;
            if (preferredLabel.Length == 0)
                ux = contents = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            else
            {
                var row = new InspectorUtility.LeftRightRow();
                row.Left.Add(new Label(preferredLabel)
                    { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 1 }});
                contents = row.Right;
                ux = row;
            }

            var styleProp = property.FindPropertyRelative(() => def.Style);
            contents.Add(new PropertyField(styleProp, "")
                { style = { flexGrow = 1, flexBasis = floatFieldWidth }});

            var curveProp = property.FindPropertyRelative(() => def.CustomCurve);
            var curveWidget = contents.AddChild(new PropertyField(curveProp, "")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth }});

            var timeProp = property.FindPropertyRelative(() => def.Time);
            var timeWidget = contents.AddChild(new InspectorUtility.CompactPropertyField(timeProp, "s")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth, marginLeft = 4 }});

            contents.TrackPropertyWithInitialCallback(styleProp, (p) =>
            {
                if (p.IsDeletedObject())
                    return;
                curveWidget.SetVisible(p.intValue == (int)CinemachineBlendDefinition.Styles.Custom);
                timeWidget.SetVisible(p.intValue != (int)CinemachineBlendDefinition.Styles.Cut);
            });

            return ux;
        }
    }
}
