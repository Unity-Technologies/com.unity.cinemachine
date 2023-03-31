/*using UnityEngine;
using Unity.Cinemachine;
using System;
using UnityEngine.UI;
using Object = UnityEngine.Object;


public class SliderInputController : InputAxisControllerBase<SliderInputController.SliderReader>
{
    void Update()
    {
        if (Application.isPlaying)
            UpdateControllers();
    }

    [Serializable]
    public class SliderReader : IInputAxisReader
    {
        
        public Slider m_Slider;

        public float GetValue(Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            if (m_Slider is not null)
                return m_Slider.value;

            return 0;
        }
    }
}*/

using UnityEngine;
using Unity.Cinemachine;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

class PlayerInputReceiver : InputAxisControllerBase<PlayerInputReceiver.SimpleReader>
{
    [SerializeField]
    PlayerInput m_PlayerInput;

    void Awake()
    {
        // Call back when the PlayerInput receives an Input.
        m_PlayerInput.onActionTriggered += OnActionTriggered;
    }
    
    void Update()
    {
        if (Application.isPlaying)
            UpdateControllers();
    }

    public void OnActionTriggered(InputAction.CallbackContext value)
    {
        // Sends the Input to all the controllers.
        for (var i = 0; i < Controllers.Count; i ++)
        {
            Controllers[i].Input.SetValue(value.action);
        }
    }
    
    [Serializable]
    public class SimpleReader : IInputAxisReader
    {
        // Assumes the action is a Vector2 for simplicity but can be changed for a float.
        Vector2 m_Value;
        [SerializeField]
        InputActionReference m_Input;
        [SerializeField]
        float m_Gain = 1;
    
        public void SetValue(InputAction action)
        {
            // If the input referenced in the inspector matches the updated one update the value.
            if (m_Input != null && m_Input.action.id == action.id)
            {
                if (action.expectedControlType == "Vector2")
                    m_Value = action.ReadValue<Vector2>();
                else
                    m_Value = new Vector2(action.ReadValue<float>(), action.ReadValue<float>());
            }
        }

        public float GetValue(Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            if(hint == IInputAxisOwner.AxisDescriptor.Hints.X)
                return m_Value.x * m_Gain;
        
            return m_Value.y * m_Gain;
        }
    }
}
