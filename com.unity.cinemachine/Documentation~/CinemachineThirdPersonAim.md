# Cinemachine Third Person Aim Extension

This extension is a complement to the [ThirdPersonFollow component](CinemachineThirdPersonFollow.md) in the Cinemachine Camera.  Its purpose is to detect the object that the camera is aiming at.

To accomplish this, the extension projects a ray from the camera's position along its forward axis, to detect the first object that intersects with that ray. The intersection point is then placed in the CinemachineCamera's `state.ReferenceLookAt`. That is the point that the camera will be considered to be looking at for algorithms that need to know it (for example, blending).

Additionally, if **Noise Cancellation** is enabled, you can use this extension to stabilize the target at the screen center, even when the camera has handheld noise enabled. Rotational noise is canceled out, but if the camera has positional noise (and a non-zero noise offset), that is preserved, and the aim corrected to maintain target stability on the screen.

> [!NOTE]
> The _ThirdPersonWithAimMode_ [sample scene](samples-tutorials.md) gives an example of use of this extension.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Aim Collision Filter__ | Objects on these layers will be detected. |
| __Ignore Tag__ | Objects with this tag are ignored. It's a good idea to set this field to the target's tag.  |
| __Aim Distance__ | How far to project the object detection ray.  |
| __Noise Cancellation__ | When this is enabled, the aim target will be stabilized on the screen when handheld noise is present on the camera.  This will only work if the Pivot Offset in the Noise component is nonzero.  |

## Parallax issues

Since there is generally an offset between the player firing origin (where the bullets come from) and the camera position, if the player were to fire a shot along its forward axis (which in the third person rig is always parallel to the camera's forward axis), then it would always miss the target by exactly that offset.  For most third-person scenarios, the appropriate thing to do is to just _ignore the discrepancy_ and pretend that the camera's target _is_ what the player is aiming at, and just fire towards that point.

In some situations, however, there might be an object that obstructs the player's view of the target but not the camera's.  In those cases, if the player were to fire it would hit that other object and not the camera's target.  Cinemachine checks for this condition and calculates the actual point that the player would hit if it were to fire. That point is available in the API (`CinemachineThirdPersonAim.AimTarget`).
