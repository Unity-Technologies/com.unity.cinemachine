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
                var f = ux.Q<FloatField>("unity-x-input");
                if (f != null)
                    f.isDelayed = true;
                f = ux.Q<FloatField>("unity-y-input");
                if (f != null)
                    f.isDelayed = true;
                f = ux.Q<FloatField>("unity-z-input");
                if (f != null)
                    f.isDelayed = true;
            });
            return ux;
        }
    }
}
