# Cinemachine Impulse

Cinemachine Impulse generates and manages camera shake in response to game events. For example, you can use Impulse to make a CinemachineCamera shake when one GameObject collides with another, or when something in your Scene explodes.

Impulse has two parts: 

**1. [Impulse Source](CinemachineImpulseSourceOverview.md)**: a component that emits a signal that originates at a point in space and propagates outwards, much like a sound wave or a shock wave. This emission is triggered by events in the game. 

The signal consists of a direction, and a curve specifying the strength of the signal as a function of time. Together, these effectively define a shake along a specified axis, lasting a specified amount of time. This shake travels outward from the point of origin, and when it reaches the location of an Impulse Listener, that listener can respond to it.

**2. [Impulse Listener](CinemachineImpulseListener.md)**: a Cinemachine extension that allows a CinemachineCamera to “hear” an impulse, and react to it by shaking.

It’s useful to think about this in terms of individual “impulses.” An impulse is a single occurrence of an Impulse Source emitting a signal. Collisions and events in your Scenes _trigger_ impulses, Impulse Sources _generate_ impulses, and Impulse Listeners _react_ to impulses.

## Getting started with Impulse

To set up and use Impulse in a Scene, do the following: 

- Add **[Cinemachine Impulse Source](CinemachineImpulseSource.md)** or **[Cinemachine Collision Impulse Source](CinemachineCollisionImpulseSource.md)** components to one or more GameObjects that you want to trigger a camera shake.

- Add a **[Cinemachine Impulse Listener](CinemachineImpulseListener.md)** extension to one or more Cinemachine CinemachineCameras so they can detect and react to impulses.