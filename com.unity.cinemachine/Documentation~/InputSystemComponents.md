# Using Input System components with Cinemachine

For more complex input configurations like supporting multiple devices, you will need to receive inputs from the `PlayerInput` component provided by the Input System package. The following section assumes you already know how to setup this component. For more information, see the [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.5/manual/index.html) documentation and samples.

### Read from PlayerInput

To read values from a `PlayerInput` with a `behaviour` set to `InvokeCSharpEvents`, you need to create a custom `InputAxisController` that subscribes to `onActionTriggered`. The example below shows how to receive and wire those inputs accordingly. Add this script to your `CinemachineCamera` and assign the `PlayerInput` field.

```cs
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
            if (m_Input!= null && m_Input.action.id == action.id)
                m_Value = action.ReadValue<Vector2>();
        }

        public float GetValue(Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            if(hint == IInputAxisOwner.AxisDescriptor.Hints.X)
                return m_Value.x * m_Gain;
        
            return m_Value.y * m_Gain;
        }
    }
}
```

For more information, see the [Cinemachine Multiple Camera](CinemachineMultipleCameras.md) documentation and example if you need to dynamically instantiate cameras.
