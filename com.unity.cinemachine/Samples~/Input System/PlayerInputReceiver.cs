using UnityEngine;
using Unity.Cinemachine;
using System;
using UnityEngine.InputSystem;

class PlayerInputReceiver : InputAxisControllerBase<SimpleReader>
{
    [SerializeField]
    private PlayerInput m_PlayerInput;

    private void Awake()
    {
        m_PlayerInput.onActionTriggered += OnActionTriggered;
    }

    public void OnActionTriggered(InputAction.CallbackContext value)
    {
        foreach (var controller in Controllers)
        {
            controller.Input.SetValue(value.action);
        }
    }
}

[Serializable]
class SimpleReader : IInputAxisReader
{
    private Vector2 m_Value;
    [SerializeField]
    private InputActionReference m_Input;
    public void SetValue(InputAction action)
    {
        if (m_Input!= null && m_Input.action.id == action.id)
            m_Value = action.ReadValue<Vector2>();
    }
    
    public float GetValue(UnityEngine.Object context, int playerIndex, bool autoEnableInput, IInputAxisOwner.AxisDescriptor.Hints hint)
    {
        if(hint == IInputAxisOwner.AxisDescriptor.Hints.X)
            return m_Value.x * 100;
        
        return m_Value.y * 100;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom InputAxisController editor, leveraging Unity.Cinemachine.Editor.InputAxisControllerEditor
/// </summary>
[UnityEditor.CustomEditor(typeof(PlayerInputReceiver))]
public class PlayerInputReceiverEditor : Unity.Cinemachine.Editor.InputAxisControllerEditor {}
#endif
