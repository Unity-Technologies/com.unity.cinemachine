# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: [*Joel Fortin*]
## UX Owner: [*Joel Fortin*]

## Testing coverage done on this package:
  * Tested the integrity of the example package contained in the cinemachine package
  * Tested the integrity of the UseCases package on test rail
  * Validation of bug fixes related the FreeLook camera
    * Mainly for the jitters happening when blending between two FreeLook
  * Test pass on the new Noise UI
    * Clone, Locate and Create options (may need additional improvements but not in this package)
  * New Aim type "Same As Follow Target"
    * Testing of the rotation transform is correctly applied when targetting another VCam (this will imply change modifying the way the camera list is evaluated, not in this package)
    * Testing of using a Unity camera as  the target, rotation and transform are correctly applied to the VCam
    * Current known limitation: The lens settings of the target don't propagate on the camera that is following it
  * Validated the new structure modification about Noise Presets being moved outside the Example scene structure due to the new Import option for example package
  * New Import Examples option tested
  * Also tested the Gizmo icon being created when the Cinemachine package is installed and not being part of the cinemachine package itself (no Gizmos folder anymore in the package)
