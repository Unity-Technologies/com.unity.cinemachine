using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CameraTarget))]
    class CameraTargetPropertyDrawer : PropertyDrawer
    {
        CameraTarget def;

        // old IMGUI implementation must remain until no more IMGUI inspectors are using it
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            DrawTargetPropertyOnGUI(rect, property.FindPropertyRelative(() => def.TrackingTarget), property);
            if (property.FindPropertyRelative(() => def.CustomLookAtTarget).boolValue)
            {
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                DrawTargetPropertyOnGUI(rect, property.FindPropertyRelative(() => def.LookAtTarget), property);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (property.FindPropertyRelative(() => def.CustomLookAtTarget).boolValue)
                height += height + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        void DrawTargetPropertyOnGUI(Rect rect, SerializedProperty property, SerializedProperty mainProperty)
        {
            const float hSpace = 2;
            float iconSize = rect.height + 4;
            rect.width -= iconSize + hSpace;
            EditorGUI.PropertyField(rect, property);
            rect.x += rect.width + hSpace; rect.width = iconSize;

            var target = property.objectReferenceValue as Transform;
            if (GUI.Button(rect, EditorGUIUtility.IconContent("_Popup"), GUI.skin.label))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Convert to Target Group"), false, () =>
                {
                    if (target != null && !target.TryGetComponent<CinemachineTargetGroup>(out var _))
                    {
                        var go = ObjectFactory.CreateGameObject("Target Group", typeof(CinemachineTargetGroup));
                        var group = go.GetComponent<CinemachineTargetGroup>();
                   
                        group.RotationMode = CinemachineTargetGroup.RotationModes.GroupAverage;
                        group.AddMember(target, 1, 0.5f);
                        property.objectReferenceValue = group.Transform;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                });
                var customProp = mainProperty.FindPropertyRelative(() => def.CustomLookAtTarget);
                var isCustom = customProp.boolValue;
                menu.AddItem(new GUIContent("Use Separate LookAt Target"), isCustom, () =>
                {
                    customProp.boolValue = !isCustom;
                    customProp.serializedObject.ApplyModifiedProperties();
                });
                menu.ShowAsContext();
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ux = new VisualElement();
            var follow = ux.AddChild(CreateTargetProperty(property.FindPropertyRelative(() => def.TrackingTarget), property));
            var lookAt = ux.AddChild(CreateTargetProperty(property.FindPropertyRelative(() => def.LookAtTarget), property));
            
            var customProp = property.FindPropertyRelative(() => def.CustomLookAtTarget);
            TrackCustomProp(customProp);
            ux.TrackPropertyValue(customProp, TrackCustomProp);
            void TrackCustomProp(SerializedProperty p) => lookAt.SetVisible(p.boolValue);

            return ux;
        }

        VisualElement CreateTargetProperty(SerializedProperty property, SerializedProperty mainProperty)
        {
            var customProp = mainProperty.FindPropertyRelative(() => def.CustomLookAtTarget);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row }};
            row.Add(new PropertyField(property) { style = { flexGrow = 1 }});
            var button = new Button { style = 
            { 
                backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("_Popup").image,
                width = InspectorUtility.SingleLineHeight, height = InspectorUtility.SingleLineHeight,
                alignSelf = Align.Center,
                paddingRight = 0, borderRightWidth = 0, marginRight = 0
            }};
            row.Add(button);

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
