# Position Composer

This CmCamera __Position Control__ behavior moves the camera to maintain a desired screen-space position for the __Tracking Target__. You can also specify offsets, damping, and composition rules. __Position Composer__ only changes the camera’s position in space; it does not rotate the camera. To control the view angle of the camera, set the CmCamera's rotation in its transform, or add a procedural [Rotation Control](CinemachineVirtualCameraAim.md) component to the CmCamera.

__Position Composer__ is good for 2D and orthographic cameras, and it also works with perspective cameras and 3D environments.

This algorithm first moves the camera along the camera Z axis until the __Tracking Target__ is at the desired distance from the camera’s X-Y plane. It then moves the camera in its X-Y plane until the __Tracking Target__ is at the desired point on the camera’s screen.

## Properties

| **Property:** || **Function:** |
|:---|:---|:---|
| __Tracked Object Offset__ || Position in target-local coordinates of the point of interest on the target to be tracked.  0, 0, 0 would be the target's origin. |
| __Lookahead Time__ || Adjusts the offset of the Cinemachine Camera from the Tracking target based on the motion of the target. Cinemachine estimates the point where the target will be this many seconds into the future. This feature is sensitive to noisy animation and can amplify the noise, resulting in undesirable camera jitter. If the camera jitters unacceptably when the target is in motion, turn down this property, or animate the target more smoothly. |
| __Lookahead Smoothing__ || The smoothness of the lookahead algorithm. Larger values smooth out jittery predictions and increase prediction lag. |
| __Lookahead Ignore Y__ || If enabled, ignore movement along the Y axis for lookahead calculations. |
| __Camera Distance__ || The distance to maintain along the camera axis from the Tracking target. |
| __Dead Zone Depth__ || Do not move the camera along its z-axis if the Tracking target is within this distance of the specified camera distance. |
| __Damping__ || How responsively the camera tries to maintain the desired position, in each of the three camera-space axes.  Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  Using different settings per axis can yield a wide range of camera behaviors. |
| __Screen Position__ || Horizontal and vertical screen position for the target. The camera moves to position the tracked object here. 0 is the screen center, -1 and 1 are the screen edges. |
| __Dead Zone Size__ || Do not move the camera horizontally or vertically when the target is within this range of the Screen Position. |
| __Soft Zone Size__ || When the target is within this range, move the camera horizontally and vertically to frame the target in the dead zone. The Damping properties affect the rate of the camera movement within this range.  |
| __Bias__ || Shifts the target position horizontally and vertically away from the center of the soft zone. |
| __Unlimited Soft Zone__ || If enabled, then the soft zone is unlimited in size. |
| __Center On Activate__ || Moves the camera to put the target at the center of the dead zone when the camera becomes live. |
