using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    class ChildListInspectorHelper
    {
        UnityEditorInternal.ReorderableList m_ChildList;

        /// <summary>
        /// Use this delegate to control the display of warning icons next to the child cameras
        /// </summary>
        public delegate string GetChildWarningMessageDelegate(object childObject);
        public GetChildWarningMessageDelegate GetChildWarningMessage = (object childObject) => "";

        /// <summary>Call from inspector's OnEnable</summary>
        public void OnEnable()
        {
            m_ChildList = null;
        }

        /// <summary>Call from inspector's OnInspectorGUI.  Returns true if child list was edited</summary>
        public bool OnInspectorGUI(SerializedProperty property)
        {
            if (Selection.objects.Length == 1)
            {
                if (m_ChildList == null)
                    SetupChildList(property);

                EditorGUI.BeginChangeCheck();
                m_ChildList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    return true;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Child Cameras cannot be displayed when multiple objects are selected.", 
                    MessageType.Info);
            }
            return false;
        }

        void SetupChildList(SerializedProperty property)
        {
            const float vSpace = 2;
            const float hSpace = 3;
            var floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            m_ChildList = new (property.serializedObject, property, true, true, true, true);

            m_ChildList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Child Camera");
                var priorityText = new GUIContent("Priority");
                var textDimensions = GUI.skin.label.CalcSize(priorityText);
                rect.x += rect.width - textDimensions.x;
                rect.width = textDimensions.x;
                EditorGUI.LabelField(rect, priorityText);
            };

            m_ChildList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += vSpace;
                rect.width -= floatFieldWidth + hSpace;
                rect.height = EditorGUIUtility.singleLineHeight;
                var element = m_ChildList.serializedProperty.GetArrayElementAtIndex(index);
                var warningText = GetChildWarningMessage(element.objectReferenceValue);
                if (!string.IsNullOrEmpty(warningText))
                {
                    var width = rect.width;
                    rect.width = rect.height;
                    EditorGUI.LabelField(rect, new GUIContent("", warningText) 
                        { image = EditorGUIUtility.IconContent("console.warnicon.sml").image });
                    width -= rect.width; rect.x += rect.width; rect.width = width;
                }
                GUI.enabled = false;
                EditorGUI.PropertyField(rect, element, GUIContent.none);
                GUI.enabled = true;

                rect.x += rect.width + hSpace; rect.width = floatFieldWidth;
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = hSpace * 2;
                DrawPriorityField(rect, new SerializedObject(element.objectReferenceValue));
                EditorGUIUtility.labelWidth = oldWidth;
            };

            m_ChildList.onChangedCallback = (UnityEditorInternal.ReorderableList l) =>
            {
                if (l.index < 0 || l.index >= l.serializedProperty.arraySize)
                    return;
                var o = l.serializedProperty.GetArrayElementAtIndex(l.index).objectReferenceValue;
                var vcam = (o != null) ? (o as CinemachineVirtualCameraBase) : null;
                if (vcam != null)
                    vcam.transform.SetSiblingIndex(l.index);
            };

            m_ChildList.onAddCallback = (UnityEditorInternal.ReorderableList l) =>
            {
                var b = property.serializedObject.targetObject as MonoBehaviour;
                var index = l.serializedProperty.arraySize;
                var vcam = CinemachineMenu.CreatePassiveCmCamera(parentObject: b == null ? null : b.gameObject);
                vcam.transform.SetSiblingIndex(index);
            };

            m_ChildList.onRemoveCallback = (UnityEditorInternal.ReorderableList l) =>
            {
                var o = l.serializedProperty.GetArrayElementAtIndex(l.index).objectReferenceValue;
                var vcam = (o != null) ? (o as CinemachineVirtualCameraBase) : null;
                if (vcam != null)
                    Undo.DestroyObjectImmediate(vcam.gameObject);
            };
        }
        
        static void DrawPriorityField(Rect rect, SerializedObject serializedObject)
        {
            var prop = serializedObject.FindProperty("PriorityAndChannel");
            var enabledProp = prop.FindPropertyRelative("Enabled");
            var priorityProp = prop.FindPropertyRelative("Priority");
            var priority = enabledProp.boolValue ? priorityProp.intValue : 0;
            var newPriority = EditorGUI.IntField(rect, " ", priority);
            if (newPriority != priority)
            {
                if (newPriority != 0)
                    enabledProp.boolValue = true;
                priorityProp.intValue = newPriority;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
