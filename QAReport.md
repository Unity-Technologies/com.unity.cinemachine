**Cinemachine 2.7.3 Testing Report**

Cinemachine 2.7.3 release is a bugfix release with the following: 

- Bugfix: 3rdPersonFollow collision resolution failed when the camera radius was large.
- Bugfix: 3rdPersonFollow damping was done in world space instead of camera space.
- Bugfix: 3rdPersonFollow stuttered when z damping was high.
- Regression fix: CinemachineInputProvider stopped providing input.
- Bugfix: Lens aspect and sensorSize were not updated when lens OverrideMode != None.

Cinemachine 2.7.3 was also regression tested using the Sample Scenes. Each scene was loaded to make sure that the Cinemachine behavior or feature it is demonstrating was ok. 

Manual testing around each of the Cinemachine Sample Scene was done.





