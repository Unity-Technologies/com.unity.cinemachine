using UnityEngine;

namespace Cinemachine.Utility
{
    public class PositionPredictor
    {
        Vector3 m_Position;

        const float kSmoothingDefault = 10;
        float mSmoothing = kSmoothingDefault;
        public float Smoothing 
        {
            get { return mSmoothing; }
            set 
            {
                if (value != mSmoothing)
                {
                    mSmoothing = value;
                    int maxRadius = Mathf.Max(10, Mathf.FloorToInt(value * 1.5f));
                    m_Velocity = new GaussianWindow1D_Vector3(mSmoothing, maxRadius);
                    m_Accel = new GaussianWindow1D_Vector3(mSmoothing, maxRadius);
                }
            }
        }

        public bool IgnoreY { get; set; }

        GaussianWindow1D_Vector3 m_Velocity = new GaussianWindow1D_Vector3(kSmoothingDefault);
        GaussianWindow1D_Vector3 m_Accel = new GaussianWindow1D_Vector3(kSmoothingDefault);

        public bool IsEmpty { get { return m_Velocity.IsEmpty(); } }

        public void ApplyTransformDelta(Vector3 positionDelta)
        {
            m_Position += positionDelta;
        }

        public void Reset()
        {
            m_Velocity.Reset();
            m_Accel.Reset();
        }

        public void AddPosition(Vector3 pos)
        {
            if (IsEmpty)
                m_Velocity.AddValue(Vector3.zero);
            else if (Time.deltaTime > Vector3.kEpsilon)
            {
                Vector3 vel = m_Velocity.Value();
                Vector3 vel2 = (pos - m_Position) / Time.deltaTime;
                if (IgnoreY)
                    vel2.y = 0;
                m_Velocity.AddValue(vel2);
                m_Accel.AddValue(vel2 - vel);
            }
            m_Position = pos;
        }

        public Vector3 PredictPosition(float lookaheadTime)
        {
            Vector3 pos = m_Position;
            if (Time.deltaTime > Vector3.kEpsilon)
            {
                int numSteps = Mathf.Min(Mathf.RoundToInt(lookaheadTime / Time.deltaTime), 6);
                float dt = lookaheadTime / numSteps;
                Vector3 vel = m_Velocity.IsEmpty() ? Vector3.zero : m_Velocity.Value();
                Vector3 accel = m_Accel.IsEmpty() ? Vector3.zero : m_Accel.Value();
                for (int i = 0; i < numSteps; ++i)
                {
                    pos += vel * dt;
                    Vector3 vel2 = vel + (accel * dt);
                    accel = Quaternion.FromToRotation(vel, vel2) * accel;
                    vel = vel2;
                }
            }
            return pos;
        }
    }

    /// <summary>Utility to perform realistic damping of float or Vector3 values.
    /// The algorithm is based on exponentially decaying the delta until only
    /// a negligible amount remains.</summary>
    public static class Damper
    {
        const float Epsilon = UnityVectorExtensions.Epsilon;

        // Get the decay constant that would leave a given residual after a given time
        static float DecayConstant(float time, float residual)
        {
            return Mathf.Log(1f / residual) / time;
        }

        // Exponential decay: decay a given quantity opver a period of time
        static float DecayedRemainder(float initial, float decayConstant, float deltaTime)
        {
            return initial / Mathf.Exp(decayConstant * deltaTime);
        }

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
            if (dampTime < Epsilon || Mathf.Abs(initial) < Epsilon)
                return initial;
            if (deltaTime < Epsilon)
                return 0;
            float k = -kLogNegligibleResidual / dampTime; //DecayConstant(dampTime, kNegligibleResidual);

#if CINEMACHINE_EXPERIMENTAL_DAMPING
            // Try to reduce damage caused by frametime variability
            float step = Time.fixedDeltaTime;
            if (deltaTime != step)
                step /= 5;
            int numSteps = Mathf.FloorToInt(deltaTime / step);
            float vel = initial * step / deltaTime;
            float decayConstant = Mathf.Exp(-k * step);
            float r = 0;
            for (int i = 0; i < numSteps; ++i)
                r = (r + vel) * decayConstant;
            float d = deltaTime - (step * numSteps);
            if (d > Epsilon)
                r = Mathf.Lerp(r, (r + vel) * decayConstant, d / step);
            return initial - r;
#else
            return initial * (1 - Mathf.Exp(-k * deltaTime));
#endif
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
    }

    /// <summary>Tracks an object's velocity with a filter to determine a reasonably
    /// steady direction for the object's current trajectory.</summary>
    public class HeadingTracker
    {
        struct Item
        {
            public Vector3 velocity;
            public float weight;
            public float time;
        };
        Item[] mHistory;
        int mTop;
        int mBottom;
        int mCount;

        Vector3 mHeadingSum;
        float mWeightSum = 0;
        float mWeightTime = 0;

        Vector3 mLastGoodHeading = Vector3.zero;

        /// <summary>Construct a heading tracker with a given filter size</summary>
        /// <param name="filterSize">The size of the filter.  The larger the filter, the 
        /// more constanct (and laggy) is the heading.  30 is pretty big.</param>
        public HeadingTracker(int filterSize)
        {
            mHistory = new Item[filterSize];
            float historyHalfLife = filterSize / 5f; // somewhat arbitrarily
            mDecayExponent = -Mathf.Log(2f) / historyHalfLife;
            ClearHistory();
        }

        /// <summary>Get the current filter size</summary>
        public int FilterSize { get { return mHistory.Length; } }

        void ClearHistory()
        {
            mTop = mBottom = mCount = 0;
            mWeightSum = 0;
            mHeadingSum = Vector3.zero;
        }

        static float mDecayExponent;
        static float Decay(float time) { return Mathf.Exp(time * mDecayExponent); }

        /// <summary>Add a new velocity frame.  This should be called once per frame,
        /// unless the velocity is zero</summary>
        /// <param name="velocity">The object's velocity this frame</param>
        public void Add(Vector3 velocity)
        {
            if (FilterSize == 0)
            {
                mLastGoodHeading = velocity;
                return;
            }
            float weight = velocity.magnitude;
            if (weight > UnityVectorExtensions.Epsilon)
            {
                Item item = new Item();
                item.velocity = velocity;
                item.weight = weight;
                item.time = Time.time;
                if (mCount == FilterSize)
                    PopBottom();
                ++mCount;
                mHistory[mTop] = item;
                if (++mTop == FilterSize)
                    mTop = 0;

                mWeightSum *= Decay(item.time - mWeightTime);
                mWeightTime = item.time;
                mWeightSum += weight;
                mHeadingSum += item.velocity;
            }
        }

        void PopBottom()
        {
            if (mCount > 0)
            {
                float time = Time.time;
                Item item = mHistory[mBottom];
                if (++mBottom == FilterSize)
                    mBottom = 0;
                --mCount;

                float decay = Decay(time - item.time);
                mWeightSum -= item.weight * decay;
                mHeadingSum -= item.velocity * decay;
                if (mWeightSum <= UnityVectorExtensions.Epsilon || mCount == 0)
                    ClearHistory();
            }
        }

        /// <summary>Decay the history.  This should be called every frame.</summary>
        public void DecayHistory()
        {
            float time = Time.time;
            float decay = Decay(time - mWeightTime);
            mWeightSum *= decay;
            mWeightTime = time;
            if (mWeightSum < UnityVectorExtensions.Epsilon)
                ClearHistory();
            else
                mHeadingSum = mHeadingSum * decay;
        }

        /// <summary>Get the filtered heading.</summary>
        /// <returns>The filtered direction of motion</returns>
        public Vector3 GetReliableHeading()
        {
            // Update Last Good Heading
            if (mWeightSum > UnityVectorExtensions.Epsilon
                && (mCount == mHistory.Length || mLastGoodHeading.AlmostZero()))
            {
                Vector3  h = mHeadingSum / mWeightSum;
                if (!h.AlmostZero())
                    mLastGoodHeading = h.normalized;
            }
            return mLastGoodHeading;
        }
    }
}
