#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
using Cinemachine;
using System.Collections.Generic;

//namespace Cinemachine.Timeline
//{
    internal sealed class CinemachineMixer : PlayableBehaviour
    {
        // The brain that this track controls
        private CinemachineBrain mBrain;
        private int mBrainOverrideId = -1;
        private bool mPlaying;

        // Registry of all vcams that are present in the track, active or not
        List<List<CinemachineVirtualCameraBase>> mAllCamerasForScrubbing;
        float mMaxDampingTime;
        List<float> mTimestamps;
        float mLastScrubbedTime;

        void ScrubToHere()
        {
            if (mBrain == null)
                return;
            if (mTimestamps == null)
                mTimestamps = new List<float>();
            float endTime = TargetPositionCache.CurrentTime;
            TargetPositionCache.GetTimestamps(endTime - mMaxDampingTime, endTime, mTimestamps);

            for (int t = 0; t < mTimestamps.Count; ++t)
            {
                mLastScrubbedTime = mTimestamps[t];
                var deltaTime = t == 0 ? -1 : mLastScrubbedTime - TargetPositionCache.CurrentTime;
                TargetPositionCache.CurrentTime = mLastScrubbedTime;

                // Update all relevant vcams, leaf-most first
                for (int i = mAllCamerasForScrubbing.Count-1; i >= 0; --i)
                {
                    var sublist = mAllCamerasForScrubbing[i];
                    for (int j = sublist.Count - 1; j >= 0; --j)
                    {
                        var vcam = sublist[j];
                        vcam.InternalUpdateCameraState(mBrain.DefaultWorldUp, deltaTime);
//Debug.Log("t = " + TargetPositionCache.CurrentTime + ", dt = " + deltaTime);
                    }
                }
            }
            TargetPositionCache.CurrentTime = endTime;
        }

        public override void OnGraphStart(Playable playable)
        {
            base.OnGraphStart(playable);

            if (mAllCamerasForScrubbing == null)
                mAllCamerasForScrubbing = new List<List<CinemachineVirtualCameraBase>>();

            // Build our vcam registry for scrubbing updates
            mMaxDampingTime = 0;
            for (int i = 0; i < playable.GetInputCount(); ++i)
            {
                var clip = (ScriptPlayable<CinemachineShotPlayable>)playable.GetInput(i);
                CinemachineShotPlayable shot = clip.GetBehaviour();
                if (shot != null && shot.IsValid)
                {
                    var vcam = shot.VirtualCamera;
                    mMaxDampingTime = Mathf.Max(mMaxDampingTime, vcam.GetMaxDampTime());
                    int parentLevel = 0;
                    for (ICinemachineCamera p = vcam.ParentCamera; p != null; p = p.ParentCamera)
                        ++parentLevel;
                    while (mAllCamerasForScrubbing.Count <= parentLevel)
                        mAllCamerasForScrubbing.Add(new List<CinemachineVirtualCameraBase>());
                    if (mAllCamerasForScrubbing[parentLevel].IndexOf(vcam) < 0)
                        mAllCamerasForScrubbing[parentLevel].Add(vcam);
                }
            }
        }
        
        public override void OnPlayableDestroy(Playable playable)
        {
            if (mBrain != null)
                mBrain.ReleaseCameraOverride(mBrainOverrideId); // clean up
            mBrainOverrideId = -1;
            mAllCamerasForScrubbing = null;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            mPlaying = info.evaluationType == FrameData.EvaluationType.Playback;
            if (Application.isPlaying)
                TargetPositionCache.Enabled = false;
            else
            {
                TargetPositionCache.Enabled = true;
                TargetPositionCache.Recording = mPlaying;
                TargetPositionCache.CurrentTime 
                    = (float)playable.GetGraph().GetRootPlayable(0).GetTime();
                TargetPositionCache.CurrentRealTime = TargetPositionCache.CurrentTime;
                if (!mPlaying)
                    ScrubToHere();
            }
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
#if true // GML experiment
                    deltaTime = TargetPositionCache.CurrentTime - mLastScrubbedTime;
Debug.Log(deltaTime);
#else
                    deltaTime = Time.unscaledDeltaTime;
                    float time = Time.realtimeSinceStartup;
                    if (mLastOverrideTime < 0 || time - mLastOverrideTime > Time.maximumDeltaTime * 5)
                        deltaTime = -1; // paused long enough - kill time-dependent stuff
                    mLastOverrideTime = time;
#endif
                }
            }
            return deltaTime;
        }
    }
//}
#endif
