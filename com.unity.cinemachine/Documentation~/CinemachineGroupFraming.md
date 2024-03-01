# Group Framing

This CinemachineCamera extension adds the ability to frame one or more targets when they are members of a CinemachineTargetGroup. It can be used to dynamically adjust the zoom or to move the camera closer to or farther from the targets, to keep them in the frame at the desired size.

For this to work, the CinemachineCamera's Tracking Target must be a CinemachineTargetGroup, with at least one member, and having a nonzero size.

## Properties

| **Property:** || **Function:** |
|:---|:---|:---|
| __Framing Mode__ || Specifies the screen dimensions to consider when framing.  |
| | _Horizontal_ | Consider only the horizontal dimension. Ignore vertical framing. |
| | _Vertical_ | Consider only the vertical dimension. Ignore horizontal framing. |
| | _Horizontal And Vertical_ | Use the larger of the horizontal and vertical dimensions to get the best fit. |
| __Adjustment Mode__ || How to adjust the camera in depth to get the desired framing. You can zoom, dolly in or out, or do both.  |
| | _Zoom Only_ | Don’t move the camera, only adjust the FOV. |
| | _Dolly Only_ | Move the camera, don’t change the FOV. |
| | _Dolly Then Zoom_ | Move the camera as much as permitted by the ranges, then adjust the FOV if necessary to make the shot. |
| __Lateral Adjustment__ || How to adjust the camera horizontally and vertically to get the desired framing. You can change position to reframe, or rotate the camera to reframe.  |
| | _Change Position_ | Camera is moved horizontally and vertically until the desired framing is achieved. |
| | _Change Rotation_ | Camera is rotated to achieve the desired framing. |
| __Framing Size__ || The screen-space bounding box that the targets should occupy. Use 1 to fill the whole screen, 0.5 to fill half the screen, and so on. |
| __Center Offset__ || A nonzero value will offset the group in the camera frame. |
| __Damping__ || How gradually to make the framing adjustment. A larger number gives a slower response, smaller numbers a snappier one. |
| __Dolly Range__ || The allowable range that the camera may be moved in order to achieve the desired framing. A negative distance is towards the target, and a positive distance is away from the target. |
| __FOV Range__ || If adjusting FOV, it will be clamped to this range.  |
| __Ortho Size Range__ || If adjusting Orthographic Size, it will be clamped to this range.  |



