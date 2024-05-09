# Procedural Motion

On its own, a Cinemachine Camera is a **passive** GameObject that acts as a camera placeholder that you can, for example:
* Place in a fixed location with a static aim.
* Parent to another GameObject to make it move and rotate along with it.
* Manipulate via a custom script to move and rotate it and control its lens.

However, for more sophisticated results, you can add **procedural** behaviors and extensions to any Cinemachine Camera to make it dynamically move, shake, track targets, compose its own shots, respond to user inputs, move along a predefined path, react to external impulse signals, produce post-processing effects, and more.


## Procedural behaviors and extensions

The [Cinemachine Camera component](CinemachineCamera.md) allows you to select a variety of behaviors and extensions to drive the Cinemachine Camera's position, rotation, and lens.

### Position and Rotation Control

Select and configure **Position Control** and **Rotation Control** behaviors to make the Cinemachine Camera **move** and **aim** the Unity Camera according to some constraints or criteria.

Most of the available behaviors are designed to [track or look at a target GameObject](#target-gameobject-tracking).  Additionally, some behaviors support user input to orbit or rotate the camera.

With these behaviors, you can:
* Follow a target with a [fixed offset](CinemachineFollow.md), in [orbital configuration](CinemachineOrbitalFollow.md), or as a [third or first person](CinemachineThirdPersonFollow.md).

* Compose shots with adaptive camera [position](CinemachinePositionComposer.md) and [rotation](CinemachineRotationComposer.md), or centered [hard look](CinemachineHardLookAt.md), to keep the target in the camera frame.

* Apply the target's [position](CinemachineHardLockToTarget.md) and [rotation](CinemachineRotateWithFollowTarget.md) to the camera instead of having the target in the camera frame. 
  
* Move the camera along a predefined Spline to simulate a [dolly camera path](CinemachineSplineDolly.md).

* Rotate the camera around configurable [pan and tilt](CinemachinePanTilt.md) axes.

### Noise

Select and configure a [**Noise** behavior](CinemachineBasicMultiChannelPerlin.md) to make the Cinemachine Camera **shake** and simulate real-world physical camera qualities for cinematic effect.

At each frame update, Cinemachine adds noise separately from the movement of the camera to follow a target. Noise does not influence the camera’s position in future frames. This separation ensures that properties like **damping** behave as expected.

### Extensions

Add an **Extension** to augment the behavior of a Cinemachine Camera for more specific or advanced needs.

For example, the [Deoccluder](CinemachineDeoccluder.md) extension moves a camera out of the way of GameObjects that obstruct the camera’s view of its target.

Here is the list of all available Cinemachine Camera Extensions:

  * [Cinemachine Auto Focus](CinemachineAutoFocus.md)
  * [Cinemachine Confiner 3D](CinemachineConfiner3D.md)
  * [Cinemachine Confiner 2D](CinemachineConfiner2D.md)
  * [Cinemachine Decollider](CinemachineDecollider.md)
  * [Cinemachine Deoccluder](CinemachineDeoccluder.md)
  * [Cinemachine Follow Zoom](CinemachineFollowZoom.md)
  * [Cinemachine FreeLook Modifier](CinemachineFreeLookModifier.md)
  * [Cinemachine Group Framing](CinemachineGroupFraming.md)
  * [Cinemachine Pixel Perfect](CinemachinePixelPerfect.md)
  * [Cinemachine Post Processing](CinemachinePostProcessing.md)
  * [Cinemachine Recomposer](CinemachineRecomposer.md)
  * [Cinemachine Shot Quality Evaluator](CinemachineShotQualityEvaluator.md)
  * [Cinemachine Storyboard](CinemachineStoryboard.md)
  * [Cinemachine Third Person Aim](CinemachineThirdPersonAim.md)
  * [Cinemachine Volume Settings](CinemachineVolumeSettings.md)
  <!---* Cinemachine Camera Offset (component/extension, missing in docs)--->


## Target GameObject tracking

Target GameObject tracking is a key element in defining procedural motion. Offsets and screen compositions are specified in relation to these targets, so as the targets move around in the world, the cameras adjust themselves to keep the shot.  

### Tracking Target and Look At Target properties

By default, a Cinemachine Camera has a single **Tracking Target** property, which serves two purposes:

* It specifies a Transform for the Cinemachine Camera to move with when you define a position control behavior that requires it.
* It specifies the LookAt target, which is the Transform to aim at when you define a rotation control behavior that requires it.

> [!NOTE]
> If you need to use two different Transforms for these purposes, select **Use Separate LookAt Target** option via the button at the right of the **Tracking Target** field.

### Target tracking and blends

The target is also relevant when Cinemachine performs blends between shots. Cinemachine attempts to maintain the shot's desired screen position for the target, and if the target changes between shots, Cinemachine performs an interpolation between the targets' positions.  

If no target is specified for a camera blend, then Cinemachine can only interpolate the position and rotation independently, which often results in the object of interest moving around on the screen in undesirable ways. If Cinemachine knows what is the object of interest, it can correct that problem.


## Behavior and extension selection

When you select behaviors or add extensions from the Cinemachine Camera component, Unity automatically adds extra components to the Cinemachine Camera GameObject. To modify the Cinemachine Camera behavior, you must then edit the properties of these additional components.

> [!NOTE]
> You can get the same result by adding these components manually as any other [GameObject component](https://docs.unity3d.com/Manual/UsingComponents.html).

If no procedural components are present, the Cinemachine Camera controls the Unity Camera position and rotation by its [Transform](https://docs.unity3d.com/Manual/class-Transform.html).

### Custom behaviors and extensions

You can write custom scripts inheriting the `CinemachineComponentBase` or `CinemachineExtension` class to implement your own custom moving behaviors or extensions. When you create such a behavior or extension, it becomes automatically available for selection among the existing ones.

## Additional resources

* [Add procedural behavior to a Cinemachine Camera](setup-procedural-behavior.md)