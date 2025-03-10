# Using the Cinemachine Pixel Perfect extension

Both the __Pixel Perfect Camera__ and Cinemachine modify the Camera’s orthographic size. Using these two systems together in a single Scene would cause them to fight for control over the Camera and produce unwanted results. The __Cinemachine Pixel Perfect__ extension solves this incompatibility.

__Cinemachine Pixel Perfect__ is an [extension](concept-procedural-motion.md#extensions) for the __CinemachineCamera__ that alters the orthographic size of the CinemachineCamera. The extension detects the presence of the Pixel Perfect Camera component, and uses the component settings to calculate for the correct orthographic size of the CinemachineCamera that best retains the Sprites in a pixel-perfect resolution.

To add this extension to your CinemachineCameras, use the __Add Extension__ dropdown menu on the CinemachineCamera Inspector window. Add this extension to each CinemachineCamera in your Project.

For each CinemachineCamera attached with this extension, the Pixel Perfect Camera component then calculates a pixel-perfect orthographic size that best matches the original size of the CinemachineCamera during __Play Mode __ or when __Run In Edit Mode__ is enabled. This is done to match the original framing of each CinemachineCamera as close as possible when the pixel-perfect calculations are implemented.

When the [Cinemachine Brain](CinemachineBrain.md) component [blends](CinemachineBlending.md) between multiple CinemachineCameras, the rendered image is temporarily not pixel-perfect during the transition between cameras. The image becomes pixel-perfect once the view fully transitions to a single CinemachineCamera.

The following are the current limitations of the extension:

- When a CinemachineCamera with the Pixel Perfect extension is set to follow a [Target Group](CinemachineTargetGroup.md), there may be visible choppiness when the CinemachineCamera is positioned with the Framing Transposer component.
- If the __Upscale Render Texture__ option is enabled on the Pixel Perfect Camera, there are less possible pixel-perfect resolutions that match the original orthographic size of the CinemachineCameras. This may cause the framing of the CinemachineCameras to be off by quite a large margin after the pixel-perfect calculations.

