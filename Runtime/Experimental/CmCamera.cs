#if CINEMACHINE_EXPERIMENTAL_VCAM
using UnityEngine;
using System;
using System.Linq;

namespace Cinemachine
{
    /// <summary>
    ///
    /// NOTE: THIS CLASS IS EXPERIMENTAL, AND NOT FOR PUBLIC USE
    ///
    /// Lighter-weight version of the CinemachineVirtualCamera.
    ///
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Cinemachine/CmCamera")]
    public class CmCamera : CinemachineVirtualCameraBase
    {
        /// <summary>Object for the camera children to look at (the aim target)</summary>
        [Tooltip("Object for the camera children to look at (the aim target).")]
        [NoSaveDuringPlay]
        [VcamTargetProperty]
        public Transform m_LookAt = null;

        /// <summary>Object for the camera children wants to move with (the body target)</summary>
        [Tooltip("Object for the camera children wants to move with (the body target).")]
        [NoSaveDuringPlay]
        [VcamTargetProperty]
        public Transform m_Follow = null;

        /// <summary>Specifies the LensSettings of this Virtual Camera.
        /// These settings will be transferred to the Unity camera when the vcam is live.</summary>
        [Tooltip("Specifies the lens properties of this Virtual Camera.  This generally mirrors the "
            + "Unity Camera's lens settings, and will be used to drive the Unity camera when the vcam is active.")]
        public LensSettings m_Lens = LensSettings.Default;

        /// <summary> Collection of parameters that influence how this virtual camera transitions from
        /// other virtual cameras </summary>
        public TransitionParams m_Transitions;

        void Reset()
        {
            DestroyComponents();
        }

        /// <summary>Validates the settings avter inspector edit</summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Lens.Validate();
        }

        /// <summary>The camera state, which will be a blend of the child rig states</summary>
        override public CameraState State { get { return m_State; } }

        /// <summary>The camera state, which will be a blend of the child rig states</summary>
        protected CameraState m_State = CameraState.Default;

        /// <summary>Get the current LookAt target.  Returns parent's LookAt if parent
        /// is non-null and no specific LookAt defined for this camera</summary>
        override public Transform LookAt
        {
            get { return ResolveLookAt(m_LookAt); }
            set { m_LookAt = value; }
        }

        /// <summary>Get the current Follow target.  Returns parent's Follow if parent
        /// is non-null and no specific Follow defined for this camera</summary>
        override public Transform Follow
        {
            get { return ResolveFollow(m_Follow); }
            set { m_Follow = value; }
        }

        /// <summary>This is called to notify the vcam that a target got warped,
        /// so that the vcam can update its internal state to make the camera
        /// also warp seamlessy.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            if (target == Follow)
            {
                transform.position += positionDelta;
                m_State.RawPosition += positionDelta;
            }
            
            // TODO: m_Components
            for (int i = 0; i < m_Components.Length; ++i)
            {
                if (m_Components[i] != null)
                    m_Components[i].OnTargetObjectWarped(target, positionDelta);
            }
            base.OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace pposition to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            PreviousStateIsValid = false;
            transform.position = pos;
            transform.rotation = rot;
            m_State.RawPosition = pos;
            m_State.RawOrientation = rot;

            for (int i = 0; i < m_Components.Length; ++i)
                if (m_Components[i] != null)
                    m_Components[i].ForceCameraPosition(pos, rot);

            base.ForceCameraPosition(pos, rot);
        }
        
        /// <summary>
        /// Query components and extensions for the maximum damping time.
        /// </summary>
        /// <returns>Highest damping setting in this vcam</returns>
        public override float GetMaxDampTime()
        {
            float maxDamp = base.GetMaxDampTime();
            
            for (int i = 0; i < m_Components.Length; ++i)
                if (m_Components[i] != null)
                    maxDamp = Mathf.Max(maxDamp, m_Components[i].GetMaxDampTime());
            return maxDamp;
        }

        /// <summary>If we are transitioning from another FreeLook, grab the axis values from it.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            InvokeOnTransitionInExtensions(fromCam, worldUp, deltaTime);
            bool forceUpdate = false;
            if (m_Transitions.m_InheritPosition && fromCam != null  
                && !CinemachineCore.Instance.IsLiveInBlend(this))
            {
                ForceCameraPosition(fromCam.State.FinalPosition, fromCam.State.FinalOrientation);
            }
            
            for (int i = 0; i < m_Components.Length; ++i)
            {
                if (m_Components[i] != null
                        && m_Components[i].OnTransitionFromCamera(
                            fromCam, worldUp, deltaTime, ref m_Transitions))
                    forceUpdate = true;
            }
            if (forceUpdate)
            {
                InternalUpdateCameraState(worldUp, deltaTime);
                InternalUpdateCameraState(worldUp, deltaTime);
            }
            else
                UpdateCameraState(worldUp, deltaTime);
            if (m_Transitions.m_OnCameraLive != null)
                m_Transitions.m_OnCameraLive.Invoke(this, fromCam);
        }

        /// <summary>Internal use only.  Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  All 3 child rigs are updated,
        /// and a blend calculated, depending on the value of the Y axis.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateTargetCache();

            FollowTargetAttachment = 1;
            LookAtTargetAttachment = 1;

            // Initialize the camera state, in case the game object got moved in the editor
            m_State = PullStateFromVirtualCamera(worldUp, ref m_Lens);

            // Do our stuff
            SetReferenceLookAtTargetInState(ref m_State);
            InvokeComponentPipeline(ref m_State, worldUp, deltaTime);
            ApplyPositionBlendMethod(ref m_State, m_Transitions.m_BlendHint);

            // Push the raw position back to the game object's transform, so it
            // moves along with the camera.
            if (Follow != null)
                transform.position = State.RawPosition;
            if (LookAt != null)
                transform.rotation = State.RawOrientation;
            
            // Signal that it's all done
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
            PreviousStateIsValid = true;
        }
        
        /// <summary>
        /// Returns true, when the vcam has extensions or components that require input.
        /// </summary>
        internal override bool RequiresUserInput()
        {
            return base.RequiresUserInput() ||
                m_Components != null && m_Components.Any(t => t != null && t.RequiresUserInput);
        }

        Transform mCachedLookAtTarget;
        CinemachineVirtualCameraBase mCachedLookAtTargetVcam;

        /// <summary>Set the state's refeenceLookAt target to our lookAt, with some smarts
        /// in case our LookAt points to a vcam</summary>
        protected void SetReferenceLookAtTargetInState(ref CameraState state)
        {
            Transform lookAtTarget = LookAt;
            if (lookAtTarget != mCachedLookAtTarget)
            {
                mCachedLookAtTarget = lookAtTarget;
                mCachedLookAtTargetVcam = null;
                if (lookAtTarget != null)
                    mCachedLookAtTargetVcam = lookAtTarget.GetComponent<CinemachineVirtualCameraBase>();
            }
            if (lookAtTarget != null)
            {
                if (mCachedLookAtTargetVcam != null)
                    state.ReferenceLookAt = mCachedLookAtTargetVcam.State.FinalPosition;
                else
                    state.ReferenceLookAt = TargetPositionCache.GetTargetPosition(lookAtTarget);
            }
        }

        protected CameraState InvokeComponentPipeline(
            ref CameraState state, Vector3 worldUp, float deltaTime)
        {
            // TODO: Try to get "m_Components" every frame from all components. 
            // TODO: Performance is critical -> find fastest way, performance test to ensure it is really performant. No allocs.

            // Extensions first
            InvokePrePipelineMutateCameraStateCallback(this, ref state, deltaTime);

            // Apply the component pipeline
            for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body;
                stage <= CinemachineCore.Stage.Finalize; ++stage)
            {
                var c = m_Components[(int)stage];
                if (c != null)
                    c.PrePipelineMutateCameraState(ref state, deltaTime);
            }
            CinemachineComponentBase postAimBody = null;
            for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body;
                stage <= CinemachineCore.Stage.Finalize; ++stage)
            {
                var c = m_Components[(int)stage];
                if (c != null)
                {
                    if (stage == CinemachineCore.Stage.Body && c.BodyAppliesAfterAim)
                    {
                        postAimBody = c;
                        continue; // do the body stage of the pipeline after Aim
                    }
                    c.MutateCameraState(ref state, deltaTime);
                }
                InvokePostPipelineStageCallback(this, stage, ref state, deltaTime);
                if (stage == CinemachineCore.Stage.Aim)
                {
                    if (c == null)
                        state.BlendHint |= CameraState.BlendHintValue.IgnoreLookAtTarget; // no aim
                     // If we have saved a Body for after Aim, do it now
                    if (postAimBody != null)
                    {
                        postAimBody.MutateCameraState(ref state, deltaTime);
                        InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Body, ref state, deltaTime);
                    }
                }
            }

            return state;
        }

        // TODO: No need for m_Components cache and all the related stuff like (OnComponentCacheUpdated, Gets, Adds, destroys)
        // TODO: however, we may want to store it for the frame, because a lot of things may use it same frame? depends on cost of getting the most up-to-date from components
        [NoSaveDuringPlay]
        internal CinemachineComponentBase[] m_Components = new CinemachineComponentBase[(int)CinemachineCore.Stage.Finalize + 1];

        public override void UpdateComponentCache()
        {
            var components = GetComponents<CinemachineComponentBase>();
            for (int i = 0; i < components.Length; ++i)
            {
                var stage = (int) components[i].Stage;
                if (m_Components[stage] != (components[i]))
                {
                    if (m_Components[stage] != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(m_Components[stage]);
#else
                        Destroy(m_Components[stage]);
#endif
                    }

                    m_Components[stage] = components[i];
                }
            }
        }
        
        /// <summary>Notification that the component cache has just been update,
        /// in case a subclass needs to do something extra</summary>
        protected virtual void OnComponentCacheUpdated() {}

        /// <summary>Get the component set for a specific stage.</summary>
        /// <param name="stage">The stage for which we want the component</param>
        /// <returns>The Cinemachine component for that stage, or null if not defined</returns>
        public CinemachineComponentBase GetCinemachineComponent(CinemachineCore.Stage stage)
        {
            var i = (int)stage;
            return i >= 0 && i < m_Components.Length ? m_Components[i] : null;
        }

        /// <summary>Get an existing component of a specific type from the cinemachine pipeline.</summary>
        public T GetCinemachineComponent<T>() where T : CinemachineComponentBase => 
            m_Components.OfType<T>().Select(c => c).FirstOrDefault();

        /// <summary>Destroy all the CinmachineComponentBase components</summary>
        protected void DestroyComponents()
        {
            for (int i = 0; i < m_Components.Length; ++i)
            {
#if UNITY_EDITOR
                DestroyImmediate(m_Components[i]);
#else
                Destroy(m_Components[i]);
#endif
                m_Components[i] = null;
            }
            UpdateComponentCache();
        }
    }
}
#endif
