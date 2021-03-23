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
            "Add CinemachineInputProvider", "Adds CinemachineInputProvider to this vcam, if it does not have one already, " +
            "enabling the vcam to read input from Input Actions. By default, a simple mouse XY input action is added.");

        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawRemainingPropertiesInInspector();
            
            var rect = EditorGUILayout.GetControlRect(true);
            if (GUI.Button(rect, m_InputProviderAddLabel))
            {
                var myScript = ((CinemachinePOV)target);
                
                // parent is the vcam, because POV is a hidden child gameobject
                var myGO = myScript.transform.parent.gameObject; 
                var inputProvider = myGO.GetComponent<CinemachineInputProvider>();
                if (inputProvider == null)
                {
                    inputProvider = myGO.AddComponent<CinemachineInputProvider>();
                    inputProvider.XYAxis = CinemachineDefaultMouseInput.GetInputActionReference();
                }
            }
        }
        
        void OnEnable()
        {
            Target.UpdateInputAxisProvider();
        }
    }
}
