# Set up a basic Cinemachine environment

Set up your Unity project with the [minimum required elements](concept-essential-elements.md) to make a functional Cinemachine environment:

* Create a passive Cinemachine Camera – with no specific behavior by definition,
* Ensure the Cinemachine Brain is present in the Unity Camera, and
* Adjust the Cinemachine Camera properties and see how it affects the Unity Camera.

> [!NOTE]
> Your Scene must include only one Unity Camera – a GameObject with a [Camera](https://docs.unity3d.com/Manual/class-Camera.html) component.

## Add a Cinemachine Camera

1. In the Scene view, [navigate the Scene](https://docs.unity3d.com/Manual/SceneViewNavigation.html) to get the point of view you want to frame with the Cinemachine Camera.

2. In the Unity menu, select **GameObject** > **Cinemachine** > **Cinemachine Camera**.

   Unity adds a new GameObject with:
   * A [Cinemachine Camera](CinemachineCamera.md) component and
   * A Transform that matches the latest position and orientation of the Scene view camera.

## Verify the Cinemachine Brain presence

When you create a first Cinemachine Camera in a Scene, Unity automatically adds a Cinemachine Brain to the Unity Camera, unless the Unity Camera already includes one. To verify it:

1. In the Hierarchy, select your Unity Camera – the GameObject that includes the Camera component.

2. In the Inspector, verify that the GameObject includes a [Cinemachine Brain](CinemachineBrain.md) component.

> [!NOTE]
> You can manually add the Cinemachine Brain component as any other component to the Unity Camera GameObject if needed, but remember that only one Unity Camera in the Scene must have a Cinemachine Brain.

> [!NOTE]
> Once the Cinemachine Brain is present, the Unity Camera's transform and lens settings are locked and cannot be changed directly in the Camera inspector. You can only change these properties of the camera by changing the corresponding properties of the CinemachineCamera.

## Adjust the Cinemachine Camera properties

1. Open the Game view.  
   It shows the Scene through the lens of the Unity Camera according to the current settings of the Cinemachine Camera.

2. In the Hierarchy, select the Cinemachine Camera GameObject.

3. In the Inspector, adjust the properties to precisely frame your shot according to your needs:
   * In the **Transform**, adjust the **Position** and **Rotation**.
   * In the **Cinemachine Camera** component, adjust the **Lens** properties.

> [!NOTE]
> This Cinemachine Camera is the only one and latest you created, and as such, that you enabled. As a result, notice that the Unity Camera automatically inherits from the changes you perform on this Cinemachine Camera.

## Next steps

Here are potential tasks you might want to do now:

* [Create multiple Cinemachine Cameras and manage transitions between them](setup-multiple-cameras.md).
* [Create a Cinemachine Camera with procedural behavior: example for a camera that follows a character](setup-procedural-behavior.md).
* [Manage a choreographed sequence of Cinemachine Camera shots with Timeline](setup-timeline.md).

## Additional resources

* [Preview and author a Cinemachine Camera in first person](preview-and-author-in-first-person.md)
