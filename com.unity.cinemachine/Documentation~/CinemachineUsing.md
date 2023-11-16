# Using Cinemachine

Using Cinemachine requires a new way of thinking about working with cameras. For example, you might have invested heavily in carefully scripted camera behaviors. You may choose to keep those scripts and adapt them for use with Cinemachine cameras, however you may find that Cinemachine can natively give the same results, if not better, in less time.


## Cinemachine Cameras

Cinemachine does not create new cameras. Instead, it directs a **single Unity camera** for multiple shots. You compose these shots with [__CinemachineCameras__](CinemachineCamera.md) (also referred to sometimes as _Virtual Cameras_).  CinemachineCameras move and rotate the Unity camera and control its settings.

The CinemachineCameras are separate GameObjects from the Unity Camera, and behave independently. They are not nested within each other. For example, a Scene might look like this:

![A Scene containing a Unity camera with Cinemachine Brain (blue) and multiple CinemachineCameras (red)](images/CinemachineSceneHierarchy.png)

The main tasks that the CinemachineCamera does for you:

* Positions the Unity camera in the Scene.
* Aims the Unity camera at something.
* Adds procedural noise to the Unity camera. Noise simulates things like hand-held effects or vehicle shakes.

Cinemachine encourages you to create many CinemachineCameras. The CinemachineCamera is designed to consume little processing power. If your Scene is performance-sensitive, deactivate all but the essential CinemachineCameras at any given moment for best performance.

It is recommended that you use a single CinemachineCamera for a single shot. Take advantage of this to create dramatic or subtle cuts or blends. Examples:

* For a cutscene where two characters exchange dialog, use three CinemachineCameras: one camera for a mid-shot of both characters, and separate CinemachineCameras for a close-up of each character. Use Timeline to synchronize audio with the CinemachineCameras.

* Duplicate an existing CinemachineCamera so that both CinemachineCameras are in the same position in the Scene. For the second CinemachineCamera, change the FOV or composition. When a player enters a trigger volume, Cinemachine blends from the first to the second CinemachineCamera to emphasize a change in action.

One CinemachineCamera has control of the Unity camera at any point in time. This is the __live__ CinemachineCamera. The exception to this rule is when a blend occurs from one CinemachineCamera to the next. During the blend, both CinemachineCameras are live.

## Cinemachine Brain

The [**Cinemachine Brain**](CinemachineBrain.md) is a component in the Unity Camera itself. The Cinemachine Brain monitors all active CinemachineCameras in the Scene. To specify the next live CinemachineCamera, you [activate or deactivate](https://docs.unity3d.com/Manual/DeactivatingGameObjects.html) the desired CinemachineCamera's game object, or boost the desired CinemachineCamera's priority. Cinemachine Brain then chooses the most recently activated CinemachineCamera with the same or higher priority as the live CinemachineCamera. It performs a cut or blend between the previous and new CinemachineCameras.

You can respond to dynamic game events in real time by manipulating CinemachineCamera priorities or by activating and deactivating them. This is particularly useful for live gameplay, where action isn’t always predictable. Use [Timeline](CinemachineTimeline.md) to choreograph cameras in predictable situations, like cutscenes. Timeline overrides the Cinemachine Brain priority system to give you precise, to-the-frame camera control.

## Blending between CinemachineCameras

One of the most powerful features of Cinemachine is its ability to smoothly blend between shots.

A Cinemachine blend is not a fade, wipe, or dissolve. Rather, Cinemachine Brain performs a smooth animation of the position, rotation, and other settings of the Unity camera from one CinemachineCamera to the next, taking care to preserve the view of the target object, and to respect the Up direction.

Blends are what allow you to create sophicticated camera motion by combining realtively simple shots and blending between them as controlled by a timeline, or in response to real-time game events.
