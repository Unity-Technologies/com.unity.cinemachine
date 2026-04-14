# Cinemachine Camera Manager Events

Use the Cinemachine Camera Manager Events component to handle events sent for a specific Cinemachine Camera Manager.

For more information, refer to [Cinemachine Events](CinemachineEvents.md).

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Camera Manager__ | This is the Cinemachine Camera Manager emitting the events.  If null and the current GameObject has a Cinemachine Camera Manager component, that component will be used. |
| __Camera Activated Event__ | This is called at the beginning of a blend, when a camera becomes live.  Parameters are: brain, incoming camera. A cut is considered to be a blend of length zero. |
| __Camera Deactivated Event__ | This event will fire whenever a Cinemachine Camera stops being live.  If a blend is involved, then the event will fire after the last frame of the blend. |
| __Blend Created Event__ | This event will fire whenever a new Cinemachine blend is created. Handlers can modify the settings of the blend (but not the cameras).  Note: BlendCreatedEvents are NOT sent for timeline blends, as those are expected to be controlled 100% by timeline. To modify the blend algorithm for timeline blends, you can install a handler for CinemachineCore.GetCustomBlender. |
| __Blend Finished Event__ | This event will fire whenever a Cinemachine Camera finishes blending in.  It will not fire if the blend length is zero. |
| __Camera Cut Event__ | This is called when a zero-length blend happens. |

## Additional resources

* [Cinemachine Camera Events](CinemachineCameraEvents.md)
* [Cinemachine Brain Events](CinemachineBrainEvents.md)
