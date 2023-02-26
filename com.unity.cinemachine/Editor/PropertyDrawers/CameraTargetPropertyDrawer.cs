using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CameraTarget))]
    class CameraTargetPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            CameraTarget def = new ();
            var ux = new VisualElement();
            var customProp = property.FindPropertyRelative(() => def.CustomLookAtTarget);
            var follow = ux.AddChild(CreateTargetProperty(property.FindPropertyRelative(() => def.TrackingTarget), customProp));
            var lookAt = ux.AddChild(CreateTargetProperty(property.FindPropertyRelative(() => def.LookAtTarget), customProp));
            
            TrackCustomProp(customProp);
            ux.TrackPropertyValue(customProp, TrackCustomProp);
            void TrackCustomProp(SerializedProperty p) => lookAt.SetVisible(p.boolValue);

            return ux;
        }

        VisualElement CreateTargetProperty(SerializedProperty property, SerializedProperty customProp)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row }};
            row.Add(new PropertyField(property) { style = { flexGrow = 1 }});
            var button = row.AddChild(new Button { style = 
            { 
                backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("_Popup").image,
                width = InspectorUtility.SingleLineHeight, height = InspectorUtility.SingleLineHeight,
                alignSelf = Align.Center,
                paddingRight = 0, borderRightWidth = 0, marginRight = 0
            }});

            var manipulator = new ContextualMenuManipulator((evt) => 
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
                evt.menu.AppendAction("Use Separate LookAt Target", 
                    (action) => 
                    {
                        customProp.boolValue = !customProp.boolValue;
                        customProp.serializedObject.ApplyModifiedProperties();
                    }, 
                    (status) => 
                    {
                        return customProp.boolValue ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    }
                );  
            });
            manipulator.activators.Clear();
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            button.AddManipulator(manipulator);
            return row;
        }
    }
}
