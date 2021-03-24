using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePOV))]
    sealed class CinemachinePOVEditor : BaseEditor<CinemachinePOV>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawRemainingPropertiesInInspector();
            
#if CINEMACHINE_UNITY_INPUTSYSTEM
            var myScript = (CinemachinePOV) target;
            CinemachineDefaultMouseInput.InputProviderButton(EditorGUILayout.GetControlRect(true), 
                myScript.transform.parent.gameObject);
#endif
        }
        
        void OnEnable()
        {
            Target.UpdateInputAxisProvider();
        }
    }
}
