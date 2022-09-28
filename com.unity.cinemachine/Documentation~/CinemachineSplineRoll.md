# Cinemachine Spline Roll

This behaviour adds Roll to a Spline.  Roll is rotation about the spline's tangent.  Add data points to set the roll at specific points along the spline.  Roll will be interpolated between those points.  This behaviour will also draw a railroad-track gizmo in the scene view, to help visualize the roll.

If you add this behaviur to the Spline itself, then any [Cm Camera](CmCamera.md) or [Cinemachine Spline Cart](CinemachineSplineCart.md) that follows the path will respect the roll.  If instead you add this behaviour to the CmCamera itself, then the roll will be visible only to that CmCamera.

