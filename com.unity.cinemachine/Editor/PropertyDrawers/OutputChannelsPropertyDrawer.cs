using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(OutputChannels))]
    partial class OutputChannelsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            List<string> choices = new ();
            var settings = CinemachineChannelNames.InstanceIfExists;
            var names = Enum.GetNames(typeof(OutputChannels));
            for (int i = 0; i < names.Length; ++i)
                choices.Add(settings == null || settings.ChannelNames.Length <= i
                    ? names[i] : settings.ChannelNames[i]);

            var row = new InspectorUtility.LabeledRow(preferredLabel, property.tooltip);
            var selector = row.Contents.AddChild(new MaskField(choices, 1)
                { tooltip = property.tooltip, style = { flexBasis = 100, flexGrow = 1, marginLeft = 2 }});
            selector.BindProperty(property);

            row.Contents.Add(InspectorUtility.MiniPopupButton(null, new ContextualMenuManipulator((evt) =>
            {
                evt.menu.AppendAction("Edit Channel Names...",
                    (action) =>
                    {
                        if (CinemachineChannelNames.Instance != null)
                            Selection.activeObject = CinemachineChannelNames.Instance;
                    }
                );
            })));

            return row;
        }
    }
}
