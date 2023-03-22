# Cinemachine Brain Events

When Cinemachine Cameras are activated, global events are sent via CinemachineCore.  Scripts can add listeners to those events and take action based on them.  Listeners will receive events for all cameras and all brains.

Sometimes it's desirable to have events sent only for a specific Cinemachine Brain, so that scripts can be notified based on this specific brain's activity without having to provide code to filter the events.  The Cinemachine Brain Events component fills this need.

If you add it to a CinemachineBrain object, it will expose events that will be fired based on that brain's activity.  Any listeners you add will be called when the events happen for that brain.

If you are looking for evemnts that fire for a specific CinemachineCamera, see [Cinemachine Camera Events](CinemachineCameraEvents.md).

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Camera Activated Event__ | This is called at the beginning of a blend, when a camera becomes live.  Parameters are: brain, incoming camera. A cut is considered to be a blend of length 0 |
| __Camera Cut Event__ | This is called when a zero-length blend happens. |
| __Camera Updated Event__ | This event is sent immediately after the brain has processed all the CinemachineCameras, and has updated the main Camera.  Code that depends on the main camera position or that wants to modify it can be executed from this event handler. |

