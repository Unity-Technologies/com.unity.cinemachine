# 3rd person follow

Use Cinemachine Virtual Camera’s 3rd Person Follow to keep the camera at a constant position and distance relative to the Follow target (subject to damping controls), tracking the target’s movement and rotation.

The virtual camera is bolted to a virtual rig that is attached to the target. The red rods and dots visible around the target in the scene view reflect the rig setup as well as the camera position and distance relative to the target.

The rig is defined by three points: the **Shoulder Offset**, the **Vertical Arm Length**, and the **Camera Distance**.

Thus, the camera position is completely controlled by the Follow target’s position and orientation, and its orientation will always match that of the Follow target.



For example, a rig set up with these values:

![](images/CinemachineRigInspectorValuesExample.png)



Produces this rig in the Scene view:

![](images/CinemachineRigSceneView.png)

A virtual rig in the Scene view, using the values in the Inspector screenshot above.

A: Follow target origin point

B: Shoulder Offset point

C: Hand offset point

- The rig pivots horizontally around the Follow target’s origin point (A), and vertically around the **Shoulder Offset** point (B).
- An arm extends vertically from the **Shoulder Offset** point, at the end of which is another point, called the hand offset point (C).
- The **Camera Distance** point defines how far from the hand offset point to position the camera, in the direction of the Follow target’s local back vector. The camera is oriented to match the Follow target’s forward vector. The camera always looks directly at the hand.

Note: C rotates around B. B rotates around A.



Which results in this Game view:



![](images/CinemachineRigGameViewExample.png)

Note: In this example, the camera is 2 meters behind the hand.

This rig can be used effectively, with a suitable shoulder offset, to produce a “third-person” camera, where the character is offset in the frame and the camera looks over the character’s shoulder. With different settings, it can be used for a 1st-person style camera.



## Controlling the Camera

There is no direct input control for the camera. You must have a controller script that moves and rotates the Follow target; the camera will position and orient itself relative to that. When the Follow target is the character itself, the camera’s rotation will always match the character’s rotation. When the Follow target is an invisible GameObject that can rotate independently of the character, the camera will then orbit the character.

## Built-in Collision Resolution

The built in collision resolution means the camera always keeps the target in sight, despite intervening obstacles. When the target moves too close to an obstacle, the rig will bend and stretch to keep the camera outside the obstacle but always with the target in view.

## Shaky Movement, Steady Aim

When combined with the Cinemachine3rdPersonAim (LINK) extension, the result is a powerful rig that can maintain steady aim for a shooter-type game, even when the camera movement is shaky or noisy. Cinemachine3rdPersonAim will re-adjust the camera orientation to maintain a fixed point at the center of the screen, correcting for variations due to hand-held camera noise or shaking target motion.

## Properties:

|**Property:**|**Function:**|
|:---|:---|
| Damping                 | The responsiveness of the camera in tracking the target. Each axis can have its own setting. The value is the approximate time it takes the camera to catch up to the target's new position. Small numbers make the camera more responsive. Larger numbers make the camera respond more slowly. |
| Shoulder Offset         | Position of the shoulder pivot relative to the follow target origin. This offset is in target-local space. |
| Vertical Arm Length     | Vertical offset of the hand in relation to the shoulder. Arm length will affect the follow target's screen position when the camera rotates vertically. |
| Camera Side             | Specifies the camera position along the shoulder (left, right, or somewhere in-between). |
| Camera Distance         | The distance behind the hand the camera is placed.           |
| Camera Collision Filter | Specifies which layers will be affected or excluded from collision resolution. |
| Ignore Tag              | Obstacles with this tag will be ignored by collision resolution. It is recommended to set this field to the target's tag. |
| Camera Radius           | Specifies how close the camera can get to collidable obstacles without adjusting its position. |
| Collision Damping       |How gradually the camera returns to its normal position after having been corrected by the built-in collision resolution system. Higher numbers will move the camera more gradually back to normal.|
