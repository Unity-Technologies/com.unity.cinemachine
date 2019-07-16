using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Base class for a Cinemachine Virtual Camera extension module.
    /// Hooks into the Cinemachine Pipeline.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.API)]
    public abstract class CinemachineExtension : MonoBehaviour
    {
        /// <summary>Useful constant for very small floats</summary>
        protected const float Epsilon = Utility.UnityVectorExtensions.Epsilon;

        /// <summary>Get the associated CinemachineVirtualCameraBase</summary>
        public CinemachineVirtualCameraBase VirtualCamera
        {
            get
            {
                if (m_vcamOwner == null)
                    m_vcamOwner = GetComponent<CinemachineVirtualCameraBase>();
                return m_vcamOwner;
            }
        }
        CinemachineVirtualCameraBase m_vcamOwner;

        /// <summary>Connect to virtual camera pipeline.
        /// Override implementations must call this base implementation</summary>
        protected virtual void Awake()
        {
            ConnectToVcam(true);
        }

#if UNITY_EDITOR
        /// <summary>Does nothing.  This is only here so we get the little "enabled"
        /// checkbox in the inspector</summary>
        void Update() {}

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptReload()
        {
            var extensions = Resources.FindObjectsOfTypeAll(
                typeof(CinemachineExtension)) as CinemachineExtension[];
            foreach (var e in extensions)
                e.ConnectToVcam(true);
        }
#endif
        /// <summary>Disconnect from virtual camera pipeline.
        /// Override implementations must call this base implementation</summary>
        protected virtual void OnDestroy()
        {
            ConnectToVcam(false);
        }

        /// <summary>Connect to virtual camera.  Implementation must be safe to be called
        /// redundantly.  Override implementations must call this base implementation</summary>
        /// <param name="connect">True if connectinf, false if disconnecting</param>
        protected virtual void ConnectToVcam(bool connect)
        {
            if (connect && VirtualCamera == null)
                Debug.LogError("CinemachineExtension requires a Cinemachine Virtual Camera component");
            if (VirtualCamera != null)
            {
                if (connect)
                    VirtualCamera.AddExtension(this);
                else
                    VirtualCamera.RemoveExtension(this);
            }
            mExtraState = null;
        }

        /// <summary>Legacy support.  This is only here to avoid changing the API
        /// to make PostPipelineStageCallback() public</summary>
        public void InvokePostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            PostPipelineStageCallback(vcam, stage, ref state, deltaTime);
        }

        /// <summary>
        /// This callback will be called after the virtual camera has implemented
        /// each stage in the pipeline.  This method may modify the referenced state.
        /// If deltaTime less than 0, reset all state info and perform no damping.
        /// </summary>
        protected abstract void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime);

        /// <summary>This is called to notify the extension that a target got warped,
        /// so that the extension can update its internal state to make the camera
        /// also warp seamlessy.  Base class implementation does nothing.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public virtual void OnTargetObjectWarped(Transform target, Vector3 positionDelta) {}

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation must be called by any overridden method.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True to request a vcam update of internal state</returns>
        public virtual bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) { return false; }

        /// <summary>Because extensions can be placed on manager cams and will in that
        /// case be called for all the vcam children, vcam-specific state information
        /// should be stored here.  Just define a class to hold your state info
        /// and use it exclusively when calling this.</summary>
        protected T GetExtraState<T>(ICinemachineCamera vcam) where T : class, new()
        {
            if (mExtraState == null)
                mExtraState = new Dictionary<ICinemachineCamera, System.Object>();
            System.Object extra = null;
            if (!mExtraState.TryGetValue(vcam, out extra))
                extra = mExtraState[vcam] = new T();
            return extra as T;
        }

        /// <summary>Ineffeicient method to get all extra state infor for all vcams.
        /// Intended for Editor use only, not runtime!
        /// </summary>
        protected List<T> GetAllExtraStates<T>() where T : class, new()
        {
            var list = new List<T>();
            if (mExtraState != null)
                foreach (var v in mExtraState)
                    list.Add(v.Value as T);
            return list;
        }

        private Dictionary<ICinemachineCamera, System.Object> mExtraState;
    }
}
