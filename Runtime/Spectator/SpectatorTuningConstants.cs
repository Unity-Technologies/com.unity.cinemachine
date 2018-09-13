using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spectator
{
    public class SpectatorTuningConstants : ScriptableObject 
    {
        public enum WeightID
        {
            None = 0,

            /// Start Game-specific IDs here
            ClientStart = 1,
            
            /// Spectator reserved from here on
            SpectatorReservedStart = 10000,
            ThreadDecay,
            ShotDecay,
            NoveltyBoost,
            NewThreadBoost,
            NewShotBoost,
            CameraTypeSelectionBoost,
            CameraLensSelectionBoost,
            PlayerSelectionBoost
        }

        public float ThreadStarvationBoostAmount = 100;
        public float NewThreadBoostAmount = 100;
        public float NewThreadBoostDecayTime = 6;

        public float NewShotBoostAmount = 100;
        public float NewShotBoostDecayTime = 6;

        public float CameraTypeSelectionBoost = 1000;
        public float CameraLensSelectionBoost = 500;
        public float PlayerSelectionBoost = 1000;
        public float SelectionDecayTime = 0.2f;

        public float OptimalTargetDistanceMultiplier = 0.2f;
    }
}