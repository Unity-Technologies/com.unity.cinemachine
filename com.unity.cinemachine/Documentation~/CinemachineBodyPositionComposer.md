# Position Composer

This Cinemachine Camera __Body__ algorithm moves the camera in a fixed screen-space relationship to the __Tracking Target__. You can also specify offsets, damping, and composition rules. __Position Composer__ only changes the camera’s position in space. It does not re-orient or otherwise aim the camera.

__Position Composer__ is designed for 2D and orthographic cameras, but it works also with perspective cameras and 3D environments.

This algorithm first moves the camera along the camera Z axis until the __Tracking Target__ is at the desired distance from the camera’s X-Y plane. It then moves the camera in its X-Y plane until the __Tracking Target__ is at the desired point on the camera’s screen.

If the __Tracking Target__ is a [Target Group](CinemachineTargetGroup.md), then additional properties are available to frame the entire group.

## Properties

| **Property:** || **Function:** |
|:---|:---|:---|
| __Lookahead Time__ || Adjusts the offset of the Cinemachine Camera from the Tracking target based on the motion of the target. Cinemachine estimates the point where the target will be this many seconds into the future. This feature is sensitive to noisy animation and can amplify the noise, resulting in undesirable camera jitter. If the camera jitters unacceptably when the target is in motion, turn down this property, or animate the target more smoothly. |
| __Lookahead Smoothing__ || The smoothness of the lookahead algorithm. Larger values smooth out jittery predictions and increase prediction lag. |
| __Lookahead Ignore Y__ || If enabled, ignore movement along the Y axis for lookahead calculations. |
| __Camera Distance__ || The distance to maintain along the camera axis from the Tracking target. |
| __Dead Zone Depth__ || Do not move the camera along its z-axis if the Tracking target is within this distance of the specified camera distance. |
| __Damping__ || How responsively the camera tries to maintain the desired position, in each of the three camera-space axes.  Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  Using different settings per axis can yield a wide range of camera behaviors. |
| __Screen Position__ || Horizontal and vertical screen position for the target. The camera moves to position the tracked object here. 0 is screen center, -1 and 1 are the screen edges. |
| __Dead Zone Size__ || Do not move the camera horizontally or vertically when the target is within this range of the Screen Position. |
| __Soft Zone Size__ || When the target is within this range, move the camera horizontally and vertically to frame the target in the dead zone. The Damping properties affect the rate of the camera movement witin this range.  |
| __Bias__ || Shifts the target position horizontally and vertically away from the center of the soft zone. |
| __Unlimited Soft Zone__ || If enabled, then the soft zone is unlimited in size. |
| __Center On Activate__ || Moves the camera to put the target at the center of the dead zone when the camera becomes live. |
| __Group Framing Mode__ || Group Framing is available when the Tracking Target is a [Target Group](CinemachineTargetGroup.md). Specifies the screen dimensions to consider when framing.  |
| | _Horizontal_ | Consider only the horizontal dimension. Ignore vertical framing. |
| | _Vertical_ | Consider only the vertical dimension. Ignore horizontal framing. |
| | _Horizontal And Vertical_ | Use the larger of the horizontal and vertical dimensions to get the best fit. |
| | _None_ | Don’t do any framing adjustment. |
| __Adjustment Mode__ || How to adjust the camera to get the desired framing. You can zoom, dolly in or out, or do both. Available when the Tracking Target is a Target Group.  |
| | _Zoom Only_ | Don’t move the camera, only adjust the FOV. |
| | _Dolly Only_ | Move the camera, don’t change the FOV. |
| | _Dolly Then Zoom_ | Move the camera as much as permitted by the ranges, then adjust the FOV if necessary to make the shot. |
| __Group Framing Size__ || The bounding box that the targets should occupy. Use 1 to fill the whole screen, 0.5 to fill half the screen, and so on. Available when the Tracking Target is a Target Group.  |
| __Dolly Range__ || The allowable range that the camera may be moved in order to achieve the desired framing.  Negaive distance is towards the target, positive distance is away from the target. Available when the Tracking Target is a Target Group.  |
| __Target Distance Range__ || Set this to limit the camera distance from the target.  The camera may only be psitioned in this range, regardless of the other settings. Available when the Tracking Target is a Target Group.  |
| __Fov Range__ || If adjusting FOV, do not set the FOV outside of this range. Available when the Tracking Target is a Target Group.  |
| __Ortho Size Range__ || If adjusting Orthographic Size, do not set it outside of this range. Available when the Tracking Target is a Target Group.  |



