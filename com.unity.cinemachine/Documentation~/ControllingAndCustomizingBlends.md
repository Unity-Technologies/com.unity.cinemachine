# Controlling and Customizing Blends

Cinemachine offers a number of ways to control how one camera blends to another when the active camera changes.  The easiest and most common ways involve setting up assets to define rules about how to blend between specific cameras or families of cameras.  

For more advanced users, it's possible to drive blend styles based on game events or other dynamic criteria, and even to customize the blend algorithm itself - but these techniques will require some custom coding.

## Default Blend

The most basic strategy is to set up a Default Blend in the CinemachineBrain.  This blend setting will be used for all blends which are not covered by more specific settings and rules.

Default Blend is available in the Cinemachine Brain, and in Cinemachine Manager Cameras which control a set of child cameras (such as ClearShot, StateDrivenCamera, etc).

## BlenderSettings Asset

All Cinemachine components having a Default Blend setting also have a Custom Blends setting which holds a BlenderSettings asset.  This asset contains a list of blend settings to apply when blending between cameras with specific names.  With it, you can control how individual cameras blend to other cameras by setting their blend curves and blend durations.  Cameras which are not covered by specific rules listed here will be picked up by the default blend.

## Timeline Shot Overlapping

Independently of Default Blend and Blender Settings assets, the Timeline also controls blends which are made by overlapping Cinemachine Shots on the Timeline's Cinemachine Track.  When blends are created this way, they are explicitly controlled by Timeline, and are not affected by the other blend control settings.  The blend duration is determined by the overlap size, and the blend curve is controlled by the easing curves specified in the Cinemachine Shot (which by default are ease-in-ease-out).

Note that this precise control is obtained only when overlapping Cinemachine Shots.  If you use Activation Tracks in Timeline to activate and deactivate Cinemachine Cameras, then the standard blending controls (Default Blend and Blender Settings) will apply for those blends.

## `CinemachineCore.BlendCreatedEvent`

Every time a blend is created by Cinemachine, an event is fired giving you a chance to override the settings of that blend based on arbitrary dynamic criteria.

If you install a handler for this event (perhaps by using the CinemachineBrainEvents class), then your handler can check things like game context and decide either to allow the blend to remain as-is, or to override such things as blend style, blend duration, or blend algorithm.

This is an advanced technique and requires scripting to implement the event handler.

Note that this event is NOT fired for blends created by overlapping Timeline Shots.  This is because there is an expectation that Timeline precisely controls the blending, and this cannot be overridden.

## `CinemachineCore.GetCustomBlender`

The most advanced level of control is customizing the blend algorithm itself.  Cinemachine has a sophisticated algorithm for lerping camera states (`CameraState.Lerp()`), which interpoltes position, rotation, lens settings, and other attributes while taking into account blend hints and the screen position of the lookAt target.

These things are all lerped at the same rate, so position, rotation, and lens all change together.  You may have a situation where you want the rotation to happen first, or the position to follow a path, or some other requirement not handled natively by Cinemachine.

In this case, your recourse is to author a custom blender, which implements the `CinemachineBlend.IBlender` interface.  You can provide Cinemachine with this blender by hooking into the `CinemachineCore.GetCustomBlender` delegate.  Cinemachine will call this whenever a blend is created - even from Timeline.  You can check the cameras to be blended and whatever other state you like, and either provide a custom blender or returen null for the default one.

Cinemachine comes with two sample scenes - `Early LookAt Custom Blend` and `Perspective To Ortho Custom Blend` which illustrate this technique.  Coding profficiency is required.
