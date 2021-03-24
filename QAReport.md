**Cinemachine 2.6.4-pre.1 Testing Report**

Cinemachine 2.6.4-pre.1 release is a bugfix release with the following:

-  Bugfix: 3rdPersonFollow collision resolution failed when the camera radius was large.
-  Bugfix: 3rdPersonFollow damping was done in world space instead of camera space.
-  Bugfix: 3rdPersonFollow stuttered when z damping was high.
-  Bugfix: 3rdPersonFollow FOV blended incorrectly when ReferenceLookAt was set to a faraway target.
-  Bugfix: Position predictor was not properly reset.
-  Bugfix: FramingTransposer's TargetMovementOnly damping caused a flick.
-  Bugfix: CM StoryBoard lost viewport reference after hot reload.
-  Bugfix: CM StoryBoard had a 1 pixel border.
-  Bugfix: SaveDuringPlay also works on prefab instances.
-  Bugfix: Re-awakened long-idle vcams could have a single frame with a huge deltaTime.
-  Bugfix (1290171): Impulse manager was not cleared at playmode start.
-  Bugfix (1272146): Adding a vcam to a prefab asset caused errors in the console.

Regression testing using the Sample Scenes was done. Each scene was loaded to make sure that the Cinemachine behavior or feature it is demonstrating was ok. 

Manual testing around each of the Cinemachine Sample Scene was done.
