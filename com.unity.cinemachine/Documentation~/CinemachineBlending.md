# Blending between CmCameras

Use blending properties to specify how the Cinemachine Brain component performs a blend between CmCameras.

A Cinemachine blend is not a fade, wipe, or dissolve. Rather, Cinemachine Brain performs a smooth animation of the position, rotation, and other settings of the Unity camera from one CmCamera to the next.

For blends between specific CmCameras, use the __Custom Blends__ list in the Cinemachine Brain component. Use the __Default Blend__ property in Cinemachine Brain to specify blends between CmCameras that do not have custom blends.

![Custom Blends list in Cinemachine Brain](images/CinemachineCustomBlends.png)

The __From__ and __To__ settings are name-based, not references. This means that Cinemachine finds cameras by matching their names to the settings. They are not linked to specific GameObjects.  The built-in dropdowns can be used to select a CmCamera from the current scene, or the name can be typed directly into the text boxes.  If a name does not match any CmCamera in the current scene, the field will be highlighted in yellow.

Use the reserved name **\*\*ANY CAMERA\*\*** to blend from or to any CmCamera.

When Cinemachine begins a transition from one CmCamera to another, it will look in this asset for an entry that matches the upcoming transition, and apply that blend definition.  

- If none is found, then the CinemachineBrain's DefaultBlend setting will apply.  
- If multiple entries in the Custom Blends asset match the upcoming transition, Cinemachine will choose the one with the strongest specificity. For example, if blending from vcam1 to vcam2, and the custom blends asset contains an entry for _vcam1-to-AnyCamera_, and another entry for _vcam1-to-vcam2_, then the _vcam1-to-vcam2_ entry will apply.
- If multiple entries in the Custom Blends asset match the upcoming transition with equally-strong specificity, then the first one found will apply.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __From__ || The name of the CmCamera to blend from. Use the name \*\*ANY CAMERA\*\* to blend from any CmCamera. This property is available only for custom blends. |
| __To__ || The name of the CmCamera to blend to. Use the name \*\*ANY CAMERA\*\* to blend to any CmCamera. This property is available only for custom blends. |
| __Style Default Blend__ || Shape of the blend curve. |
| | _Cut_ | Zero-length blend. |
| | _Ease In Out_ | S-shaped curve, giving a gentle and smooth transition. |
| | _Ease In_ | Linear out of the outgoing CmCamera, and ease into the incoming CmCamera. |
| | _Ease Out_ | Ease out of the outgoing CmCamera, and blend linearly into the incoming CmCamera. |
| | _Hard In_ | Ease out of the outgoing CmCamera, and accelerate into the incoming CmCamera. |
| | _Hard Out_ | Accelerate out of the outgoing CmCamera, and ease into the incoming CmCamera. |
| | _Linear_ | Linear blend. mechanical-looking. |
| | _Custom_ | Custom blend curve. Allows you to draw a custom blend curve. |
| __Time__ || Duration (in seconds) of the blend. |


