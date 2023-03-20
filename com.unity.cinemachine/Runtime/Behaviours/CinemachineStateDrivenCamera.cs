using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
#if CINEMACHINE_UNITY_ANIMATION
    /// <summary>
    /// This is a virtual camera "manager" that owns and manages a collection
    /// of child Virtual Cameras.  These child vcams are mapped to individual states in
    /// an animation state machine, allowing you to associate specific vcams to specific
    /// animation states.  When that state is active in the state machine, then the
    /// associated camera will be activated.
    ///
    /// You can define custom blends and transitions between child cameras.
    ///
    /// In order to use this behaviour, you must have an animated target (i.e. an object
    /// animated with a state machine) to drive the behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [ExcludeFromPreset]
    [AddComponentMenu("Cinemachine/Cinemachine State Driven Camera")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineStateDrivenCamera.html")]
    public class CinemachineStateDrivenCamera : CinemachineCameraManagerBase
    {
        /// <summary>The state machine whose state changes will drive this camera's choice of active child</summary>
        [Space]
        [Tooltip("The state machine whose state changes will drive this camera's choice of active child")]
        [NoSaveDuringPlay]
        [FormerlySerializedAs("m_AnimatedTarget")]
        public Animator AnimatedTarget;

        /// <summary>Which layer in the target FSM to observe</summary>
        [Tooltip("Which layer in the target state machine to observe")]
        [NoSaveDuringPlay]
        [FormerlySerializedAs("m_LayerIndex")]
        public int LayerIndex;

        /// <summary>This represents a single instruction to the StateDrivenCamera.  It associates
        /// an state from the state machine with a child Virtual Camera, and also holds
        /// activation tuning parameters.</summary>
        [Serializable]
        public struct Instruction
        {
            /// <summary>The full hash of the animation state</summary>
            [Tooltip("The full hash of the animation state")]
            [FormerlySerializedAs("m_FullHash")]
            public int FullHash;
            /// <summary>The virtual camera to activate when the animation state becomes active</summary>
            [Tooltip("The virtual camera to activate when the animation state becomes active")]
            [FormerlySerializedAs("m_VirtualCamera")]
            public CinemachineVirtualCameraBase Camera;
            /// <summary>How long to wait (in seconds) before activating the virtual camera.
            /// This filters out very short state durations</summary>
            [Tooltip("How long to wait (in seconds) before activating the virtual camera. "
                + "This filters out very short state durations")]
            [FormerlySerializedAs("m_ActivateAfter")]
            public float ActivateAfter;
            /// <summary>The minimum length of time (in seconds) to keep a virtual camera active</summary>
            [Tooltip("The minimum length of time (in seconds) to keep a virtual camera active")]
            [FormerlySerializedAs("m_MinDuration")]
            public float MinDuration;
        };

        /// <summary>The set of instructions associating virtual cameras with states.
        /// These instructions are used to choose the live child at any given moment</summary>
        [Tooltip("The set of instructions associating virtual cameras with states.  "
        + "These instructions are used to choose the live child at any given moment")]
        [FormerlySerializedAs("m_Instructions")]
        public Instruction[] Instructions;

        /// <summary>
        /// The blend which is used if you don't explicitly define a blend between two Virtual Camera children.
        /// </summary>
        [Tooltip("The blend which is used if you don't explicitly define a blend between two Virtual Camera children")]
        [FormerlySerializedAs("m_DefaultBlend")]
        public CinemachineBlendDefinition DefaultBlend = new (CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);

        /// <summary>
        /// This is the asset which contains custom settings for specific child blends.
        /// </summary>
        [Tooltip("This is the asset which contains custom settings for specific child blends")]
        [FormerlySerializedAs("m_CustomBlends")]
        public CinemachineBlenderSettings CustomBlends = null;

        /// <summary>Internal API for the Inspector editor.  This implements nested states.</summary>
        [Serializable]
        internal struct ParentHash
        {
            /// <summary>Internal API for the Inspector editor</summary>
            public int Hash;
            /// <summary>Internal API for the Inspector editor</summary>
            public int HashOfParent;
        }
        /// <summary>Internal API for the Inspector editor</summary>
        [HideInInspector][SerializeField] internal ParentHash[] HashOfParent = null;

        [SerializeField, HideInInspector, FormerlySerializedAs("m_LookAt")] Transform m_LegacyLookAt;
        [SerializeField, HideInInspector, FormerlySerializedAs("m_Follow")] Transform m_LegacyFollow;

        CinemachineVirtualCameraBase m_LiveChild;
        CinemachineBlend m_ActiveBlend;
        ICinemachineCamera m_TransitioningFrom;
        CameraState m_State = CameraState.Default;

        float m_ActivationTime = 0;
        Instruction m_ActiveInstruction;
        float m_PendingActivationTime = 0;
        Instruction m_PendingInstruction;
        Dictionary<int, int> m_InstructionDictionary;
        Dictionary<int, int> m_StateParentLookup;
        List<AnimatorClipInfo>  m_clipInfoList = new List<AnimatorClipInfo>();

        protected override void Reset()
        {
            base.Reset();
            Instructions = null;
            AnimatedTarget = null;
            LayerIndex = 0;
            Instructions = null;
            DefaultBlend = new (CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);
            CustomBlends = null;
        }

        protected internal override void PerformLegacyUpgrade(int streamedVersion)
        {
            base.PerformLegacyUpgrade(streamedVersion);
            if (streamedVersion < 20220721)
            {
                DefaultTarget = new DefaultTargetSettings 
                { 
                    Enabled = true,
                    Target = new CameraTarget
                    {
                        LookAtTarget = m_LegacyLookAt, 
                        TrackingTarget = m_LegacyFollow, 
                        CustomLookAtTarget = m_LegacyLookAt != m_LegacyFollow 
                    }
                };
                m_LegacyLookAt = m_LegacyFollow = null;
            }
        }

        /// <summary>Get the current "best" child virtual camera, that would be chosen
        /// if the State Driven Camera were active.</summary>
        public override ICinemachineCamera LiveChild => PreviousStateIsValid ? m_LiveChild : null;

        /// <summary>
        /// Get the current active blend in progress.  Will return null if no blend is in progress.
        /// </summary>
        public override CinemachineBlend ActiveBlend => PreviousStateIsValid ? m_ActiveBlend : null;

        /// <summary>The State of the current live child</summary>
        public override CameraState State => m_State;

        /// <summary>Notification that this virtual camera is going live.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            m_TransitioningFrom  = fromCam;
            InternalUpdateCameraState(worldUp, deltaTime);
        }

        /// <summary>Internal use only.  Do not call this method.
        /// Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  This implementation
        /// updates all the children, chooses the best one, and implements any required blending.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateCameraCache();
            if (!PreviousStateIsValid)
            {
                m_ActiveBlend = null;
                m_LiveChild = null;
                ValidateInstructions();
            }

            var best = ChooseCurrentCamera();
            if (best != null && !best.gameObject.activeInHierarchy)
            {
                best.gameObject.SetActive(true);
                best.UpdateCameraState(worldUp, deltaTime);
            }

            var previousCam = LiveChild;
            m_LiveChild = best;

            // Are we transitioning cameras?
            if (previousCam != LiveChild && LiveChild != null)
            {
                // Notify incoming camera of transition
                LiveChild.OnTransitionFromCamera(previousCam, worldUp, deltaTime);

                // Generate Camera Activation event in the brain if live
                CinemachineCore.Instance.GenerateCameraActivationEvent(LiveChild, previousCam);

                if (previousCam != null)
                {
                    // Create a blend (will be null if a cut)
                    m_ActiveBlend = CreateActiveBlend(previousCam, LiveChild, LookupBlend(previousCam, LiveChild));

                    // If cutting, generate a camera cut event if live
                    if (m_ActiveBlend == null || !m_ActiveBlend.Uses(previousCam))
                        CinemachineCore.Instance.GenerateCameraCutEvent(LiveChild);
                }
            }

            // Advance the current blend (if any)
            if (m_ActiveBlend != null)
            {
                m_ActiveBlend.TimeInBlend += (deltaTime >= 0)
                    ? deltaTime : m_ActiveBlend.Duration;
                if (m_ActiveBlend.IsComplete)
                    m_ActiveBlend = null;
            }

            if (m_ActiveBlend != null)
            {
                m_ActiveBlend.UpdateCameraState(worldUp, deltaTime);
                m_State = m_ActiveBlend.State;
            }
            else if (LiveChild != null)
            {
                if (m_TransitioningFrom  != null)
                    LiveChild.OnTransitionFromCamera(m_TransitioningFrom , worldUp, deltaTime);
                m_State =  LiveChild.State;
            }
            m_TransitioningFrom  = null;
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
            PreviousStateIsValid = true;
        }

        /// <summary>API for the inspector editor.  Animation module does not have hashes
        /// for state parents, so we have to invent them in order to implement nested state
        /// handling</summary>
        /// <param name="parentHash">Parent state's hash</param>
        /// <param name="clip">The clip to create the fake hash for</param>
        /// <returns>The fake hash</returns>
        internal static int CreateFakeHash(int parentHash, AnimationClip clip)
        {
            return Animator.StringToHash(parentHash.ToString() + "_" + clip.name);
        }

        // Avoid garbage string manipulations at runtime
        struct HashPair { public int parentHash; public int hash; }
        Dictionary<AnimationClip, List<HashPair>> m_HashCache;
        int LookupFakeHash(int parentHash, AnimationClip clip)
        {
            if (m_HashCache == null)
                m_HashCache = new Dictionary<AnimationClip, List<HashPair>>();
            if (!m_HashCache.TryGetValue(clip, out var list))
            {
                list = new List<HashPair>();
                m_HashCache[clip] = list;
            }
            for (int i = 0; i < list.Count; ++i)
                if (list[i].parentHash == parentHash)
                    return list[i].hash;
            int newHash = CreateFakeHash(parentHash, clip);
            list.Add(new HashPair() { parentHash = parentHash, hash = newHash });
            m_StateParentLookup[newHash] = parentHash;
            return newHash;
        }

        /// <summary>Internal API for the inspector editor.</summary>
        internal void ValidateInstructions()
        {
            if (Instructions == null)
                Instructions = Array.Empty<Instruction>();
            m_InstructionDictionary = new Dictionary<int, int>();
            for (int i = 0; i < Instructions.Length; ++i)
            {
                if (Instructions[i].Camera != null
                    && Instructions[i].Camera.transform.parent != transform)
                {
                    Instructions[i].Camera = null;
                }
                m_InstructionDictionary[Instructions[i].FullHash] = i;
            }

            // Create the parent lookup
            m_StateParentLookup = new Dictionary<int, int>();
            if (HashOfParent != null)
                foreach (var i in HashOfParent)
                    m_StateParentLookup[i.Hash] = i.HashOfParent;

            // Zap the cached current instructions
            m_HashCache = null;
            m_ActivationTime = m_PendingActivationTime = 0;
            m_ActiveBlend = null;
        }

        CinemachineVirtualCameraBase ChooseCurrentCamera()
        {
            var children = ChildCameras;
            if (children == null || children.Count == 0)
            {
                m_ActivationTime = 0;
                return null;
            }
            var defaultCam = children[0];
            if (AnimatedTarget == null || !AnimatedTarget.gameObject.activeSelf
                || AnimatedTarget.runtimeAnimatorController == null
                || LayerIndex < 0 || !AnimatedTarget.hasBoundPlayables
                || LayerIndex >= AnimatedTarget.layerCount)
            {
                m_ActivationTime = 0;
                return defaultCam;
            }

            // Get the current state
            int hash;
            if (AnimatedTarget.IsInTransition(LayerIndex))
            {
                // Force "current" state to be the state we're transitioning to
                var info = AnimatedTarget.GetNextAnimatorStateInfo(LayerIndex);
                AnimatedTarget.GetNextAnimatorClipInfo(LayerIndex, m_clipInfoList);
                hash = GetClipHash(info.fullPathHash, m_clipInfoList);
            }
            else
            {
                var info = AnimatedTarget.GetCurrentAnimatorStateInfo(LayerIndex);
                AnimatedTarget.GetCurrentAnimatorClipInfo(LayerIndex, m_clipInfoList);
                hash = GetClipHash(info.fullPathHash, m_clipInfoList);
            }

            // If we don't have an instruction for this state, find a suitable default
            while (hash != 0 && !m_InstructionDictionary.ContainsKey(hash))
                hash = m_StateParentLookup.ContainsKey(hash) ? m_StateParentLookup[hash] : 0;

            var now = CinemachineCore.CurrentTime;
            if (m_ActivationTime != 0)
            {
                // Is it active now?
                if (m_ActiveInstruction.FullHash == hash)
                {
                    // Yes, cancel any pending
                    m_PendingActivationTime = 0;
                    return m_ActiveInstruction.Camera;
                }

                // Is it pending?
                if (PreviousStateIsValid)
                {
                    if (m_PendingActivationTime != 0 && m_PendingInstruction.FullHash == hash)
                    {
                        // Has it been pending long enough, and are we allowed to switch away
                        // from the active action?
                        if ((now - m_PendingActivationTime) > m_PendingInstruction.ActivateAfter
                            && ((now - m_ActivationTime) > m_ActiveInstruction.MinDuration
                                || m_PendingInstruction.Camera.Priority.Value > m_ActiveInstruction.Camera.Priority.Value))
                        {
                            // Yes, activate it now
                            m_ActiveInstruction = m_PendingInstruction;
                            m_ActivationTime = now;
                            m_PendingActivationTime = 0;
                        }
                        return m_ActiveInstruction.Camera;
                    }
                }
            }
            // Neither active nor pending.
            m_PendingActivationTime = 0; // cancel the pending, if any

            if (!m_InstructionDictionary.ContainsKey(hash))
            {
                // No defaults set, we just ignore this state
                if (m_ActivationTime != 0)
                    return m_ActiveInstruction.Camera;
                return defaultCam;
            }

            // Can we activate it now?
            var newInstr = Instructions[m_InstructionDictionary[hash]];
            if (newInstr.Camera == null)
                newInstr.Camera = defaultCam;
            if (PreviousStateIsValid && m_ActivationTime > 0)
            {
                if (newInstr.ActivateAfter > 0
                    || ((now - m_ActivationTime) < m_ActiveInstruction.MinDuration
                        && newInstr.Camera.Priority.Value
                        <= m_ActiveInstruction.Camera.Priority.Value))
                {
                    // Too early - make it pending
                    m_PendingInstruction = newInstr;
                    m_PendingActivationTime = now;
                    if (m_ActivationTime != 0)
                        return m_ActiveInstruction.Camera;
                    return defaultCam;
                }
            }
            // Activate now
            m_ActiveInstruction = newInstr;
            m_ActivationTime = now;
            return m_ActiveInstruction.Camera;
        }

        int GetClipHash(int hash, List<AnimatorClipInfo> clips)
        {
            // Find the strongest-weighted animation clip sub-state
            var bestClip = -1;
            for (var i = 0; i < clips.Count; ++i)
                if (bestClip < 0 || clips[i].weight > clips[bestClip].weight)
                    bestClip = i;

            // Use its hash
            if (bestClip >= 0 && clips[bestClip].weight > 0)
                hash = LookupFakeHash(hash, clips[bestClip].clip);

            return hash;
        }

        CinemachineBlendDefinition LookupBlend(
            ICinemachineCamera fromKey, ICinemachineCamera toKey)
        {
            // Get the blend curve that's most appropriate for these cameras
            var blend = DefaultBlend;
            if (CustomBlends != null)
            {
                var fromCameraName = (fromKey != null) ? fromKey.Name : string.Empty;
                var toCameraName = (toKey != null) ? toKey.Name : string.Empty;
                blend = CustomBlends.GetBlendForVirtualCameras(
                        fromCameraName, toCameraName, blend);
            }
            if (CinemachineCore.GetBlendOverride != null)
                blend = CinemachineCore.GetBlendOverride(fromKey, toKey, blend, this);
            return blend;
        }
    }
#endif
}
