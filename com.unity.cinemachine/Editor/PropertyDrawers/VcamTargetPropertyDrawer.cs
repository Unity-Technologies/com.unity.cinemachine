using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(VcamTargetPropertyAttribute))]
    internal sealed class VcamTargetPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float hSpace = 2;
            float iconSize = rect.height + 4;
            rect.width -= iconSize + hSpace;
            EditorGUI.PropertyField(rect, property, EditorGUI.BeginProperty(rect, label, property));
            rect.x += rect.width + hSpace; rect.width = iconSize;

            var oldEnabled = GUI.enabled;
            var target = property.objectReferenceValue as Transform;
            if (target == null || target.GetComponent<CinemachineTargetGroup>() != null)
                GUI.enabled = false;
            if (GUI.Button(rect, EditorGUIUtility.IconContent("_Popup"), GUI.skin.label))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Convert to TargetGroup"), false, () =>
                {
                    GameObject go = ObjectFactory.CreateGameObject("Target Group", typeof(CinemachineTargetGroup));
                    var group = go.GetComponent<CinemachineTargetGroup>();
                   
                    group.m_RotationMode = CinemachineTargetGroup.RotationMode.GroupAverage;
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
            VisualElement prop = new PropertyField(property);
            row.Add(prop);
            prop.style.flexGrow = 1;

            var button = new VisualElement 
            { 
                style = 
                { 
                    backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("_Popup").image,
                    width = InspectorUtility.SingleLineHeight,
                    height = InspectorUtility.SingleLineHeight,
                    alignSelf = Align.Center,
                    paddingRight = 0,
                    borderRightWidth = 0,
                    marginRight = 0
                }
            };
            button.AddToClassList("unity-button");
            row.Add(button);

            var target = property.objectReferenceValue as Transform;
            var disableTargetGroup = target == null || target.TryGetComponent<CinemachineTargetGroup>(out var _);
            var manipulator = new ContextualMenuManipulator((evt) => 
            {
                evt.menu.AppendAction("Convert to TargetGroup", 
                    (action) => 
                    {
                        var go = ObjectFactory.CreateGameObject("Target Group", typeof(CinemachineTargetGroup));
                        var group = go.GetComponent<CinemachineTargetGroup>();
                   
                        group.m_RotationMode = CinemachineTargetGroup.RotationMode.GroupAverage;
                        group.AddMember(target, 1, 1);
                        property.objectReferenceValue = group.Transform;
                        property.serializedObject.ApplyModifiedProperties();
                    }, 
                    (status) => disableTargetGroup ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal
                );                
            });
            manipulator.activators.Clear();
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            button.AddManipulator(manipulator);
            return row;
        }
    }
}
