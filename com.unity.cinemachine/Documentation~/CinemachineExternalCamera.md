# Cinemachine External Camera - (deprecated)

**Note:** This component is **deprecated** in favour of using a normal CmCamera with _None_ in both __Position Control__ and __Rotation Control__.

This component will expose a non-cinemachine camera to the cinemachine ecosystem, allowing it to participate in blends. Just add it as a component alongside an existing Unity Camera component. You will need to take steps (e.g. disabling the Camera component) to ensure that the Camera doesn't fight with the main Cinemachine Camera.

## Properties:

| **Property:** | **Function:** |
|:---|:---|
| __Look At__ | The object that the camera is looking at, if defined. This can be empty, but setting this may improve the quality of the blends to and from this camera. |
| __Blend Hint__ | Hint for blending positions to and from this camera.  |
