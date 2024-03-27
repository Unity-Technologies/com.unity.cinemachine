# 2D graphics

Cinemachine supports orthographic cameras. When you set the Unity camera's projection to Orthographic, Cinemachine adjusts to accommodate it. In CinemachineCamera properties for __Lens__, __FOV__ is replaced by __Orthographic Size__. Note that settings related to FOV and certain FOV-oriented behaviors such as [Follow Zoom](CinemachineFollowZoom.md) have no effect if the camera is orthographic.

In orthographic environments, it doesn’t usually make sense to rotate the camera. Accordingly, Cinemachine provides the [Position Composer](CinemachinePositionComposer.md) to handle framing and composition without rotating the camera.

When the main camera has a Pixel Perfect component, you can add a The [Cinemachine Pixel Perfect extension](CinemachinePixelPerfect.md) to your CinemachineCamera to enable it to play well with the Pixel Perfect environment.

To confine the camera to a specific region of your 2D world, you can use the [Cinemachine Confiner 2D extension](CinemachineConfiner2D.md)

