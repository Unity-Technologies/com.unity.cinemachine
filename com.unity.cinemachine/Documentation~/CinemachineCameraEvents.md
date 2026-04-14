# Cinemachine Camera Events

Use the Cinemachine Camera Events component to handle events sent for a specific Cinemachine Camera.

For more information, refer to [Cinemachine Events](CinemachineEvents.md).

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Event Target__ | This is the object whose events are being monitored.  If null and the current GameObject has a CinemachineVirtualCameraBase component, that component will be used. |
| __Camera Activated Event__ | This is called at the beginning of a blend, when a camera becomes live.  Parameters are: brain, incoming camera. A cut is considered to be a blend of length zero. |
| __Camera Deactivated Event__ | This event will fire whenever a Cinemachine Camera stops being live.  If a blend is involved, then the event will fire after the last frame of the blend. |
| __Blend Created Event__ | This event will fire whenever a new Cinemachine blend is created. Handlers can modify the settings of the blend (but not the cameras).  Note: BlendCreatedEvents are NOT sent for timeline blends, as those are expected to be controlled 100% by timeline. To modify the blend algorithm for timeline blends, you can install a handler for CinemachineCore.GetCustomBlender. |
| __Blend Finished Event__ | This event will fire whenever a Cinemachine Camera finishes blending in.  It will not fire if the blend length is zero. |

## Additional resources

* [Cinemachine Camera Manager Events](CinemachineCameraManagerEvents.md)
* [Cinemachine Brain Events](CinemachineBrainEvents.md)
