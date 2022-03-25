using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOrbitalFollow))]
    [CanEditMultipleObjects]
    internal class CinemachineOrbitalFollowEditor : BaseEditor<CinemachineOrbitalFollow>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            switch (Target.BindingMode)
            {
                default:
                case CinemachineTransposer.BindingMode.LockToTarget:
                    if (Target.RotationDampingMode == CinemachineTransposer.AngularDampingMode.Euler)
                        excluded.Add(FieldPath(x => x.QuaternionDamping));
                    else
                        excluded.Add(FieldPath(x => x.RotationDamping));
                    break;
                case CinemachineTransposer.BindingMode.WorldSpace:
                    excluded.Add(FieldPath(x => x.RotationDampingMode));
                    excluded.Add(FieldPath(x => x.RotationDamping));
                    excluded.Add(FieldPath(x => x.QuaternionDamping));
                    break;
                case CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp:
                    excluded.Add(FieldPath(x => x.RotationDampingMode));
                    excluded.Add(FieldPath(x => x.RotationDamping));
                    excluded.Add(FieldPath(x => x.QuaternionDamping));
//GML TODO                    excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
                    break;
            }
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool noFollow = false;
            bool noHandler = false;
            for (int i = 0; i < targets.Length; ++i)
            {
                noFollow |= (targets[i] as CinemachineOrbitalFollow).FollowTarget == null;
                noHandler |= !(targets[i] as CinemachineOrbitalFollow).HasInputHandler;
            }

            if (noFollow)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Orbital Follow requires a Follow target.",
                    MessageType.Warning);
            }
            if (noHandler)
            {
                InspectorUtility.HelpBoxWithButton(
                    "Orbital Follow has no input axis controller behaviour.", MessageType.Info,
                    new GUIContent("Add Input\nController"), () =>
                    {
                        Undo.SetCurrentGroupName("Add Input Axis Controller");
                        for (int i = 0; i < targets.Length; ++i)
                        {
                            var t = targets[i] as CinemachineOrbitalFollow;
                            if (!t.HasInputHandler)
                            {
                                var controller = t.VirtualCamera.GetComponent<InputAxisController>();
                                if (controller == null)
                                    Undo.AddComponent<InputAxisController>(t.VirtualCamera.gameObject);
                                else if (!controller.enabled)
                                {
                                    Undo.RecordObject(controller, "enable controller");
                                    controller.enabled = true;
                                }
                            }
                        }
                    });
            }
            DrawRemainingPropertiesInInspector();
        }
    }
}
