# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: [*Joel Fortin*]
## UX Owner: [*Joel Fortin*]

## Testing coverage done on this package:
Cinemachine 2.1.11
Packman integration
-	Old projects already containing the plugin will generate error in the console if updated with package manager. The correct way to update to the latest packman plugin would be to delete the Cinemachine folder you have in the project then download the package from package manager.
-	Also templates work fine with cinemachine except for the “lightweight” template, where the blending camera preview in edit mode will simply not work. The work needed to fix is too risky to include cinemachine in template for 2018.1 so the package will only be discoverable for now.
New features tested:
-	New Blend style option on LookAt Target
-	Storyboard extension on VCam
-	Min, Max, Wrap range on POV, Orbital and FreeLook
-	FreeLook Y axis recentering
-	Normalized unit option on Paths and length label
-	New LookAhead Ignore Y Axis movement option
-	Custom blend curves for Camera transitions
-	Predictor support to time pause
-	
Issues found with CM package 2.1.11-beta.1 currently in production and fixed/validated in 2.1.11-rc.4 (currently living is staging)
-	Building the game was failing using cinemachine from the cache
-	The duration of custom blend curve was always 1 sec
-	Undo problem on Storyboard extension
-	Enable/Disable checkbox on Extension component wasn’t working
-	Ignore Y axis in LookAhead wasn’t working
-	Storyboard Split and Center settings had issues since a change to canvas code in 2018.1.0b5
-	Removal of “Shortest way out” option in Collider extension

Cinemachine Example package
-	Testing the integrity of the example scenes to validate they behave the same as before
-	Also created some of those examples from scratch with the package manager plugin
