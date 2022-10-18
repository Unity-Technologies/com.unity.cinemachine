# Upgrading a Project from Cinemachine 2.X

Cinemachine 3.0 is a major version change from CM 2.X, and the API and data format have changed significantly. Scripts written for the CM 2.X API are unlikely to run with 3.X without manual intervention. Also, the CM camera instances in your project will themselves need upgrading.

While it is possible to upgrade an existing project from CM 2.X, you should think carefully about whether you are willing to put in the work. __It might be better in many cases just to stick with CM 2.X__, which will continue to be supported for a while in parallel with CM 3.X.  If you do choose to consider upgrading your project, this guide will show you some pointers to make the process smoother.

There are 2 steps to take when upgrading an existing project from CM 2.X:
1. Upgrade any custom scripts to support the new API
1. Upgrade the project data

## What has Changed in the API

Some Components were replaced by new components, others were renamed.  Field names have changed.  For most of these issues, you will see errors or deprecation warnings in the console, which will point you to the areas in your code that need attention.  You should upgrade these first.

One thing to note: the new CmCamera class which replaces CinemachineVirtualCamera and CinemachineFreeLook inherits from CinemachineVirtualCameraBase.  If you can replace your script references to use this base class wherever possible, then existing object references will be preserved, since the old classes also inherited from this same base class.

### New Components with Clearer Names

Old components have beed replaced by new components.  These are not renames, they are new component types.  If your scripts refer to any of them, they will need to be updated.
- CinemchineVirtualCamera is replaced by [CmCamera](CmCamera.md).
- CinemachineFreeLook is replaced by [CmCamera](CmCamera.md).
- CinemachinePath and CinemachineSmoothPath are replaced by Spline Container, provided by Unity's new native spline implementation.
- CinemachineDollyCart is replaced by [CinemachineSplineCart](CinemachineSplineCart.md).
- CinemachineTransposer is replaced by [CinemachineFollow](CinemachineFollow.md).
- CinemachineOrbitalTransposer is replaced by [CinemachineOrbitalFollow](CinemachineOrbitalFollow.md)
- CinemachineFramingTransposer is replaced by [CinemachinePositionComposer](CinemachinePositionComposer.md).
- CinemachineComposer is replaced by [CinemachineRotationComposer](CinemachineRotationComposer.md).
- CinemachinePOV is replaced by [CinemachinePanTilt](CinemachinePanTilt.md).
- CinemachineTrackedDolly is replaced by [CinemachineSplineDolly](CinemachineSplineDolly.md).
- CinemachineGroupComposer is replaced by the [CinemachineGroupFraming](CinemachineGroupFraming.md) extension used in conjunction with [CinemachineRotationCompoer](CinemachineRotationComposer.md).

### Renamed Components

- CinemachineCollider has been renamed to [CinemachineDeoccluder](CinemachineDeoccluder.md)

### Fields Renamed

The old convention of using "m_FieldName" has been changed to follow Unity's latest naming conventions.  Consequently, all of the "m_" prefixes have been removed from field names, everywhere.  If your scripts don't compile because of this, the first remedy is to remove the "m_" from the field name that your script is referencing.  Most of the time, that will be enough.  Occasionally, some field names were changed more significantly.  It should be fairly easy to find the appropriate replacements.

### Cleaner Object Structure, No Hidden GameObjects

Cinemachine 2.x implemented the CM pipline on a hidden GameObject child of the vcam, named "cm".  This has beed removed in CM 3.0, and CM pipeline components (such as OrbitalFollow or RotationComposer) are now implemented directly as components on the CmCamera GameObject.  You can access them as you would any other components: `GetCinemcachineComponent()` is no longer necessary, just use `GetComponent()`.

You will now see the `cm` children of your legacy CM vcams in the hierarchy, because CM3 unhides them.  This is not a license to mess with them.  We recommend that you get rid of them by upgrading the objects to their CM3 equivalents.

### New Input Handling
User input has beed decoupled from the Cinemachine Components: they no longer directly read user input, but expect to be driven by an external component.  [CinemachineInputAxisController](CinemachineInputAxisController.md) is provided to to this job, but you could also choose to implement your own input controller.


## Upgrading the Project Data

Once the scripts are using the new API, you can upgrade the project data to convert legacy CM objects to their CM3 counterparts.  Cinemachine comes with a data upgrade tool to facilitate this.  It's not a trivial operation, because in addition to the vcam objects in your scene, it's also necessary to upgrade prefabs and animation assets that might be referencing them.

You can launch the CM data upgrade tool from any CM VirtualCamera or FreeLook inspector:

![Launching the Upgrader tool](images/CinemachineUpgraderLauncher.png)

![Upgrader tool](images/Upgrader.png)

If you want to upgrade only the Cinemachine object being inspected, you can do this provided that it isn't a prefab instance.  In this case, it will upgrade only the inspected objects, replacing them with CM3 equiavlents.  Undo is supported, so you can try it out then change your mind if you want.  

Note that any script references to this object will be lost (because the class will change), as will any animation tracks that are writing to fields inside this camera (because classes and field names have changed).  If you have script references or animation tracks or if this camera is part of a prefab or prefab instance, then you need to use the "Upgrade Entire Project" option, which will scan the project for references and make the appropriate updates.

You can also choose to update all the CM objects in the current scene.  Again, this will not update any assets outside of the scene, so it is not appropriate for any but the simplest of scenes.  Undo is also supported for this operation.

The "Upgrade Entire Project" option will upgrade all the objects in all the scenes and all the prefabs.  There is logic to handle animation tracks, script references, and prefab instances.  It's a major operation and every scene and prefab in the project will be opened and saved multiple times.  Undo is not supported, so be sure to make a complete backup first.


