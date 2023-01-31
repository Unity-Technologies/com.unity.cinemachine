# Cinemachine Spline Cart

__Cinemachine Spline Cart__ is a component that constrains the transform of its GameObject to a __Spline__ . Use it to animate a GameObject along a path, or as a tracking target for CinemachineCamera.

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---
| __Spline__ || The spline to follow. |
| __Update Method__ || When to move the cart when speed is non-zero. Use __Update__ or __LateUpdate__ for normal updating and use __Fixed Update__ for updates in sync with the Physics module. |
| __Position Units__ || The unit of measure for __Position__.  |
| | _Knot_ | Use knots along the spline. The value 0 represents the first knot on the spline, 1 is the second knot, and so on. Non-integer values represent points in between the knots |
| | _Distance_ | Distance along the spline, in normal distance units. 0 is the beginning of the spline. |
| | _Normalized_ | The value 0 represents the beginning of the spline, 1 is the end of the spline. |
| __Speed__ || Move the cart with this speed. The value is interpreted according to __Position Units__. |
| __Position__ || The position along the spline at which to place the cart. This can be animated directly or, if the speed is non-zero, will be updated automatically at a time specified by the __Update Method__. The value is interpreted according to __Position Units__. |
| __Automatic Dolly__ || Controls whether automatic motion along the spline occurs. |
| __Method__ || Controls how automatic dollying occurs. You can implement your own extensions to this by writing a custom SplineAutoDolly.ISplineAutoDolly class. |
| | _None_ | No automatic dollying occurs. You must control the CinemachineCamera's position on the spline by setting PathPosition. |
| | _Fixed Speed_ | Camera travels along the path at a fixed speed, which you can set. |
| | _Nearest Point To Target_ | Positions the camera at the point on the spline that is closest to the Tracking Target's position. A Tracking Target is required in the CinemachineCamera. You can also specify an offset from the closest point, to tune the camera's position. |
