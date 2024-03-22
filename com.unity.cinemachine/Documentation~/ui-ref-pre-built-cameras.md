# Pre-built Cinemachine Cameras

The Cinemachine package includes a series of shortcuts for pre-built Cinemachine Cameras that target specific use cases.

To use a pre-built Cinemachine Cameras, from the Editor's menu, select **GameObject** > **Cinemachine**, and then select the camera type according to the following list:

| Menu item | Description |
| :--- | :--- |
| **State-Driven Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and handles them according to [animation state changes](CinemachineStateDrivenCamera.md). |
| **Targeted Cameras** > **Follow Camera** | Creates a Cinemachine Camera with preselected behaviors suitable for [following and framing a character](setup-follow-camera.md). |
| **Targeted Cameras** > **Target Group Camera** | Creates a Cinemachine Camera with preselected behaviors and extension suitable for [following and framing groups](GroupingTargets.md). Also creates an empty Target Group and assigns it to the Tracking Target field of the new Cinemachine camera. |
| **Targeted Cameras** > **FreeLook Camera** | Creates a Cinemachine Camera with preselected behaviors and extensions suitable for [character-centric free look scenarios with user input](FreeLookCameras.md). |
| **Targeted Cameras** > **Third Person Aim Camera** | Creates a Cinemachine Camera with preselected position behavior and aim extension suitable for [third-person aiming scenarios](ThirdPersonCameras.md).  The ThirdPerson Aim camera is a fixed rig that is driven by the tracking target's rotation and position. No direct user control is provided. For a camera that provides user control of the camera position, choose **FreeLook Camera**|
| **Targeted Cameras** > **2D Camera** | Creates a Cinemachine Camera with preselected [position composer behavior](CinemachinePositionComposer.md) suitable for 2D game scenarios. |
| **Cinemachine Camera** | Creates a default Cinemachine Camera without preselected behaviors.<br />Use it to [create a passive Cinemachine Camera](setup-cinemachine-environment.md) or to [build your own custom Cinemachine Camera](setup-procedural-behavior.md) from scratch.  The camera will be positioned and rotated to match the current scene view camera. |
| **Sequencer Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and handles them according to [a specified sequence](CinemachineSequencerCamera.md). |
| **Dolly Camera with Spline** | Creates a Cinemachine Camera with preselected behaviors to [make it move along a Spline](CinemachineUsingSplinePaths.md). Also creates a spline and assigns it to the camera. You can modify this spline or replace it with another one. |
| **Dolly Cart with Spline** | Creates an empty GameObject with its [Transform constrained to a Spline](CinemachineSplineCart.md).<br />Use it to animate any GameObject along a path, or as a tracking target for Cinemachine Camera. Also creates a spline and assigns it to the dolly cart. You can modify this spline or replace it with another one. |
| **Mixing Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and [and provides a continuous blend according to a specified weighting](CinemachineMixingCamera.md). |
| **ClearShot Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and chooses from them according to [shot quality criteria](CinemachineClearShot.md). |

> [!NOTE]
> If you right-click on a GameObject and create one of the targeted cameras, the "Tracking Target" of the new camera will automatically be populated with the object on which you right-clicked.
