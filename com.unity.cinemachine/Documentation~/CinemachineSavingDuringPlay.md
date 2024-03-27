# Saving in Play Mode

It’s often most convenient to adjust camera settings while the game is playing. But normally, Unity does not save your changes to the Scene when you exit Play Mode. Cinemachine has a special feature to preserve the tweaks you make during Play Mode.  It doesn’t save structural changes, like adding or removing a behavior. With the exception of certain properties, Cinemachine preserves most of the settings in your CinemachineCameras when you exit Play Mode.

When you exit Play Mode, Cinemachine scans the Scene to collect any changed properties in the CinemachineCameras.  Cinemachine saves these changes a second or so after exiting. Use the __Edit > Undo__ command to revert these changes.

Check __Save During Play__ on any CinemachineCamera in the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) to enable this feature.  This is a global property, not per-camera, so you only need to check or uncheck it once.

Cinemachine components have the special attribute `[SaveDuringPlay]` to enable this functionality. Specific fields are excluded from being saved by having the `[NoSaveDuringPlay]` attribute added to the field.

You can also use the `[SaveDuringPlay]` and `[NoSaveDuringPlay]` on your own custom scripts, to acquire the same functionality for them as well.

