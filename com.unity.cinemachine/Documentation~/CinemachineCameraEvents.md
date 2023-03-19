# Cinemachine Camera Events

When Cinemachine Cameras are activated, global events are sent via CinemachineCore.  Scripts can add listeners to those events and take action based on them.  Listeners will receive events for all cameras.

Sometimes it's desirable to have events sent only for a specific camera, so that scripts can be notified based on this specific camera's activity without having to provide code to filter the events.  The Cinemachine Camera Events component fills this need.

If you add it to a CinemachineCamera, it will expose events that will be fired based on that camera's activity.  Any listeners you add will be called when the events happen for that camera.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __On Camera Live__ | This is called at the beginning of a blend, when a camera becomes live.  Parameters are: incoming camera, outgoing camera. A cut is considered to be a blend of length 0 |

