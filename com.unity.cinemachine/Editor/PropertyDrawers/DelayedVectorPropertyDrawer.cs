using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(DelayedVectorAttribute))]
    class DelayedVectorPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ux = new InspectorUtility.LabeledRow(property.displayName, property.tooltip);
            ux.Contents.AddChild(ChildField(property.FindPropertyRelative("x")));
            ux.Contents.AddChild(ChildField(property.FindPropertyRelative("y")));
            ux.Contents.AddChild(ChildField(property.FindPropertyRelative("z")));
            ux.Contents.style.marginLeft = -6;
            ux.Contents.style.minWidth = 100;
            return ux;

            // Unfortunatley this mess is necessary because we want to support delayed-friendly dragging
            // (which cancels delay while label-dragging), but it's not possible to replace the
            // existing draggers provided by the default PropertyField, so we have to make our own fields
            // and do our best to mimic the layout of the default ones.
            VisualElement ChildField(SerializedProperty p)
            {
                if (p == null)
                    return new VisualElement { style = { flexBasis = 30, flexGrow = 1, marginLeft = 8 } };
                var f = new InspectorUtility.CompactPropertyField(p)
                    { style = { flexBasis = 30, flexGrow = 1, marginLeft = 8 } };
                f.Label.style.minWidth = 16;
                f.OnInitialGeometry(() =>
                {
                    f.SafeSetIsDelayed();
                    var ff = f.Q<FloatField>();
                    if (ff != null)
                    {
                        ff.style.marginLeft = -1;
                        ff.style.marginRight = -3;
                    }
                });
                return f;
            }
        }
    }
}
