#if CINEMACHINE_UNITY_INPUTSYSTEM

using System.Collections.Generic;
using System.Linq;
using Cinemachine.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cinemachine
{
    /// <summary>
    /// Provides a simple API to create a button that can add a CinemachineInputProvider component with a default
    /// input asset controlling XY axis from the InputSystem package to the gameObject if it does not
    /// already have CinemachineInputProvider.
    /// </summary>
    class CinemachineDefaultMouseInput
    {
        static CinemachineDefaultMouseInput s_Instance;
        InputActionReference m_InputActionReference = null;
        
        /// <summary>
        /// Initialize-on-demand singleton.
        /// </summary>
        /// <returns>Initialized instance</returns>
        public static CinemachineDefaultMouseInput GetInstance() {
            if (s_Instance == null) {
                s_Instance = new CinemachineDefaultMouseInput();
            }
            return s_Instance;
        }

        CinemachineDefaultMouseInput()
        {
            var inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Packages/com.unity.inputsystem/" +
                "InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions");

            InputAction look;
            var enumerator = inputActionAsset.GetEnumerator();
            for (int i = 0; i < 40; ++i)
            {
                if (enumerator.Current != null && 
                    enumerator.Current.ToString() == "Player/Look[/Mouse/delta,/Pen/delta]")
                {
                    look = enumerator.Current;
                    m_InputActionReference = InputActionReference.Create(look);
                    m_InputActionReference.name = "Generic Look";
                    break;
                }
                enumerator.MoveNext();
            }
        }
        InputActionReference GetInputActionReference()
        {
            return m_InputActionReference;
        }

        static GUIContent s_InputProviderAddLabel = new GUIContent(
            "Add CinemachineInputProvider", "Adds CinemachineInputProvider to this vcam, if it does not have one already, " +
            "enabling the vcam to read input from Input Actions. By default, a simple mouse XY input action is added.");
        
        /// <summary>
        /// Adds an information sign and a button that adds adds CinemachineInputProvider component to the vcam with a
        /// default look control (XY axis), if the gameobject has at least one component or extension that requires
        /// input and the vcam does not already have a CinemachineInputProvider component. For a component or extension
        /// to require input, the component or extension needs to override RequiresUserInput in CinemachineComponentBase or
        /// CinemachineExtension respectively.
        /// <seealso cref="CinemachineVirtualCameraBaseEditor{T}"/>
        /// </summary>
        /// <param name="gameObject">The gameObject to which we'd like to add the CinemachineInputProvider
        /// via a Button interface</param>
        public void InputProviderButton(GameObject gameObject)
        {
            var inputProvider = gameObject.GetComponent<CinemachineInputProvider>();
            if (inputProvider != null) return;
            EditorGUILayout.HelpBox("InputSystem package is installed, but it is not used to control this vcam.", 
                MessageType.Info);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect = EditorGUILayout.GetControlRect(true);
            if (GUI.Button(rect, s_InputProviderAddLabel))
            {
                inputProvider = gameObject.AddComponent<CinemachineInputProvider>();
                inputProvider.XYAxis = GetInputActionReference();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Checks whether components or extensions require user input.
        /// </summary>
        /// <param name="components">Components to check.</param>
        /// <param name="extensions">Extensions to check.</param>
        /// <returns></returns>
        public bool UserInputRequiredByComponentsOrExtensions(
            CinemachineComponentBase[] components, List<CinemachineExtension> extensions)
        { 
            return components != null && components.Any(t => t != null && t.RequiresUserInput) || 
                extensions != null && extensions.Any(t => t != null && t.RequiresUserInput);
        }
    }
}

#endif