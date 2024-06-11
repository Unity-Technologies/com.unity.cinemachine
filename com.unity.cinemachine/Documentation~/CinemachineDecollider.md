# Cinemachine Decollider

__Cinemachine Decollider__ is an [extension](concept-procedural-motion.md#extensions) for the [Camera](CinemachineCamera.md). It post-processes the final position of the CinemachineCamera to pull the camera out of colliding objects.  Although collisions will be resolved in the direction of the camera target, no attempt will be made to preserve the line of sight to the target.  For that, please use [CinemachineDeoccluder](CinemachineDeoccluder.md).

The Decollider combines two algorithms:
1. **Terrain Resolution**.  A ray is cast from above the camera in the downwards direction.  If it hits a collider above the camera and the collider is on one of the specified Terrain Layers, the camera is moved upwards to the hit point, placing the camera on top of the collider.
1. **Obstacle Resolution**.  Obstacles on the specified layers that overlap the camera are detected and the camera is moved out of them in the direction of the camera target.  If the camera sphere does not actually intersect an object, it will not be moved.

If a layer is present in both the Terrain Resolution layer mask and the Obstacle decollision layer mask, then the layer will be considered by the terrain algorithm only, and not by the obstacle decollision algorithm.

The Decollider uses a [Physics Raycaster](https://docs.unity3d.com/Manual/script-PhysicsRaycaster.html). Therefore, Cinemachine Decollider requires that potential obstacles have [collider](https://docs.unity3d.com/Manual/CollidersOverview.html) volumes. There is a performance cost for this requirement. If this cost is prohibitive in your game, consider implementing this functionality differently.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Camera Radius__ | Distance to maintain from any obstacle or terrain. Try to keep this value small for the best results. Increase it if necessary to keep the camera from clipping the near edge of obsacles. |
| __Decollision__ | When enabled, will attempt to push the camera out of intersecting objects. |
| __Obstacle Layers__ | Objects on these layers will be detected. |
| __Use Follow Target__ | When enabled, the Decollider will move the camera towards the Follow target instead of the LookAt target. |
| __Y Offset__ | When Use Follow Target is enabled, the Follow target's Y position will be considered to be offset by this much in its local vertical direction. |
| __Terrain Resolution__ | When enabled, will attempt to place the camera on top of terrain layers. |
| __Terrain Layers__ | Colliders on these layers will be detected. |
| __Maximum Raycast__ | Specifies the maximum length of a raycast used to find terrain colliders. |
| __Damping__ | How quickly to return the camera to its normal position when a position adjustment is no longer needed. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. |
| __Smoothing Time__ | Minimum number of seconds to hold the camera at the nearest point to the target. Can be used to reduce excess camera movement in environments with lots of obstacles. |


