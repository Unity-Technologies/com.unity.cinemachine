# Top-down games

Cinemachine Virtual Cameras are modeled after human camera operators and how they operate real-life cameras.  As such, they have a sensitivity to the up/down axis, and always try to avoid introducing roll into the camera framing.  Because of this sensitivity, the Virtual Camera avoids looking straight up or down for extended periods.  They do it in passing, but if the __Look At__ target is often straight up or down, they will not always give the desired result.

**Tip:** You can deliberately roll by animating properties like __Dutch__ in a Virtual Camera.

If you are building a top-down game where the cameras look straight down, the best practice is to redefine the up direction, for the purposes of the camera.  You do this by setting the __World Up Override__ in the [Cinemachine Brain](CinemachineBrainProperties.html) to a GameObject whose local up points in the direction that you want the Virtual Camera’s up to normally be.   This is applied to all Virtual Cameras controlled by that Brain.

