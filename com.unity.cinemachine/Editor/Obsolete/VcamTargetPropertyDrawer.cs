#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace Unity.Cinemachine.Editor
{
    [Obsolete]
    [CustomPropertyDrawer(typeof(VcamTargetPropertyAttribute))]
    class VcamTargetPropertyDrawer : PropertyDrawer
    {
        // old IMGUI implementation must remain until no more IMGUI inspectors are using it
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float hSpace = 2;
            float iconSize = rect.height + 4;
            rect.width -= iconSize + hSpace;
            EditorGUI.PropertyField(rect, property, EditorGUI.BeginProperty(rect, label, property));
            rect.x += rect.width + hSpace; rect.width = iconSize;

            var oldEnabled = GUI.enabled;
            var target = property.objectReferenceValue as Transform;
            if (target == null || target.TryGetComponent<CinemachineTargetGroup>(out var _))
                GUI.enabled = false;
            if (GUI.Button(rect, EditorGUIUtility.IconContent("_Popup"), GUI.skin.label))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Convert to TargetGroup"), false, () =>
                {
                    GameObject go = ObjectFactory.CreateGameObject("Target Group", typeof(CinemachineTargetGroup));
                    var group = go.GetComponent<CinemachineTargetGroup>();
                   
                    group.RotationMode = CinemachineTargetGroup.RotationModes.GroupAverage;
                    group.AddMember(target, 1, 1);
                    property.objectReferenceValue = group.Transform;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.ShowAsContext();
            }
            GUI.enabled = oldEnabled;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row }};
            row.Add(new PropertyField(property) { style = { flexGrow = 1 }});
            row.Add(InspectorUtility.MiniPopupButton(null, new ContextualMenuManipulator((evt) => 
            {
                evt.menu.AppendAction("Convert to TargetGroup", 
                    (action) => 
                    {
                        var go = ObjectFactory.CreateGameObject("Target Group", typeof(CinemachineTargetGroup));
                        var group = go.GetComponent<CinemachineTargetGroup>();
                        var target = property.objectReferenceValue as Transform;
                   
                        group.RotationMode = CinemachineTargetGroup.RotationModes.GroupAverage;
                        group.AddMember(target, 1, 1);
                        property.objectReferenceValue = group.Transform;
                        property.serializedObject.ApplyModifiedProperties();
                    }, 
                    (status) => 
                    {
                        var target = property.objectReferenceValue as Transform;
                        var disable = target == null || target.TryGetComponent<CinemachineTargetGroup>(out var _);
                        return disable ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
                    }
                );                
            })));
            return row;
        }
    }
}
#endif
