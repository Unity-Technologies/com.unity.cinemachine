# Samples and tutorials

Several sample scenes and video tutorials are available in addition to this documentation to demonstrate how to use the Cinemachine features in real-world scenarios.

## Sample scenes

The Cinemachine package includes sample scenes that you can [import to your project](samples-import.md) to discover many Cinemachine features in different contexts of use.

### 2D Samples

Once you import the 2D Samples set, the following scenes are available in the `Assets/Samples/Cinemachine/<version number>/2D Samples` folder.

| Sample scene | Use cases and key features |
| :--- | :--- |
| **2D Fighters** | <ul> <li>Set up a camera that dynamically composes the shot according to the remaining players in the game.</li> <li>Emulate players that move along predefined paths.</li> </ul> |
| **2D Platformer** | <ul> <li>Set up a custom camera group that switches cameras based on the player state.</li> <li>Set up a camera that frames a boss room entirely when the player enters the room.</li> <li>Confine the camera to prevent it from seeing outside the game map.</li> </ul> |
| **Camera Magnets** | <ul> <li>Set up "magnets" that attract the camera's attention according to the player's proximity.</li> <li>Confine the camera to prevent it from seeing outside the game map.</li> </ul> |

### 3D Samples

Once you import the 3D Samples set, the following scenes are available in the `Assets/Samples/Cinemachine/<version number>/3D Samples` folder.

| Sample scene | Use cases and key features |
| :--- | :--- |
| **Brain Update Modes** | <ul> <li>Prevent animated characters from jittering in the camera view.</li> <li>Understand the effect of the Cinemachine Brain Update Method according to the way you move the framed characters.</li> </ul> |
| **Clear Shot** | <ul> <li>Set up a group of fixed and moving cameras that target the same player from different perspectives.</li> <li>Automatically select the best shot based on occlusion (when the current camera loses sight of the player).</li> </ul> |
| **Cutscene** | <ul> <li>Set up a cutscene with camera blends and asset animations in Timeline.</li> <li>Trigger a cutscene that blends in from the game camera, plays through, and then blends back out to the game camera.</li> </ul> |
| **Early LookAt Custom Blend** | <ul> <li>Author a custom camera blending algorithm.</li> <li>Hook into Cinemachine's blend creation to override the default blend algorithm.</li> </ul> |
| **Fly around** | <ul> <li>Set up a first-person fly around camera with basic height and speed controls. |
| **FreeLook Deoccluder** | <ul> <li>Set up a free look camera that handles occlusion by walls to keep the player in view.</li> <li>Set up the scene and the camera to consider certain objects as transparent and ignore them in the occlusion evaluation.</li> </ul> |
| **FreeLook on Spherical Surface** | <ul> <li>Set up a free look camera that automatically re-orients to follow a player that can walk on any surface.</li> <li>Set up the camera to follow the character either lazily or actively.</li> </ul> |
| **Impulse Wave** | <ul> <li>Set up the the camera and objects in the scene to make them react to impulse waves.</li> <li>Invoke an impulse from a fixed epicenter.</li> <li>Trigger an impulse when the player jumps.</li> </ul> |
| **Lock-on Target** | <ul> <li>Set up a simple third-person free look camera to look at the player and rotate around it with the mouse.</li> <li>Set up a camera that looks at the player and locks on a boss character when the player enters a trigger zone.</li> </ul>  |
| **Mixing Camera** | <ul> <li>Set up a camera group that continuously blends multiple cameras as a function of the car speed.</li> <li>Set up a fixed camera that activates for a cut-in when the car enters a two-ramp stunt zone, to frame the car jump from the side.</li> </ul> |
| **Perspective To Ortho Custom Blend** | <ul> <li>Author a custom camera blending algorithm.</li> <li>Hook into Cinemachine's blend creation to override the default blend algorithm.</li>  <li>Create a smooth perspective-to-orthographic blend, which is not natively supported by Cinemachine.</li> </ul> |
| **Portals** | <ul> <li>Seamlessly teleport a player and its FreeLook camera.</li>  </ul> |
| **Running Race** | <ul> <li>Set up a clear shot group of cameras that each follow a different runner.</li> <li>Customize the clear shot quality assessment to always have the race leader in the center of the camera view.</li> <li>Set up an on-demand camera that frames all runners.</li> <li>Emulate players that move along predefined paths.</li> </ul>  |
| **Split Screen Car** | <ul> <li>Display two racing cars in a split screen configuration.</li> </ul> |
| **Third Person With Aim Mode** | <ul> <li>Set up a camera that follows the player as it moves, jumps, and sprints.</li> <li>Set up a special camera that moves and aims according to the player aiming controller, and use a dynamic crosshair.</li> <li>Add noise to a camera to emulate a handheld effect and ignore this effect to keep accuracy when the player aims and fires projectiles.</li> </ul> |
| **Third Person With Roadie Run** | <ul> <li>Set up a camera that follows the player as it moves, jumps, aims, and fires projectiles, and use a dynamic crosshair.</li> <li>Set up a camera recoil effect when the player fires projectiles.</li> <li>Set up a special roadie run camera that automatically activates when the player sprints, with increased noise and no crosshair.</li> </ul> |

### Input System Samples

Once you import the Input System Samples set, the following scene is available in the `Assets/Samples/Cinemachine/<version number>/Input System Samples` folder.

| Sample scene | Use cases and key features |
| :--- | :--- |
| **Split Screen Multiplayer** | <ul> <li>Author a custom Cinemachine input handler that interacts with the Player Input component of the Input System package.</li> <li>Dynamically add multiple players in split screen, each with their own free look camera system.</li> </ul> |

### Simple Player Controller

Several of the samples listed above make use of Cinemachine's [Simple Player Controller](SimplePlayerController.md), a basic but versatile player controller that you can use in your own projects. It is a suite of scripts that you can combine and configure to create character controllers which you can use in different contexts to implement several types of character movement.

## Tutorials

> [!NOTE]
> This section links to videos that were made with a previous version of Cinemachine. You might notice a few changes in the interface and the naming of some elements, but all the explained concepts and shown functionality still apply to the latest Cinemachine version.

[A "Using Cinemachine" series of video tutorials](https://www.youtube.com/playlist?list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) is available online in Unity's official YouTube channel. Discover various Cinemachine use cases and watch the immediate effects resulting from the corresponding project setup.

| Video | Description |
| :--- | :--- |
| [Getting Started](https://www.youtube.com/watch?v=x6Q5sKXjZOM&list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) | Keep a camera focused on a Transform and follow it as it moves around in the Scene. |
| [Track & Dolly](https://www.youtube.com/watch?v=q1fkx94vHtg&list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) | Track targets by setting paths for the Cinemachine Cameras to move between. |
| [State Driven Cameras](https://www.youtube.com/watch?v=2X00qXErxIM&list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) | Link Cinemachine Cameras to animation states and further customize their behavior in the Scene. |
| [Free Look](https://www.youtube.com/watch?v=X33t13gOBFw&list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) | Create an orbital camera system with input from the player and maintain control of the camera composition at different orbit stages. |
| [Clear Shot](https://www.youtube.com/watch?v=I9w-agFYZ3I&list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) | Dynamically cut between Cinemachine Cameras in the Scene as a tracked target is occluded. |
| [Post Processing](https://www.youtube.com/watch?v=jFqOEvrVZeE&list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) | With the addition of the post-processing stack, Cinemachine allows to blend easily between different effects on the cameras or in the Scene. |
| [Cinemachine 2D](https://www.youtube.com/watch?v=mWqX8GxeCBk&list=PLX2vGYjWbI0TQpl4JdfEDNO1xK_I34y8P) | Use Cinemachine composition tools with an orthographic virtual camera in a 2D Project. |
