# Body properties

Use the Body properties to specify the algorithm that moves the Virtual Camera in the Scene. To rotate the camera, set the [Aim properties](CinemachineVirtualCameraAim.md).

![__Body__ properties, with the __Transposer__ algorithm (red)](images/CinemachineBody.png)

Cinemachine includes these algorithms for moving a Cm Camera:


- [__None__](CinemachineBodyDoNothing.md): does not procedurally move the Camera.
- [__3rd Person follow__](Cinemachine3rdPersonFollow.md): Pivots the camera horizontally and vertically around the player, with the pivot point at the __Tracking Target__, following the rotation of the tracking target.
- [__Position Composer__](CinemachineBodyPositionComposer.md): moves in a fixed screen-space relationship to the __Tracking Target__.
- [__Hard Lock to Target__](CinemachineBodyHardLockTarget.md): uses the same position as the __Tracking Target__.
- [__Orbital Transposer__](CinemachineBodyOrbitalTransposer.md): moves in a variable relationship to the __Tracking Target__, optionally accepting player input.
- [__Spline Dolly__](CinemachineBodyTrackedDolly.md): moves along a predefined path, specified by a Spline.
- [__Transposer__](CinemachineBodyTransposer.md): moves in a fixed relationship to the __Tracking Target__.











