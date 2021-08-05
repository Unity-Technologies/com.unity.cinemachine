**Cinemachine 2.8.0 Testing Report**

The following were verified and tested in Unity 2021.2.0b3:

- Bugfix: Freelook prefabs won't get corrupted after editing the Prefab via its instances.
- Bugfix: 3rdPersonFollow works with Aim components now.
- Bugfix: Blends between vcams, that are rotated so that their up vector is different from World up, are correct now.
- Bugfix: POV recentering did not always recenter correctly, when an axis range was limited.
- Bugfix: Collider sometimes bounced a little when the camera radius was large.
- Bugfix: CinemachineVolumeSettings inspector was making the game view flicker.
- Bugfix: CinemachineVolumeSettings inspector displayed a misleading warning message with URP when focus tracking was enabled.
- Bugfix: Rapidly toggling active cameras before the blends were finished did not use the correct blend time.
- AimingRig sample scene updated with a better reactive crosshair design.
- Added API accessor for Active Blend in Clearshot and StateDrivenCamera.
- Bugfix: Virtual Cameras were not updating in Edit mode when Brain's BlendUpdateMode was FixedUpdate.
- Bugfix: Lens mode override was not working correctly in all cases.
- Collider2D inspector: added warning when collider is of the wrong type.



Regression testing was done mainly using the Cinemachine Sample Scenes.







