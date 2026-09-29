# Cinemachine Camera Offset

To shift the shot without changing the behaviors that position and aim the camera, add the Cinemachine Camera Offset [extension](concept-procedural-motion.md#extensions).

The extension applies a final positional offset to the camera, in camera space.

You can shift the camera before or after noise takes effect.

## Properties

The Cinemachine Camera Offset component contains the following properties.

| **Property** | **Description** |
|:---|:---|
| **Offset** | Sets the amount to offset the camera's position, in camera space. The default is (0, 0, 0). |
| **Apply After** | Sets the stage of the Cinemachine pipeline after which Cinemachine applies the offset. The options are: <ul> <li>**Body**: Applies the offset after the position control behavior positions the camera.</li> <li>**Aim**: Applies the offset after the rotation control behavior rotates the camera, but before Cinemachine applies noise. This is the default.</li> <li>**Noise**: Applies the offset after Cinemachine applies noise.</li> <li>**Finalize**: Applies the offset after all standard Cinemachine Camera processing is complete.</li> </ul> |
| **Preserve Composition** | Re-adjusts the aim after applying the offset, to preserve the screen position of the Look At target as much as possible. This property has an effect only when **Apply After** is set to **Aim**, **Noise**, or **Finalize**, and the Cinemachine Camera has a [Look At target](CinemachineCamera.md). By default, the **Tracking Target** serves as the Look At target. |

If you use one of the behaviors that runs the Aim stage before the Body stage, for example [Cinemachine Position Composer](CinemachinePositionComposer.md), the following applies:

- If you set **Apply After** to **Aim**, Cinemachine applies the offset before it positions the camera.
- If you set **Apply After** to **Body**, Cinemachine applies the offset after it positions and rotates the camera.
