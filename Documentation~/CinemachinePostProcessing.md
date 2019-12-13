# Post Processing Extension

Use the Cinemachine Post Processing [extension](CinemachineVirtualCameraExtensions.html) to attach a Postprocessing V2 profile to a Virtual Camera.

**Note**: Unity recommends using Postprocessing V2 instead of Postprocessing V1.

The Cinemachine Post Processing extension holds a Post-Processing Profile asset to apply to a Virtual Camera when it is activated. If the camera is blending with another Virtual Camera, then the blend weight is applied to the Post Process effects also.

Before attaching post processing profiles to Virtual Cameras, you first need to set up your project to use post processing. You need to do this setup only once.

To set up project to use Post Processing V2 with Cinemachine:

1. [Install](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html) the Postprocessing V2 package.

2. In the Unity menu, choose __Cinemachine > Import Post Processing V2 Adaptor Asset Package__.

3. Select your Unity camera with Cinemachine Brain in the [Scene](https://docs.unity3d.com/Manual/UsingTheSceneView.html) view.

4. [Add the component](https://docs.unity3d.com/Manual/UsingComponents.html) named __Post-Process Layer__.  This will anble Post Process profiles to affect the Camera.

To add a Post Process profile to a Virtual Camera

3. Select your Virtual Camera in the [Scene](https://docs.unity3d.com/Manual/UsingTheSceneView.html) view or [Hierarchy](https://docs.unity3d.com/Manual/Hierarchy.html) window.

4. In the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html), choose __Add Extension > CinemachinePostProcessing__, then configre the Profile asset to have the effects you want when this virtual camera is live.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Profile__ | The Post-Processing profile to activate when this Virtual Camera is live. |
| __Focus Tracks Target__ | Check to set Focus Distance to the distance between the camera and the Look At target. |
| __Offset__ | When Focus Tracks Target is checked, this offset is applied to the target position when setting the focus, focus distance.  If there is no Look At target, then this is the offset from the Unity camera position, the actual focus distance. |


