using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spectator
{
    public class TransitionEvaluator 
    {
        public struct ShotInfo
        {
            /// <summary>World position/orientation of the camera</summary>
            public Transform camera;
            /// <summary>Worldspace velocity of the camera, distance/second.  May be zero</summary>
            public Vector3 cameraVelocity;
            /// <summary>Camera vertical FOV</summary>
            public float fov;
            /// <summary>Position/orientation of all the visible camera targets</summary>
            public List<Transform> targets;
            /// <summary>Worldspace velocity of the targets, distance/second.  May be zero or null<</summary>
            public List<Vector3> targetVelocities;
        }

        /// <summary>
        /// Evaluate the quality of a transition from one shot to another.
        /// </summary>
        /// <param name="shotA">The current shot</param>
        /// <param name="shotB">The proposed shot</param>
        /// <returns>Transition Quality rating.  1==great, 0==shit</returns>
        public float EvaluateShotTransitionQuality(ShotInfo shotA, ShotInfo shotB)
        {
            // TODO
            return 1;
        }
    }
}