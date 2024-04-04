# Cinemachine Input Axis Controller

Cinemachine cameras don't directly process user input. Instead, they expose axes that are meant to be _driven_, either by script, animation, or by user input. As much as possible Cinemachine remains agnostic about where the input is coming from. This way, it can be compatible with Unity's Input package, Unity's legacy input manager, or other third-party input systems.

Included with Cinemachine is the CinemachineInputAxisController component. When you add it to a CinemachineCamera, it auto-detects any axes that can be driven by user input and exposes settings to allow you to control those axis values.

It is compatible with both Unity's Input Package and Unity's legacy input manager. You can also use it as a template for writing your own custom input handlers.

The Input Axis Controller not only maps inputs to the exposed axes, it also provides settings for each axis to tune the responsiveness with accel/decel and gain.

If you like, you can also use CinemachineInputAxisController with your own scripts to drive input axes, for example in scripts that implement player motion. See the Cinemachine Sample scenes for examples of this.

## Usage

This component makes it easy to control a `CinemachineCamera` in a single player environment with a mouse and keyboard or a controller.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Player Index__ | Which player's input controls to query. Leave this at the default value of -1 for single-player games. Otherwise, this should be the index of the player in the `UnityEngine.Input.InputUser.all` list. This setting only appears if Unity's Input package is installed. |
| __Auto Enable Inputs__ | If Unity's Input package is installed, this option is available. It will automatically enable any mapped input actions at startup |
| __Scan Recursively__ | If set, a recursive search for IInputAxisOwners behaviours will be performed.  Otherwise, only behaviours attached directly to this GameObject will be considered, and child objects will be ignored. |
| __Suppress Input While Blending__ | If set and if this component is attached to a CinemachineCamera, input will not be processed while the camera is participating in a blend. |
| __Ignore Time Scale__ | If set, then input will be processed using unscaled deltaTime, and not scaled deltaTime.  This allows input to continue even when the timescale is set to 0. |
| __Enabled__ | The controller will drive the input axis while this value is true.  If false, the axis will not be driven by the controller. |
| __Legacy Input__ | If the legacy input manager is being used, the Input Axis Name to query is specified here. |
| __Legacy Gain__ | If the legacy input manager is being used, the input value read will be multiplied by this amount. |
| __Input Action__ | If the Unity Input package is being used, the Input Action reference to drive the axis is set here. |
| __Gain__ | If the Unity Input package is being used, the input value read is multiplied by this amount. |
| __Input Value__ | The input value read during this frame |
| __Accel Time__ | The time it takes for the input value to accelerate to a larger value |
| __Decel Time__ | The time it takes for the input value to decelerate to a smaller value |
| __Cancel Delta Time__ | This will cancel the built-in deltaTime compensation done by the input axis.  Enable this if the input value is inherently dependent on frame time.  For example, mouse deltas will naturally be bigger for longer frames, so in this case the default deltaTime scaling should be canceled. |

## Creating your own Input Axis Controller

The default implementation of `CinemachineInputAxisController` can process input sources from the Input package and from Unity's legacy input system. 

For more complex scenarios (such as for a mobile device control), you can extend this default functionality and create your own Input Axis Controller using a script.

The following example shows how to use a custom Input Controller script to control a Camera with a slider for a mobile device. The sample code can be used as a template and is easily modified to be used with other objects.

```cs
using UnityEngine;
using Unity.Cinemachine;
using System;
using UnityEngine.UI;
using Object = UnityEngine.Object;

//The component that you will add to your CinemachineCamera.
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
}
```

For more information, see the [Input System Components](InputSystemComponents.md) documentation if you need to setup a local multiplayer input with the Input System package.
