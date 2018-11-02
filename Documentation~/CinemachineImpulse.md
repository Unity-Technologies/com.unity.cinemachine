# Impulse

Cinemachine Impulse generates and manages camera shake in response to game events. For example, you can use Impulse to make a Cinemachine Virtual Camera shake when one GameObject collides with another, or when something in your Scene explodes.

Impulse has three parts: 

- **[Raw vibration signal](CinemachineImpulseRawSignal):** a vibration curve in up to 6 dimensions: X, Y, Z, pitch, roll, yaw. 
- **[Impulse Source](CinemachineImpulseSourceOverview):** a component that emits the raw vibration signal from a point in Scene space, and defines signal characteristics such as duration, intensity, and range.
- **[Impulse Listener](CinemachineImpulseListener):** a Cinemachine extension that allows a Virtual Camera to “hear” an impulse, and react to it by shaking.

It’s useful to think about this in terms of individual “impulses.” An impulse is a single raw vibration signal emitted from an Impulse Source. Collisions and events in your Scenes _trigger_ Impulse Sources, Impulse Sources _generate_ impulses, and Impulse Listeners _react_ to impulses.

## Getting started with Impulse

To set up and use Impulse in a Scene, do the following: 

- Add **Cinemachine Impulse Source** or **Cinemachine Collision Impulse Source** components to one or more GameObjects that you want to trigger camera shake.
- Connect Raw Signals to the Impulse Sources. These can be **6D Noise Profile**s, **3D Fixed Signals**, or custom signal types that you create yourself.
- Add a **Cinemachine Impulse Listener** extension to one or more Cinemachine virtual cameras so they can detect and react to impulses.