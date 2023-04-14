using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(EmbeddedBlenderSettingsPropertyAttribute))]
    class EmbeddedBlenderSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is CinemachineCameraManagerBase m)
            {
                return EmbeddedAssetEditorUtility.EmbeddedAssetInspector<CinemachineBlenderSettings>(property, (ed) =>
                {
                    var editor = ed as CinemachineBlenderSettingsEditor;
                    if (editor != null)
                        editor.GetAllVirtualCameras = (list) => list.AddRange(m.ChildCameras);
                });
            }
            return EmbeddedAssetEditorUtility.EmbeddedAssetInspector<CinemachineBlenderSettings>(property, null);
        }
    }
}

