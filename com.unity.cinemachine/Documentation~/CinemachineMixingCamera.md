# Cinemachine Mixing Camera

The __Cinemachine Mixing Camera__ component uses the weighted average of its child CmCameras to compute the position and other properties of the Unity camera.

![Cinemachine Mixing Camera with two child CmCameras (red)](images/CinemachineMixingCamera.png)

Mixing Camera manages up to eight child CmCameras. In the Mixing Camera component, these CmCameras are fixed slots, not a dynamic array. Mixing Camera uses this implementation to support weight animation in Timeline. Timeline cannot animate array elements.

To create a Mixing Camera:

1. In the Unity menu, choose __GameObject > Cinemachine > Mixing Camera__.
A new Mixing Camera appears in the [Hierarchy](https://docs.unity3d.com/Manual/Hierarchy.html) window. By default, Unity also adds two CmCameras as children of the Mixing Camera.

2. Adjust the children CmCameras.

3. Add up to six more child cameras.

4. Select the Mixing Camera in the Hierarchy window then adjust the Child Camera Weights in the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) window.

![Child Camera Weights (red) and their contributions to the final position (blue)](images/CinemachineMixingCameraChildren.png)

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Solo__ | Toggles whether or not the CmCamera is temporarily live. Use this property to get immediate visual feedback in the [Game view](https://docs.unity3d.com/Manual/GameView.html) to adjust the CmCamera. |
| __Game View Guides__ | Toggles the visibility of compositional guides in the Game view. These guides are available when Tracking Target specifies a GameObject and the CmCamera has a screen-composition behaviour, such as Position Composer or Rotation Composer. This setting is shared by all CmCameras. |
| __Save During Play__ | Check to [apply the changes while in Play mode](CinemachineSavingDuringPlay.md).  Use this feature to fine-tune a CmCamera without having to remember which properties to copy and paste. This setting is shared by all CmCameras. |
| __Priority__ | This is used to control which of several active CmCameras should be live, when not controlled by Timeline.  By default, priority is 0.  Enable this to specify a custom priority value.  A higher value indicates a higher priority.  Negative values are also allowed. Cinemachine Brain chooses the next live CmCamera from all CmCameras that are activated and have the same or higher priority as the current live CmCamera. This property has no effect when using a CmCamera with Timeline. |
| __Standby Update__ | Controls how often the CmCamera is updated when the CmCamera is not live. |
| __Default Target__ | If enabled, this target will be used as fallback if child CmCameras don't specify a Tracking Target of their own |
| __Show Debug Text__ | If enabled, current state information will be displayed in the Game View |
| __Child Camera Weights__ | The weight of the CmCamera. Each child CmCamera has a corresponding Weight property. Note that setting one camera's weight to 1 does not put the other weights to zero.  The contribution of any individual camera is its weight divided by the sum of all the child weights. |
| __Mix Result__ | A graphical representation of the weights of the child CmCameras. The light part of the bar of each child camera represents the proportion of its contribution to the final position of the Mixing Camera. When the bar is completely dark, the camera makes no contribution to the position of the Mixing Camera. |


