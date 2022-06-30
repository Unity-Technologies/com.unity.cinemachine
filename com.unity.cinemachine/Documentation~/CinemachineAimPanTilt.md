# Pan Tilt

This Virtual Camera __Aim__ algorithm pans and tilts the camera in response to the user’s input.  This component does not read user input itself; it expects to be driven by an __Input Axis Controller__ component.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Reference Frame__ || Defines the reference frame against which pan and tilt rotations are made.  |
| | _Parent Object_ | If the CmCamera has a parent object, then that parent's local axes will be used as the reference frame.  If there is no parent object, world axes will be used. |
| | _World_ | World axes will be used as the reference frame. |
| | _Tracking Target_ | If the CmCamera has a Tracking target, then that object's local axes will be used as the reference frame.  If there is no parent object, world axes will be used. |
| | _LookAt Target_ | If the CmCamera has a LookAt target, then that object's local axes will be used as the reference frame.  If there is no parent object, world axes will be used. |
| __Pan Axis__ || Controls the horizontal rotation of the Camera.  |
| | _Value_ | The current value of the axis, in degrees. |
| | _Center_ | The value that Recentering will recenter to, if Recentering is enabled. |
| | _Range_ | The minimum and maximum for the Value. |
| | _Wrap_ | If enabled, the axis wraps around when it reaches the end of its range, forming a loop. |
| | _Recentering_ | Enables automatic recentering when the player gives no input. |
| | _Wait_ | If no user input has been detected on the axis, the axis waits this long in seconds before recentering. |
| | _Time_ | How long recentering will take, once it has started. |
| __Tilt Axis__ || Controls the vertical rotation of the Camera.  |
| | _Value_ | The current value of the axis, in degrees. |
| | _Center_ | The value that Recentering will recenter to, if Recentering is enabled. |
| | _Range_ | The minimum and maximum for the Value. Must fall inside of [-90, 90]. |
| | _Wrap_ | If enabled, the axis wraps around when it reaches the end of its range, forming a loop. |
| | _Recentering_ | Enables automatic recentering when the player gives no input. |
| | _Wait_ | If no user input has been detected on the axis, the axis waits this long in seconds before recentering. |
| | _Time_ | How long recentering will take, once it has started. |

