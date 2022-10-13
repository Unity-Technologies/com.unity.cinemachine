# Cinemachine Blend List Camera

The __Cinemachine Blend List Camera__ component executes a sequence of blends or cuts among its child CmCameras.

When the Blend List camera is activated, it executes its list of instructions, activating the first child CmCamera in the list, holding for a designated time, then cutting or blending to the next child, and so on. The Blend List camera holds the last CmCamera until Cinemachine Brain or Timeline deactivates the Blend List camera.

**Tip**: Use a Blend List Camera instead of  [Timeline](CinemachineTimeline.md) for simpler, automatic sequences.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Solo__ || Toggles whether or not the CmCamera is temporarily live. Use this property to get immediate visual feedback in the [Game view](https://docs.unity3d.com/Manual/GameView.html) to adjust the CmCamera. |
| __Game View Guides__ || Toggles the visibility of compositional guides in the Game view. These guides are available when Tracking Target specifies a GameObject and the CmCamera has a screen-composition behaviour, such as Position Composer or Rotation Composer. This setting is shared by all CmCameras. |
| __Save During Play__ || Check to [apply the changes while in Play mode](CinemachineSavingDuringPlay.md).  Use this feature to fine-tune a CmCamera without having to remember which properties to copy and paste. This setting is shared by all CmCameras. |
| __Custom Output__ || This setting controls how the output of this CmCamera is used by the CinemachineBrain.  Enable this to use Priorities or custom CM output channels. |
|| _Channel_ | This controls which CinemachineBrain will be driven by this camera.  It is needed when there are multiple CinemachineBrains in the scene (for example, when implementing split-screen). |
|| _Priority_ | This is used to control which of several active CmCameras should be live, when not controlled by Timeline. By default, priority is 0.  Use this to specify a custom priority value. A higher value indicates a higher priority. Negative values are also allowed. Cinemachine Brain chooses the next live CmCamera from all CmCameras that are activated and have the same or higher priority as the current live CmCamera. This property has no effect when using a CmCamera with Timeline. |
| __Standby Update__ || Controls how often the CmCamera is updated when the CmCamera is not live. |
| __Default Target__ || If enabled, this target will be used as fallback if child CmCameras don't specify a Tracking Target of their own |
| __Show Debug Text__ || If enabled, current state information will be displayed in the Game View |
| __Loop__ || When enabled, the child CmCameras will cycle indefintely instead of stopping on the last CmCamera in the list. |
| __Instructions__ || The set of instructions for enabling child cameras. |
