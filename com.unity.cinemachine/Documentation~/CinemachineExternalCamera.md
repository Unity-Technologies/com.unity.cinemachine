# Cinemachine External Camera - (deprecated)

**Note:** This component is **deprecated** in favour of using a normal CinemachineCamera with _None_ in both __Position Control__ and __Rotation Control__.

This component will expose a non-cinemachine camera to the cinemachine ecosystem, allowing it to participate in blends. Just add it as a component alongside an existing Unity Camera component. You will need to take steps (e.g. disabling the Camera component) to ensure that the Camera doesn't fight with the main Cinemachine Camera.

## Properties:

| **Property:** | **Function:** |
|:---|:---|:---|
| __Look At__ || The object that the camera is looking at, if defined. This can be empty, but setting this may improve the quality of the blends to and from this camera. |
| __Blend Hint__ || Provides hints for blending positions to and from the CinemachineCamera. Values can be combined together. |
| | _Spherical Position_ | During a blend, camera will take a spherical path around the Tracking target. |
| | _Cylindrical Position_ | During a blend, camera will take a cylindrical path around the Tracking target (vertical co-ordinate is linearly interpolated). |
| | _Screen Space Aim When Targets Differ_ | During a blend, Tracking target position will interpolate in screen space instead of world space. |
| | _Inherit Position_ | When this CinemachineCamera goes live, force the initial position to be the same as the current position of the Unity Camera, if possible. |
| | _IgnoreTarget_ | Don't consider the Tracking Target when blending rotations, just to a spherical interpolation. |
