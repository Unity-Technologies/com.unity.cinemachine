# Position Composer

This CinemachineCamera __Position Control__ behavior moves the camera to maintain a desired screen-space position for the __Tracking Target__. You can also specify offsets, damping, and composition rules. __Position Composer__ only changes the camera’s position in space; it does not rotate the camera. To control the view angle of the camera, set the CinemachineCamera's rotation in its transform, or add a procedural [Rotation Control](CinemachineCamera.md#set-procedural-components-and-add-extension) component to the CinemachineCamera.

__Position Composer__ is good for 2D and orthographic cameras, and it also works with perspective cameras and 3D environments.

This algorithm first moves the camera along the camera Z axis until the __Tracking Target__ is at the desired distance from the camera’s X-Y plane. It then moves the camera in its X-Y plane until the __Tracking Target__ is at the desired point on the camera’s screen.

## Properties

| **Property** || **Function** |
|:---|:---|:---|
| __Target Offset__ || Position in target-local coordinates of the point of interest on the target to be tracked.  0, 0, 0 would be the target's origin. |
| __Lookahead Time__ || Adjusts the offset of the Cinemachine Camera from the Tracking target based on the motion of the target. Cinemachine estimates the point where the target will be this many seconds into the future. This feature is sensitive to noisy animation and can amplify the noise, resulting in undesirable camera jitter. If the camera jitters unacceptably when the target is in motion, turn down this property, or animate the target more smoothly. |
| __Lookahead Smoothing__ || The smoothness of the lookahead algorithm. Larger values smooth out jittery predictions and increase prediction lag. |
| __Lookahead Ignore Y__ || If enabled, ignore movement along the Y axis for lookahead calculations. |
| __Camera Distance__ || The distance to maintain along the camera axis from the Tracking target. |
| __Dead Zone Depth__ || Do not move the camera along its z-axis if the Tracking target is within this distance of the specified camera distance. |
| __Damping__ || How responsively the camera tries to maintain the desired position, in each of the three camera-space axes.  Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  Using different settings per axis can yield a wide range of camera behaviors. |
| __Screen Position__ || Horizontal and vertical screen position for the target. The camera adjusts to position the tracked object here. 0 is the screen center, -0.5 and 0.5 are the screen edges. |
| __Dead Zone__ || The camera will not adjust when the target is within this range of the Screen Position. |
|| _Size_| The width and height of the region where the camera will not respond to target movement, expressed as a fraction of screen size.  This region is centered around the Screen Position.  A value of 1 means full screen width or height. |
| __Hard Limits__ || The camera will not allow the target to be outside of the hard limits. |
|| _Size_ | The size of the region in which the camera can place the target, expressed as a fraction of screen size.  This region is by default centered around the Screen Position, but can be shifted using the Offset setting.  A value of 1 means full screen width or height. |
|| _Offset_ | Shifts the hard limits horizontally or vertically relative to the Target Position. |
| __Center On Activate__ || Moves the camera to put the target at the center of the dead zone when the camera becomes live. |

## Shot composition

[!include[](includes/shot-composition.md)]
