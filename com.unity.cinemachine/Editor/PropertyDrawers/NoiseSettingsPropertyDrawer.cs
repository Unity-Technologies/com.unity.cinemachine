using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(NoiseSettings))]
    class NoiseSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return EmbeddedAssetEditorUtility.AssetSelectorWithPresets<NoiseSettings>(property, null, "Presets/Noise");
        }
    }
}
