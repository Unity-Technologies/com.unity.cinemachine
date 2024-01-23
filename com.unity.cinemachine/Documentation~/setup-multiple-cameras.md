# Set up multiple Cinemachine Cameras and transitions

Set up and manage a Cinemachine environment with multiple Cinemachine Cameras:

1. Create multiple Cinemachine Cameras with different properties,
2. Manage Cinemachine Camera transitions in the Cinemachine Brain, and
3. Test the transitions in Play mode.

> [!NOTE]
> Your Scene must include only one Unity Camera – a GameObject with a [Camera](https://docs.unity3d.com/Manual/class-Camera.html) component.

## Add Cinemachine Cameras

1. In the Scene view, [navigate the Scene](https://docs.unity3d.com/Manual/SceneViewNavigation.html) to get the point of view you want to frame with one Cinemachine Camera.

2. In the Unity menu, select **GameObject** > **Cinemachine** > **Cinemachine Camera**.

   Unity adds a new GameObject with a [Cinemachine Camera](CinemachineCamera.md) component and a Transform that matches the latest position and orientation of the Scene view camera.

   At this point, you can also [verify](setup-cinemachine-environment.md#verify-the-cinemachine-brain-presence) that the Unity Camera includes a [Cinemachine Brain](CinemachineBrain.md) component.

3. Continue to navigate the Scene and create one or two additional Cinemachine Cameras the same way but with different positions and rotations.

## Manage transitions between Cinemachine Cameras

1. In the Hierarchy, select your Unity Camera – the GameObject that includes the Camera component.

2. In the Inspector, in the [Cinemachine Brain](CinemachineBrain.md) component:
   * Select a **Default Blend** to use between all Cinemachine Cameras, OR
   * Create and target an asset that defines **Custom Blends** to use between specific pairs of Cinemachine Cameras.

## Test the transitions in Play mode

1. Enter [Play mode](https://docs.unity3d.com/Manual/GameView.html).

2. Change the [active status](https://docs.unity3d.com/Manual/class-GameObject.html) of each Cinemachine Camera GameObject in turn to see how they blend between each other according to the way you've set up the Cinemachine Brain.

3. Exit Play mode.


## Next steps

* [Create a Cinemachine Camera with procedural behavior: example for a camera that follows a character](setup-procedural-behavior.md).
* Manage a choreographed sequence of Cinemachine Camera shots with Timeline.