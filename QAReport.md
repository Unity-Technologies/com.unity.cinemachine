**Cinemachine 2.8.0-pre.1 Testing Report**

The following additions were verified and tested in Unity 2021.1.5f1:

- Verified simplified modes to Impulse Source and the secondary reaction settings to Impulse Listener.
- Verified Storyboard support for ScreenSpaceOverlay and ScreenSpaceCamera camera render modes.
- Verified new DampingIntoCollision and DampingFromCollision properties to Cinemachine3rdPersonFollow to control how gradually the camera moves to correct for occlusions.
- Verified ability for vcam to have a negative near clip plane.
- Verified Draggable Game Window Guides toggle in Cinemachine preferences. Verified that when disabled, Game Window guides are only for visualization.
- Verified that the default PostProcessing profile priority is now configurable and defaults to 1000. (static public float s_VolumePriority = 1000f;)
- Verified that Cinemachine3rdPersonFollow now operates without the physics module and without collision resolution.
- Tested new sample scene: **Boss cam** that demonstrates a camera setup to follow the player and to look at the player and the boss. The scene provides examples of custom extensions.
- Tested new Sample scene: **2D zoom**, showing how to zoom an orthographic camera with mouse scroll.
- Tested new Sample scene: **2D fighters**, showing how to add/remove targets gradually to/from a TargetGroup based on some conditions (here, it is the y coord of the players).
- The following Bugfix were verified in Unity 2021.1.5f1:
  - Bugfix: Reversing a blend in progress respects asymmetric blend times.
  - Bugfix: 3rdPersonFollow collision resolution failed when the camera radius was large.
  - Bugfix: 3rdPersonFollow damping occured in world space instead of camera space.
  - Bugfix: 3rdPersonFollow stuttered when Z damping was high.
  - Bugfix: Lens aspect and sensorSize were updated when lens OverrideMode != None.
  - Bugfix: Changing targets on a live vcam misbehaved.
  - Bugfix: Framing transposer did not handle empty groups.
  - Bugfix: Interrupting a transition with InheritPosition enabled did not work.
  - Bugfix: Cinemachine3rdPersonFollow handled collisions by default, now it is disabled by default.
  - Bugfix: SaveDuringPlay saved some components that did not have the SaveDuringPlay attribute.
  - Bugfix: CinemachineCollider's displacement damping was being calculated in world space instead of camera space.
  - Bugfix: TrackedDolly sometimes introduced spurious rotations if Default Up and no Aim behaviour.
  - Regression fix: CinemachineInputProvider stopped providing input.
  - Regression fix: CmPostProcessing and CmVolumeSettings components setting Depth of Field now works correctly with Framing Transposer.
  - Regression fix: 3rdPersonFollow kept player in view when Z damping was high.
  - Regression fix: Physical camera properties were overwritten by vcams when "override mode: physical" was not selected.

- Regression testing was done mainly using the Cinemachine Sample Scenes.







