# Using Cinemachine

Using Cinemachine requires a new way of thinking about working with cameras. For example, you might have invested heavily in carefully scripted camera behaviors. However, Cinemachine can give the same results, if not better, in less time.


## Cinemachine Cameras

Cinemachine does not create new cameras. Instead, it directs a single Unity camera for multiple shots. You compose these shots with __CmCameras__ (also referred to sometimes as _Virtual Cameras_).  CinemachineCameras move and rotate the Unity camera and control its settings.

The CinemachineCameras are separate GameObjects from the Unity Camera, and behave independently. They are not nested within each other. For example, a Scene might look like this:

![A Scene containing a Unity camera with Cinemachine Brain (blue) and multiple CinemachineCameras (red)](images/CinemachineSceneHierarchy.png)

The main tasks that the CinemachineCamera does for you:

* Positions the Unity camera in the Scene.
* Aims the Unity camera at something.
* Adds procedural noise to the Unity camera. Noise simulates things like hand-held effects or vehicle shakes.

Cinemachine encourages you to create many CinemachineCameras. The CinemachineCamera is designed to consume little processing power. If your Scene is performance-sensitive, deactivate all but the essential CinemachineCameras at any given moment for best performance.

It is recommended that you use a single CinemachineCamera for a single shot. Take advantage of this to create dramatic or subtle cuts or blends. Examples:

* For a cutscene where two characters exchange dialog, use three CinemachineCameras: one camera for a mid-shot of both characters, and separate CinemachineCameras for a close-up of each character. Use Timeline to synchronize audio with the CinemachineCameras.

* Duplicate an existing CinemachineCamera so that both CinemachineCameras are in the same position in the Scene. For the second CinemachineCamera, change the FOV or composition. When a player enters a trigger volume, Cinemachine blends from the first to the second CinemachineCamera to emphasize a change in action.

One CinemachineCamera has control of the Unity camera at any point in time. This is the __live__ CinemachineCamera. The exception to this rule is when a blend occurs from one CinemachineCamera to the next. During the blend, both CinemachineCameras are live.

## Cinemachine Brain

The Cinemachine Brain is a component in the Unity Camera itself. The Cinemachine Brain monitors all active CinemachineCameras in the Scene. To specify the next live CinemachineCamera, you [activate or deactivate](https://docs.unity3d.com/Manual/DeactivatingGameObjects.html) the desired CinemachineCamera's game object. Cinemachine Brain then chooses the most recently activated CinemachineCamera with the same or higher priority as the live CinemachineCamera. It performs a cut or blend between the previous and new CinemachineCameras.

**Tip**: Use Cinemachine Brain to respond to dynamic game events in real time. It allows your game logic to control the camera by manipulating priorities. This is particularly useful for live gameplay, where action isn’t always predictable. Use [Timeline](CinemachineTimeline.md) to choreograph cameras in predictable situations, like cutscenes. Timeline overrides the Cinemachine Brain priority system to give you precise, to-the-frame camera control.

## Positioning and Aiming

Use the [__Position Control__ properties](CinemachineVirtualCameraBody.md) in a CinemachineCamera to specify how to move it in the Scene. Use the [__Rotation Control__ properties](CinemachineVirtualCameraAim.md) to specify how to aim it.

By default, a CinemachineCamera has a single Tracking Target, which is used for two purposes:

* It specifies a GameObject for the CinemachineCamera to move with (position control).
* It specifies the LookAt target, that is the GameObject to aim at (rotation control).

If you want to use two different GameObjects for these purposes, that is done by enabling the Separate LookAt Target option in the CinemachineCamera's inspector: 

![Enabling Separate LookAt target](images/SeparateLookAtTarget.png)
![Enabling Separate LookAt target](images/SeparateLookAtTarget2.png)

Cinemachine includes a variety of procedural algorithms to control positioning and aiming. Each algorithm solves a specific problem, and exposes properties to customize the algorithm for your specific needs. Cinemachine implements these algorithms as `CinemachineComponent` objects. Use the `CinemachineComponentBase` class to implement a custom moving or aiming behavior.

The __Position Control__ properties offer the following procedural algorithms for moving the CinemachineCamera in a Scene:

* __Tracking__: Move in a fixed relationship to the __Tracking__ target, with optional damping.
* __Position Composer__: Move in a fixed screen-space relationship to the __Tracking__ target, with optional damping.
* __Orbital Follow__: Move in a variable relationship to the __Tracking__ target, optionally controlled by player input.
* __Spline Dolly__: Move along a predefined Spline path.
* __Hard Lock to Target__: Use the same position and as the __Tracking__ target.
* __Third Person Follow__: Place the camera on a configurable rigid rig attached to the __Tracking__ target.  The rig rotates with the target.  This is useful for TPS and POV cameras.
* __Do Nothing__: Do not procedurally move the CinemachineCamera.  Position is controlled directly by the CinemachineCamera's transform, which can be controlled by a custom script.

The __Rotation Control__ properties offer the following procedural algorithms for rotating a CinemachineCamera to face the __Look At__ target:

* __Rotation Composer__: Keep the __Look At__ target in the camera frame, with compositional constraints.
* __Pan Tilt__: Rotate the CinemachineCamera based on the user’s input.
* __Same As Follow Target__: Set the camera’s rotation to the rotation of the __Tracking__ target.
* __Hard Look At__: Keep the __Look At__ target in the center of the camera frame.
* __Do Nothing__: Do not procedurally rotate the CinemachineCamera.  Rotation is controlled directly by the CinemachineCamera's transform, which can be controlled by a custom script.


## Composing a shot

The [__Position Composer__](CinemachinePositionComposer.md) and [__Rotation Composer__](CinemachineRotationComposer.md) algorithms define areas in the camera frame for you to compose a shot:

* __Dead zone__: The area of the frame in which Cinemachine keeps the target. The target can move within this region and the CinemachineCamera will not adjust to reframe it until the target leaves the dead zone.

* __Soft zone__: If the target enters this region of the frame, the camera will adjust to put it back in the dead zone. It will do this slowly or quickly, according to the time specified in the Damping settings.

* __Screen Position__: The screen position of the center of the dead zone.  0 is the center of the screen, +1 and -1 are the edges.

* __Damping__: Simulates the lag that a real camera operator introduces while operating a heavy physical camera. Damping specifies how quickly or slowly the camera reacts when the target enters the __soft zone__ while the camera tracks the target. Use small numbers to simulate a more responsive camera, rapidly moving or aiming the camera to keep the target in the __dead zone__. Larger numbers simulate heavier cameras, The larger the value, the more Cinemachine allows the target to traverse the soft zone.

The __Game View Guides__ gives an interactive, visual indication of these areas. The guides appear as tinted areas in the [Game view](https://docs.unity3d.com/Manual/GameView.html).

![Game Window Guides gives a visual indication of the damping, screen, soft zone, and dead zone](images/CinemachineGameWindowGuides.png)

The clear area indicates the __dead zone__. The blue-tinted area indicates the __soft zone__. The __Screen Position__ is the center of the __Dead Zone__. The red-tinted area indicates the __no pass__ area, which the camera prevents the target from entering. The yellow square indicates the target itself.

Adjust these areas to get a wide range of camera behaviors. To do this, drag their edges in the Game view or edit their properties in the Inspector window. For example, use a larger __soft zone__ for a fast-moving target, or enlarge __dead zone__ to create an area in the middle of the camera frame that is immune to target motion. Use this feature for things like animation cycles, where you don’t want the camera to track the target if it moves just a little.

## Using noise to simulate camera shake

Real-world physical cameras are often heavy and cumbersome. They are hand-held by the camera operator or mounted on unstable objects like moving vehicles. Use [Noise properties](CinemachineNoiseProfiles.md) to simulate these real-world qualities for cinematic effect. For example, you could add a camera shake when following a running character to immerse the player in the action.

At each frame update, Cinemachine adds noise separately from the movement of the camera to follow a target. Noise does not influence the camera’s position in future frames. This separation ensures that properties like __damping__ behave as expected.