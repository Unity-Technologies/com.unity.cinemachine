# Position Control

Use the Position Control behaviours to specify the algorithm that moves the CmCamera in the Scene. To rotate the camera, set the [Rotation Control](CinemachineVirtualCameraAim.md) behaviours.

![__Position Control__, with the __Follow__ algorithm (red)](images/CinemachineBody.png)

Cinemachine includes these behaviours for moving a CmCamera:

- [__None__](CinemachineBodyDoNothing.md): does not procedurally move the Camera.
- [__Follow__](CinemachineFollow.md): moves in a fixed relationship to the __Tracking Target__.
- [__Orbital Follow__](CinemachineOrbitalFollow.md): moves in a variable relationship to the __Tracking Target__, optionally accepting player input.
- [__3rd Person follow__](Cinemachine3rdPersonFollow.md): Pivots the camera horizontally and vertically around the player, with the pivot point at the __Tracking Target__, following the rotation of the tracking target.
- [__Position Composer__](CinemachinePositionComposer.md): moves in a fixed screen-space relationship to the __Tracking Target__.
- [__Hard Lock to Target__](CinemachineHardLockToTarget.md): uses the same position as the __Tracking Target__.
- [__Spline Dolly__](CinemachineSplineDolly.md): moves along a predefined path, specified by a Spline.











