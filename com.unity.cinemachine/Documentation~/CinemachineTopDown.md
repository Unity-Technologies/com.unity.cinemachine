# Top-down games

Cinemachine CinemachineCameras are modelled after human camera operators and how they operate real-life cameras. As such, they have a sensitivity to the up/down axis, and always try to avoid introducing roll into the camera framing. Because of this sensitivity, the CinemachineCamera avoids looking straight up or down for extended periods. They may do it in passing, but if the __Look At__ target is straight up or down for extended periods, they will not always give the desired result.

If you are building a top-down game where the cameras look straight down, the best practice is to redefine the up direction, for the purposes of the camera.  You do this by setting the __World Up Override__ in the [Cinemachine Brain](CinemachineBrain.md) to a GameObject whose local up points in the direction that you want the CinemachineCamera’s up to normally be. This definition of Up is applied to all CinemachineCameras controlled by that Cinemachine Brain.

