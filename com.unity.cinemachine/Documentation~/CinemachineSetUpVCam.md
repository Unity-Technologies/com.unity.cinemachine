# Setting up CmCameras

In your project, organize your Scene Hierarchy to have a single Unity camera with a CinemachineBrain component and many CmCameras.

To add a CmCamera that follows and looks at a target:

1. In the Unity menu, choose __GameObject > Cinemachine > Follow Camera__. <br/>Unity adds a new GameObject with a CmCamera component. If necessary, Unity also adds a [Cinemachine Brain](CinemachineBrainProperties.md) component to the Unity camera GameObject for you.
2. Use the __Tracking Target__ property to specify a GameObject to follow. <br/>The CmCamera automatically positions the Unity camera relative to this GameObject at all times, and rotates the camera to look at the GameObject, even as you move it in the Scene.
3. [Customize the CmCamera](CinemachineVirtualCamera.md) as needed. <br/>Choose the algorithm for following and looking at, and adjust settings such as the follow offset, the follow damping, the screen composition, and the damping used when re-aiming the camera.


To add a passive CmCamera:

1. In the Unity menu, choose __GameObject > Cinemachine > CmCamera__. <br/>Unity adds a new GameObject with a CmCamera component. If necessary, Unity also adds a [Cinemachine Brain](CinemachineBrainProperties.md) component to the Unity camera GameObject for you.
2. The CmCamera is created by default to match the scene view camera.  You can set the CmCamera's transform and lens properties as you like.


