# Simple Player Controller

Cinemachine's Simple Player Controller is a suite of scripts that can be combined and configured to create character controllers which can be used in different contexts to implement several types of character movement. All of these scripts are provided as educational sample code, and the expectation is that you use them as a starting point, modifying and customizing them to suit your needs.

The Simple Player Controller can work with Unity's [Character Controller](https://docs.unity3d.com/ScriptReference/CharacterController.html) behaviour or without it, according to your requirements.  When the Character controller is present, character movement and grounded state are delegated to the Character Controller.  Otherwise, the Simple Player Controller manages its own position and does a raycast to locate the ground.

The Simple Player Controller provides Idle, Walk, Sprint, and Jump functionality.

The player's forward can be locked to the camera's forward (strafe mode), or it can be independent (free look mode); it can also be a combination of the two (locked when moving).  Gravity's direction is locked to the world's up, or it can follow the player's local up, enabling the player to walk on walls and ceilings.

## User Input

User input can be in one of three modes:

 - **Camera Space:** forward motion is in the camera's forward direction, 
 - **World Space:** forward motion is in the world's forward direction, 
 - **Player Space:** forward motion is in the player's forward direction.  

User input is configured using the same mechanism that is used for CinemachineCameras: InputAxis members and [CinemachineInputAxisController](CinemachineInputAxisController.md) behaviours.  This is done to ensure that the controller remains agnostic about the input implementation.  It works for Unity's Input package as well as Unity's legacy input manager, and can be adapted for third-party input managers.  This is the only dependency on Cinemachine.  Apart from that, the Simple Character Controller is a standalone solution.

## Strafe Mode

The controller can be in strafe mode or not, and the state of this mode can be changed dynamically.  In strafe mode, the player does not turn to face the direction of motion, but instead can move sideways or backwards.  Otherwise, the player turns to face its direction of motion.

## Support for ThirdPersonFollow

By default, SimplePlayerController does not have knowledge or control of the camera: that is left to Cinemachine.  However, Cinemachine's [ThirdPersonFollow](CinemachineThirdPersonFollow.md) component delegates control of the camera's viewing angle to the object being followed.  To support this case, you can add a Player Aiming Core child GameObject to the player, and give it a SimplePlayerAimController behaviour which works in conjunction with the SImplePlayerController and controls the camera's viewing angle and aim direction.


## Architecture

Apart from user input, the controller has no dependency on Cinemachine.  It doesn't care how the cameras are implemented.  It only needs to know from which camera to extract the input frame (i.e. to know what "forward" means).  For that, it just uses `Camera.main` (or an override `Camera` object that you may provide).

The Simple Player Controller is a suite of behaviours, each responsible for a specific element of the character's movement.  You can mix and match these behaviours to create the character controller that you need.  The behaviours are detailed below.

### SimplePlayerControllerBase

This is the base class for SimplePlayerController and SimplePlayerController2D.  You can also use it as a base class for your custom controllers.  It provides the following services and settings:

**Services:**
 - 2D motion axes (MoveX and MoveZ)
 - Jump button
 - Sprint button
 - API for strafe mode

**Actions:**
 - PreUpdate - invoked at the beginning of `Update()`
 - PostUpdate - invoked at the end of `Update()`
 - StartJump - invoked when the player starts jumping
 - EndJump - invoked when the player stops jumping

**Events:**
 - Landed - invoked when the player lands on the ground

**Settings:**

| Setting | Description |
| :--- | :--- |
| **Speed** | Ground speed when walking. |
| **Sprint Speed** | Ground speed when sprinting. |
| **Jump Speed** | Initial vertical speed when jumping.  Gravity will gradually reduce this speed until it becomes negative and the player starts falling.|
| **Sprint Jump Speed** | Same as Jump Speed, but the value may be different, to implement a stronger jump. |

### SimplePlayerController

Building on top of SimplePlayerControllerBase, this is the 3D character controller.  It pushes the character around and makes it jump, but does not manage any animation.  For that, add a SimplePlayerAnimator component (or a custom variant of it).

SimplePlayerController provides the following services and settings:

- Damping (applied to the player's velocity, and to the player's rotation)
- Strafe Mode
- Gravity
- Input Frames (which reference frame is used fo interpreting input: Camera, World, or Player)
- Ground Detection (using raycasts, or delegating to Character Controller)
- Camera Override (camera is used only for determining the input frame)

This behaviour should be attached to the player GameObject's root.  It moves the GameObject's transform.  If the GameObject also has a Unity Character Controller component, the Simple Player Controller delegates grounded state and movement to it.  If the GameObject does not have a Character Controller, the Simple Player Controller manages its own movement and does raycasts to test for grounded state.

Simple Player Controller does its best to interpret User input in the context of the selected reference frame.  Generally, this works well, but in Camera mode, the player may potentially transition from being upright relative to the camera to being inverted.  When this happens, there can be a discontinuity in the interpretation of the input.  The Simple Player Controller has an ad-hoc technique of resolving this discontinuity (you can see this in the code), but it is only used in this very specific situation.

**Additional Settings:**

| Setting | | Description |
| :--- | :--- | :--- |
| **Damping** |  | Transition duration (in seconds) when the player changes velocity or rotation. |
| **Strafe** |  | Makes the player strafe when moving sideways, otherwise it turns to face the direction of motion. |
| **Input Forward** |  | Reference frame for the input controls. |
| | _Camera_ | Input forward is camera forward direction. |
| | _Player_ | Input forward is Player's forward direction. |
| | _World_ | Input forward is World forward direction. |
| **Up Mode** |  | Up direction for computing motion. |
| | _Player_ | Move in the Player's local XZ plane. |
| | _World_ | Move in global XZ plane. |
| **Camera Override** | | If non-null, take the input frame from this camera instead of Camera.main. Useful for split-screen games. |
| **Ground Layers** |  | Layers to include in ground detection via Raycasts. |
| **Gravity** |  | Force of gravity in the down direction (m/s^2). |

### SimplePlayerController2D

This is a very basic 2D implementation of SimplePlayerControllerBase.  It requires a [Rigidbody2D](https://docs.unity3d.com/ScriptReference/Rigidbody2D.html) component to be placed on the player GameObject.  Because it works with a Rigidbody2D, motion control is implemented in the `FixedUpdate()` method.  Ground detection only works if the player has a small trigger collider under its feet.

**Additional Settings:**

| Setting | Description |
| :--- | :--- |
| **Player Geometry** | Reference to the child object that holds the player's visible geometry.  It is rotated to face the direction of motion |
| **Motion Control While In Air** | Makes possible to influence the direction of motion while the character is in the air.  Otherwise, the more realistic rule that the feet must be touching the ground applies |

### SimplePlayerAnimator

This is a behaviour whose job it is to drive animation based on the player's motion.  It is a sample implementation that you can modify or replace with your own.  As shipped, it is hardcoded to work specifically with the sample `CameronSimpleController` Animation controller, which is set up with states that the SimplePlayerAnimator knows about.  You can modify the SimplePlayerAnimator to work with your own animation controller.

SimplePlayerAnimator works with or without a SimplePlayerControllerBase alongside.  Without one, it monitors the transform's position and drives the animation accordingly.  You can see it used like this in some of the sample scenes, such as RunningRace or ClearShot.  In this mode, is it unable to detect the player's grounded state, so it always assumes that the player is grounded.

When a SimplePlayerControllerBase is detected, the SimplePlayerAnimator installs callbacks and expects to be driven by the SimplePlayerControllerBase using the STartJump, EndJump, and PostUpdate callbacks.

The animation clip speeds can be controlled using the following settings.  Out of the box, they are tuned to work with the provided sample animations, in such a way as to ensure that the feet don't slide on the ground when the player is moving.  Remember: these tunings are specifically for the provided sample animations.  If you replace them, you should re-tune the values with appropriate settings.

**Tuning Settings:**

| Setting | Description |
| :--- | :--- |
| **Normal Walk Speed** | Tune this to the animation in the model: feet should not slide when walking at this speed. |
| **Normal Sprint Speed** | Tune this to the animation in the model: feet should not slide when sprinting at this speed. |
| **Max Sprint Scale** | Never speed up the sprint animation more than this, to avoid absurdly fast movement. |
| **Jump Animation Scale** | Scale factor for the overall speed of the jump animation. |

### SimplePlayerAimController

This is a behaviour that works in conjunction with the SimplePlayerController to control the rotation of an invisible child object of the player.  It is intended to be used with Cinemachine's ThirdPersonFollow component, and the child object to be used as the CinemachineCamera's Tracking Target.  When used this way, the SimplePlayerAimController controls the camera's viewing angle based on the user's input.

This component expects to be in a child object (the _Aiming Core_) of a player that has a SimplePlayerController behaviour.  It works intimately with that component.  The purpose of the Aiming Core is to decouple the camera rotation from the player rotation.  Camera rotation is determined by the rotation of the Aiming Core GameObject, and this behaviour provides input axes for controlling it.  

When the Aiming Core is used as the target for a CinemachineCamera with a ThirdPersonFollow component, the camera looks along the core's forward axis, and pivots around the core's origin.  The Aiming Core is also used to define the origin and direction of player shooting, if player has that ability.  To implement player shooting, add a SimplePlayerShoot behaviour to the Aiming Core GameObject.  

The ThirdPersonWithAimMode sample scene shows an example of how to set this up.

**Settings:**

| Setting | | Description |
| :--- | :--- | :--- |
| **Player Rotation** |  | How the player's rotation is coupled to the camera's rotation. |
| | _Coupled_ | The player rotates with the camera.  Sideways movement results in strafing. |
| | _Coupled When Moving_ | The camera can rotate freely around the player when the player is stationary, but the player rotates to face camera forward when it starts moving. |
| | _Decoupled_ | The player's rotation is independent from the camera's rotation. |
| **Rotation Damping** |  | How fast the player rotates to face the camera direction when the player starts moving.  Only used when Player Rotation is Coupled When Moving. |
| **Horizontal Look** | | Horizontal Rotation input axis.  Value is in degrees, with 0 being centered. |
| **Vertical Look** |  | Vertical Rotation input axis.  Value is in degrees, with 0 being centered. |

### SimplePlayerShoot

This component manages player shooting.  It is expected to be on the player object, or on a child SimplePlayerAimController object of the player.

If an AimTargetManager is specified, then the behaviour aims at that target.  Otherwise, the behaviour aims in the forward direction of the player object, or of the SimplePlayerAimController object if it exists and is not decoupled from the player rotation.

**Settings:**

| Setting | Description |
| :--- | :--- |
| **Bullet Prefab** | The bullet prefab to instantiate when firing. |
| **Max Bullets Per Sec** | Maximum bullets to fire per second. |
| **Fire** | Boolean Input Axis for firing.  Value is 0 (not firing) or 1 (firing). |
| **AimTarget Manager** | Target to Aim towards. If null, the aim is defined by the forward vector of this GameObject. |
| **Fire Event** | Event that's triggered when firing. |


### SimplePlayerOnSurface

This behaviour keeps a player upright on surfaces.  It can be used to make the player walk on walls and ceilings or on the surfaces of arbitrary meshes.  It rotates the player so that its Up direction matches the surface normal where it is standing.  This script assumes that the pivot point of the player is at the bottom.

The _FreeLook on Spherical Surface_ sample scene shows an example of use of this behaviour.

Raycasts are used to detect walkable surfaces.

When using this component, SimpleSplayerController's Up Mode should be set to _Player_, and it should not have a Character Controller component, as that does not play nicely with nonstandard Up directions.

Also, when CinemachineCameras are being used to track the character, the [CinemachineBrain](CinemachineBrain.md)'s World Up Override setting should be set to the Player, so that the Camera's Up matches the Player's Up.

**Settings:**

| Setting | Description |
| :--- | :--- |
| **Rotation Damping** | How fast the player rotates to match the surface normal. |
| **Ground Layers** | Layers to consider as ground. |
| **Max Raycast Distance** | How far to raycast when checking for ground. |
| **Player Height** | The approximate height of the player.  Used to compute where raycasts begin. |
| **Free Fall Recovery** | Makes the player fall towards the nearest surface when in free fall. |
| **Surface Changed** | This event is fired when the player moves from one surface to another.  If the surfaces are moving, then this is a good opportunity to reparent the player. |

