using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePOV))]
    sealed class CinemachinePOVEditor : BaseEditor<CinemachinePOV>
    {
        GUIContent m_InputProviderAddLabel = new GUIContent(
            "Add cm input provider", "Adds CinemachineInputProvider to this vcam, enabling it to read input from" +
            "input actions using the UnityEngine.Input package API.");

        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawRemainingPropertiesInInspector();
            
            var rect = EditorGUILayout.GetControlRect(true);
            if (GUI.Button(rect, m_InputProviderAddLabel))
            {
                var myGO = ((CinemachinePOV)target).gameObject;
                var inputProvider = myGO.GetComponent<CinemachineInputProvider>();
                if (inputProvider == null)
                {
                    inputProvider = myGO.AddComponent<CinemachineInputProvider>();
                    //inputProvider.XYAxis = 
                }
            }
        }
        
        void OnEnable()
        {
            Target.UpdateInputAxisProvider();
        }
    }
}
