# Pre-built Cinemachine Cameras

The Cinemachine package includes a series of shortcuts for pre-built Cinemachine Cameras that target specific use cases.

To use a pre-built Cinemachine Cameras, from the Editor's menu, select **GameObject** > **Cinemachine**, and then select the camera type according to the following list:

| Menu item | Description |
| :--- | :--- |
| **State-Driven Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and handles them according to [animation state changes](CinemachineStateDrivenCamera.md). |
| **Targeted Cameras** > **Follow Camera** | Creates a Cinemachine Camera with preselected behaviors suitable for [following and framing a character](setup-follow-camera.md). |
| **Targeted Cameras** > **Target Group Camera** | Creates a Cinemachine Camera with preselected behaviors and extension suitable for [following and framing groups](GroupingTargets.md). |
| **Targeted Cameras** > **FreeLook Camera** | Creates a Cinemachine Camera with preselected behaviors and extensions suitable for [character-centric free look scenarios with user input](FreeLookCameras.md). |
| **Targeted Cameras** > **Third Person Aim Camera** | Creates a Cinemachine Camera with preselected position behavior and aim extension suitable for [third-person aiming scenarios](ThirdPersonCameras.md). |
| **Targeted Cameras** > **2D Camera** | Creates a Cinemachine Camera with preselected [position composer behavior](CinemachinePositionComposer.md) suitable for 2D game scenarios. |
| **Cinemachine Camera** | Creates a default Cinemachine Camera without preselected behaviors.<br />Use it to [create a passive Cinemachine Camera](setup-cinemachine-environment.md) or to [build your own custom Cinemachine Camera](setup-procedural-behavior.md) from scratch. |
| **Sequencer Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and handles them according to [a specified sequence](CinemachineSequencerCamera.md). |
| **Dolly Camera with Spline** | Creates a Cinemachine Camera with preselected behaviors to [make it move along a Spline](CinemachineUsingSplinePaths.md). |
| **Dolly Cart with Spline** | Creates an empty GameObject with its [Transform constrained to a Spline](CinemachineSplineCart.md).<br />Use it to animate any GameObject along a path, or as a tracking target for Cinemachine Camera. |
| **Mixing Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and [blends their properties according to a specified weighing](CinemachineMixingCamera.md). |
| **ClearShot Camera** | Creates a Manager Camera that parents a group of Cinemachine Cameras and handles them according to [shot quality criteria](CinemachineClearShot.md). |
