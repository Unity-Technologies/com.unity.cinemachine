# Cinemachine Input Axis Controller

Cinemachine cameras don't directly process user input.  Instead, they expose axes that are meant to be _driven_, either by script, or animation, or by user input.  As much as possible Cinemachine remains agnostic about where the input is coming from.  This way, it can be compatible with Unity's Input package, Unity's legacy input manager, or other third-party input systems.

Included with Cinemachine is the CinemachineAxisController component.  When you add it to a CmCamera, it detects any axes that can be driven by user input and exposes settings that allow you to control those axis values.

It is compatible with both Unity's Input Package and Unity's legacy input manager, and can even be customized to support 3rd-party input systems.  You can also use it as a template for writing your own custom input handlers.

The Input Axis Controller not only maps inputs to the exposed axes, it also provides settings for each axis to implement recentering, and to tune the responsiveness with accel/decel and gain.

If you like, you can also use CinemachineAxisController with your own scripts to drive input axes, for example in scripts that implement player motion.  See the Cinemachine Sample scenes for examples of this.


## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Player Index__ | Which player's input controls to query.  Leave this at the default value of -1 for single-player games.  Otherwise this should be the index of the player in the `UnityEngine.Input.InputUser.all` list.  This setting only appears if Unity's Input package is installed. |
| __Auto Enable Inputs__ | If Unity's Input package is installed, this option is available.  It will automatically enable any mapped input actions at startup |
| __Legacy Input__ | If the legacy input manager is being used, the Input Axis Name to query is specified here. |
| __Legacy Gain__ | If the legacy input manager is being used, the input value read will be multiplied by this amount. |
| __Input Action__ | If the Unity Input package is being used, the Input Action reference to drive the axis is set here. |
| __Gain__ | If the Unity Input package is being used, the input value read is multiplied by this amount. |
| __Input Value__ | The input value read this frame |
| __Accel Time__ | The time it takes for the input value to accelerate to a larger value |
| __Decel Time__ | The time it takes for the input value to decelerate to a smaller value |
| __Recentering Wait__ | If recentering is enabled, it will wait this many seconds after the last user input to begin recentering. |
| __Recentering Time__ | The time it takes for the recentering to complete, once it has started. |
