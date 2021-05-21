**Cinemachine 2.6.5-pre.1 Testing Report**

Cinemachine 2.6.5-pre.1 release is a bugfix release with the following:

-  Bugfix: Framing transposer now handles empty groups
-  Bugfix: BlendListCamera inspector reset did not reset properly.
-  Bugfix: Cinemachine3rdPersonFollow handled collisions by default, now it is disabled by default.
-  Regression fix: Entries in the custom blends editor in CM Brain inspector were not selectable.
-  Bugfix: SaveDuringPlay saves only components that have the SaveDuringPlay attribute.
-  Bugfix: Reversing a blend in progress respects asymmetric blend times.
-  Regression fix: CmPostProcessing and CmVolumeSettings components setting Depth of Field did not work correctly with Framing Transposer.
-  Regression fix: 3rdPersonFollow keeps player in view when Z damping is high
-  Bugfix: CinemachineCollider's displacement damping was being calculated in world space instead of camera space.
-  Bugfix: TrackedDolly sometimes introduced spurious rotations if Default Up and no Aim behaviour.
-  Added ability for vcam to have a negative near clip plane
-  Default PostProcessing profile priority is now configurable, and defaults to 1000

Regression testing using the Sample Scenes was done. Each scene was loaded to make sure that the Cinemachine behavior or feature it is demonstrating was ok. 

Manual testing around each of the Cinemachine Sample Scene was done.







