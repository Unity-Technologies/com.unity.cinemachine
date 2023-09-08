# Cinemachine Terrain Decollider

__Cinemachine Terrsin Decollider__ is an [extension](CinemachineVirtualCameraExtensions.md) for the [Camera](CinemachineCamera.md). It post-processes the final position of the CinemachineCamera to ensure that the camera is placed above the Colliders that form the ground.  It does this by projecting a ray downwards from high above the camera.  The fisrt object it finds is considered to be the terrain surface, and the camera will be placed there.

The Terrain Decollider uses a [Physics Raycaster](https://docs.unity3d.com/Manual/script-PhysicsRaycaster.html). Therefore, Cinemachine Terrain Decollider requires that potential obstacles have [collider](https://docs.unity3d.com/Manual/CollidersOverview.html) volumes. There is a performance cost for this requirement. If this cost is prohibitive in your game, consider implementing this functionality differently.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Collide Against__ || The Terrain Decollider considers GameObjects in these layers to be potential obstacles. It ignores GameObjects that are not in the selected layers. |
| __Maximum Raycast__ || The length of the raycast (m) that will be performed.  The raycast will originate this high above the camera.  For best performance, make this number as small as possible. |
| __Camera Radius__ || Distance to maintain from any obstacle. Try to keep this value small for the best results. Increase it if you are seeing inside obstacles due to a large FOV on the camera. |
| __Smoothing Time__ |  | Minimum number of seconds to hold the camera at the farthest correction point. Can be used to reduce excess camera movement on bumpy terrains. |
| __Damping__ || How quickly to return the camera to its normal position after a correction has gone away. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. |


