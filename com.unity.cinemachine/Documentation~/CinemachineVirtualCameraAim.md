# Rotation Control properties

Use the Rotation Control properties to specify how to rotate the CmCamera. To change the camera’s position, use the [Position Control properties](CinemachineVirtualCameraBody.md).

![Aim properties, with the Composer algorithm (red)](images/CinemachineAim.png)

- [__None__](CinemachineAimDoNothing.md): Do not procedurally rotate the CmCamera.  The rotation can be controlled by object parenting, or by custom script, or by animation.
- [__Rotation Composer__](CinemachineAimComposer.md): Keep the __Look At__ target in the camera frame, with composition controls and damping.
- [__Hard Look At__](CinemachineAimHardLook.md): Keep the __Look At__ target in the center of the camera frame.
- [__Pan Tilt__](CinemachineAimPOV.md): Rotate the CmCamera, optionally based on the user’s input.
- [__Same As Follow Target__](CinemachineAimSameAsFollow.md): Set the camera’s rotation to the rotation of the __Tracking Target__.