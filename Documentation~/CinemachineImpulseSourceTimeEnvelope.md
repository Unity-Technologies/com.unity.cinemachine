# Time envelope

Vibrations from a real-world impact get stronger until they reach their peak strength, then weaken until the vibration stops. How long this cycle takes depends on the strength of the impact, and the characteristics of the GameObjects involved.

For instance, striking a concrete wall with a hammer produces a short, sharp impact. The vibrations reach their peak strength almost instantly and stop almost instantly. Striking a large sheet of thin metal, on the other hand, produces sustained vibration, which starts suddenly, stays at peak intensity for a while, and gradually softens. 

Use an Impulse Source’s **Time Envelope** properties to control this cycle when impacts in the Scene shake the camera. The time envelope has three properties:

- **Attack** controls the Impulse signal’s transition to peak intensity.
- **Sustain Time** specifies how long the signal stays at peak intensity.
- **Decay** controls the signal’s transition from peak intensity to zero.

Both **Attack** and **Decay** consist of a duration value that specifies how long the transition takes, and a curve that defines how it happens (for example, whether it happens gradually or suddenly). The curves are optional; if you leave them blank in the Inspector window, Impulse uses its default curves, which are suitable for most purposes. **Sustain Time** is a duration value only.

Taken together, these properties control how long vibrations from an impact occurrence last, and how they fade in and fade out. However, they don’t account for the strength of the impact. To do that, enable the **Scale with Impact** property. When it’s enabled, the time envelope scales according the strength of an impact. Stronger impacts make the envelope longer, and weaker ones make it shorter. This does not affect the envelope’s proportions.