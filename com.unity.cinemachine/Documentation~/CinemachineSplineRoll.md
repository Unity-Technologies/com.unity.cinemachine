# Cinemachine Spline Roll

This behavior adds Roll to a Spline. Roll is the rotation about the spline's tangent. Add data points to set the roll at specific points along the spline. Roll will be interpolated between those points. This behavior will also draw a railroad-track Gizmo in the Scene view, to help visualize the roll.

If you add this behavior to the Spline itself, then any [Cm Camera](CinemachineCamera.md) or [Cinemachine Spline Cart](CinemachineSplineCart.md) that follows the path will respect the roll. If instead you add this behavior to the CinemachineCamera itself, then the roll will be visible only to that CinemachineCamera.

