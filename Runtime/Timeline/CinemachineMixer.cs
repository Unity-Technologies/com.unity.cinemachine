#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
using Cinemachine;

//namespace Cinemachine.Timeline
//{
    internal sealed class CinemachineMixer : PlayableBehaviour
    {
        // The brain that this track controls
        private CinemachineBrain mBrain;
        private int mBrainOverrideId = -1;
        private bool mPlaying;

        public override void OnPlayableDestroy(Playable playable)
        {
            if (mBrain != null)
                mBrain.ReleaseCameraOverride(mBrainOverrideId); // clean up
            mBrainOverrideId = -1;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            mPlaying = info.evaluationType == FrameData.EvaluationType.Playback;
        }

        struct ClipInfo
        {
            public ICinemachineCamera vcam;
            public float weight;
            public double localTime;
            public double duration;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);

            // Get the brain that this track controls.
            // Older versions of timeline sent the gameObject by mistake.
            GameObject go = playerData as GameObject;
            if (go == null)
                mBrain = (CinemachineBrain)playerData;
            else
                mBrain = go.GetComponent<CinemachineBrain>();
            if (mBrain == null)
                return;

            // Find which clips are active.  We can process a maximum of 2.
            // In the case that the weights don't add up to 1, the outgoing weight
            // will be calculated as the inverse of the incoming weight.
            int activeInputs = 0;
            ClipInfo clipA = new ClipInfo();
            ClipInfo clipB = new ClipInfo();
            for (int i = 0; i < playable.GetInputCount(); ++i)
            {
                float weight = playable.GetInputWeight(i);
                var clip = (ScriptPlayable<CinemachineShotPlayable>)playable.GetInput(i);
                CinemachineShotPlayable shot = clip.GetBehaviour();
                if (shot != null && shot.IsValid
                    && playable.GetPlayState() == PlayState.Playing
                    && weight > 0)
                {
                    clipA = clipB;
                    clipB.vcam = shot.VirtualCamera;
                    clipB.weight = weight;
                    clipB.localTime = clip.GetTime();
                    clipB.duration = clip.GetDuration();
                    if (++activeInputs == 2)
                        break;
                }
            }

            // Figure out which clip is incoming
            bool incomingIsB = clipB.weight >= 1 || clipB.localTime < clipB.duration / 2;
            if (activeInputs == 2)
            {
                if (clipB.localTime < clipA.localTime)
                    incomingIsB = true;
                else if (clipB.localTime > clipA.localTime)
                    incomingIsB = false;
                else
                    incomingIsB = clipB.duration >= clipA.duration;
            }

            // Override the Cinemachine brain with our results
            ICinemachineCamera camA = incomingIsB ? clipA.vcam : clipB.vcam;
            ICinemachineCamera camB = incomingIsB ? clipB.vcam : clipA.vcam;
            float camWeightB = incomingIsB ? clipB.weight : 1 - clipB.weight;
            mBrainOverrideId = mBrain.SetCameraOverride(
                    mBrainOverrideId, camA, camB, camWeightB, GetDeltaTime(info.deltaTime));
        }

        float mLastOverrideTime;
        float GetDeltaTime(float deltaTime)
        {
            if (!mPlaying)
            {
                // We're scrubbing or paused
                if (mBrainOverrideId < 0)
                    mLastOverrideTime = -1;

                // When force-scrubbing in playmode, we use timeline's suggested deltaTime
                // otherwise we look at the real clock for scrubbing in edit mode
                if (!Application.isPlaying)
                {
                    deltaTime = Time.unscaledDeltaTime;
                    float time = Time.realtimeSinceStartup;
                    if (mLastOverrideTime < 0 || time - mLastOverrideTime > Time.maximumDeltaTime * 5)
                        deltaTime = -1; // paused long enough - kill time-dependent stuff
                    mLastOverrideTime = time;
                }
            }
            return deltaTime;
        }
    }
//}
#endif
