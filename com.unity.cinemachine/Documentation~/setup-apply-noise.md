# Apply noise to simulate camera shake

To apply a noise behavior to a Cinemachine Camera:

1. In the Hierarchy, select your Cinemachine Camera.

2. In the Inspector, in the Cinemachine Camera component, select **Noise** and then select **Basic Multi Channel Perlin**.

   This adds a noise behavior to the Cinemachine Camera.

3. In the Basic Multi Channel Perlin component, under **Noise Profile**, choose an existing noise profile asset or [create your own profile](CinemachineNoiseProfiles.md).

4. Use **Amplitude Gain** and **Frequency Gain** to fine-tune the noise.

Noise is meant to be used for things such as hand-held camera effects, where the noise is continuous. For sudden shakes (e.g. in response to events like explosions), we recommend the use of Impulse [Impulse](CinemachineImpulse.md) rather than Noise.
