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

#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
        class ScrubbingCacheHelper
        {
            // Registry of all vcams that are present in the track, active or not
            List<List<CinemachineVirtualCameraBase>> mAllCamerasForScrubbing;

            public void Init(Playable playable)
            {
                // Build our vcam registry for scrubbing updates
                mAllCamerasForScrubbing = new List<List<CinemachineVirtualCameraBase>>();
                for (int i = 0; i < playable.GetInputCount(); ++i)
                {
                    var clip = (ScriptPlayable<CinemachineShotPlayable>)playable.GetInput(i);
                    CinemachineShotPlayable shot = clip.GetBehaviour();
                    if (shot != null && shot.IsValid)
                    {
                        var vcam = shot.VirtualCamera;
                        int nestLevel = 0;
                        for (ICinemachineCamera p = vcam.ParentCamera; p != null; p = p.ParentCamera)
                            ++nestLevel;
                        while (mAllCamerasForScrubbing.Count <= nestLevel)
                            mAllCamerasForScrubbing.Add(new List<CinemachineVirtualCameraBase>());
                        if (mAllCamerasForScrubbing[nestLevel].IndexOf(vcam) < 0)
                            mAllCamerasForScrubbing[nestLevel].Add(vcam);
                    }
                }
            }

            float GetMaxDampTime()
            {
                float maxDampingTime = 0;
                for (int i = mAllCamerasForScrubbing.Count - 1; i >= 0; --i)
                {
                    var sublist = mAllCamerasForScrubbing[i];
                    for (int j = sublist.Count - 1; j >= 0; --j)
                    {
                        var vcam = sublist[j];
                        maxDampingTime = Mathf.Max(maxDampingTime, vcam.GetMaxDampTime());
                    }
                }
                // Impose upper limit on damping time, to avoid simulating too many frames
                return Mathf.Min(maxDampingTime, 4.0f); 
            }

            public void ScrubToHere(
                float currentTime, TargetPositionCache.Mode cacheMode, CinemachineBrain brain)
            {
                if (brain == null)
                    return;
                TargetPositionCache.CacheMode = cacheMode;
                TargetPositionCache.CurrentTime = currentTime;
                if (cacheMode != TargetPositionCache.Mode.Playback)
                    return;
            
                float stepSize = TargetPositionCache.CacheStepSize;

                var up = brain.DefaultWorldUp;
                var endTime = TargetPositionCache.CurrentTime;
                var startTime = Mathf.Max(
                    TargetPositionCache.CacheTimeRange.Start, endTime - GetMaxDampTime());
                var numSteps = Mathf.FloorToInt((endTime - startTime) / stepSize);
                for (int step = numSteps; step >= 0; --step)
                {
                    var t = Mathf.Max(startTime, endTime - step * stepSize);
                    TargetPositionCache.CurrentTime = t;
                    var deltaTime = (step == numSteps) ? -1 
                        : (t - startTime < stepSize ? t - startTime : stepSize);

                    // Update all relevant vcams, leaf-most first
                    for (int i = mAllCamerasForScrubbing.Count - 1; i >= 0; --i)
                    {
                        var sublist = mAllCamerasForScrubbing[i];
                        for (int j = sublist.Count - 1; j >= 0; --j)
                        {
                            var vcam = sublist[j];
                            vcam.InternalUpdateCameraState(up, deltaTime);
                            if (deltaTime < 0)
                                vcam.ForceCameraPosition(
                                    TargetPositionCache.GetTargetPosition(vcam.transform), 
                                    TargetPositionCache.GetTargetRotation(vcam.transform));
                        }
                    }
                }
            }
        }
        ScrubbingCacheHelper m_ScrubbingCacheHelper;
#endif

#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
        public override void OnGraphStart(Playable playable)
        {
            base.OnGraphStart(playable);
            m_ScrubbingCacheHelper = null;
        }
#endif
        
        public override void OnPlayableDestroy(Playable playable)
        {
            if (mBrain != null)
                mBrain.ReleaseCameraOverride(mBrainOverrideId); // clean up
            mBrainOverrideId = -1;
#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
            m_ScrubbingCacheHelper = null;
#endif
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            mPlaying = info.evaluationType == FrameData.EvaluationType.Playback;
#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
            if (Application.isPlaying || !TargetPositionCache.UseCache)
                TargetPositionCache.CacheMode = TargetPositionCache.Mode.Disabled;
            else
            {
                if (m_ScrubbingCacheHelper == null)
                {
                    m_ScrubbingCacheHelper = new ScrubbingCacheHelper();
                    m_ScrubbingCacheHelper.Init(playable);
                }
                m_ScrubbingCacheHelper.ScrubToHere(
                    (float)playable.GetGraph().GetRootPlayable(0).GetTime(), 
                    mPlaying ? TargetPositionCache.Mode.Record : TargetPositionCache.Mode.Playback,
                    mBrain);
            }
#else
            TargetPositionCache.CacheMode = TargetPositionCache.Mode.Disabled;
#endif
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

        float GetDeltaTime(float deltaTime)
        {
            if (mPlaying || Application.isPlaying)
                return deltaTime;

            // We're scrubbing or paused
            if (TargetPositionCache.CacheMode == TargetPositionCache.Mode.Playback
                && TargetPositionCache.HasHurrentTime)
            {
                return 0;
            }
            return -1;
        }
    }
//}
#endif
