using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineImpulseDefinitionPropertyAttribute))]
    internal sealed class CinemachineImpulseDefinitionPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        
        float HeaderHeight { get { return EditorGUIUtility.singleLineHeight * 1.5f; } }
        float DrawHeader(Rect rect, string text)
        {
            float delta = HeaderHeight - EditorGUIUtility.singleLineHeight;
            rect.y += delta; rect.height -= delta;
            EditorGUI.LabelField(rect, new GUIContent(text), EditorStyles.boldLabel);
            return HeaderHeight;
        }

        string HeaderText(SerializedProperty property)
        {
            var attrs = property.serializedObject.targetObject.GetType()
                .GetCustomAttributes(typeof(HeaderAttribute), false);
            if (attrs != null && attrs.Length > 0)
                return ((HeaderAttribute)attrs[0]).header;
            return null;
        }

        List<string> mHideProperties = new List<string>();

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            CinemachineImpulseDefinition myClass = null; // to access name strings
            SignalSourceAsset asset = null;
            float height = 0;
            mHideProperties.Clear();
            string prefix = prop.name;
            prop.NextVisible(true); // Skip outer foldout
            do
            {
                if (!prop.propertyPath.Contains(prefix)) // if it is part of an array, then it won't StartWith prefix
                    break;
                string header = HeaderText(prop);
                if (header != null)
                    height += HeaderHeight + vSpace;

                // Do we hide this property?
                bool hide = false;
                if (prop.name == SerializedPropertyHelper.PropertyName(() => myClass.m_RawSignal))
                    asset = prop.objectReferenceValue as SignalSourceAsset;
                if (prop.name == SerializedPropertyHelper.PropertyName(() => myClass.m_RepeatMode))
                    hide = asset == null || asset.SignalDuration <= 0;
                else if (prop.name == SerializedPropertyHelper.PropertyName(() => myClass.m_Randomize))
                    hide = asset == null || asset.SignalDuration > 0;

                if (hide)
                    mHideProperties.Add(prop.name);
                else
                    height += EditorGUI.GetPropertyHeight(prop, false) + vSpace;
            } while (prop.NextVisible(prop.isExpanded));
            return height;
        }

        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            string prefix = prop.name;
            prop.NextVisible(true); // Skip outer foldout
            do
            {
                if (!prop.propertyPath.Contains(prefix)) // if it is part of an array, then it won't StartWith prefix
                    break;
                string header = HeaderText(prop);
                if (header != null)
                {
                    rect.height = HeaderHeight;
                    DrawHeader(rect, header);
                    rect.y += HeaderHeight + vSpace;
                }
                if (mHideProperties.Contains(prop.name))
                    continue;
                rect.height = EditorGUI.GetPropertyHeight(prop, false);
                EditorGUI.PropertyField(rect, prop);
                rect.y += rect.height + vSpace;
            } while (prop.NextVisible(prop.isExpanded));
        }
    }
}
