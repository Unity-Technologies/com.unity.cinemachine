# Extensions

Extensions are components that augment the behavior of a CinemachineCamera. For example, the [Deoccluder](CinemachineDeoccluder.md) extension moves a camera out of the way of GameObjects that obstruct the camera’s view of its target.

Cinemachine includes a variety of extensions. Create your own custom extensions by inheriting the `CinemachineExtension` class.

To add an extension to a CinemachineCamera:

1. Select your CinemachineCamera in the [Scene](https://docs.unity3d.com/Manual/UsingTheSceneView.html) view or [Hierarchy](https://docs.unity3d.com/Manual/Hierarchy.html) window.

2. In the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html), use the __Add Extension__ drop-down menu to choose the extension.  The chosen behaviour will be added to the CinemachineCamera GameObject.

