using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spectator
{
    public struct VolatileWeight 
    {
        const float Epsilon = 0.0001f;
        const float kDecayConstant = 4.60517018599f; // Log(1/0.01) where 0.01 = negligible residual

        public SpectatorTuningConstants.WeightID id;
        public float amount;
        public float decayTime;

        // Exponential decay
        public float Decay(float deltaTime)
        {
            if (decayTime < Epsilon || Mathf.Abs(amount) < Epsilon)
                amount = 0;
            else
                amount = amount / Mathf.Exp(kDecayConstant * deltaTime / decayTime);
            return amount;
        }
    }

    public class VolatileWeightSet
    {
        const int growAmount = 4;
        public VolatileWeight[] weights = new VolatileWeight[growAmount];

        int GetIndexForId(SpectatorTuningConstants.WeightID id)
        {
            int size = weights.Length;
            for (int i = 0; i < size; ++i)
                if (weights[i].id == id || weights[i].id == SpectatorTuningConstants.WeightID.None)
                    return i;
            VolatileWeight[] newWeights = new VolatileWeight[size + growAmount];
            weights = newWeights;
            return size;
        }

        public void SetWeight(VolatileWeight w)
        {
            int i = GetIndexForId(w.id);
            weights[i] = w;
        }

        public float Decay(float deltaTime)
        {
            float a = 0;
            for (int i = 0; i < weights.Length && weights[i].id != SpectatorTuningConstants.WeightID.None; ++i)
                a += weights[i].Decay(deltaTime);
            return a;
        }
    }
}
