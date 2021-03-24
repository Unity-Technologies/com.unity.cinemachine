#if CINEMACHINE_UNITY_INPUTSYSTEM

using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cinemachine
{
    /// <summary>
    /// Finds InputSystem package's default mouse control look input asset, and returns a reference to it.
    /// </summary>
    static class CinemachineDefaultMouseInput
    {
        static InputActionReference s_InputActionReference = null;
        static CinemachineDefaultMouseInput()
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
                    s_InputActionReference = InputActionReference.Create(look);
                    s_InputActionReference.name = "PlayerLook";
                    break;
                }
                enumerator.MoveNext();
            }
        }
        static InputActionReference GetInputActionReference()
        {
            return s_InputActionReference;
        }

        static GUIContent m_InputProviderAddLabel = new GUIContent(
            "Add CinemachineInputProvider", "Adds CinemachineInputProvider to this vcam, if it does not have one already, " +
            "enabling the vcam to read input from Input Actions. By default, a simple mouse XY input action is added.");
        public static void InputProviderButton(GameObject myGameObject)
        {
            var inputProvider = myGameObject.GetComponent<CinemachineInputProvider>();
            if (inputProvider != null) return;
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(
                "InputSystem package is installed, but it is not used to control this vcam.", 
                MessageType.Info);
            var helpBoxHeight = GUILayoutUtility.GetLastRect().height;
            var rect = EditorGUILayout.GetControlRect(true);
            rect.height = helpBoxHeight;
            if (GUI.Button(rect, m_InputProviderAddLabel))
            {
                inputProvider = myGameObject.AddComponent<CinemachineInputProvider>();
                inputProvider.XYAxis = GetInputActionReference();
            }
            GUILayout.EndHorizontal();
        }
    }
}

#endif