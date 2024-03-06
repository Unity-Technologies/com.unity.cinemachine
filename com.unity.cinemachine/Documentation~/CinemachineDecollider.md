# Cinemachine Decollider

__Cinemachine Decollider__ is an [extension](CinemachineVirtualCameraExtensions.md) for the [Camera](CinemachineCamera.md). It post-processes the final position of the CinemachineCamera to resolve camera collisions.  No attempt will be made to preserve the line of sight to the target.  For that, please see [CinemachineDeoccluder](CinemachineDeoccluder.md).

The Decollider combines two algorithms:
1. **Terrain Resolution**.  A ray is cast from above the camera in the downwards direction.  If it hits a collider above the camera and the collider is on one of the specified Terrain Layers, the camera is moved upwards to the hit point.
1. **Obstacle Resolution**.  Obstacles on the specified layers that overlap the camera are detected and the camera is moved out of them by taking the shortest path.  Note that Unity's service for detecting overlapping obstacles does not work for all kinds of colliders, so it's generally a good idea to put surface-type obstacles on the terrain layer, and wall-type obstacles onto the obstacle resolution layer.

If a layer is present in both the Terrain Resolution layer mask and the Obstacle decollision layer mask, then the layer will be considered by the terrain algorithm, but not by the obstacle decollision algorithm.

The Decollider uses a [Physics Raycaster](https://docs.unity3d.com/Manual/script-PhysicsRaycaster.html). Therefore, Cinemachine Decollider requires that potential obstacles have [collider](https://docs.unity3d.com/Manual/CollidersOverview.html) volumes. There is a performance cost for this requirement. If this cost is prohibitive in your game, consider implementing this functionality differently.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Camera Radius__ || Distance to maintain from any obstacle or terrain. Try to keep this value small for the best results. Increase it if necessary to keep the camera from clipping the near edge of obsacles. |
| __Preserve Composition__ || If enabled, will re-adjust the aim to preserve the screen position of the LookAt target as much as possible. |
| __Decollision__ |  | When enabled, will attempt to push the camera out of intersecting objects |
|  | __Obstacle Layers__ | Objects on these layers will be detected |
| __Terrain Resolution__ |  | When enabled, will attempt to place the camera on top of terrain layers |
|  | __Terrain Layers__ | Colliders on these layers will be detected |
|  | __Maximum Raycast__ | Specifies the maximum length of a raycast used to find terrain colliders. |
|  | __Damping__ | How quickly to return the camera to its normal position after a terran adjustment has gone away. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. |


