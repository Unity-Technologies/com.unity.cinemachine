# Cinemachine and Timeline

For situations where choreographed cameras are desired, you can use [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest) to activate, deactivate, and blend between CinemachineCameras.

**Tip**: For simple shot sequences, you can also use a [Cinemachine Sequencer Camera](CinemachineSequencerCamera.md) instead of Timeline.

## Live Cinemachine Camera selection

Timeline overrides the priority-based decisions made by the [Cinemachine Brain](CinemachineBrain.md). When the timeline finishes, the control [returns to the Cinemachine Brain](concept-activations-transitions.md).

## Cinemachine Track and Shot Clips

You control Cinemachine Cameras in Timeline with **Cinemachine Shot Clips** in a **Cinemachine Track**. Each shot clip points to a CinemachineCamera to activate and then deactivate. Use a sequence of shot clips to specify the order and duration of each shot.

## Cinemachine Camera transitions
To cut between two CinemachineCameras, place the clips next to each other.

To blend between two CinemachineCameras, overlap the clips.

![](images/CinemachineTimelineShotClips.png)  
_Example: Cinemachine Shot Clips in Timeline, with a cut between Shots A-B and a blend between Shots B-C._

You can make the interrupting shot blend in or out by setting its Ease In and Ease Out Duration times to non-zero. 

![Shot Easing](images/ShotEasing.png)

## Multiple Cinemachine Tracks

You can have multiple Cinemachine Tracks in the same timeline. The tracks lower down in the timeline override any tracks higher up. By having a shot on a lower track become active while a higher-up CinemachineShot clip is active, you can interrupt the shot with another one.

## Additional resources

* [Set up Timeline with Cinemachine Cameras](setup-timeline.md)