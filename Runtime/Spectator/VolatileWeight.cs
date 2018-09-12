using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spectator
{
    public struct VolatileWeight 
    {
        const float Epsilon = 0.0001f;
        const float kNegligibleResidual = 0.01f;

        public SpectatorTuningConstants.WeightID id;
        public float amount;
        public float decayTime;

        public float Decay(float deltaTime)
        {
            if (decayTime < Epsilon || Mathf.Abs(amount) < Epsilon)
                amount = 0;
            else
            {
                // Exponential decay
                float k = Mathf.Log(1f / kNegligibleResidual) / decayTime;
                amount = amount / Mathf.Exp(k * deltaTime);
            }
            return amount;
        }
    }
}
