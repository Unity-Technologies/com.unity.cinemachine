using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(TargetTracking.TrackerSettings))]
    class TrackerSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            TargetTracking.TrackerSettings def = new();
            var modeProp = property.FindPropertyRelative(() => def.BindingMode);
            var rotModeProp = property.FindPropertyRelative(() => def.AngularDampingMode);

            var ux = new VisualElement();

            ux.Add(new PropertyField(modeProp));
            var rotModeField = ux.AddChild(new PropertyField(rotModeProp));
            var rotDampingField = ux.AddChild(new PropertyField(property.FindPropertyRelative(() => def.RotationDamping)));
            var quatDampingField = ux.AddChild(new PropertyField(property.FindPropertyRelative(() => def.QuaternionDamping)));
            ux.Add(new PropertyField(property.FindPropertyRelative(() => def.PositionDamping)));

            ux.TrackPropertyWithInitialCallback(modeProp, (modeProp) => UpdateRotVisibility());
            ux.TrackPropertyWithInitialCallback(rotModeProp, (modeProp) => UpdateRotVisibility());

            void UpdateRotVisibility()
            {
                if (modeProp.serializedObject == null)
                    return; // object deleted
                var mode = (TargetTracking.BindingMode)modeProp.intValue;
                var rotMode = (TargetTracking.AngularDampingMode)rotModeProp.intValue;

                bool showRot = mode != TargetTracking.BindingMode.WorldSpace && mode != TargetTracking.BindingMode.LazyFollow;
                bool showQuat = mode == TargetTracking.BindingMode.LockToTarget && rotMode == TargetTracking.AngularDampingMode.Quaternion;

                rotModeField.SetVisible(mode == TargetTracking.BindingMode.LockToTarget);
                rotDampingField.SetVisible(showRot && !showQuat);
                quatDampingField.SetVisible(showRot && showQuat);
            }

            return ux;
        }
    }
}
