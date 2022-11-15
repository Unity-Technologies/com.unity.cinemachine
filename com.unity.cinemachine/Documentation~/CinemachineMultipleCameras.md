# Split-Screen and Multiple Unity Cameras

By design, CmCameras are not directly linked to CinemachineBrains.  Instead, active CmCameras in the scene are dynamically found by the Brain, allowing them to be brought into existence via prefab instantiation or scene loading.  By default, if multiple CinemachineBrains exist in the scene, they will all find the same CmCameras and consequently display the same thing.  To assign a specific CmCamera to a specific Brain, Cinemachine Channels are used.  This works the same way as Unity Layers.  

First, set your CmCamera to output to the desired channel:

![Cinemachine Channels Camera](images/CinemachineChannels-camera.png)

Next, add that channel to the CinemachineBrain's Channel mask.  Multiple channels may be prsent simultaneously in the mask.  The CinemachineBrain will use only those CmCameras that output to channels that are present in the mask.  All other CmCameras will be ignored.

![Cinemachine Channels   Brain](images/CinemachineChannels%20-%20brain.png)

