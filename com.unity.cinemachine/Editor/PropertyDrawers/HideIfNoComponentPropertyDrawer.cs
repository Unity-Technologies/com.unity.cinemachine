using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(HideIfNoComponentAttribute))]
    class HideIfNoComponentPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = (HideIfNoComponentAttribute)attribute;

            var ux = new PropertyField(property);
            ux.TrackAnyUserActivity(() =>
            {
                if (property.IsDeletedObject())
                    return;
                bool hasComponent = false;
                var targets = property.serializedObject.targetObjects;
                for (int i = 0; !hasComponent && i < targets.Length; ++i)
                    if (targets[i] is MonoBehaviour b)
                        hasComponent = b.TryGetComponent(a.ComponentType, out _);
                ux.SetVisible(hasComponent);
            });
            return ux;
        }
    }
}
