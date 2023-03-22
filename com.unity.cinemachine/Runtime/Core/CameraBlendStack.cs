using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This interface is specifically for Timeline.  Do not use it.
    /// </summary>
    public interface ICameraOverrideStack
    {
        /// <summary>
        /// Override the current camera and current blend.  This setting will trump
        /// any in-game logic that sets virtual camera priorities and Enabled states.
        /// This is the main API for the timeline.
        /// </summary>
        /// <param name="overrideId">Id to represent a specific client.  An internal
        /// stack is maintained, with the most recent non-empty override taking precedence.
        /// This id must be > 0.  If you pass -1, a new id will be created, and returned.
        /// Use that id for subsequent calls.  Don't forget to
        /// call ReleaseCameraOverride after all overriding is finished, to
        /// free the OverrideStack resources.</param>
        /// <param name="camA">The camera to set, corresponding to weight=0.</param>
        /// <param name="camB">The camera to set, corresponding to weight=1.</param>
        /// <param name="weightB">The blend weight.  0=camA, 1=camB.</param>
        /// <param name="deltaTime">Override for deltaTime.  Should be Time.FixedDelta for
        /// time-based calculations to be included, -1 otherwise.</param>
        /// <returns>The override ID.  Don't forget to call ReleaseCameraOverride
        /// after all overriding is finished, to free the OverrideStack resources.</returns>
        int SetCameraOverride(
            int overrideId,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime);

        /// <summary>
        /// See SetCameraOverride.  Call ReleaseCameraOverride after all overriding
        /// is finished, to free the OverrideStack resources.
        /// </summary>
        /// <param name="overrideId">The ID to released.  This is the value that
        /// was returned by SetCameraOverride</param>
        void ReleaseCameraOverride(int overrideId);

        // GML todo: delete this
        /// <summary>
        /// Get the current definition of Up.  May be different from Vector3.up.
        /// </summary>
        Vector3 DefaultWorldUp { get; }
    }
    
    
    /// <summary>
    /// Implements an overridable stack of blend states.
    /// </summary>
    internal class CameraBlendStack : ICameraOverrideStack
    {
        class StackFrame
        {
            public int id;
            public CinemachineBlend Blend = new (null, null, null, 0, 0);
            public bool Active => Blend.IsValid;

            // Working data - updated every frame
            public CinemachineBlend WorkingBlend = new (null, null, null, 0, 0);
            public BlendSourceVirtualCamera WorkingBlendSource = new (null);

            // Used by Timeline Preview for overriding the current value of deltaTime
            public float DeltaTimeOverride;

            // Used for blend reversal.  Range is 0...1,
            // representing where the blend started when reversed mid-blend
            public float BlendStartPosition;
        }

        // Current game state is always frame 0, overrides are subsequent frames
        readonly List<StackFrame> m_FrameStack = new ();
        int m_NextFrameId = 1;

        // To avoid GC memory alloc every frame
        static readonly AnimationCurve k_DefaultLinearAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // GML todo: delete this
        /// <summary>Get the default world up for the virtual cameras.</summary>
        public Vector3 DefaultWorldUp => Vector3.up;

        /// <inheritdoc />
        public int SetCameraOverride(
            int overrideId,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime)
        {
            if (overrideId < 0)
                overrideId = m_NextFrameId++;

// GML todo: how to handle BlendHints.BlendOutFromSnapshot?

            var frame = m_FrameStack[FindFrame(overrideId)];
            frame.DeltaTimeOverride = deltaTime;
            frame.Blend.CamA = camA;
            frame.Blend.CamB = camB;
            frame.Blend.BlendCurve = k_DefaultLinearAnimationCurve;
            frame.Blend.Duration = 1;
            frame.Blend.TimeInBlend = weightB;

            // In case vcams are inactive game objects, make sure they get initialized properly
            var cam = camA as CinemachineVirtualCameraBase;
            if (cam != null)
                cam.EnsureStarted();
            cam = camB as CinemachineVirtualCameraBase;
            if (cam != null)
                cam.EnsureStarted();

            return overrideId;
            
            // local function to get the frame index corresponding to the ID
            int FindFrame(int withId)
            {
                int count = m_FrameStack.Count;
                for (int i = count - 1; i > 0; --i)
                    if (m_FrameStack[i].id == withId)
                        return i;
                // Not found - add it
                m_FrameStack.Add(new StackFrame { id = withId });
                return m_FrameStack.Count - 1;
            }
        }

        /// <inheritdoc />
        public void ReleaseCameraOverride(int overrideId)
        {
            for (int i = m_FrameStack.Count - 1; i > 0; --i)
            {
                if (m_FrameStack[i].id == overrideId)
                {
                    m_FrameStack.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>Call this when object is enabled</summary>
        public void OnEnable()
        {
            // Make sure there is a first stack frame
            m_FrameStack.Clear();
            m_FrameStack.Add(new StackFrame());
        }

         /// <summary>Call this when object is disabled</summary>
        public void OnDisable()
        {
            m_FrameStack.Clear();
            m_NextFrameId = -1;
        }

        /// <summary>Has OnEnable been called?</summary>
        public bool IsInitialized => m_FrameStack.Count > 0;

        /// <summary>Clear the state of the root frame: no current camera, no blend.</summary>
        public void ResetRootFrame()
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new StackFrame());
            else
                m_FrameStack[0] = new StackFrame();
        }

        /// <summary>
        /// Call this every frame with the current active camera of the root frame.
        /// </summary>
        /// <param name="activeCamera">Current active camera (pre-override)</param>
        /// <param name="deltaTime">How much time has elapsed, for computing blends</param>
        /// <param name="lookupBlend">Delegate to use to find a blend definition, when a blend is being created</param>
        public void UpdateRootFrame(
            ICinemachineCamera activeCamera, float deltaTime, 
            CinemachineBlendDefinition.LookupBlendDelegate lookupBlend)
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new StackFrame());

            // Update the in-game frame (frame 0)
            var frame = m_FrameStack[0];

            // Are we transitioning cameras?
            var outGoingCamera = frame.Blend.CamB;

            if (activeCamera != outGoingCamera)
            {
                // Do we need to create a game-play blend?
                if (lookupBlend != null && activeCamera != null && activeCamera.IsValid
                    && outGoingCamera != null && outGoingCamera.IsValid && deltaTime >= 0)
                {
                    // Create a blend (curve will be null if a cut)
                    var blendDef = lookupBlend(outGoingCamera, activeCamera);
                    float blendDuration = blendDef.BlendTime;
                    float BlendStartPosition = 0;
                    if (blendDef.BlendCurve != null && blendDuration > UnityVectorExtensions.Epsilon)
                    {
                        if (frame.Blend.IsComplete)
                        {
                            // new blend
#if false // GML todo: think about this
                            if ((outGoingCamera.State.BlendHint & CameraState.BlendHints.BlendOutFromSnapshot) != 0)
                                frame.Blend.CamA = new StaticPointVirtualCamera(outGoingCamera.State, outGoingCamera.Name);
                            else
#endif
                            frame.Blend.CamA = outGoingCamera;  
                        }
                        else
                        {
                            // Special case: if backing out of a blend-in-progress
                            // with the same blend in reverse, adjust the blend time
                            // to cancel out the progress made in the opposite direction
                            if ((frame.Blend.CamA == activeCamera 
                                    || (frame.Blend.CamA as BlendSourceVirtualCamera)?.Blend.CamB == activeCamera) 
                                && frame.Blend.CamB == outGoingCamera)
                            {
                                // How far have we blended?  That is what we must undo
                                var progress = frame.BlendStartPosition 
                                    + (1 - frame.BlendStartPosition) * frame.Blend.TimeInBlend / frame.Blend.Duration;
                                blendDuration *= progress;
                                BlendStartPosition = 1 - progress;
                            }
                            
                            // Chain to existing blend.
                            // Special check here: if incoming is InheritPosition and if it's already live
                            // in the outgoing blend, use a snapshot otherwise there could be a pop
                            if ((activeCamera.State.BlendHint & CameraState.BlendHints.InheritPosition) != 0 
                                && frame.Blend.Uses(activeCamera))
                            {
                                frame.Blend.CamA = new StaticPointVirtualCamera(
                                    frame.Blend.State, frame.Blend.CamB.Name + " mid-blend");
                            }
                            else
                            {
                                frame.Blend.CamA = new BlendSourceVirtualCamera(
                                    new CinemachineBlend(
                                        frame.Blend.CamA, frame.Blend.CamB,
                                        frame.Blend.BlendCurve, frame.Blend.Duration,
                                        frame.Blend.TimeInBlend));
                            }
                        }
                    }
                    frame.Blend.BlendCurve = blendDef.BlendCurve;
                    frame.Blend.Duration = blendDuration;
                    frame.Blend.TimeInBlend = 0;
                    frame.BlendStartPosition = BlendStartPosition;
                }
                // Set the current active camera
                frame.Blend.CamB = activeCamera;
            }

            // Advance the current blend (if any)
            if (frame.Blend.CamA != null)
            {
                frame.Blend.TimeInBlend += (deltaTime >= 0) ? deltaTime : frame.Blend.Duration;
                if (frame.Blend.IsComplete)
                {
                    // No more blend
                    frame.Blend.CamA = null;
                    frame.Blend.BlendCurve = null;
                    frame.Blend.Duration = 0;
                    frame.Blend.TimeInBlend = 0;
                }
            }
        }

        /// <summary>
        /// Compute the current blend, taking into account
        /// the in-game camera and all the active overrides.  Caller may optionally
        /// exclude n topmost overrides.
        /// </summary>
        /// <param name="outputBlend">Receives the nested blend</param>
        /// <param name="numTopLayersToExclude">Optionally exclude the last number 
        /// of overrides from the blend</param>
        public void ComputeCurrentBlend(
            ref CinemachineBlend outputBlend, int numTopLayersToExclude)
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new StackFrame());

            // Resolve the current working frame states in the stack
            int lastActive = 0;
            int topLayer = Mathf.Max(1, m_FrameStack.Count - numTopLayersToExclude);
            for (int i = 0; i < topLayer; ++i)
            {
                StackFrame frame = m_FrameStack[i];
                if (i == 0 || frame.Active)
                {
                    frame.WorkingBlend.CamA = frame.Blend.CamA;
                    frame.WorkingBlend.CamB = frame.Blend.CamB;
                    frame.WorkingBlend.BlendCurve = frame.Blend.BlendCurve;
                    frame.WorkingBlend.Duration = frame.Blend.Duration;
                    frame.WorkingBlend.TimeInBlend = frame.Blend.TimeInBlend;
                    if (i > 0 && !frame.Blend.IsComplete)
                    {
                        if (frame.WorkingBlend.CamA == null)
                        {
                            if (m_FrameStack[lastActive].Blend.IsComplete)
                                frame.WorkingBlend.CamA = m_FrameStack[lastActive].Blend.CamB;
                            else
                            {
                                frame.WorkingBlendSource.Blend = m_FrameStack[lastActive].WorkingBlend;
                                frame.WorkingBlend.CamA = frame.WorkingBlendSource;
                            }
                        }
                        else if (frame.WorkingBlend.CamB == null)
                        {
                            if (m_FrameStack[lastActive].Blend.IsComplete)
                                frame.WorkingBlend.CamB = m_FrameStack[lastActive].Blend.CamB;
                            else
                            {
                                frame.WorkingBlendSource.Blend = m_FrameStack[lastActive].WorkingBlend;
                                frame.WorkingBlend.CamB = frame.WorkingBlendSource;
                            }
                        }
                    }
                    lastActive = i;
                }
            }
            var workingBlend = m_FrameStack[lastActive].WorkingBlend;
            outputBlend.CamA = workingBlend.CamA;
            outputBlend.CamB = workingBlend.CamB;
            outputBlend.BlendCurve = workingBlend.BlendCurve;
            outputBlend.Duration = workingBlend.Duration;
            outputBlend.TimeInBlend = workingBlend.TimeInBlend;
        }

        /// <summary>
        /// Set the current root blend.  Can be used to modify the root blend state.
        /// <param name="blend">The new blend.  If null, current blend will be force-completed.</param>
        /// </summary>
        public void SetRootBlend(CinemachineBlend blend)
        {
            if (IsInitialized)
            {
                if (blend == null)
                    m_FrameStack[0].Blend.Duration = 0;
                else
                    m_FrameStack[0].Blend = blend;
            }
        }

        /// <summary>
        /// Get the current active deltaTime override, defined in the topmost override frame.
        /// </summary>
        /// <returns>The active deltaTime override, or -1 for none</returns>
        public float GetDeltaTimeOverride()
        {
            for (int i = m_FrameStack.Count - 1; i > 0; --i)
            {
                var frame = m_FrameStack[i];
                if (frame.Active)
                    return frame.DeltaTimeOverride;
            }
            return -1;
        }
    }
}
