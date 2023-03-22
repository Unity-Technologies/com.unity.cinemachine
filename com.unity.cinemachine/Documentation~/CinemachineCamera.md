# CinemachineCamera

The CinemachineCamera is a component that you add to an empty GameObject. It represents a Cinemachine Camera in the Unity Scene.

At any time, each CinemachineCamera may be in one of these states:

* __Live__: The CinemachineCamera actively controls a Unity camera that has a Cinemachine Brain. When a Cinemachine Brain blends from one CinemachineCamera to the next, both CinemachineCameras are live. When the blend is complete, there is only one live CinemachineCamera.

* __Standby__: The CinemachineCamera doesn’t control the Unity camera. However, it still follows and aims at its targets, and updates. A CinemachineCamera in this state is activated and has a priority that is the same as or lower than the live CinemachineCamera.

* __Disabled__: The CinemachineCamera doesn’t control the Unity camera and doesn’t actively follow or aim at its targets. A CinemachineCamera in this state doesn’t consume processing power. To disable a CinemachineCamera, deactivate its GameObject. The CinemachineCamera is present but disabled in the Scene. However, even though the GameObject is deactivated, the CinemachineCamera can still control the Unity camera if the CinemachineCamera is participating in a blend, or if it is invoked by Timeline.

## Passive Cameras
On its own, the CinemachineCamera is a passive object, meaning that its transform can be controlled or parented, just like any other GameObject. It becomes a placeholder for the real camera: when it is Live, the Unity camera is positioned to match the CinemachineCamera's transform, and its lens is set to match as well. Furthermore, as part of the Cinemachine ecosystem, it can participate in blends and be controlled from a Cinemachine track in the timeline. You can also add effects like impulse, post-processing,  noise, and other extensions to increase the punch of the shot.

## Going Procedural
However, the real magic comes when you add Procedural Components to bring the camera to life, allowing it to robustly track targets and compose its own shots. For this, you can add __Position Control__, __Rotation Control__, and __Noise__ behaviors to drive the CinemachineCamera's position, rotation, and lens. The CinemachineCamera applies these settings to the Unity Camera when [Cinemachine Brain](CinemachineBrain.md) or [Timeline](CinemachineTimeline.md) transfers control of the Unity camera to the CinemachineCamera. 

![CinemachineCamera properties](images/CmCameraInspector.png)

## Properties

| **Property:** || **Function:** |
|:---|:---|:---|
| __Solo__ || Toggles whether or not the CinemachineCamera is temporarily live. Use this property to get immediate visual feedback in the [Game view](https://docs.unity3d.com/Manual/GameView.html) to adjust the CinemachineCamera. |
| __Game View Guides__ || Toggles the visibility of compositional guides in the Game view. These guides are available when Tracking Target specifies a GameObject and the CinemachineCamera has a screen-composition behavior, such as Position Composer or Rotation Composer. This setting is shared by all CinemachineCameras. |
|| _Disabled_ | Game View Guides are not displayed. |
|| _Passive_ | Game View Guides are displayed while the relevant components are selected. |
|| _Interactive_ | Game View Guides are displayed while the relevant components are selected, and can be dragged in the Game View with the mouse to change the settings. |
| __Save During Play__ || Check to [apply the changes while in Play mode](CinemachineSavingDuringPlay.md).  Use this feature to fine-tune a CinemachineCamera without having to remember which properties to copy and paste. This setting is shared by all CinemachineCameras. |
| __Priority And Channel__ || This setting controls how the output of this CinemachineCamera is used by the CinemachineBrain.  Enable this to use Priorities or custom CM output channels. |
|| _Channel_ | This is used to control which CinemachineBrain will be driven by this camera.  It is needed when there are multiple CinemachineBrains in the scene (for example, when implementing split-screen). |
|| _Priority_ | This is used to control which of several active CinemachineCameras should be live, when not controlled by Timeline. By default, priority is 0.  Use this to specify a custom priority value. A higher value indicates a higher priority. Negative values are also allowed. Cinemachine Brain chooses the next live CinemachineCamera from all CinemachineCameras that are activated and have the same or higher priority as the current live CinemachineCamera. This property has no effect when using a CinemachineCamera with Timeline. |
| __Tracking Target__ || The target GameObject that the CinemachineCamera procedurally follows. The procedural algorithms use this target as input when updating the position and rotation of the Unity camera. |
| __Look At Target__ || If enabled, this specifies a distinct target GameObject at which to aim the Unity camera. The [Rotation Control properties](CinemachineVirtualCameraAim.md) use this target to update the rotation of the Unity camera. |
| __Standby Update__ || Controls how often the CinemachineCamera is updated when the CinemachineCamera is not live. |
| __Lens__ || These properties mirror their counterparts in the properties for the [Unity camera](https://docs.unity3d.com/Manual/class-Camera.html). |
| | _Field Of View_ | The camera view in vertical degrees. For example, to specify the equivalent of a 50mm lens on a Super 35 sensor, enter a Field of View of 19.6 degrees. This property is available when the Unity camera with the Cinemachine Brain component uses a Projection of Perspective. You can also use [Scene Handles](handles.md) to modify this property. |
| | _Presets_ | A drop-down menu of settings for commonly-used lenses. Choose **Edit Presets** to add or edit the asset that contains a default list of lenses. |
| | _Orthographic Size_ | When using an orthographic camera, defines the half-height of the camera view, in world coordinates. Available when the Unity camera with the Cinemachine Brain component uses a Projection of Orthographic. |
| | _Near Clip Plane_ | The closest point relative to the camera where drawing occurs. You can also use [Scene Handles](handles.md) to modify this property.|
| | _Far Clip Plane_ | The furthest point relative to the camera where drawing occurs. You can also use [Scene Handles](handles.md) to modify this property.|
| | _Dutch_ | The Dutch angle. Tilts the Unity camera on the z-axis, in degrees. This property is unique to the CinemachineCamera; there is no counterpart property in the Unity camera. |
| | _GateFit_ | Physical cameras only: How the image is fitted to the sensor if the aspect ratios differ |
| | _SensorSize_ | Physical cameras only: This is the actual size of the image sensor (in mm) |
| | _LensShift_ | Physical cameras only: Position of the gate relative to the film back |
| | _FocusDistance_ | Physical cameras only: Distance from the camera lens at which focus is sharpest.  The Depth of Field Volume override uses this value if you set FocusDistanceMode to Camera.  Otherwise, this value is ignored. |
| | _Iso_ | Physical cameras only: The sensor sensitivity (ISO) |
| | _ShutterSpeed_ | Physical cameras only: The exposure time, in seconds |
| | _Aperture_ | Physical cameras only: The aperture number, in f-stop |
| | _BladeCount_ | Physical cameras only: The number of diaphragm blades |
| | _Curvature_ | Physical cameras only: Maps an aperture range to blade curvature |
| | _BarrelClipping_ | Physical cameras only: The strength of the \"cat-eye\" effect on bokeh (optical vignetting) |
| | _Anamorphism_ | Physical cameras only: Stretches the sensor to simulate an anamorphic look.  Positive values distort the camera vertically, negative values distort the camera horizontally |
|  __Mode Override__ || Allows you to select a different camera mode to apply to the [Unity camera](https://docs.unity3d.com/Manual/class-Camera.html) component when Cinemachine activates this CinemachineCamera. <br />__Important:__ For this override to take effect, you must enable the Lens Mode Override option in the CinemachineBrain inspector, and specify a default lens mode there. |
| | _None_ | Leaves the __Projection__ and __Physical Camera__ properties unchanged in the Camera. |
| | _Orthographic_ | Sets the __Projection__ property to __Orthographic__. |
| | _Perspective_ | Sets the __Projection__ property to __Perspective__ and *disables* the __Physical Camera__ feature and properties. |
| | _Physical_ | Sets the __Projection__ property to __Perspective__ and *enables* the __Physical Camera__ feature and properties. |
| __Blend Hint__ || Provides hints for blending positions to and from the CinemachineCamera. Values can be combined together. |
| | _Spherical Position_ | During a blend, camera will take a spherical path around the Tracking target. |
| | _Cylindrical Position_ | During a blend, camera will take a cylindrical path around the Tracking target (vertical co-ordinate is linearly interpolated). |
| | _Screen Space Aim When Targets Differ_ | During a blend, Tracking target position will interpolate in screen space instead of world space. |
| | _Inherit Position_ | When this CinemachineCamera goes live, force the initial position to be the same as the current position of the Unity Camera, if possible. |
| | _Ignore Target_ | Don't consider the Tracking Target when blending rotations, just to a spherical interpolation. |
| | _Freeze When Blending Out_ | Normally the camera remain live when blending, because it creates the smoothest transitions.  If this hint is enabled, then the camera will not update while blending out; instead it will create a snapshot of its state and blending will be done from that. |
| __Position Control__ || Shortcut for setting the procedural positioning behavior of the CinemachineCamera.  |
| __Rotation Control__ || Shortcut for setting the procedural rotation behavior of the CinemachineCamera.  |
| __Noise__ || Shortcut for setting the procedural noise behavior of the CinemachineCamera.  |
| __Add Extension__ || Shortcut for adding procedural extension behaviors to the CinemachineCamera.  |


