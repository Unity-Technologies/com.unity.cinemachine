# Post Processing Extension

Use the Cinemachine Post Processing [extension](concept-procedural-motion.md#extensions) to attach a Postprocessing V2 profile to a CinemachineCamera.

**Note 1**: Unity recommends using Postprocessing V2 instead of Postprocessing V1.

**Note 2**: With HDRP and URP 7 and up, The PostProcessing package is deprecated, and is implemented natively by HDRP and URP.  In that case, please see the __CinemachineVolumeSettings__ extension.

The Cinemachine Post Processing extension holds a Post-Processing Profile asset to apply to a CinemachineCamera when it is activated. If the camera is blending with another CinemachineCamera, then the blend weight is applied to the Post Process effects also.

Before attaching post processing profiles to CinemachineCameras, you first need to set up your project to use post processing.

To set up a project to use Post Processing V2 with Cinemachine:

1. [Install](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html) the Postprocessing V2 package.

2. Select your Unity camera with Cinemachine Brain in the [Scene](https://docs.unity3d.com/Manual/UsingTheSceneView.html) view.

3. [Add the component](https://docs.unity3d.com/Manual/UsingComponents.html) named __Post-Process Layer__.  This will enable Post Process profiles to affect the Camera.

To add a Post Process profile to a CinemachineCamera

4. Select your CinemachineCamera in the [Scene](https://docs.unity3d.com/Manual/UsingTheSceneView.html) view or [Hierarchy](https://docs.unity3d.com/Manual/Hierarchy.html) window.

5. In the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html), choose __Add Extension > CinemachinePostProcessing__, then configre the Profile asset to have the effects you want when this CinemachineCamera is live.


> [!NOTE]
> In some cases, particularly when blending to and from empty profiles, you might get a sudden change or pop in the effects.  If this happens, the best solution is to avoid blending to and from empty profiles by adding effects with default settings.  If this is not practical, then you can add `CINEMACHINE_TRANSPARENT_POST_PROCESSING_BLENDS` to your project's scripting defines.  However, this has the side effect of making postprocessing blends more transparent in their center, possibly revealing global effects behind them.

## Properties:

| **Property:** || **Function:** |
|:---||:---|
| __Profile__ || The Post-Processing profile to activate when this CinemachineCamera is live. |
| __Focus Tracks Target__ || This is obsolete, please use __Focus Tracking__. |
| __Focus Tracking__ || If the profile has the appropriate overrides, will set the base focus distance to be the distance from the selected target to the camera. The __Focus Offset__ field will then modify that distance. |
|| _None_ | No focus tracking. |
|| _Look At Target_ | Focus offset is relative to the LookAt target. |
|| _Follow Target_ | Focus offset is relative to the Follow target. |
|| _Custom Target_ | Focus offset is relative to the Custom target. |
|| _Camera_ | Focus offset is relative to the camera |
| __Focus Target__ || The target to use if __Focus Tracks Target__ is set to _Custom Target_.|
| __Focus Offset__ || Used when __Focus Tracking__ is not _None_.  Offsets the sharpest point away from the location of the focus target. |
| __Weight__ || The weight of the dynamic volume that will be created, when the camera is fully blended in.  This will blend to and from 0 along with the camera.|


