# Cinemachine Follow component

This CinemachineCamera __Position Control__ behavior moves the CinemachineCamera to maintain a fixed offset relative to the __Tracking Target__. It also applies damping.

The fixed offset can be interpreted in various ways, depending on the Binding Mode.

## Properties

| **Property** || **Function** |
|:---|:---|:---|
| __[Binding Mode](#binding-modes)__ || How to use to interpret the offset from the target. |
| | _World Space_ | The offset is interpreted in world space relative to the origin of the Follow target. The camera will not change position when the target rotates. |
| | _Lock To Target_ | Makes the CinemachineCamera use the local frame of the Follow target. When the target rotates, the camera moves with it to maintain the offset and to maintain the same view of the target. |
| | _Lock To Target With World Up_ | Makes the CinemachineCamera use the local frame of the Follow target with tilt and roll set to 0. This binding mode ignores all target rotations except yaw. |
| | _Lock To Target No Roll_ | Makes the CinemachineCamera use the local frame of the Follow target, with roll set to 0. |
| | _Lock To Target On Assign_ | Makes the orientation of the CinemachineCamera match the local frame of the Follow target, at the moment when the CinemachineCamera is activated or when the target is assigned. This offset remains constant in world space. The camera does not rotate along with the target. |
| | _Lazy Follow_ | Lazy follow interprets the offset and damping values in camera-local space. This mode emulates the action a human camera operator would take when instructed to follow a target. The camera attempts to move as little as possible to maintain the same distance from the target; the direction of the camera with respect to the target does not matter. Regardless of the orientation of the target, the camera tries to preserve the same distance and height from it. |
| __Follow Offset__ || The desired offset from the target at which the CinemachineCamera will be positioned. Set X, Y, and Z to 0 to place the camera at the center of the target. The default is 0, 0, and -10, respectively, which places the camera behind the target. |
| __Position Damping__ || How responsively the camera tries to maintain the offset in the x, y, and z axes. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.  |
| __Angular Damping Mode__ || Can be Euler or Quaternion. In Euler mode, individual values can be set for Pitch, Roll, and Yaw damping, but gimbal lock may become an issue. In Quaternion mode, only a single value is used, but it is impervious to gimbal lock.  |
| __Rotation Damping__ || How responsively the camera tracks the target's pitch, yaw, and roll, when in Euler angular damping mode. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly.|
| __Quaternion Damping__ || How responsively the camera tracks the target's rotation, when in Quaternion Angular Damping Mode.|

## Binding Modes

[!include[](includes/binding-modes.md)]
