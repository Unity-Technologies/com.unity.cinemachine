# Amplitude

The amplitude of the raw impulse signal controls the strength of the vibration for each impact. There are two ways to adjust the amplitude for a given Impulse Source.

## Changing the signal amplitude
The **Amplitude Gain** property amplifies or attenuates the raw impulse signal. It affects all impacts, all the time. Think of it as a global “volume” setting for turning the strength of an Impulse Source’s vibrations up or down.

## Changing the magnitude of the velocity vector
Changing the magnitude of the **Velocity** vector when generating the signal also scales the signal amplitude, but the effect is per-impact rather than global. By adjusting the velocity of individual Impulse events, you can set things up so that light impacts make smaller vibrations, and heavy impacts make bigger ones. 

- For the **Cinemachine Impulse Source** component you must set the velocity vector yourself via a script.  The signal’s amplitude scales by the magnitude of this vector.

- The **Cinemachine Collision Impulse Source** component calculates the velocity vector automatically according to rules defined by the three properties in the **How To Generate The Impulse** section. 

These global and per-impact adjustments are multiplied to calculate the actual amplitude of each impact.