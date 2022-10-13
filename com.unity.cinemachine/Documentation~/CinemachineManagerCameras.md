# Managing and grouping CmCameras

A __manager__ camera oversees many CmCameras but acts as a single CmCamera from the point of view of Cinemachine Brain and Timeline.

Cinemachine includes these manager cameras:

* [Mixing Camera](CinemachineMixingCamera.md): Uses the weighted average of up to eight child CmCameras.

* [Blend List Camera](CinemachineBlendListCamera.md): Executes a sequence of blends or cuts of its child CmCameras.

* [Clear Shot Camera](CinemachineClearShot.md): Picks the child CmCamera with the best view of the target.

* [State-Driven Camera](CinemachineStateDrivenCamera.md): Picks a child CmCamera in reaction to changes in animation state.

Because manager cameras act like normal CmCameras, you can nest them. In other words, create arbitrarily complex camera rigs that combine regular CmCameras and manager cameras.

