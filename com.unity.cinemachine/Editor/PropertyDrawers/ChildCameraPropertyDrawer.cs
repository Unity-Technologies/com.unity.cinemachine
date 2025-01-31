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
            if (property.serializedObject.targetObject is CinemachineCameraManagerBase manager)
            {
                var vcamSel = new PopupField<UnityEngine.Object>() { style = {flexBasis = 10, flexGrow = 1}};
                vcamSel.BindProperty(property);
                vcamSel.formatListItemCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.formatSelectedValueCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.TrackAnyUserActivity(() =>
                {
                    if (manager != null && manager.ChildCameras != null)
                    {
                        vcamSel.choices = manager.ChildCameras.Cast<UnityEngine.Object>().ToList();
                        vcamSel.SetValueWithoutNotify(property.objectReferenceValue);
                    }
                });
                if (preferredLabel.Length == 0)
                    return vcamSel;

                var row = new InspectorUtility.LabeledRow(preferredLabel, property.tooltip);
                row.Contents.AddChild(vcamSel);
                return row;
            }
            return new PropertyField(property, preferredLabel);
        }
    }
}
