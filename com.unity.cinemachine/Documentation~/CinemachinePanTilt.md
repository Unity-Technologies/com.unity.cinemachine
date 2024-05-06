# Pan Tilt

This CinemachineCamera __Rotation Control__ behavior pans and tilts the camera in response stimulus, for instance the user’s input. This component does not read user input itself; it can be be driven by an [Cinemachine Input Axis Controller](CinemachineInputAxisController.md) component or by some other means that you devise.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Reference Frame__ || Defines the reference frame against which pan and tilt rotations are made.  |
| | _Parent Object_ | If the CinemachineCamera has a parent object, then that parent's local axes will be used as the reference frame. If there is no parent object, world axes will be used. |
| | _World_ | World axes will be used as the reference frame. |
| | _Tracking Target_ | If the CinemachineCamera has a Tracking target, then that object's local axes will be used as the reference frame. If there is no parent object, world axes will be used. |
| | _LookAt Target_ | If the CinemachineCamera has a LookAt target, then that object's local axes will be used as the reference frame. If there is no parent object, world axes will be used. |
| __Pan Axis__ || Controls the horizontal rotation of the Camera.  |
| | _Value_ | The current value of the axis, in degrees. |
| | _Center_ | The value that Recentering will recenter to, if Recentering is enabled. |
| | _Range_ | The minimum and maximum for the Value. |
| | _Wrap_ | If enabled, the axis wraps around when it reaches the end of its range, forming a loop. |
| __Tilt Axis__ || Controls the vertical rotation of the Camera.  |
| | _Value_ | The current value of the axis, in degrees. |
| | _Center_ | The value that Recentering will recenter to, if Recentering is enabled. |
| | _Range_ | The minimum and maximum for the Value. Must fall inside of [-90, 90]. |
| | _Wrap_ | If enabled, the axis wraps around when it reaches the end of its range, forming a loop. |
| __Recentering__ | | If enabled for an axis, Recentering will gradually return the axis value to the recentering target. |
|  | _Wait_ | If recentering is enabled for an axis, it will wait this many seconds after the last user input before beginning the recentering process. |
|  | _Time_ | The time it takes for the recentering to complete, once it has started. |
| __Recenter Target__ || When Axis Recentering happens, it will recenter towards this target.  |
| | _Axis Center_ | Recenter to the Center value defined within the axis. |
| | _Tracking Target Forward_ | Recenter to the value that aligns with the Tracking Target's forward axis. |
| | _LookAt Target Forward_ | Recenter to the value that aligns with the LookAt Target's forward axis. |

