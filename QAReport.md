**Cinemachine 2.6.6 Testing Report**

Cinemachine 2.6.6 release is a bugfix release with the following:

-  Bugfix: Freelook prefabs won't get corrupted after editing the Prefab via its instances.
-  Bugfix: 3rdPersonFollow works with Aim components now.
-  Bugfix: Blends between vcams, that are rotated so that their up vector is different from World up, are correct now.
-  Bugfix: 3rdPersonFollow's shoulder now changes smoothly with respect to world-up vector changes.
-  Bugfix: POV recentering did not always recenter correctly, when an axis range was limited.
-  Bugfix: CinemachineVolumeSettings inspector was making the game view flicker.
-  Bugfix: CinemachineVolumeSettings inspector displayed a misleading warning message with URP when focus tracking was enabled.
-  Bugfix: Rapidly toggling active cameras before the blends were finished did not use the correct blend time.
-  AimingRig sample scene updated with a better reactive crosshair design.
-  Added API accessor for Active Blend in Clearshot and StateDrivenCamera.
-  Bugfix: Virtual Cameras were not updating in Edit mode when Brain's BlendUpdateMode was FixedUpdate.

All the above were tested and verified in Unity 2020.3.14f1.

Regression testing using the Sample Scenes was done. Each scene was loaded to make sure that the Cinemachine behavior or feature it is demonstrating was ok. 







