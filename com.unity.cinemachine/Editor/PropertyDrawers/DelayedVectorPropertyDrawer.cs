using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(DelayedVectorAttribute))]
    class DelayedVectorPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ux = new PropertyField(property);
            ux.OnInitialGeometry(() =>
            {
                ux.SafeSetIsDelayed("unity-x-input");
                ux.SafeSetIsDelayed("unity-y-input");
                ux.SafeSetIsDelayed("unity-z-input");
            });
            return ux;
        }
    }
}
