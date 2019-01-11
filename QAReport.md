# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: [*Joel Fortin*]
## UX Owner: [*Joel Fortin*]

## Testing coverage done on this package:
/*Cinemachine 2.3.0 test status*/

**Overall Test pass**
This version is a Cinemachine 2.2.8 port to Unity 2019.1 in almost its integrity. Basic testing has been done on that version to make sure the Example scene are still working. 

The only thing that was added to that version is :

- Gizmos folder is not created automatically in the Assets folder anymore, it is now part of the package and leaves the Assets folder clean. TESTED
- A dependendy on the Timeline package has been added. Since Timeline is now a package, installing Cinemachine was logging a lot of errors in the console. TESTED

**Important Note**

- Cinemachine 2.3.0 is the only version that supported by Unity 2019.1
- Using older version of Cinemachine in 2019.1 will report error in the console because of the Timeline dependency
- Cinemachine 2.3.0 should not be visible from the package list in 2018.3
- Cinemachine 2.2.8 and older shouldn't be visible from the package list in 2019.1 either
- Old projects saved in previous version will report errors in the console, an automatic update mechanism is being developped for fix this.
- Various optimization accross different components

**Minor issue that still need to be fixed**

- Cinemachine logo doesn't appear beside the Main Camera logo when a Brain is created. (Only appens on fresh new project, as soon as we enter in Play mode, the logo will appear in the hierarchy as it should) (Logged as --> https://fogbugz.unity3d.com/f/cases/1109688/)
