**Cinemachine 2.8.0-exp.1 Testing Report**

The following additions were verified and tested in Unity 2021.1.1f1:

- Tested new simplified modes for the Cinemachine impulse generation in the context of the Impulse sample scene. Verified the secondary reaction settings to the Impulse Listener.
- Verified Storyboard render modes support for ScreenSpaceOverlay and ScreenSpaceCamera.
- Verified Damping Into Collision and Damping From Collision properties to Cinemachine 3rdPersonFollow to control how gradually the camera moves to correct for occlusions. Tested using AimingRig sample scene from Cinemachine package.
- Verified that VCam can now have a negative near clip plane when VCam set to Orthographic.
- Verified option to make game-view guides visible but not clickable in Cinemachine preference.
- Verified that when Input System package is installed, there is a button in virtual camera inspectors to auto-generate CinemachineInputProvider component if missing.
- Verified that default CinemachinePostProcessing profile priority is now configurable and defaults to 1000.
- Verified that Cinemachine3rdPersonFollow can operate without the physics module, and without collision resolution.

- The following Bugfix were verified in Unity 2021.1.1f1:
  - Bugfix: 3rdPersonFollow collision resolution was failing when the camera radius was large.
  - Bugfix: 3rdPersonFollow damping was being done in world space instead of camera space.
  - Bugfix: 3rdPersonFollow was stuttering when Z damping was high.
  - Regression fix: CinemachineInputProvider had stopped providing input.
  - Bugfix: Lens aspect and sensorSize were not getting updated if lens OverrideMode != None.
  - Bugfix: Changing targets on a live vcam was misbehaving.
  - Bugfix: Framing transposer now handles empty groups.
  - Bugfix: Interrupting a transition with InheritPosition enabled was broken.
  - Bugfix: Cinemachine3rdPersonFollow was not handling collision by default.
  - Bugfix: SaveDuringPlay saves only components that have the SaveDuringPlay attribute.
  - Regression fix: Entries in the custom blends editor in CM Brain inspector were not selectable.
  
  
- Regression testing was done mainly using the Cinemachine Sample Scenes.







