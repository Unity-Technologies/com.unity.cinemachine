# Transposer

This Virtual Camera __Body__ algorithm moves the Virtual Camera in a fixed relationship to the __Follow__ target. It also applies offsets and damping.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Binding Mode__ || The coordinate space to use when interpreting the offset from the target. Cinemachine also uses this coordinate space for the up vector. Cinemachine keeps the camera oriented in the up direction while aiming. For example, for a "Door Cam or “Hood Cam" in a car racing game, use Local Space Locked To Target. |
| | _Lock To Target On Assign_|  Make the Virtual Camera use the Follow target’s local frame at the moment that the Virtual Camera is activated or when the target is assigned. This offset remains constant in world space. Also, the camera does not rotate along with the target. |
| | _Lock To Target With World Up_ | Make the Virtual Camera use the Follow target’s local frame with tilt and roll set to 0. Use this binding mode if your target flips over.  |
| | _Lock To Target No Roll_ | Make the Virtual Camera use the Follow target’s local frame, with roll set to 0. |
| | _Lock To Target_ | Make the Virtual Camera use the Follow target’s local frame. As the target rotates, the camera rotates around it to maintain the offset. |
| | _World Space_ | Make the Virtual Camera use the origin of the Scene.  |
| | _Simple Follow With World Up_ | Move the Virtual Camera relative to the Follow target using camera-local axes. This mode emulates what a human camera operator would do when instructed to follow a target. |
| __Follow Offset__ || The distance to maintain the Virtual Camera relative to the Follow target. Set X, Y, and Z to 0 to place the camera at the centre of the target. The default is 0, 0, -10, respectively, which places the camera behind the target. |
| __X Damping__ || How responsively the camera tries to maintain the offset in the x-axis. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  |
| __Y Damping__ || How responsively the camera tries to maintain the offset in the y-axis. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.   |
| __Z Damping__ || How responsively the camera tries to maintain the offset in the z-axis. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.   |
| __Pitch Damping__ || How responsively the camera tracks the target rotation’s x angle. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  |
| __Yaw Damping__ || How responsively the camera tracks the target rotation’s y angle. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  |
| __Roll Damping__ || How responsively the camera tracks the target rotation’s z angle. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  |



