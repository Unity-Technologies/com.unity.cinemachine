# Cinemachine Decollider

__Cinemachine Decollider__ is an [extension](CinemachineVirtualCameraExtensions.md) for the [Camera](CinemachineCamera.md). It post-processes the final position of the CinemachineCamera to resolve camera collisions.  Collisions will be resolved in the direction of the Tracking Target, but no attempt will be made to preserve the line of sight to the target.  For that, please see [CinemachineDeoccluder](CinemachineDeoccluder.md).

The Decollider uses a [Physics Raycaster](https://docs.unity3d.com/Manual/script-PhysicsRaycaster.html). Therefore, Cinemachine Decollider requires that potential obstacles have [collider](https://docs.unity3d.com/Manual/CollidersOverview.html) volumes. There is a performance cost for this requirement. If this cost is prohibitive in your game, consider implementing this functionality differently.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Collide Against__ || Cinemachine Decollider considers GameObjects in these layers to be potential obstacles. It ignores GameObjects that are not in the selected layers. |
| __Minimum Distance From Target__ || Ignore obstacles that are less than this distance from the target's pivot point. |
| __Camera Radius__ || Distance to maintain from any obstacle. Try to keep this value small for the best results. Increase it if you are seeing inside obstacles due to a large FOV on the camera. |
| __Smoothing Time__ |  | Minimum number of seconds to hold the camera at the nearest point to the target. Can be used to reduce excess camera movement in environments with lots of obstacles. |
| __Damping__ || How quickly to return the camera to its normal position after a collision has gone away. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. |


