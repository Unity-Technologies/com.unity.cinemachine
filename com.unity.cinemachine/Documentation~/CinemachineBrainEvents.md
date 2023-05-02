# Cinemachine Brain Events

When Cinemachine Cameras are activated, global events are sent via CinemachineCore.  Scripts can add listeners to those events and take action based on them.  Listeners will receive events for all cameras and all brains.

Sometimes it's desirable to have events sent only for a specific Cinemachine Brain or Cinemachine Camera Manager, so that scripts can be notified based on this specific objects's activity without having to provide code to filter the events.  The Cinemachine Brain Events component fills this need.

If you add it to a CinemachineBrain object or other object implementing `ICinemachineMixer` such as a Cinemachine Camera Manager, it will expose events that will be fired based on that objects's activity.  Any listeners you add will be called when the events happen for that object.

If you are looking for events that fire for a specific CinemachineCamera, see [Cinemachine Camera Events](CinemachineCameraEvents.md).

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Camera Activated Event__ | This is called at the beginning of a blend, when a camera becomes live.  Parameters are: brain, incoming camera. A cut is considered to be a blend of length zero. |
| __Camera Deactivated Event__ | This event will fire whenever a Cinemachine Camera stops being live.  If a blend is involved, then the event will fire after the last frame of the blend. |
| __Blend Created Event__ | This event will fire whenever a new Cinemachine blend is created. Handlers can modify the settings of the blend (but not the cameras).  Note: BlendCreatedEvents are NOT sent for timeline blends, as those are expected to be controlled 100% by timeline. To modify the blend algorithm for timeline blends, you can install a handler for CinemachineCore.GetCustomBlender. |
| __Blend Finished Event__ | This event will fire whenever a Cinemachine Camera finishes blending in.  It will not fire if the blend length is zero. |
| __Camera Cut Event__ | This is called when a zero-length blend happens. |
| __Brain Updated Event__ | This event is sent immediately after the brain has processed all the CinemachineCameras, and has updated the main Camera.  Code that depends on the main camera position or that wants to modify it can be executed from this event handler. |

