# Create a Third Person Camera

While it is possible - and often appropriate - to make a Third Person camera using a [FreeLook Camera](FreeLookCameras.md), there are cases where that doesn't give you all the control you need.  For example, when you want to have an over-the-shoulder offset, or precise aiming control (in a shooter, for instance) and want to keep that control while blending to an aiming camera, it can be difficult to maintain the desired precision with the FreeLook.

To address this problem, Cinemachine provides the [Third Person Follow](CinemachineThirdPersonFollow.md) behaviour.  The paradigm for using this behaviour is not the same as with the FreeLook.  Specifically, the ThirdPersonCamera is rigidly attached to the Tracking target, and to aim the camera, you must rotate the target itself.  The camera's forward direction will always match the Target's forward direction, even though the camera is offset a little from the target, as specified in the rig settings.

This means that aim control is not built into the camera, it must be provided by the target.  Often, the target will be an invisible child object of the player, thus decoupling player aim from player model rotation.  See these Cinemachine samples for examples of how this might be implemented:

 - ThirdPersonWithAimMode
 - ThirdPersonWithRoadieRun

These samples make use of the [Third Person Aim extension](CinemachineThirdPersonAim.md) which uses raycasts to determine what the camera is aiming at, and ensures that this point is locked to screen center, even if there is procedural noise active on the camera.
