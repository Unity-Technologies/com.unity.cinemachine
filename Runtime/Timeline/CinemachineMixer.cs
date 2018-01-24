using UnityEngine;
using UnityEngine.Playables;

namespace Cinemachine.Timeline
{
    public sealed class CinemachineMixer : PlayableBehaviour
    {
        // The brain that this track controls
        private CinemachineBrain mBrain;
        private int mBrainOverrideId = -1;
        private bool mPlaying;

        public override void OnGraphStop(Playable playable)
        {
            if (mBrain != null)
                mBrain.ReleaseCameraOverride(mBrainOverrideId); // clean up
            mBrainOverrideId = -1;
            DestroyCanvas();
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            mPlaying = info.evaluationType == FrameData.EvaluationType.Playback;
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
            ICinemachineCamera camA = null;
            ICinemachineCamera camB = null;
            Texture texA = null;
            Texture texB = null;
            float camWeightB = 1f;
            float texWeightB = 0;
            for (int i = 0; i < playable.GetInputCount(); ++i)
            {
                CinemachineShotPlayable shot
                    = ((ScriptPlayable<CinemachineShotPlayable>)playable.GetInput(i)).GetBehaviour();
                float weight = playable.GetInputWeight(i);
                if (shot != null && shot.IsValid
                    && playable.GetPlayState() == PlayState.Playing
                    && weight > 0.0001f)
                {
                    if (shot.m_StoryboardImage)
                    {
                        texA = texB;
                        texWeightB = weight;
                        texB = shot.m_Image;
                    }
                    if (!shot.m_StoryboardImage || shot.VirtualCamera != null)
                    {
                        camA = camB;
                        camWeightB = weight;
                        camB = shot.VirtualCamera;
                    }
                    if (++activeInputs == 2)
                        break;
                }
            }

            // Override the Cinemachine brain with our results
            mBrainOverrideId = mBrain.SetCameraOverride(
                    mBrainOverrideId, camA, camB, camWeightB, GetDeltaTime(info.deltaTime));

            // Display the storyboard overlays
            DisplayCanvasOverlays(texA, texB, texWeightB);
        }

        float mLastOverrideFrame;
        float GetDeltaTime(float deltaTime)
        {
            if (!mPlaying)
            {
                if (mBrainOverrideId < 0)
                    mLastOverrideFrame = -1;
                float time = Time.realtimeSinceStartup;
                deltaTime = Time.unscaledDeltaTime;
                if (!Application.isPlaying 
                    && (mLastOverrideFrame < 0 || time - mLastOverrideFrame > Time.maximumDeltaTime))
                {
                    deltaTime = -1;
                }
                mLastOverrideFrame = time;
            }
            return deltaTime;
        }


        // Storyboard support
        GameObject mCanvas;
        UnityEngine.UI.RawImage mRawImageA;
        UnityEngine.UI.RawImage mRawImageB;

        void CreateCanvas()
        {
            mCanvas = new GameObject("_CM_Mixer_canvas", typeof(RectTransform));
            mCanvas.hideFlags = HideFlags.HideAndDontSave;

            var c = mCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            var go = new GameObject("RawImageA", typeof(RectTransform));
            go.transform.SetParent(mCanvas.transform);
            mRawImageA = go.AddComponent<UnityEngine.UI.RawImage>();

            go = new GameObject("RawImageB", typeof(RectTransform));
            go.transform.SetParent(mCanvas.transform);
            mRawImageB = go.AddComponent<UnityEngine.UI.RawImage>();
        }
        
        void DestroyCanvas()
        {
            if (mCanvas != null)
                Object.DestroyImmediate(mCanvas);
            mCanvas = null;
            mRawImageA = null;
            mRawImageB = null;
        }

        void DisplayCanvasOverlays(Texture texA, Texture texB, float texWeightB)
        {
            if (texB == null)
            {
                if (mCanvas != null)
                    mCanvas.SetActive(false);
            }
            else
            {
                if (mCanvas == null)
                    CreateCanvas();
                mCanvas.SetActive(true);

                Color color = Color.white;
                mRawImageA.texture = texA;
                mRawImageA.color = color;
                mRawImageA.rectTransform.localPosition = Vector3.zero;
                mRawImageA.rectTransform.localRotation = Quaternion.identity;
                mRawImageA.rectTransform.localScale = Vector3.one;
                mRawImageA.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
                mRawImageA.gameObject.SetActive(texA != null);

                mRawImageB.texture = texB;
                color.a = texWeightB;
                mRawImageB.color = color;
                mRawImageB.rectTransform.localPosition = Vector3.zero;
                mRawImageB.rectTransform.localRotation = Quaternion.identity;
                mRawImageB.rectTransform.localScale = Vector3.one;
                mRawImageB.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
                mRawImageB.gameObject.SetActive(texB != null);
            }
        }
    }
}
