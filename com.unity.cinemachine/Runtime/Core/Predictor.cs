using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a utility to implement position predicting.
    /// </summary>
    public struct PositionPredictor
    {
        Vector3 m_Velocity;
        Vector3 m_SmoothDampVelocity;
        Vector3 m_Pos;
        bool m_HavePos;

        /// <summary>How much to smooth the predicted result.  Must be >= 0, roughly corresponds to smoothing time.</summary>
        public float Smoothing;

        /// <summary>Have any positions been logged for smoothing?</summary>
        /// <value>True if no positions have yet been logged, in which case smoothing is impossible</value>
        public bool IsEmpty => !m_HavePos;

        /// <summary>Get the current position of the tracked object, as set by the last call to AddPosition().
        /// This is only valid if IsEmpty returns false.</summary>
        /// <value>The current position of the tracked object, as set by the last call to AddPosition()</value>
        public Vector3 CurrentPosition => m_Pos;

        /// <summary>
        /// Apply a delta to the target's position, which will be ignored for
        /// smoothing purposes.  Use this when the target's position gets warped.
        /// </summary>
        /// <param name="positionDelta">The position change of the target object</param>
        public void ApplyTransformDelta(Vector3 positionDelta) => m_Pos += positionDelta;

        /// <summary>
        /// Apply a delta to the target's rotation, which will be applied to
        /// the internal target velocity.  Use this then the target's rotation gets warped.
        /// </summary>
        /// <param name="rotationDelta">The rotation change of the target object</param>
        public void ApplyRotationDelta(Quaternion rotationDelta)
        {
            m_Velocity = rotationDelta * m_Velocity;
            m_SmoothDampVelocity = rotationDelta * m_SmoothDampVelocity;
        }

        /// <summary>Reset the lookahead data, clear all the buffers.</summary>
        public void Reset()
        {
            m_HavePos = false;
            m_SmoothDampVelocity = Vector3.zero;
            m_Velocity = Vector3.zero;
        }

        /// <summary>Add a new target position to the history buffer</summary>
        /// <param name="pos">The new target position</param>
        /// <param name="deltaTime">deltaTime since the last target position was added</param>
        public void AddPosition(Vector3 pos, float deltaTime)
        {
            if (deltaTime < 0)
                Reset();
            if (m_HavePos && deltaTime > UnityVectorExtensions.Epsilon)
            {
                var vel = (pos - m_Pos) / deltaTime;
                bool slowing = vel.sqrMagnitude < m_Velocity.sqrMagnitude;
                m_Velocity = Vector3.SmoothDamp(
                    m_Velocity, vel, ref m_SmoothDampVelocity, Smoothing / (slowing ? 30 : 10),
                    float.PositiveInfinity, deltaTime);
            }
            m_Pos = pos;
            m_HavePos = true;
        }

        /// <summary>Predict the target's position change over a given time from now</summary>
        /// <param name="lookaheadTime">How far ahead in time to predict</param>
        /// <returns>The predicted position change (current velocity * lookahead time)</returns>
        public Vector3 PredictPositionDelta(float lookaheadTime) => m_Velocity * lookaheadTime;
    }

    /// <summary>Utility to perform realistic damping of float or Vector3 values.
    /// The algorithm is based on exponentially decaying the delta until only
    /// a negligible amount remains.</summary>
    public static class Damper
    {
        const float Epsilon = UnityVectorExtensions.Epsilon;

        // Get the decay constant that would leave a given residual after a given time
        static float DecayConstant(float time, float residual) => Mathf.Log(1f / residual) / time;

        // Exponential decay: decay a given quantity over a period of time
        static float DecayedRemainder(float initial, float decayConstant, float deltaTime)
            => initial / Mathf.Exp(decayConstant * deltaTime);

        /// <summary>Standard residual</summary>
        public const float kNegligibleResidual = 0.01f;
        const float kLogNegligibleResidual = -4.605170186f; // == math.Log(kNegligibleResidual=0.01f);

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public static float Damp(float initial, float dampTime, float deltaTime)
        {
#if CINEMACHINE_EXPERIMENTAL_DAMPING
            if (!Time.inFixedTimeStep)
                return StableDamp(initial, dampTime, deltaTime);
#endif
            return StandardDamp(initial, dampTime, deltaTime);
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public static Vector3 Damp(Vector3 initial, Vector3 dampTime, float deltaTime)
        {
            for (int i = 0; i < 3; ++i)
                initial[i] = Damp(initial[i], dampTime[i], deltaTime);
            return initial;
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public static Vector3 Damp(Vector3 initial, float dampTime, float deltaTime)
        {
            for (int i = 0; i < 3; ++i)
                initial[i] = Damp(initial[i], dampTime, deltaTime);
            return initial;
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        // Internal for testing
        internal static float StandardDamp(float initial, float dampTime, float deltaTime)
        {
            if (dampTime < Epsilon || Mathf.Abs(initial) < Epsilon)
                return initial;
            if (deltaTime < Epsilon)
                return 0;
            return initial * (1 - Mathf.Exp(kLogNegligibleResidual * deltaTime / dampTime));
        }

        /// <summary>
        /// Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.
        ///
        /// This is a special implementation that attempts to increase visual stability
        /// in the context of an unstable framerate.
        ///
        /// It relies on AverageFrameRateTracker to track the average framerate.
        /// </summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        // Internal for testing
        internal static float StableDamp(float initial, float dampTime, float deltaTime)
        {
            if (dampTime < Epsilon || Mathf.Abs(initial) < Epsilon)
                return initial;
            if (deltaTime < Epsilon)
                return 0;

            // Try to reduce damage caused by frametime variability, by pretending
            // that the value to decay has accumulated steadily over many constant-time subframes.
            // We simulate being called for each subframe.  This does result in a longer damping time,
            // so we compensate with AverageFrameRateTracker.DampTimeScale, which is calculated
            // every frame based on the average framerate over the past number of frames.
            float step = Mathf.Min(deltaTime, AverageFrameRateTracker.kSubframeTime);
            int numSteps = Mathf.FloorToInt(deltaTime / step);
            float vel = initial * step / deltaTime; // the amount that accumulates each subframe
            float k = Mathf.Exp(kLogNegligibleResidual * AverageFrameRateTracker.DampTimeScale * step / dampTime);

            // ====================================
            // This code is equivalent to:
            //     float r = 0;
            //     for (int i = 0; i < numSteps; ++i)
            //         r = (r + vel) * k;
            // (partial sum of geometric series)
            float r = vel;
            if (Mathf.Abs(k - 1) < Epsilon)
                r *= k * numSteps;
            else
            {
                r *= k - Mathf.Pow(k, numSteps + 1);
                r /= 1 - k;
            }
            // ====================================

            // Handle any remaining quantity after the last step
            r = Mathf.Lerp(r, (r + vel) * k, (deltaTime - (step * numSteps)) / step);

            return initial - r;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
#endif
        // Internal for testing.
        // This class keeps a running calculation of the average framerate over a fixed
        // time window.  Used to smooth out framerate fluctuations for determining the
        // correct damping constant.
        internal static class AverageFrameRateTracker
        {
            const int kBufferSize = 100;

            static float[] s_Buffer = new float[kBufferSize];
            static int s_NumItems = 0;
            static int s_Head = 0;
            static float s_Sum = 0;

            public const float kSubframeTime = 1.0f / 1024.0f; // Do not change this without also changing SetDampTimeScale()

            public static float FPS { get; private set; }
            public static float DampTimeScale { get; private set; }

#if UNITY_EDITOR
            static AverageFrameRateTracker() => Reset();
#endif

            [RuntimeInitializeOnLoadMethod]
            static void Initialize()
            {
#if CINEMACHINE_EXPERIMENTAL_DAMPING
                // GML TODO: use a different hook
                Application.onBeforeRender -= Append;
                Application.onBeforeRender += Append;

                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
#endif
                Reset();
            }

            static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Reset();

            // Internal for testing
            internal static void Reset()
            {
                s_NumItems = 0;
                s_Head = 0;
                s_Sum = 0;
                FPS = 60;
                SetDampTimeScale(FPS);
            }

            static void Append()
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Reset();
                    return;
                }
#endif
                var dt = Time.unscaledDeltaTime;
                if (++s_Head == kBufferSize)
                    s_Head = 0;
                if (s_NumItems == kBufferSize)
                    s_Sum -= s_Buffer[s_Head];
                else
                    ++s_NumItems;
                s_Sum += dt;
                s_Buffer[s_Head] = dt;

                FPS = s_NumItems / s_Sum;
                SetDampTimeScale(FPS);
            }

            // Internal for testing
            internal static void SetDampTimeScale(float fps)
            {
                // Approximation computed heuristically, and curve-fitted to sampled data.
                // Valid only for kSubframeTime = 1.0f / 1024.0f
                DampTimeScale = 2.0f - 1.81e-3f * fps + 7.9e-07f * fps * fps;
            }
        }
    }
}
