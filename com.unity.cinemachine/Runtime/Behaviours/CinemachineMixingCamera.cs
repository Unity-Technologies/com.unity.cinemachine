using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// CinemachineMixingCamera is a "manager camera" that takes on the state of
    /// the weighted average of the states of its child virtual cameras.
    ///
    /// A fixed number of slots are made available for cameras, rather than a dynamic array.
    /// We do it this way in order to support weight animation from the Timeline.
    /// Timeline cannot animate array elements.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [ExcludeFromPreset]
    [AddComponentMenu("Cinemachine/Cinemachine Mixing Camera")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineMixingCamera.html")]
    public class CinemachineMixingCamera : CinemachineCameraManagerBase
    {
        /// <summary>The maximum number of tracked cameras.  If you want to add
        /// more cameras, do it here in the source code, and be sure to add the
        /// extra member variables and to make the appropriate changes in
        /// GetWeight() and SetWeight().
        /// The inspector will figure itself out based on this value.</summary>
        public const int MaxCameras = 8;

        /// <summary>Weight of the first tracked camera</summary>
        [Tooltip("The weight of the first tracked camera")]
        [FormerlySerializedAs("m_Weight0")]
        public float Weight0 = 0.5f;
        /// <summary>Weight of the second tracked camera</summary>
        [Tooltip("The weight of the second tracked camera")]
        [FormerlySerializedAs("m_Weight1")]
        public float Weight1 = 0.5f;
        /// <summary>Weight of the third tracked camera</summary>
        [Tooltip("The weight of the third tracked camera")]
        [FormerlySerializedAs("m_Weight2")]
        public float Weight2 = 0.5f;
        /// <summary>Weight of the fourth tracked camera</summary>
        [Tooltip("The weight of the fourth tracked camera")]
        [FormerlySerializedAs("m_Weight3")]
        public float Weight3 = 0.5f;
        /// <summary>Weight of the fifth tracked camera</summary>
        [Tooltip("The weight of the fifth tracked camera")]
        [FormerlySerializedAs("m_Weight4")]
        public float Weight4 = 0.5f;
        /// <summary>Weight of the sixth tracked camera</summary>
        [Tooltip("The weight of the sixth tracked camera")]
        [FormerlySerializedAs("m_Weight5")]
        public float Weight5 = 0.5f;
        /// <summary>Weight of the seventh tracked camera</summary>
        [Tooltip("The weight of the seventh tracked camera")]
        [FormerlySerializedAs("m_Weight6")]
        public float Weight6 = 0.5f;
        /// <summary>Weight of the eighth tracked camera</summary>
        [Tooltip("The weight of the eighth tracked camera")]
        [FormerlySerializedAs("m_Weight7")]
        public float Weight7 = 0.5f;


        CameraState m_CameraState = CameraState.Default;
        Dictionary<CinemachineVirtualCameraBase, int> m_IndexMap;
        float m_LiveChildPercent;

        /// <summary>Makes sure the weights are non-negative</summary>
        void OnValidate()
        {
            for (int i = 0; i < MaxCameras; ++i)
                SetWeight(i, Mathf.Max(0, GetWeight(i)));
        }

        /// <inheritdoc />
        protected override void Reset()
        {
            base.Reset();
            for (var i = 0; i < MaxCameras; ++i)
                SetWeight(i, i == 0 ? 1 : 0);
        }

        /// <inheritdoc />
        public override CameraState State => m_CameraState;

        /// <inheritdoc />
        public override string Description
        {
            get
            {
                if (LiveChild == null)
                    return "[(none)]";
                var sb = CinemachineDebug.SBFromPool();
                sb.Append("[");
                sb.Append(LiveChild.Name);
                sb.Append(" ");
                sb.Append(Mathf.RoundToInt(m_LiveChildPercent));
                sb.Append("%]");
                var text = sb.ToString();
                CinemachineDebug.ReturnToPool(sb);
                return text;
            }
        }

        /// <summary>Get the weight of the child at an index.</summary>
        /// <param name="index">The child index. Only immediate CinemachineVirtualCameraBase
        /// children are counted.</param>
        /// <returns>The weight of the camera.  Valid only if camera is active and enabled.</returns>
        public float GetWeight(int index)
        {
            switch (index)
            {
                case 0: return Weight0;
                case 1: return Weight1;
                case 2: return Weight2;
                case 3: return Weight3;
                case 4: return Weight4;
                case 5: return Weight5;
                case 6: return Weight6;
                case 7: return Weight7;
            }
            Debug.LogError("CinemachineMixingCamera: Invalid index: " + index);
            return 0;
        }

        /// <summary>Set the weight of the child at an index.</summary>
        /// <param name="index">The child index. Only immediate CinemachineVirtualCameraBase
        /// children are counted.</param>
        /// <param name="w">The weight to set.  Can be any non-negative number.</param>
        public void SetWeight(int index, float w)
        {
            switch (index)
            {
                case 0: Weight0 = w; return;
                case 1: Weight1 = w; return;
                case 2: Weight2 = w; return;
                case 3: Weight3 = w; return;
                case 4: Weight4 = w; return;
                case 5: Weight5 = w; return;
                case 6: Weight6 = w; return;
                case 7: Weight7 = w; return;
            }
            Debug.LogError("CinemachineMixingCamera: Invalid index: " + index);
        }

        /// <summary>Get the weight of the child CinemachineVirtualCameraBase.</summary>
        /// <param name="vcam">The child camera.</param>
        /// <returns>The weight of the camera.  Valid only if camera is active and enabled.</returns>
        public float GetWeight(CinemachineVirtualCameraBase vcam)
        {
            UpdateCameraCache();
            if (m_IndexMap.TryGetValue(vcam, out var index))
                return GetWeight(index);
            return 0;
        }

        /// <summary>Set the weight of the child CinemachineVirtualCameraBase.</summary>
        /// <param name="vcam">The child camera.</param>
        /// <param name="w">The weight to set.  Can be any non-negative number.</param>
        public void SetWeight(CinemachineVirtualCameraBase vcam, float w)
        {
            UpdateCameraCache();
            if (m_IndexMap.TryGetValue(vcam, out var index))
                SetWeight(index, w);
            else
                Debug.LogError("CinemachineMixingCamera: Invalid child: "
                    + ((vcam != null) ? vcam.Name : "(null)"));
        }

        /// <inheritdoc />
        public override bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false)
        {
            if (dominantChildOnly)
                return LiveChild == vcam;
            var children = ChildCameras;
            for (int i = 0; i < MaxCameras && i < children.Count; ++i)
                if ((ICinemachineCamera)children[i] == vcam)
                    return GetWeight(i) > UnityVectorExtensions.Epsilon && children[i].isActiveAndEnabled;
            return false;
        }

        /// <inheritdoc />
        protected override bool UpdateCameraCache()
        {
            if (!base.UpdateCameraCache())
                return false;

            m_IndexMap = new Dictionary<CinemachineVirtualCameraBase, int>();
            for (var i = 0; i < ChildCameras.Count; ++i)
                m_IndexMap.Add(ChildCameras[i], i);
            return true;
        }

        /// <inheritdoc />
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            for (int i = 0; i < MaxCameras && i < ChildCameras.Count; ++i)
                ChildCameras[i].OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
        }

        /// <inheritdoc />
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateCameraCache();

            CinemachineVirtualCameraBase liveChild = null;
            var children = ChildCameras;
            float highestWeight = 0;
            float totalWeight = 0;
            for (var i = 0; i < MaxCameras && i < children.Count; ++i)
            {
                var vcam = children[i];
                if (vcam.isActiveAndEnabled)
                {
                    float weight = Mathf.Max(0, GetWeight(i));
                    if (weight > UnityVectorExtensions.Epsilon)
                    {
                        totalWeight += weight;
                        if (totalWeight == weight)
                            m_CameraState = vcam.State;
                        else
                            m_CameraState = CameraState.Lerp(m_CameraState, vcam.State, weight / totalWeight);

                        if (weight > highestWeight)
                        {
                            highestWeight = weight;
                            liveChild = vcam;
                        }
                    }
                }
            }
            m_LiveChildPercent = totalWeight > 0.001f ? (highestWeight * 100 / totalWeight) : 0;
            SetLiveChild(liveChild, worldUp, deltaTime);
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_CameraState, deltaTime);
            PreviousStateIsValid = true;
        }

        /// <inheritdoc />
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime) => null;
    }
}
