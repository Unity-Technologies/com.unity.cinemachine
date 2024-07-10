# Cinemachine Spline Smoother

This behaviour can be attached to a GameObject with a SplineContainer.  It proivdes a function to apply smoothing to the spline.  Smoothing auto-adjusts the knot settings to maintain second-order smoothness of the spline, making it suitable for camera paths.

When this behaviour is present while editing the spline, it will automatically adjust the knot tangents to maintain smoothness.  Do not adjust the knot tangents manually; they will be overwritten by the smoother.

At runtime, this behaviour does nothing.

### Properties

| Property | Description |
| --- | --- | --- |
| __Auto Smooth__ | If checked, the spline will be automatically smoothed whenever it is modified. |
| __Smooth Spline Now__ | Invokes the spline smoothing function to adjust the knot tangents. |


