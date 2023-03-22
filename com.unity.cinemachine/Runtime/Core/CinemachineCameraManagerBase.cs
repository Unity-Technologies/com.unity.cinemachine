using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a virtual camera "manager" that owns and manages a collection
    /// of child Cm Cameras.
    /// </summary>
    public abstract class CinemachineCameraManagerBase : CinemachineVirtualCameraBase, ICinemachineMixer
    {
        /// <summary>If enabled, a default target will be available.  It will be used
        /// if a child rig needs a target and doesn't specify one itself.</summary>
        [Serializable]
        public struct DefaultTargetSettings
        {
            /// <summary>If enabled, a default target will be available.  It will be used
            /// if a child rig needs a target and doesn't specify one itself.</summary>
            [Tooltip("If enabled, a default target will be available.  It will be used "
                + "if a child rig needs a target and doesn't specify one itself.")]
            public bool Enabled;

            /// <summary>Default target for the camera children, which may be used if the child rig 
            /// does not specify a target of its own.</summary>
            [NoSaveDuringPlay]
            [Tooltip("Default target for the camera children, which may be used if the child rig "
                + "does not specify a target of its own.")]
            public CameraTarget Target;
        }        

        /// <summary>If enabled, a default target will be available.  It will be used
        /// if a child rig needs a target and doesn't specify one itself.</summary>
        [FoldoutWithEnabledButton]
        public DefaultTargetSettings DefaultTarget;

        /// State for CreateActiveBlend().  Used in the case of backing out of a blend in progress.
        List<CinemachineVirtualCameraBase> m_ChildCameras;
        readonly BlendManager m_BlendManager = new ();
        CameraState m_State = CameraState.Default;

        /// <summary>Reset the component to default values.</summary>
        protected virtual void Reset()
        {
            Priority = default;
            OutputChannel = OutputChannel.Default;
            DefaultTarget = default;
            InvalidateCameraCache();
        }
        
        /// <summary>
        /// Standard MonoBehaviour OnEnable.  Derived classes must call base class implementation.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            m_BlendManager.OnEnable();
            InvalidateCameraCache();
        }

        /// <summary>
        /// Standard MonoBehaviour OnDisable.  Derived classes must call base class implementation.
        /// </summary>
        protected override void OnDisable()
        {
            m_BlendManager.OnDisable();
            base.OnDisable();
        }

        /// <summary>Gets a brief debug description of this virtual camera, for use when displaying debug info</summary>
        public override string Description => m_BlendManager.Description;

        /// <summary>The resulting CameraState for the current live child and blend</summary>
        public override CameraState State => m_State;

        /// <summary>Check whether the vcam a live child of this camera.</summary>
        /// <param name="cam">The Virtual Camera to check</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public override bool IsLiveChild(ICinemachineCamera cam, bool dominantChildOnly = false)
            => m_BlendManager.IsLive(cam, dominantChildOnly);

        /// <summary>The list of child cameras.  These are just the immediate children in the hierarchy.</summary>
        public List<CinemachineVirtualCameraBase> ChildCameras 
        { 
            get 
            { 
                UpdateCameraCache(); 
                return m_ChildCameras; 
            }
        }

        /// <summary>Is there a blend in progress?</summary>
        public bool IsBlending => m_BlendManager.IsBlending;

        /// <summary>
        /// Get the current active blend in progress.  Will return null if no blend is in progress.
        /// </summary>
        public CinemachineBlend ActiveBlend => PreviousStateIsValid ? m_BlendManager.ActiveBlend : null;

        /// <summary>
        /// Get the current active camera.  Will return null if no camera is active.
        /// </summary>
        public ICinemachineCamera LiveChild => PreviousStateIsValid ? m_BlendManager.ActiveVirtualCamera : null;

        /// <summary>Get the current LookAt target.  Returns parent's LookAt if parent
        /// is non-null and no specific LookAt defined for this camera</summary>
        public override Transform LookAt
        {
            get 
            {
                if (!DefaultTarget.Enabled)
                    return null;
                return ResolveLookAt(DefaultTarget.Target.CustomLookAtTarget 
                    ? DefaultTarget.Target.LookAtTarget : DefaultTarget.Target.TrackingTarget); 
            }
            set 
            {
                DefaultTarget.Enabled = true;
                DefaultTarget.Target.CustomLookAtTarget = true; 
                DefaultTarget.Target.LookAtTarget = value; 
            }
        }

        /// <summary>Get the current Follow target.  Returns parent's Follow if parent
        /// is non-null and no specific Follow defined for this camera</summary>
        public override Transform Follow
        {
            get 
            { 
                if (!DefaultTarget.Enabled)
                    return null;
                return ResolveFollow(DefaultTarget.Target.TrackingTarget); 
            }
            set 
            { 
                DefaultTarget.Enabled = true;
                DefaultTarget.Target.TrackingTarget = value; 
            }
        }

        /// <summary>This is called to notify the vcam that a target got warped,
        /// so that the vcam can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            UpdateCameraCache();
            foreach (var vcam in m_ChildCameras)
                vcam.OnTargetObjectWarped(target, positionDelta);
            base.OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            UpdateCameraCache();
            foreach (var vcam in m_ChildCameras)
                vcam.ForceCameraPosition(pos, rot);
            base.ForceCameraPosition(pos, rot);
        }

        /// <summary>Notification that this virtual camera is going live.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            InvokeOnTransitionInExtensions(fromCam, worldUp, deltaTime);
        }

        /// <summary>Force a rebuild of the child camera cache.  
        /// Call this if CinemachineCamera children are added or removed dynamically</summary>
        public void InvalidateCameraCache() 
        {
            m_ChildCameras = null;
            PreviousStateIsValid = false;
        }

        /// <summary>Rebuild the camera cache if it's been invalidated</summary>
        /// <returns>True if a cache rebuild was performed, false if cache is up to date.</returns>
        protected virtual bool UpdateCameraCache()
        {
            if (m_ChildCameras != null)
                return false;
            PreviousStateIsValid = false;
            m_ChildCameras = new();
            GetComponentsInChildren(m_ChildCameras);
            for (int i = m_ChildCameras.Count-1; i >= 0; --i)
                if (m_ChildCameras[i].transform.parent != transform)
                    m_ChildCameras.RemoveAt(i);
            return true;
        }

        /// <summary>Makes sure the internal child cache is up to date</summary>
        protected virtual void OnTransformChildrenChanged() => InvalidateCameraCache();

        /// <summary>
        /// Set the current active camera.  All necessary blends will be created, and events generated.
        /// </summary>
        /// <param name="activeCamera">Current active camera</param>
        /// <param name="worldUp">Current world up</param>
        /// <param name="deltaTime">Current deltaTime applicable for this frame</param>
        /// <param name="lookupBlend">Delegate to use to find a blend definition, when a blend is being created</param>
        protected void SetLiveChild(
            ICinemachineCamera activeCamera, Vector3 worldUp, float deltaTime,
            CinemachineBlendDefinition.LookupBlendDelegate lookupBlend)
        {
            m_BlendManager.UpdateRootFrame(activeCamera, deltaTime, lookupBlend);
            m_BlendManager.ComputeCurrentBlend();
            m_BlendManager.ProcessActiveCamera(this, worldUp, deltaTime);
        }

        /// <summary>Cancel current active camera and all blends</summary>
        protected void ResetLiveChild() => m_BlendManager.ResetRootFrame();

        /// <summary>At the end of InternalUpdateCameraState, call this to finalize the state</summary>
        /// <param name="deltaTime">Current deltaTime for this frame</param>
        protected void FinalizeCameraState(float deltaTime)
        {
            m_State = m_BlendManager.CameraState;
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
        }
    }
}
