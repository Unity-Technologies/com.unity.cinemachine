using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
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

            ux.TrackPropertyWithInitialCallback(customProp, (p) => lookAt.SetVisible(p.boolValue));

            return ux;
        }

        VisualElement CreateTargetProperty(SerializedProperty property, SerializedProperty customProp)
        {
            var row = InspectorUtility.PropertyRow(property, out _);
            row.Contents.Add(InspectorUtility.MiniPopupButton(null, new ContextualMenuManipulator((evt) =>
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
            })));
            return row;
        }
    }
}
