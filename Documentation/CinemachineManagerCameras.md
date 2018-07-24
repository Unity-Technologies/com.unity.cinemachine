# Managing and grouping Virtual Cameras

A __manager__ camera oversees many Virtual Cameras but acts as a single Virtual Camera from the point of view of Cinemachine Brain and Timeline.

Cinemachine includes these manager cameras:

* [Free Look Camera](CinemachineFreeLook): an enhanced [Orbital Transposer](CinemachineBodyOrbitalTransposer). It manages three horizontal orbits, arranged vertically to surround an avatar.

* [Mixing Camera](CinemachineMixingCamera): uses the weighted average of up to eight child Virtual Cameras.

* [Blend List Camera](CinemachineBlendListCamera): executes a sequence of blends or cuts of its child Virtual Cameras.

* [Clear Shot Camera](CinemachineClearShot): picks the child Virtual Camera with the best view of the target.

* [State-Driven Camera](CinemachineStateDrivenCamera): picks a child Virtual Camera in reaction to changes in animation state.

Because manager cameras act like normal Virtual Cameras, you can nest them. In other words, create arbitrarily complex camera rigs that combine regular Virtual Cameras and manager cameras.

