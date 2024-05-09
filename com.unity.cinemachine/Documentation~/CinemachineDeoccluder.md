# Cinemachine Deoccluder

__Cinemachine Deoccluder__ is an [extension](concept-procedural-motion.md#extensions) for the CinemachineCamera. It post-processes the final position of the CinemachineCamera to attempt to preserve the line of sight with the __Look At__ target of the CinemachineCamera. It does this by moving away from the GameObjects that obstruct the view.

Add a Cinemachine Deoccluder extension to a CinemachineCamera to do any of the following tasks:

* Push the camera away from obstructing obstacles in the Scene.

* Place the camera in front of obstacles that come between the CinemachineCamera and its __Look At__ target.

* Evaluate shot quality. __Shot quality__ is a measure of the distance of the CinemachineCamera from its ideal position, the distance of the CinemachineCamera to its target, and the obstacles that block the view of the target. Other modules use shot quality, including [Clear Shot](CinemachineClearShot.md).

The Deoccluder uses a [Physics Raycaster](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html). Therefore, Cinemachine Deoccluder requires that potential obstacles have [collider](https://docs.unity3d.com/Manual/CollidersOverview.html) volumes. There is a performance cost for this requirement. If this cost is prohibitive in your game, consider implementing this functionality differently.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Collide Against__ || Cinemachine Deoccluder considers GameObjects in these layers to be potential obstacles. It ignores GameObjects that are not in the selected layers. |
| __Minimum Distance From Target__ || Ignore obstacles that are less than this distance from the target's pivot point. |
| __Avoid Obstacles__ || When enabled, the Deoccluder will move the camera in the Scene when the target is obscured by an obstacle. Use the Distance Limit, Camera Radius, Strategy, Smoothing Time, Minimum Occlusion Time, Damping, and DampingWhenOccluded properties to adjust how to avoid obstacles. If disabled, the Cinemachine Deoccluder will evaluate shot quality based on obstacles, but will not attempt to move the camera to improve the shot. |
| __Distance Limit__ || The maximum raycast distance when checking if the line of sight to this camera’s target is clear. Enter 0 to use the current actual distance to the target. Available when Avoid Obstacles is checked. |
| __Use Follow Target__ || When enabled, the Deoccluder will move the camera towards the Follow target instead of the LookAt target. |
| __Y Offset__ || When Use Follow Target is enabled, the Follow target's Y position will be considered to be offset by this much in its local vertical direction. |
| __Camera Radius__ || Distance to maintain from any obstacle. Try to keep this value small for the best results. Increase it if you are seeing inside obstacles due to a large FOV on the camera. Available when Avoid Obstacles is checked. |
| __Strategy__ || The way in which the Deoccluder attempts to preserve sight of the target. Available when Avoid Obstacles is checked. |
| | _Pull Camera Forward_ | Move the camera forward along its Z axis until it is in front of the obstacle that is nearest to the target. |
| | _Preserve Camera Height_ | Move the camera to an alternate point of view while attempting to keep the camera at its original height. |
| | _Preserve Camera Distance_ | Move the camera to an alternate point of view while attempting to keep the camera at its original distance from the target. |
| __Smoothing Time__ |  | Minimum number of seconds to hold the camera at the nearest point to the target. Can be used to reduce excess camera movement in environments with lots of obstacles. Available when Avoid Obstacles is checked. |
| __Damping__ || How quickly to return the camera to its normal position after an occlusion has gone away. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. Available when Avoid Obstacles is checked. |
| __Damping When Occluded__ || How quickly to move the camera to avoid an obstacle. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. Available when Avoid Obstacles is checked. |
| __Transparent Layers__ || Objects on these layers will never obstruct the view of the target. |
| __Minimum Occlusion Time__ || Do not take action unless the occulsion has lasted at least this long. |
| __Maximum Effort__ || Upper limit on how many obstacle hits to process. Higher numbers may impact performance. In most environments four (4) hits is enough. |
| **Ignore Tag** || Obstacles with this tag will be ignored. It is recommended to set this field to the target's tag. |
| __Shot Quality Evaluation__ || If enabled, gives a higher score to shots when the target is closer to an optimal distance. |
| | _Optimal Distance_ | Maximum quality boost will be given to the shot when the target is near this distance from the camera. |
| | _Near Limit_ | Quality boost drops off as the target distance gets smaller than the optimal distance.  When thie near limit is reached, quality is no longer boosted. |
| | _Far Limit_ | Quality boost drops off as the target distance gets larger than the optimal distance.  When thie far limit is reached, quality is no longer boosted. |
| | _Max Quality Boost_ | This is the quality boost that will be given when the target is at the optimal distance.  It is expresses as the fraction of the default quality that will get added in.  For example, when this value is 0.5, the shot quality will be multiplied by 1.5. |


