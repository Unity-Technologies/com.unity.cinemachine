# Rotation Composer

This CmCamera __Rotation Control__ behaviour rotates the camera to face the __Look At__ target. It also applies offsets, damping, and composition rules. It only rotates the camera, it never changes the camera's position.  Examples of targets for aiming: the upper spine or head bone of a character, vehicles, or dummy objects which are controlled or animated programmatically.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| **Tracked Object Offset** | Offset from the center of the Look At target, in target-local space. Fine-tune the tracking target position when the desired area is not the tracked object’s center. You can also use [Scene Handles](handles.md) to modify this property. |
| __Lookahead Time__ | Adjust the offset based on the motion of the Look At target. The algorithm estimates the point that the target will be this many seconds into the future. This feature is sensitive to noisy animation. It can amplify the noise, resulting in undesirable camera jitter. If the camera jitters unacceptably when the target is in motion, turn down this property or animate the target more smoothly. |
| __Lookahead Smoothing__ | Controls the smoothness of the lookahead algorithm. Larger values smooth out jittery predictions and increase prediction lag. |
| __Lookahead Ignore Y__ | Enable ths to ignore movement along the Y axis for lookahead calculations. |
| __Damping__ | How responsively the camera frames the target in horizontal and vertical directions. Use small numbers for more responsive, rapid rotation of the camera to keep the target in the dead zone. Use larger numbers for a heavier, more slowly-responding camera.  |
| __Screen Position__ | Screen position for the center of the dead zone. The camera rotates so that the target appears here. 0 is screen center, -1 and 1 are screen edges. |
| __Dead Zone Size__ | The size of the screen region within which the camera ignores any movement of the target. If the target is positioned anywhere within this region, the camera doesn't update its rotation. This is useful for ignoring minor target movement.  |
| __Soft Zone Size__ | The size of the soft zone. If the target appears in this region of the screen, the camera will rotate to push it back to the dead zone, in the time specified by the Damping setting. |
| __Bias__ | Shifts the target position horizontally and vertically away from the center of the soft zone. |
| __Center On Activate__ | Rotates the camera to put the target at the center of the dead zone when the camera becomes live. |

