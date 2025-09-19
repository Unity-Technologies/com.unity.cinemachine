# Control and customize blends

Cinemachine offers a number of ways to control how one camera blends to another when the active camera changes.

* The easiest and most common ways involve setting up assets to define rules about how to blend between specific cameras or families of cameras. 

* For more advanced users, it's possible to drive blend styles based on game events or other dynamic criteria, and even to customize the blend algorithm itself, but these techniques require some custom coding.

## Default blend

The most basic strategy is to set up a **Default Blend** in the [Cinemachine Brain](CinemachineBrain.md). Cinemachine uses this blend setting for all blends that are not covered by more specific settings and rules.

**Default Blend** is available in the Cinemachine Brain, and in Cinemachine Manager Cameras which themselves control and blend between a set of child cameras, such as [Clear Shot Camera](CinemachineClearShot.md) and [State-Driven Camera](CinemachineStateDrivenCamera.md).

## Custom blends with Blender Settings asset

All Cinemachine components that have a **Default Blend** property also have a **Custom Blends** setting which holds a [Blender Settings asset](CinemachineBlending.md).

A Blender Settings asset contains a list of blend settings to apply when blending between cameras with specific names. With this asset, you can control how individual cameras blend to other cameras by setting their blend curves and blend durations.

Any blends with cameras that are not listed in the **Blender Settings** rules fall back to the **Default Blend**.

## Timeline shot overlapping

Timeline explicitly controls blends that you make by [overlapping Cinemachine Shots](setup-timeline.md) on the Timeline's [Cinemachine Track](concept-timeline.md). These blends are not affected by the other blend control settings like **Default Blend** and **Custom Blends** with Blender Settings assets.

The blend duration is determined by the overlap size, and you can control the blend curve with the easing curves specified in the Cinemachine Shot (which by default are ease-in-ease-out).

> [!NOTE]
> This precise control is obtained only when overlapping Cinemachine Shots. If you use [Activation Tracks in Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/insp-trk-act.html) to activate and deactivate Cinemachine Cameras, then the standard blending controls (**Default Blend** and **Custom Blends** with Blender Settings asset) always apply for those blends.

## Blend Hints

Each [Cinemachine Camera](CinemachineCamera.md) has a **Blend Hint** property. This influences the way that the camera is blended with other cameras. It doesn't control the timing, but rather the algorithm, so it's orthogonal to the other controls mentioned above. 

The hints you set there can affect the way Cinemachine interpolates the camera position and rotation. You can control whether the LookAt target is taken into account. You can also control whether the blend is from a live moving camera or based on a snapshot of the outgoing camera taken at the start of the blend.

## `CinemachineCore.GetBlendOverride`

Every time Cinemachine creates a blend, a `CinemachineCore.GetBlendOverride` delegate is called giving you a chance to override the settings of that blend based on arbitrary dynamic criteria.

If you install a handler for this delegate, then your handler can check things like game context and decide either to allow the blend to remain as-is, or to override such things as blend style, blend duration, or blend algorithm.

This is an advanced technique and requires scripting to implement the event handler.

> [!NOTE]
> This delegate is NOT called for blends created by overlapping Timeline Shots. There is an expectation that Timeline precisely controls the blending, and this cannot be overridden.

## `CinemachineCore.GetCustomBlender`

The most advanced level of control is to customize the blend algorithm itself. Cinemachine has a sophisticated algorithm for lerping camera states (`CameraState.Lerp()`), which interpolates position, rotation, lens settings, and other attributes while taking into account blend hints and the screen position of the lookAt target.

These things are all lerped at the same rate, so position, rotation, and lens all change together. You may have a situation where you want the rotation to happen first, or the position to follow a path, or some other requirement that Cinemachine doesn't handle natively.

In this case, you can author a custom blender, which implements the `CinemachineBlend.IBlender` interface. You can provide Cinemachine with this blender by hooking into the `CinemachineCore.GetCustomBlender` delegate. Cinemachine calls this delegate whenever a blend is created, even from Timeline. You can check the cameras to be blended and whatever other state you like, and either provide a custom blender or return null for the default one.

Cinemachine comes with two [sample scenes](samples-tutorials.md), `Early LookAt Custom Blend` and `Perspective To Ortho Custom Blend`, which illustrate this technique. Coding profficiency is required.

## Additional resources

* [Camera control and transitions](concept-camera-control-transitions.md)
* [Cinemachine and Timeline](concept-timeline.md)
