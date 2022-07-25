using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// This is a virtual camera "manager" that owns and manages a collection
    /// of child Cm Cameras.
    /// </summary>
    public abstract class CinemachineCameraManagerBase : CinemachineVirtualCameraBase
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

        /// <summary>When enabled, the current camera and blend will be indicated in the game window, for debugging</summary>
        [Tooltip("When enabled, the current child camera and blend will be indicated in the game window, for debugging")]
        [FormerlySerializedAs("m_ShowDebugText")]
        public bool ShowDebugText;

        /// <summary>
        /// For the inspector ONLY.  Does not really need to be serialized other than for the inspector.
        /// GML todo: make this go away
        /// </summary>
        [SerializeField, HideInInspector, NoSaveDuringPlay] internal List<CinemachineVirtualCameraBase> m_ChildCameras;

        protected virtual void Reset()
        {
            DefaultTarget = default;
            ShowDebugText = false;
            InvalidateCameraCache();
        }
        
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
        public bool IsBlending => ActiveBlend != null;

        /// <summary>
        /// Returns the camera that is currently live.  If a blend is in progress, then the
        /// incoming camera is considered to be the live child.
        /// </summary>
        public abstract ICinemachineCamera LiveChild { get; }

        /// <summary>
        /// Get the current active blend in progress.  Will return null if no blend is in progress.
        /// </summary>
        public abstract CinemachineBlend ActiveBlend { get; }

        /// <summary>Gets a brief debug description of this virtual camera, for use when displaying debug info</summary>
        public override string Description
        {
            get
            {
                // Show the active camera and blend
                if (ActiveBlend != null)
                    return ActiveBlend.Description;

                ICinemachineCamera vcam = LiveChild;
                if (vcam == null)
                    return "(none)";
                var sb = CinemachineDebug.SBFromPool();
                sb.Append("["); sb.Append(vcam.Name); sb.Append("]");
                string text = sb.ToString();
                CinemachineDebug.ReturnToPool(sb);
                return text;
            }
        }

        /// <summary>Check whether the vcam a live child of this camera.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public override bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false)
        {
            return vcam == LiveChild || (ActiveBlend != null && ActiveBlend.Uses(vcam));
        }

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
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
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
        /// Call this if CmCamera children are added or removed dynamically</summary>
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
        protected override void OnEnable()
        {
            base.OnEnable();
            InvalidateCameraCache();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;
        }

        /// <summary>
        /// Uninstall the GUI handler
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
        }

        /// <summary>Makes sure the internal child cache is up to date</summary>
        protected virtual void OnTransformChildrenChanged()
        {
            InvalidateCameraCache();
        }

        /// <summary>
        /// Will only be called in Unity Editor - never in build.  Show debugging info in the game view.
        /// </summary>
        protected virtual void OnGuiHandler()
        {
#if CINEMACHINE_UNITY_IMGUI
            if (!ShowDebugText)
                CinemachineDebug.ReleaseScreenPos(this);
            else
            {
                var sb = CinemachineDebug.SBFromPool();
                sb.Append(Name); sb.Append(": "); sb.Append(Description);
                var text = sb.ToString();
                Rect r = CinemachineDebug.GetScreenPos(this, text, GUI.skin.box);
                GUI.Label(r, text, GUI.skin.box);
                CinemachineDebug.ReturnToPool(sb);
            }
#endif
        }
    }
}
