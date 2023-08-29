# Cinemachine Camera Events

When Cinemachine Cameras are activated, global events are sent via CinemachineCore.  Scripts can add listeners to those events and take action based on them.  Listeners will receive events for all cameras.

Sometimes it's desirable to have events sent only for a specific camera, so that scripts can be notified based on this specific camera's activity without having to provide code to filter the events.  The Cinemachine Camera Events component fills this need.

If you add it to a CinemachineCamera, it will expose events that will be fired based on that camera's activity.  Any listeners you add will be called when the events happen for that camera.

If you are looking for events that fire for a specific CinemachineBrain, please see [Cinemachine Brain Events](CinemachineBrainEvents.md).

If you are looking for events that fire for a specific CinemachineCameraManager, see [Cinemachine Camera Manager Events](CinemachineCameraManagerEvents.md).

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Event Target__ | This is the object whose events are being monitored.  If null and the current GameObject has a CinemachineVirtualCameraBase component, that component will be used. |
| __Camera Activated Event__ | This is called at the beginning of a blend, when a camera becomes live.  Parameters are: brain, incoming camera. A cut is considered to be a blend of length zero. |
| __Camera Deactivated Event__ | This event will fire whenever a Cinemachine Camera stops being live.  If a blend is involved, then the event will fire after the last frame of the blend. |
| __Blend Created Event__ | This event will fire whenever a new Cinemachine blend is created. Handlers can modify the settings of the blend (but not the cameras).  Note: BlendCreatedEvents are NOT sent for timeline blends, as those are expected to be controlled 100% by timeline. To modify the blend algorithm for timeline blends, you can install a handler for CinemachineCore.GetCustomBlender. |
| __Blend Finished Event__ | This event will fire whenever a Cinemachine Camera finishes blending in.  It will not fire if the blend length is zero. |

