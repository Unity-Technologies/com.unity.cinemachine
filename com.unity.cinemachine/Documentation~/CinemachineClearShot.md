# Cinemachine Clear Shot Camera

The __Cinemachine ClearShot Camera__ component chooses among its child CinemachineCameras for the best quality shot of the target. Use Clear Shot to set up complex multi-camera coverage of a Scene to guarantee a clear view of the target.

This can be a very powerful tool. CinemachineCamera children with [Cinemachine Deoccluder](CinemachineDeoccluder.md) or other shot quality evaluator extensions analyze the shot for target obstructions, optimal target distance, and so on. Clear Shot uses this information to choose the best child to activate.

**Tip:** To use a single [Cinemachine Deoccluder](CinemachineDeoccluder.md) for all CinemachineCamera children, add a Cinemachine Deoccluder extension to the ClearShot GameObject instead of each of its CinemachineCamera children. This Cinemachine Deoccluder extension applies to all of the children, as if each of them had that Deoccluder as its own extension.

If multiple child cameras have the same shot quality, the Clear Shot camera chooses the one with the highest priority.

You can also define custom blends between the ClearShot children.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Solo__ || Toggles whether or not the CinemachineCamera is temporarily live. Use this property to get immediate visual feedback in the [Game view](https://docs.unity3d.com/Manual/GameView.html) to adjust the CinemachineCamera. |
| __Game View Guides__ || Toggles the visibility of compositional guides in the Game view. These guides are available when Tracking Target specifies a GameObject and the CinemachineCamera has a screen-composition behavior, such as Position Composer or Rotation Composer. This setting is shared by all CinemachineCameras. |
| __Save During Play__ || Check to [apply the changes while in Play mode](CinemachineSavingDuringPlay.md).  Use this feature to fine-tune a CinemachineCamera without having to remember which properties to copy and paste. This setting is shared by all CinemachineCameras. |
| __Custom Output__ || This setting controls how the output of this CinemachineCamera is used by the CinemachineBrain.  Enable this to use Priorities or custom CM output channels. |
|| _Channel_ | This controls which CinemachineBrain will be driven by this camera.  It is needed when there are multiple CinemachineBrains in the scene (for example, when implementing split-screen). |
|| _Priority_ | This is used to control which of several active CinemachineCameras should be live, when not controlled by Timeline. By default, priority is 0.  Use this to specify a custom priority value. A higher value indicates a higher priority. Negative values are also allowed. Cinemachine Brain chooses the next live CinemachineCamera from all CinemachineCameras that are activated and have the same or higher priority as the current live CinemachineCamera. This property has no effect when using a CinemachineCamera with Timeline. |
| __Standby Update__ || Controls how often the Cinemachine Camera is updated when the Cinemachine Camera is not Live. Use this property to tune for performance. |
|  | _Never_ | Only update if the Cinemachine Camera is Live. Don't set this value if you're using the Cinemachine Camera in shot evaluation context. |
|  | _Always_ | Update the Cinemachine Camera every frame, even when it is not Live. |
|  | _Round Robin_ | Update the Cinemachine Camera occasionally, at a frequency that depends on how many other Cinemachine Cameras are in Standby. |
| __Default Target__ || If enabled, this target will be used as a fallback if child CinemachineCameras don't specify a Tracking Target of their own |
| __Show Debug Text__ || If enabled, current state information will be displayed in the Game View |
| __Activate After__ || Wait this many seconds before activating a new child camera. |
| __Min Duration__ || An active camera must be active for at least this many seconds, unless a higher-priority camera becomes active. |
| __Randomize Choice__ || Check to choose a random camera if multiple cameras have equal shot quality. Uncheck to use the order of the child CinemachineCameras and their priorities. |
| __Default Blend__ || The blend to use when you haven’t explicitly defined a blend between two CinemachineCameras. |


