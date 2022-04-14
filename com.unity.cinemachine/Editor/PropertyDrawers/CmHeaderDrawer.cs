using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CmHeaderAttribute))]
    sealed class CmHeaderDrawer : PropertyDrawer
    {
        const float k_PaddingLeft = 4f;
        Label m_Label;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var inspector = new VisualElement();
                
            m_Label = new Label
            {
                enableRichText = true,
                text = "<b>"+((CmHeaderAttribute)attribute).header+"</b>",
                style = { paddingLeft = k_PaddingLeft },
            };
            m_Label.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            var propertyField = new PropertyField(property);
            propertyField.AddToClassList(InspectorUtility.alignFieldClass);
            
            inspector.Add(m_Label);
            inspector.Add(propertyField);

            return inspector;
        }
        
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            m_Label.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Label.style.paddingTop = m_Label.contentRect.height / 2f;
        }
    }
}
