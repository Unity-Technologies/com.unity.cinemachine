# Cinemachine Camera activations and transitions

One Cinemachine Camera has control of the Unity Camera at any point in time, this is the **live** Cinemachine Camera. The only exception is when a [blend](#blends) occurs between two Cinemachine Cameras.

## Live Cinemachine Camera selection

The conditions that make a Cinemachine Camera the live one are different depending on the context you're using Cinemachine in: either for dynamic event response in real time or for shot sequencing in Timeline.

### Via dynamic events in real time

By default, the Cinemachine Brain handles the live Cinemachine Camera selection.

To be or become live, a Cinemachine Camera must meet the following rules:
* Its GameObject must be the most recently [activated GameObject](https://docs.unity3d.com/Manual/DeactivatingGameObjects.html) that includes a Cinemachine Camera component.
* Its Priority, a property you can optionally set in the Cinemachine Camera component, must be the highest among activated Cinemachine Cameras.

You can respond to dynamic game events in real time by manipulating Cinemachine Camera priorities or by activating and deactivating their GameObjects. This is particularly useful for live gameplay, where action isn’t always predictable.

### Via Timeline

Use [Timeline](CinemachineTimeline.md) to choreograph cameras in predictable situations, like cutscenes.

In that context, Timeline overrides the Cinemachine Brain priority system. The live Cinemachine Camera selection is based on the activation of specific Cinemachine Camera clips that give you precise, to-the-frame camera control.

## Cinemachine Camera transitions

You can manage transitions between Cinemachine Cameras each time a new one becomes live.

The ways to set up Cinemachine Camera transitions are different depending on the context you're using Cinemachine in:
* By default, you handle transition rules in the [Cinemachine Brain component](CinemachineBrain.md).
* When you're using Timeline for shot sequencing, you handle transitions directly in the Timeline Cinemachine track.

### Blends

Blends allow you to create sophisticated camera motion by combining relatively simple shots and blending between them in response to real-time game events or in a choreographed way via a timeline.

A Cinemachine blend is not a fade, wipe, or dissolve. Rather, Cinemachine performs a smooth animation of the position, rotation, and other settings of the Unity Camera from one Cinemachine Camera to the next, taking care to preserve the view of the target object, and to respect the Up direction.

![](images/concept-transition-blend.png)  
_**Blend:** The two Cinemachine Cameras simultaneously control the Unity Camera during the blend, smoothly exchanging full control over a predetermined time._

### Cuts

By definition, a cut is an abrupt transition from a shot to another. In Cinemachine, a cut between two Cinemachine Cameras corresponds to a blend that occurs instantly, without smooth transition between Cinemachine Camera properties.

![](images/concept-transition-cut.png)  
_**Cut example:** two Cinemachine Cameras taking turns controlling the Unity Camera instantly._
