# Managing and grouping CinemachineCameras

A __manager__ camera oversees many CinemachineCameras but acts as a single CinemachineCamera from the point of view of Cinemachine Brain and Timeline.

Cinemachine includes these manager cameras:

* [Mixing Camera](CinemachineMixingCamera.md): Uses the weighted average of up to eight child CinemachineCameras.

* [Sequencer Camera](CinemachineSequencerCamera.md): Executes a sequence of blends or cuts of its child CinemachineCameras.

* [Clear Shot Camera](CinemachineClearShot.md): Picks the child CinemachineCamera with the best view of the target.

* [State-Driven Camera](CinemachineStateDrivenCamera.md): Picks a child CinemachineCamera in reaction to changes in animation state.

Because manager cameras act like normal CinemachineCameras, you can nest them. In other words, create arbitrarily complex camera rigs that combine regular CinemachineCameras and manager cameras.

