# Manage grouped cameras

A __Manager Camera__ oversees many CinemachineCameras but acts as a single CinemachineCamera from the point of view of Cinemachine Brain and Timeline.

Cinemachine includes these manager cameras:

* [Sequencer Camera](CinemachineSequencerCamera.md): Executes a sequence of blends or cuts of its child CinemachineCameras.
* [Clear Shot Camera](CinemachineClearShot.md): Picks the child CinemachineCamera with the best view of the target.
* [State-Driven Camera](CinemachineStateDrivenCamera.md): Picks a child CinemachineCamera in reaction to changes in animation state.
* [Mixing Camera](CinemachineMixingCamera.md): Creates a continuous blend by using the weighted average of up to eight child CinemachineCameras.

Because manager cameras act like normal CinemachineCameras, you can nest them. In other words, create arbitrarily complex camera rigs that combine regular CinemachineCameras and manager cameras.

## Making Your Own Custom Manager Camera

It is also possible to make your own manager camera that selects its current active child according to an arbitrary algorithm that you provide.  For instance, if you are making a 2D Platformer and want a camera rig that frames itself differently according to whether the character is moving right or left, or jumping, or falling, a custom CameraManager class might be a good approach.

To do this, make a new class that inherits `CinemachineCameraManagerBase`.  This base class implements an array of CinemachineCamera children, and a blender. 

Next, implement the abstract `ChooseCurrentCamera` method.  This is called every frame while the manager is active, and should return the child camera that ought to be active this frame.  Your custom class can make that decision any way it likes.  In the example, it would look at the player state to find out the facing direction and the jumping/falling state, and choose the appropriate child camera.

If the new desired camera is different from what it was on the last frame, CinemachineCameraManagerBase will initiate a blend, according to what you set up in its DefaultBlend and CustomBlends fields.

Once you've added the child cameras with the settings you like for each player state and have wired them into your manager instance, you will have a Cinemachine rig that adjusts itself according to player state.  The rig itself will look to the rest of the system just like an ordinary CinemachineCamera, and so can be used wherever CinemachineCameras can - including being nested within other rigs.

Note that Cinemachine ships with [State-Driven Camera](CinemachineStateDrivenCamera.md), which implements this functionality provided that the relevant player state is encoded in an Animation Controller State-Machine.  You would implement your own manager in the case that the state is not being read from an Animation Controller.


### Managed Cameras need to be GameObject children of the manager
This is mainly to prevent problems that can occur if you nest managers and end up with a recursive loop.  Forcing the managed cameras to be children makes recursion impossible.
