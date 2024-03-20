# Cinemachine Rotation Composer component

This CinemachineCamera __Rotation Control__ behaviour rotates the camera to face the __Look At__ target. It also applies offsets, damping, and composition rules. It only rotates the camera, it never changes the camera's position.  Examples of targets for aiming: the upper spine or head bone of a character, vehicles, or dummy objects which are controlled or animated programmatically.

## Properties

| **Property** || **Function** |
|:---|:---|:---|
| __Target Offset__ || Offset from the center of the Look At target, in target-local space. Use this to fine-tune the target's position when the point of interest is not the tracked object’s center. You can also use [Scene Handles](handles.md) to modify this property. |
| __Lookahead Time__ || Adjust the rotation based on the motion of the Look At target. The algorithm estimates the point that the target will be this many seconds into the future. This feature is sensitive to noisy animation. It can amplify the noise, resulting in undesirable camera jitter. If the camera jitters unacceptably when the target is in motion, turn down this property or animate the target more smoothly. |
| __Lookahead Smoothing__ || Controls the smoothness of the lookahead algorithm. Larger values smooth out jittery predictions and increase prediction lag. |
| __Lookahead Ignore Y__ || Enable ths to ignore movement along the Y axis for lookahead calculations. |
| __Damping__ || How responsively the camera frames the target in horizontal and vertical directions. Use small numbers for more responsive, rapid rotation of the camera to keep the target in the dead zone. Use larger numbers for a heavier, more slowly-responding camera.  |
| __Screen Position__ || Horizontal and vertical screen position for the target. The camera adjusts to position the tracked object here. 0 is the screen center, -0.5 and 0.5 are the screen edges. |
| __Dead Zone__ || The camera will not adjust when the target is within this range of the Screen Position. |
|| _Size_| The width and height of the region where the camera will not respond to target movement, expressed as a fraction of screen size.  This region is centered around the Screen Position.  A value of 1 means full screen width or height. |
| __Hard Limits__ || The camera will not allow the target to be outside of the hard limits. |
|| _Size_ | The size of the region in which the camera can place the target, expressed as a fraction of screen size.  This region is by default centered around the Screen Position, but can be shifted using the Offset setting.  A value of 1 means full screen width or height. |
|| _Offset_ | Shifts the hard limits horizontally or vertically relative to the Target Position. |
| __Center On Activate__ || Rotates the camera to put the target at the center of the dead zone when the camera becomes live. |

## Shot composition

[!include[](includes/shot-composition.md)]
