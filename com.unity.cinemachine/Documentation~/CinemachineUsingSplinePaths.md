# Using Spline paths

A Spline path is a path formed by a Spline in a scene. Use a Spline to specify a fixed course to position or animate a CinemachineCamera. Use the [Spline Dolly](CinemachineSplineDolly.md) behaviour to make your CinemachineCamera follow a Spline path.

![Editing a dolly path in the Scene view](images/CinemachinePathScene.png)

To create a Cinemachine Camera with a dolly path:

1. In the Unity menu, choose __GameObject > Cinemachine > Dolly Camera with Spline__.
A new Cinemachine Camera and spline appear in the [Hierarchy]([https://docs.unity3d.com/Manual/Hierarchy.html](https://docs.unity3d.com/Manual/Hierarchy.html)). 

2. In the [Hierarchy]([https://docs.unity3d.com/Manual/Hierarchy.html](https://docs.unity3d.com/Manual/Hierarchy.html)) window, select the new dolly spline GameObject.

3. In the [Inspector]([https://docs.unity3d.com/Manual/UsingTheInspector.html](https://docs.unity3d.com/Manual/UsingTheInspector.html)) or in the Scene View, add and adjust waypoints.

Any Unity spline can be used as a path in Cinemachine.  Just drag it into the [Spline Dolly](CinemachineSplineDolly.md) Spline property field, and immediately the CinemachineCamera will start following the spline. 


