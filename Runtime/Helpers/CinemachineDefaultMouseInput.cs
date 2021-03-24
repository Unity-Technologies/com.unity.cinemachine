#if CINEMACHINE_UNITY_INPUTSYSTEM

using System;
using UnityEditor;
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
        public static void InputProviderButton(Rect rect, GameObject myGameObject)
        {
            if (GUI.Button(rect, m_InputProviderAddLabel))
            {
                var inputProvider = myGameObject.GetComponent<CinemachineInputProvider>();
                if (inputProvider == null)
                {
                    inputProvider = myGameObject.AddComponent<CinemachineInputProvider>();
                    inputProvider.XYAxis = GetInputActionReference();
                }
            }
        }
    }
}

#endif