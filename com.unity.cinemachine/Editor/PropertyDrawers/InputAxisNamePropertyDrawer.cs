#if ENABLE_LEGACY_INPUT_MANAGER
using UnityEditor;
using UnityEngine.UIElements;
using System;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(InputAxisNamePropertyAttribute))]
    partial class InputAxisNamePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = InspectorUtility.PropertyRow(property, out _, preferredLabel);
            var error = row.Contents.AddChild(InspectorUtility.MiniHelpIcon(
                "Invalid axis name.  See Project Settings > Input Manager for a list of defined axes",
                HelpBoxMessageType.Error));

            row.TrackPropertyWithInitialCallback(property, (p) =>
            {
                if (p.IsDeletedObject())
                    return;
                // Is the axis name valid?
                var nameError = string.Empty;
                var nameValue = p.stringValue;
                if (nameValue.Length > 0)
                    try { CinemachineCore.GetInputAxis(nameValue); }
                    catch (ArgumentException e) { nameError = e.Message; }
                error.SetVisible(!string.IsNullOrEmpty(nameError));
            });

            return row;
        }
    }
}
#endif
