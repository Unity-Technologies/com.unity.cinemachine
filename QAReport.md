# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: [*Joel Fortin*]
## UX Owner: [*Joel Fortin*]

## Testing coverage done on this package:
Cinemachine 2.2 test status
-	Timeline and CinemachineTrack
--	Tested mid-blending when we start a timeline during a blend that is happening on the game camera
--	Tested mid-blending on stacked Cinemachine tracks
-	New StandBy updated mode
--	Performance profiling validated with ~100 cameras in the scene (all in StandBy state). Always reproduces the legacy behavior
--	None mode is seemless when transitioning to another standby camera and improve the performance
-- 	Round Robin actually update one camera per frame and shows a significant performance improvement
--	Also tested current example scenes we have to regression
**Note:** About the Round Robin mode … it exists only because camera like ClearShot needs to be updated in order to find its best shot possible. With the Round Robin mode though, we may notice a small jerky effect since the child cameras aren’t evaluating the shot on every frame. Ideally, I found that using the Always update mode is the best way to go for clear shot. Making us wonder the usefulness of the Round Robin update mode.
-	Impulse Source component
--	Tested Collision Impulse Source on an object collider with ImpulseSource extension on a VCam (game camera)
--	Tested Impulse Source on a character colliding with an object have a Trigger Impulse script on it … still having the game camera to use the ImpulseSource extension
--	Tested Impulse API to Generate a signal continuously while a target stays in a trigger
--	Tested new 2D spacial range to validate the Z distance from the camera is ignored
--	Tested when both “Scale with Mass” and “Scale with Speed” activated to make sure the result is correct
-	New visual in  the Noise asset inspector
--	Functional Testing on the settings expose
-	Noise profile and Lens preset asset options tested
--	Cloning those asset from the inspector now pop up a save as dialog for more deterministic workflow
--	Also tested that cloning an asset does allow the user to save directly in the package directory cache which is read only
-	Various Regression check on existing example scenes


Bugs found (and fixed in .preview8) during testing
-	Solo Mode broken
-	Having Simple Follow on a camera always starts the game snapping the camera to a position like if it didn’t have bias or set offset
-	Orbital Transposer gizmo in the scene view, using transform gizmo is scaling the orbit ignoring the bias value set
-	LensSettings preset file location needs to be deterministic (set by the user)
-	X Recentering on FreeLook doesn’t work anymore
-	Y Axis Input field on FreeLook report errors in the console as we type the input string
-	Impulse Source Gain value must be ridiculously high for the user to see an actual noise when triggered
-	Impulse Source spacial range needs to behave differently when in 2D mode
-	User is able to clone an Impulse definition asset directly in the package cache which is read only
-	Starting a timeline in mid-blending is causing a cut on the shot when the timeline begins. (Bug Found by users)

