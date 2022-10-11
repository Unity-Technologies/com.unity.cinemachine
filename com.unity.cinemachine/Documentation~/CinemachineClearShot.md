# Cinemachine Clear Shot Camera

The __Cinemachine ClearShot Camera__ component chooses among its children CmCameras for the best quality shot of the target. Use Clear Shot to set up complex multi-camera coverage of a Scene to guarantee a clear view of the target.

This can be a very powerful tool. CmCamera children with [Cinemachine Collider](CinemachineCollider.md) extensions analyze the Scene for target obstructions, optimal target distance, and so on. Clear Shot uses this information to choose the best child to activate.

**Tip:** To use a single [Cinemachine Collider](CinemachineCollider.md) for all CmCamera children, add a Cinemachine Collider extension to the ClearShot GameObject instead of each of its CmCamera children. This Cinemachine Collider extension applies to all of the children, as if each of them had that Collider as its own extension.

If multiple child cameras have the same shot quality, the Clear Shot camera chooses the one with the highest priority.

You can also define custom blends between the ClearShot children.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Solo__ || Toggles whether or not the CmCamera is temporarily live. Use this property to get immediate visual feedback in the [Game view](https://docs.unity3d.com/Manual/GameView.html) to adjust the CmCamera. |
| __Game View Guides__ || Toggles the visibility of compositional guides in the Game view. These guides are available when Tracking Target specifies a GameObject and the CmCamera has a screen-composition behavior, such as Position Composer or Rotation Composer. This setting is shared by all CmCameras. |
| __Save During Play__ || Check to [apply the changes while in Play mode](CinemachineSavingDuringPlay.md).  Use this feature to fine-tune a CmCamera without having to remember which properties to copy and paste. This setting is shared by all CmCameras. |
| __Custom Output__ || This setting controls how the output of this CmCamera is used by the CinemachineBrain.  Enable this to use Priorities or custom CM output channels. |
|| _Channel_ | This controls which CinemachineBrain will be driven by this camera.  It is needed when there are multiple CinemachineBrains in the scene (for example, when implementing split-screen). |
|| _Priority_ | This is used to control which of several active CmCameras should be live, when not controlled by Timeline. By default, priority is 0.  Use this to specify a custom priority value. A higher value indicates a higher priority. Negative values are also allowed. Cinemachine Brain chooses the next live CmCamera from all CmCameras that are activated and have the same or higher priority as the current live CmCamera. This property has no effect when using a CmCamera with Timeline. 
| __Standby Update__ || Controls how often the CmCamera is updated when the CmCamera is not live. |
| __Default Target__ || If enabled, this target will be used as a fallback if child CmCameras don't specify a Tracking Target of their own |
| __Show Debug Text__ || If enabled, current state information will be displayed in the Game View |
| __Activate After__ || Wait this many seconds before activating a new child camera. |
| __Min Duration__ || An active camera must be active for at least this many seconds, unless a higher-priority camera becomes active. |
| __Randomize Choice__ || Check to choose a random camera if multiple cameras have equal shot quality. Uncheck to use the order of the child CmCameras and their priorities. |
| __Default Blend__ || The blend to use when you haven’t explicitly defined a blend between two CmCameras. |


