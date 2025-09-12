using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Base class for a CinemachineCamera extension module.
    /// Hooks into the Cinemachine Pipeline.  Use this to add extra processing
    /// to the vcam, modifying its generated state
    /// </summary>
    public abstract class CinemachineExtension : MonoBehaviour
    {
        /// <summary>
        /// Extensions that need to save per-vcam state should inherit from this class and add
        /// appropriate member variables.  Use GetExtraState() to access.
        /// </summary>
        protected class VcamExtraStateBase
        {
            /// <summary>The virtual camera being modified by the extension</summary>
            public CinemachineVirtualCameraBase Vcam;
        }

        CinemachineVirtualCameraBase m_VcamOwner;
        Dictionary<CinemachineVirtualCameraBase, VcamExtraStateBase> m_ExtraState;

        /// <summary>Useful constant for very small floats</summary>
        protected const float Epsilon = UnityVectorExtensions.Epsilon;

        /// <summary>Get the CinemachineVirtualCameraBase to which this extension is attached.
        /// This is distinct from the CinemachineCameras that the extension will modify,
        /// as extensions owned by manager cameras will be applied to all the CinemachineCamera children.</summary>
        public CinemachineVirtualCameraBase ComponentOwner
        {
            get
            {
                if (m_VcamOwner == null)
                    TryGetComponent(out m_VcamOwner);
                return m_VcamOwner;
            }
        }

        /// <summary>Connect to virtual camera pipeline.
        /// Override implementations must call this base implementation</summary>
        protected virtual void Awake() => ConnectToVcam(true);

        /// <summary>Disconnect from virtual camera pipeline.
        /// Override implementations must call this base implementation</summary>
        protected virtual void OnDestroy() => ConnectToVcam(false);

        /// <summary>Does nothing.  It's here for the little checkbox in the inspector.</summary>
        protected virtual void OnEnable() {}

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptReload()
        {
            var extensions = Resources.FindObjectsOfTypeAll<CinemachineExtension>();
            // Sort by execution order
            System.Array.Sort(extensions, (x, y) =>
                UnityEditor.MonoImporter.GetExecutionOrder(UnityEditor.MonoScript.FromMonoBehaviour(y))
                    - UnityEditor.MonoImporter.GetExecutionOrder(UnityEditor.MonoScript.FromMonoBehaviour(x)));
            for (int i = 0; i < extensions.Length; ++i)
                extensions[i].ConnectToVcam(true);
        }
#endif
        internal void EnsureStarted() => ConnectToVcam(true);

        /// <summary>Connect to virtual camera.  Implementation must be safe to be called
        /// redundantly.  Override implementations must call this base implementation</summary>
        /// <param name="connect">True if connecting, false if disconnecting</param>
        protected virtual void ConnectToVcam(bool connect)
        {
            if (ComponentOwner != null)
            {
                if (connect)
                    ComponentOwner.AddExtension(this);
                else
                    ComponentOwner.RemoveExtension(this);
            }
            m_ExtraState = null;
        }

        /// <summary>Override this to do such things as offset the ReferenceLookAt.
        /// Base class implementation does nothing.</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="curState">Input state that must be mutated</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        public virtual void PrePipelineMutateCameraStateCallback(
            CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime) {}

        /// <summary>Legacy support.  This is only here to avoid changing the API
        /// to make PostPipelineStageCallback() public</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
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
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected virtual void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime) {}

        /// <summary>This is called to notify the extension that a target got warped,
        /// so that the extension can update its internal state to make the camera
        /// also warp seamlessly.  Base class implementation does nothing.</summary>
        /// <param name="vcam">Virtual camera to warp</param>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public virtual void OnTargetObjectWarped(
            CinemachineVirtualCameraBase vcam, Transform target, Vector3 positionDelta) {}

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation.  This API is obsolete.
        /// Implement ForceCameraPosition(CinemachineVirtualCameraBase vcam, Vector3 pos, Quaternion rot) instead.
        /// </summary>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public virtual void ForceCameraPosition(Vector3 pos, Quaternion rot) {}

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation.
        /// </summary>
        /// <param name="vcam">Virtual camera being warped warp</param>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public virtual void ForceCameraPosition(CinemachineVirtualCameraBase vcam, Vector3 pos, Quaternion rot) {}

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation must be called by any overridden method.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True to request a vcam update of internal state</returns>
        public virtual bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) => false;

        /// <summary>
        /// Report maximum damping time needed for this extension.
        /// Only used in editor for timeline scrubbing.
        /// </summary>
        /// <returns>Highest damping setting in this extension</returns>
        public virtual float GetMaxDampTime() => 0;

        /// <summary>Because extensions can be placed on manager cams and will in that
        /// case be called for all the vcam children, vcam-specific state information
        /// should be stored here.  Just define a class to hold your state info
        /// and use it exclusively when calling this.</summary>
        /// <typeparam name="T">The type of the extra state class</typeparam>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <returns>The extra state, cast as type T</returns>
        protected T GetExtraState<T>(CinemachineVirtualCameraBase vcam) where T : VcamExtraStateBase, new()
        {
            if (m_ExtraState == null)
                m_ExtraState = new ();
            if (!m_ExtraState.TryGetValue(vcam, out var extra))
                extra = m_ExtraState[vcam] = new T { Vcam = vcam};
            return extra as T;
        }

        /// <summary>Get all extra state info for all vcams.</summary>
        /// <typeparam name="T">The extra state type</typeparam>
        /// <param name="list">The list that will get populated with the extra states.</param>
        protected void GetAllExtraStates<T>(List<T> list) where T : VcamExtraStateBase, new()
        {
            list.Clear();
            if (m_ExtraState != null)
            {
                var iter = m_ExtraState.GetEnumerator();
                while (iter.MoveNext())
                    list.Add(iter.Current.Value as T);
            }
        }
    }
}
