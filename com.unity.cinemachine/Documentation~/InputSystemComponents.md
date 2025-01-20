# Use Input System with Cinemachine

For more complex input configurations like supporting multiple devices, you will need to receive inputs from the `PlayerInput` component provided by the Input System package. The following section assumes you already know how to setup this component. For more information, see the [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.5/manual/index.html) documentation and samples.

### Read from PlayerInput

To read values from a `PlayerInput` with a `behaviour` set to `InvokeCSharpEvents`, you need to create a custom `InputAxisController` that subscribes to `onActionTriggered`. The example below shows how to receive and wire those inputs accordingly. Add this script to your `CinemachineCamera` and assign the `PlayerInput` field.

```cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// This class receives input from a PlayerInput component and dispatches it
// to the appropriate Cinemachine InputAxis.  The playerInput component should
// be on the same GameObject, or specified in the PlayerInput field.
class CustomInputHandler : InputAxisControllerBase<CustomInputHandler.Reader>
{
    [Header("Input Source Override")]
    public PlayerInput PlayerInput;

    void Awake()
    {
        // When the PlayerInput receives an input, send it to all the controllers
        if (PlayerInput == null)
            TryGetComponent(out PlayerInput);
        if (PlayerInput == null)
            Debug.LogError("Cannot find PlayerInput component");
        else
        {
            PlayerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            PlayerInput.onActionTriggered += (value) =>
            {
                for (var i = 0; i < Controllers.Count; i++)
                    Controllers[i].Input.ProcessInput(value.action);
            };
        }
    }

    // We process user input on the Update clock
    void Update()
    {
        if (Application.isPlaying)
            UpdateControllers();
    }

    // Controllers will be instances of this class.
    [Serializable]
    public class Reader : IInputAxisReader
    {
        public InputActionReference Input;
        Vector2 m_Value; // the cached value of the input

        public void ProcessInput(InputAction action)
        {
            // If it's my action then cache the new value
            if (Input != null && Input.action.id == action.id)
            {
                if (action.expectedControlType == "Vector2")
                    m_Value = action.ReadValue<Vector2>();
                else
                    m_Value.x = m_Value.y = action.ReadValue<float>();
            }
        }

        // IInputAxisReader interface: Called by the framework to read the input value
        public float GetValue(UnityEngine.Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            return (hint == IInputAxisOwner.AxisDescriptor.Hints.Y ? m_Value.y : m_Value.x);
        }
    }
}
```

For more information, see the [Cinemachine Multiple Camera](CinemachineMultipleCameras.md) documentation and example if you need to dynamically instantiate cameras.
