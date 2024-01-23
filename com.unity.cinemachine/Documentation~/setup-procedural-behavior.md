# Set up a Cinemachine Camera with procedural behavior

Set up a Cinemachine Camera that adds a basic procedural behavior to the Unity Camera; for example, to make the camera follow and look at a target GameObject.

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

## Adjust the Cinemachine Camera behavior

1. [Customize the CinemachineCamera](CinemachineCamera.md) as needed.

   Choose the desired behaviour for following and looking at, and adjust settings such as the follow offset, the follow damping, the screen composition, and the damping used when re-aiming the camera.

## Next steps

* [Create multiple Cinemachine Cameras and manage transitions between them](setup-multiple-cameras.md).
* Manage a choreographed sequence of Cinemachine Camera shots with Timeline.