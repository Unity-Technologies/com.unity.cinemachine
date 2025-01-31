# Follow and frame a character

Create and set up a Cinemachine Camera that automatically follows and frames a character.

> [!NOTE]
> Your Scene must include a GameObject you can target to follow it with the Cinemachine Camera.

## Add a "Follow" Cinemachine Camera

1. In the Unity menu, select **GameObject** > **Cinemachine** > **Targeted Cameras** > **Follow Camera**.

   Unity adds a new GameObject with:
   * A [Cinemachine Camera](CinemachineCamera.md) component,
   * A [Cinemachine Follow](CinemachineFollow.md) component handling the Cinemachine Camera behavior for **Position Control**, and
   * A [Cinemachine Rotation Composer](CinemachineRotationComposer.md) component handling the Cinemachine Camera behavior for **Rotation Control**.

2. [Verify](setup-cinemachine-environment.md#verify-the-cinemachine-brain-presence) that the Unity Camera includes a [Cinemachine Brain](CinemachineBrain.md) component.

3. In the Inspector, in the **Cinemachine Camera** component, set the **Tracking Target** property to specify the GameObject to follow and look at.

   The CinemachineCamera automatically positions the Unity camera relative to this GameObject at all times, and rotates the camera to look at the GameObject, even as you move it in the Scene.

> [!NOTE]
> If you invoked th **Follow Camera** menu item by right-clicking on the GameObject that you want to follow, the "Tracking Target" of the new camera will automatically be populated with the object on which you right-clicked.

## Adjust the Cinemachine Camera behavior

1. Use the Inspector to access the [Cinemachine Camera component](CinemachineCamera.md) properties for further configuration.

2. Adjust the properties such as:
   * The follow offset
   * The follow damping
   * The screen composition, and
   * The damping used when re-aiming the camera
   * The Lens settings
