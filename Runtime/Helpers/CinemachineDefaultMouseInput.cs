#if CINEMACHINE_UNITY_INPUTSYSTEM

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

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
        public static InputActionReference GetInputActionReference()
        {
            return s_InputActionReference;
        }
    }
}

#endif