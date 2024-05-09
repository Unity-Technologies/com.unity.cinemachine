using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(SensorSizePropertyAttribute))]
    partial class SensorSizePropertyDrawer : PropertyDrawer
    {
        static readonly List<string> s_PresetNames = new () 
        {
            "8mm",
            "Super 8mm",
            "16mm",
            "Super 16mm",
            "35mm 2-perf",
            "35mm Academy",
            "Super-35",
            "35mm TV Projection",
            "35mm Full Aperture",
            "35mm 1.85 Projection",
            "35mm Anamorphic",
            "65mm ALEXA",
            "70mm",
            "70mm IMAX"
        };

        static readonly List<Vector2> s_PresetSizes = new ()
        {
            new (4.8f, 3.5f),       // 8mm
            new (5.79f, 4.01f),     // Super 8mm
            new (10.27f, 7.49f),    // 16mm
            new (12.522f, 7.417f),  // Super 16mm
            new (21.95f, 9.35f),    // 35mm 2-perf
            new (21.946f, 16.002f), // 35mm Academy
            new (24.89f, 18.66f),   // Super-35
            new (20.726f, 15.545f), // 35mm TV Projection
            new (24.892f, 18.669f), // 35mm Full Aperture
            new (20.955f, 11.328f), // 35mm 1.85 Projection
            new (21.946f, 18.593f), // 35mm Anamorphic
            new (54.12f, 25.59f),   // 65mm ALEXA
            new (52.476f, 23.012f), // 70mm
            new (70.41f, 52.63f)    // 70mm IMAX
        };

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = InspectorUtility.PropertyRow(property, out var sensorSize, preferredLabel);
            sensorSize.style.flexGrow = 2;
            sensorSize.style.flexBasis = 0;

            var presets = row.Contents.AddChild(new PopupField<string>
            { 
                choices = s_PresetNames, 
                tooltip = "Sensor Presets", 
                style = { flexBasis = 0, flexGrow = 1, marginLeft = 5, marginTop = 0, marginBottom = 0 }
            });
            presets.RegisterValueChangedCallback((evt) =>
            {
                var index = s_PresetNames.IndexOf(evt.newValue);
                if (index >= 0)
                {
                    property.vector2Value = s_PresetSizes[index];
                    property.serializedObject.ApplyModifiedProperties();
                }
            });

            sensorSize.RegisterValueChangeCallback((evt) =>
            {
                var v = property.vector2Value;
                v.x = Mathf.Max(v.x, 0.1f);
                v.y = Mathf.Max(v.y, 0.1f);
                property.vector2Value = v;
                property.serializedObject.ApplyModifiedProperties();

                // Find matching preset (if any)
                var index = s_PresetSizes.FindIndex(e => Mathf.Approximately(v.x, e.x) && Mathf.Approximately(v.y, e.y));
                presets.SetValueWithoutNotify(index >= 0 ? s_PresetNames[index] : "Custom");
            });
            
            return row;
        }
    }
}
