using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(ChildCameraPropertyAttribute))]
    class ChildCameraPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is CinemachineCameraManagerBase m)
            {
                var row = new InspectorUtility.LabeledRow(preferredLabel, property.tooltip);
                var vcamSel = row.Contents.AddChild(new PopupField<UnityEngine.Object>() { style = {flexBasis = 10, flexGrow = 1}});
                vcamSel.BindProperty(property);
                vcamSel.formatListItemCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.formatSelectedValueCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.TrackAnyUserActivity(() => vcamSel.choices = m == null ? null : m.ChildCameras.Cast<UnityEngine.Object>().ToList());
                return row;
            }
            return new PropertyField(property, preferredLabel);
        }
    }
}
