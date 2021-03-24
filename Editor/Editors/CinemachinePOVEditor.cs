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
        }
        
        void OnEnable()
        {
            Target.UpdateInputAxisProvider();
        }
    }
}
