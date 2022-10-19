# Cinemachine Deoccluder

__Cinemachine Deoccluder__ is an [extension](CinemachineVirtualCameraExtensions.md) for the CmCamera. It post-processes the final position of the CmCamera to attempt to preserve the line of sight with the __Look At__ target of the CmCamera. It does this by moving away from the GameObjects that obstruct the view.

Add a Cinemachine Deoccluder extension to a CmCamera to do any of the following tasks:

* Push the camera away from obstructing obstacles in the Scene.

* Place the camera in front of obstacles that come between the CmCamera and its __Look At__ target.

* Evaluate shot quality. __Shot quality__ is a measure of the distance of the CmCamera from its ideal position, the distance of the CmCamera to its target, and the obstacles that block the view of the target. Other modules use shot quality, including [Clear Shot](CinemachineClearShot.md).

The Deoccluder uses a [Physics Raycaster](https://docs.unity3d.com/Manual/script-PhysicsRaycaster.html). Therefore, Cinemachine Deoccluder requires that potential obstacles have [collider](https://docs.unity3d.com/Manual/CollidersOverview.html) volumes. There is a performance cost for this requirement. If this cost is prohibitive in your game, consider implementing this functionality differently.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Collide Against__ || Cinemachine Deoccluder considers GameObjects in these layers to be potential obstacles. It ignores GameObjects that are not in the selected layers. |
| __Minimum Distance From Target__ || Ignore obstacles that are less than this distance from the target's pivot point. |
| __Avoid Obstacles__ || Check to allow the Deoccluder to move the camera in the Scene when the target is obscured by an obstacle. Use the Distance Limit, Camera Radius, and Strategy properties to adjust how to avoid obstacles. If left unchecked, the Cinemachine Deoccluder will report shot quality based on obstacles, but will not attempt to move the camera to improve the shot. |
| __Distance Limit__ || The maximum raycast distance when checking if the line of sight to this camera’s target is clear. Enter 0 to use the current actual distance to the target. Available when Avoid Obstacles is checked. |
| __Camera Radius__ || Distance to maintain from any obstacle. Try to keep this value small for the best results. Increase it if you are seeing inside obstacles due to a large FOV on the camera. Available when Avoid Obstacles is checked. |
| __Strategy__ || The way in which the Deoccluder attempts to preserve sight of the target. Available when Avoid Obstacles is checked. |
| | _Pull Camera Forward_ | Move the camera forward along its Z axis until it is in front of the obstacle that is nearest to the target. |
| | _Preserve Camera Height_ | Move the camera to an alternate point of view while attempting to keep the camera at its original height. |
| | _Preserve Camera Distance_ | Move the camera to an alternate point of view while attempting to keep the camera at its original distance from the target. |
| __Smoothing Time__ |  | Minimum number of seconds to hold the camera at the nearest point to the target. Can be used to reduce excess camera movement in environments with lots of obstacles. Available when Avoid Obstacles is checked. |
| __Damping__ || How quickly to return the camera to its normal position after an occlusion has gone away. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. Available when Avoid Obstacles is checked. |
| __Damping When Occluded__ || How quickly to move the camera to avoid an obstacle. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. Available when Avoid Obstacles is checked. |
| __Optimal Target Distance__ || If greater than zero, give a higher score to shots when the target is closer to this distance. Set this property to 0 to disable this feature. |
| __Transparent Layers__ || Objects on these layers will never obstruct the view of the target. |
| __Minimum Occlusion Time__ || Do not take action unless the occulsion has lasted at least this long. |
| __Maximum Effort__ || Upper limit on how many obstacle hits to process. Higher numbers may impact performance. In most environments four (4) hits is enough. |
| **Ignore Tag** || Obstacles with this tag will be ignored. It is recommended to set this field to the target's tag. |



