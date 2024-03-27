# Cinemachine Shot Quality Evaluator

__Cinemachine Shot Quality Evaluator__ is an [extension](concept-procedural-motion.md#extensions) for the [Camera](CinemachineCamera.md). It post-processes the final position of the CinemachineCamera to evaluate shot quality based of visibility of the LookAt target and, optionally, camera distance from it.

[CinemachineDeoccluder](CinemachineDeoccluder.md) Has this functionality bundled with it.  Use this extension if you want to evaluate shot quality without using the Deoccluder.

You can implement your own shot quality evaluator by creating a class that implements the `IShotQualityEvaluator` interface.

The Shot Quality Evaluator uses a [Physics Raycaster](https://docs.unity3d.com/Manual/script-PhysicsRaycaster.html). Therefore, Cinemachine Shot Quality Evaluator requires that potential obstructions have [collider](https://docs.unity3d.com/Manual/CollidersOverview.html) volumes. There is a performance cost for this requirement. If this cost is prohibitive in your game, consider implementing this functionality differently.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Occlusion Layers__ | Objects on these layers will be detected. |
| __Ignore Tag__ | Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag. |
| __Minimum Distance From Target__ | Obstacles closer to the target than this will be ignored. |
| __Camera Radius__ | Radius of the spherecast that will be done to check for occlusions. |
| __Distance Evaluation__ | If enabled, will evaluate shot quality based on target distance. |
| __Optimal Distance__ | If greater than zero, maximum quality boost will occur when target is this far from the camera. |
| __Near Limit__ | Shots with targets closer to the camera than this will not get a quality boost. |
| __Far Limit__ | Shots with targets farther from the camera than this will not get a quality boost. |
| __Max Quality Boost__ | High quality shots will be boosted by this fraction of their normal quality. |


