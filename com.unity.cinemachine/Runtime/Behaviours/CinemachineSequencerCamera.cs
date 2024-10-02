using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a virtual camera "manager" that owns and manages a collection
    /// of child Virtual Cameras.  When the camera goes live, these child vcams
    /// are enabled, one after another, holding each camera for a designated time.
    /// Blends between cameras are specified.
    /// The last camera is held indefinitely, unless the Loop flag is enabled.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [ExcludeFromPreset]
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Cinemachine Sequencer Camera")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSequencerCamera.html")]
    public class CinemachineSequencerCamera : CinemachineCameraManagerBase
    {
        /// <summary>When enabled, the child vcams will cycle indefinitely instead of just stopping at the last one</summary>
        [Tooltip("When enabled, the child vcams will cycle indefinitely instead of just stopping at the last one")]
        [FormerlySerializedAs("m_Loop")]
        public bool Loop;

        /// <summary>This represents a single entry in the instruction list of the BlendListCamera.</summary>
        [Serializable]
        public struct Instruction
        {
            /// <summary>The camera to activate when this instruction becomes active</summary>
            [Tooltip("The camera to activate when this instruction becomes active")]
            [FormerlySerializedAs("m_VirtualCamera")]
            [ChildCameraProperty]
            public CinemachineVirtualCameraBase Camera;

            /// <summary>How to blend to the next camera in the list (if any)</summary>
            [Tooltip("How to blend to the next camera in the list (if any)")]
            [FormerlySerializedAs("m_Blend")]
            public CinemachineBlendDefinition Blend;

            /// <summary>How long to wait (in seconds) before activating the next camera in the list (if any)</summary>
            [Tooltip("How long to wait (in seconds) before activating the next camera in the list (if any)")]
            [FormerlySerializedAs("m_Hold")]
            public float Hold;

            /// <summary>Clamp the the settings to sensible valuse.</summary>
            public void Validate() => Hold = Mathf.Max(Hold, 0);
        };

        /// <summary>The set of instructions for enabling child cameras</summary>
        [Tooltip("The set of instructions for enabling child cameras.")]
        [FormerlySerializedAs("m_Instructions")]
        public List<Instruction> Instructions = new ();

        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("m_LookAt")] Transform m_LegacyLookAt;
        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("m_Follow")] Transform m_LegacyFollow;

        float m_ActivationTime = -1; // The time at which the current instruction went live
        int m_CurrentInstruction = 0;

        /// <inheritdoc />
        protected override void Reset()
        {
            base.Reset();
            Loop = false;
            Instructions = null;
        }

        void OnValidate()
        {
            if (Instructions != null)
            {
                for (int i = 0; i < Instructions.Count; ++i)
                {
                    var e = Instructions[i];
                    e.Validate();
                    Instructions[i] = e;
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void PerformLegacyUpgrade(int streamedVersion)
        {
            base.PerformLegacyUpgrade(streamedVersion);
            if (streamedVersion < 20220721)
            {
                if (m_LegacyLookAt != null || m_LegacyFollow != null)
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
        }
        
        /// <inheritdoc />
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            m_ActivationTime = CinemachineCore.CurrentTime;
            m_CurrentInstruction = 0;
        }

        /// <inheritdoc />
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
        {
            if (!PreviousStateIsValid)
                m_CurrentInstruction = -1;

            AdvanceCurrentInstruction(deltaTime);
            return (m_CurrentInstruction >= 0 && m_CurrentInstruction < Instructions.Count)
                ? Instructions[m_CurrentInstruction].Camera : null;
        }

        /// <summary>Returns the current blend of the current instruction.</summary>
        /// <param name="outgoing">The camera we're blending from (ignored).</param>
        /// <param name="incoming">The camera we're blending to (ignored).</param>
        /// <returns>The blend to use for this camera transition.</returns>
        protected override CinemachineBlendDefinition LookupBlend(
            ICinemachineCamera outgoing, ICinemachineCamera incoming) => Instructions[m_CurrentInstruction].Blend;
            
        /// <inheritdoc />
        protected override bool UpdateCameraCache()
        {
            Instructions ??= new ();
            return base.UpdateCameraCache();
        }

        void AdvanceCurrentInstruction(float deltaTime)
        {
            if (ChildCameras == null || ChildCameras.Count == 0 
                || m_ActivationTime < 0 || Instructions.Count == 0)
            {
                m_ActivationTime = -1;
                m_CurrentInstruction = -1;
                return;
            }

            var now = CinemachineCore.CurrentTime;
            if (m_CurrentInstruction < 0 || deltaTime < 0)
            {
                m_ActivationTime = now;
                m_CurrentInstruction = 0;
            }
            if (m_CurrentInstruction > Instructions.Count - 1)
            {
                m_ActivationTime = now;
                m_CurrentInstruction = Instructions.Count - 1;
            }

            var holdTime = Instructions[m_CurrentInstruction].Hold 
                + Instructions[m_CurrentInstruction].Blend.BlendTime;
            var minHold = m_CurrentInstruction < Instructions.Count - 1 || Loop ? 0 : float.MaxValue;
            if (now - m_ActivationTime > Mathf.Max(minHold, holdTime))
            {
                m_ActivationTime = now;
                ++m_CurrentInstruction;
                if (Loop && m_CurrentInstruction == Instructions.Count)
                    m_CurrentInstruction = 0;
            }
        }
    }
}
