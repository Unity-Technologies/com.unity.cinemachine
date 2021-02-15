**Cinemachine 2.7.2 Testing Report**

For the Cinemachine 2.7.2 release, the following bug fixes were tested and verified:

- Bugfix (1293429): Brain could choose vcam with not the highest priority in some cases
- Bugfix (CMCL-193): SaveDuringPlay also works on prefab instances
- Bugfix (1272146):  Adding vcam to a prefab asset no longer causes errors in console
- Bugfix (1290171): Impulse manager was not cleared at playmode start
- Bugfix (CMCL-190): Nested Scrub Bubble sample removed (filenames too long), available now as embedded package
- Bugfix (CMCL-194): Compilation guards for physics, animation, and imgui. Cinemachine does not hard depend on anything now
- Bugfix (CMCL-203): CM StoryBoard had a 1 pixel border
- Bugfix (CMCL-203): CM StoryBoard lost viewport reference after hot reload
- Bugfix (CMCL-204): FramingTransposer's TargetMovementOnly damping caused a flick.
- Bugfix (CMCL-208): FreeLook small drift when no user input if SimpleFollowWithWorldUp
- Bugfix (CMCL-214): InheritPosition did not work with SimpleFollow binding mode
- Bugfix (CMCL-145): cleanup straggling post processing profiles when no active vcams
- Bugfix (CMCL-221): Checking whether the Input Action passed to CinemachineInputHandler is enabled before using it.
- Bugfix (CMCL-191): 3rdPersonFollow FOV was blended incorrectly when ReferenceLookAt was set to a faraway target
- Bugfix (CMCL-202): Position predictor not properly reset
- Bugfix (CMCL-207): Create via menu doesn't create as child of selected object
- Bugfix (CMCL-217): Post-processing profiles not cleaned up when no active vcams
- Bugfix (CMCL-223): Install CinemachineExamples Asset Package menu item was failing on 2018.4 / macOS

The following changes were done and tested in the Cinemachine Sample Scenes:

- New sample scene (FadeOutNearbyObjects) demonstrating fade out effect for objects between camera and target using shaders. The example includes a Cinemachine extension giving convenient control over the shader parameters
- New sample scene (2DConfinerComplex) demonstrating new CinemachineConfiner2D extension.
- Updated CharacterMovement2D script in 2D sample scenes (2DConfinedTargetGroup, 2DConfiner, 2DConfinerUndersized, 2DTargetGroup) to make jumping responsive.

Changes were also don with the previously released CinemachineConfiner2D. Both changes were validated and tested.

- CinemachineConfiner2D now handles cases where camera window is oversized
- Updated 2DConfinedTargetGroup and 2DConfiner scenes to use new CinemachineConfiner2D extension.



The Cinemachine 2.7.2 was also regression tested using the Sample Scenes. Each scene was loaded to make sure that the Cinemachine behavior or feature it is demonstrating was ok. Manual testing around each of the Cinemachine Sample Scene was done.





