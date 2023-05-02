using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This interface is specifically for Timeline.  The cinemachine timeline track
    /// drives its target via this interface.
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
        /// <param name="priority">The priority to assign to the override.  Higher priorities take 
        /// precedence over lower ones.  This is not connected to the Priority field in the 
        /// individual CinemachineCameras, but the function is analogous.</param>
        /// <param name="camA">The camera to set, corresponding to weight=0.</param>
        /// <param name="camB">The camera to set, corresponding to weight=1.</param>
        /// <param name="weightB">The blend weight.  0=camA, 1=camB.</param>
        /// <param name="deltaTime">Override for deltaTime.  Should be Time.FixedDelta for
        /// time-based calculations to be included, -1 otherwise.</param>
        /// <returns>The override ID.  Don't forget to call ReleaseCameraOverride
        /// after all overriding is finished, to free the OverrideStack resources.</returns>
        int SetCameraOverride(
            int overrideId,
            int priority,
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
    class CameraBlendStack : ICameraOverrideStack
    {
        const float kEpsilon = UnityVectorExtensions.Epsilon;

        class StackFrame : NestedBlendSource
        {
            public int Id;
            public int Priority;
            public readonly CinemachineBlend Source = new (null, null, null, 0, 0);
            public float DeltaTimeOverride;

            // If blending in from a snapshot, this holds the source state
            readonly SnapshotBlendSource m_Snapshot = new ();
            ICinemachineCamera m_SnapshotSource;
            float m_SnapshotBlendWeight;

            public StackFrame() : base(new (null, null, null, 0, 0)) {}
            public bool Active => Source.IsValid;

            // This is a little tricky, because we only want to take a new snapshot at the start 
            // of a blend.  In other cases, we reuse the last snapshot taken, until a new blend starts.
            public ICinemachineCamera GetSnapshotIfAppropriate(ICinemachineCamera cam, float weight)
            {
                if (cam == null || (cam.State.BlendHint & CameraState.BlendHints.FreezeWhenBlendingOut) == 0)
                {
                    // No snapshot required - reset it
                    m_Snapshot.TakeSnapshot(null);
                    return cam;
                }
                // A snapshot is needed
                if (m_SnapshotSource != cam || m_SnapshotBlendWeight > weight)
                {
                    // At this point we're pretty sure this is a new blend,
                    // so we take a new snapshot of the camera state
                    m_Snapshot.TakeSnapshot(cam);
                    m_SnapshotSource = cam;
                    m_SnapshotBlendWeight = weight;
                }
                // Use the most recent snapshot
                return m_Snapshot;
            }
        }

        // Current game state is always frame 0, overrides are subsequent frames
        readonly List<StackFrame> m_FrameStack = new ();
        int m_NextFrameId = 1;

        // To avoid GC memory alloc every frame
        static readonly AnimationCurve s_DefaultLinearAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // GML todo: delete this
        /// <summary>Get the default world up for the virtual cameras.</summary>
        public Vector3 DefaultWorldUp => Vector3.up;

        /// <inheritdoc />
        public int SetCameraOverride(
            int overrideId,
            int priority,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime)
        {
            if (overrideId < 0)
                overrideId = m_NextFrameId++;

            var frame = m_FrameStack[FindFrame(overrideId, priority)];
            frame.DeltaTimeOverride = deltaTime;
            frame.Source.CamA = camA;
            frame.Source.CamB = camB;
            frame.Source.Duration = 1;
            frame.Source.BlendCurve = s_DefaultLinearAnimationCurve;
            frame.Source.TimeInBlend = weightB;

            // In case vcams are inactive game objects, make sure they get initialized properly
            if (camA is CinemachineVirtualCameraBase vcamA)
                vcamA.EnsureStarted();
            if (camB is CinemachineVirtualCameraBase vcamB)
                vcamB.EnsureStarted();

            return overrideId;
            
            // local function to get the frame index corresponding to the ID
            int FindFrame(int withId, int priority)
            {
                int count = m_FrameStack.Count;
                int index;
                for (index = count - 1; index > 0; --index)
                    if (m_FrameStack[index].Id == withId)
                        return index;

                // Not found - add it
                for (index = 1; index < count; ++index)
                    if (m_FrameStack[index].Priority > priority)
                        break;
                m_FrameStack.Insert(index, new StackFrame { Id = withId, Priority = priority });
                return index;
            }
        }

        /// <inheritdoc />
        public void ReleaseCameraOverride(int overrideId)
        {
            for (int i = m_FrameStack.Count - 1; i > 0; --i)
            {
                if (m_FrameStack[i].Id == overrideId)
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

        /// <summary>This holds the function that performs a blend lookup.
        /// This is used to find a blend definition, when a blend is being created.</summary>
        public CinemachineBlendDefinition.LookupBlendDelegate LookupBlendDelegate { get; set; }

        /// <summary>Clear the state of the root frame: no current camera, no blend.</summary>
        public void ResetRootFrame()
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new StackFrame());
            else
            {
                var frame = m_FrameStack[0];
                frame.Blend.CamA = frame.Blend.CamB = null;
                frame.Source.CamA = frame.Source.CamB = null;
            }
        }

        /// <summary>
        /// Call this every frame with the current active camera of the root frame.
        /// </summary>
        /// <param name="activeCamera">Current active camera (pre-override)</param>
        /// <param name="up">Current world up</param>
        /// <param name="deltaTime">How much time has elapsed, for computing blends</param>
        public void UpdateRootFrame(
            ICinemachineCamera activeCamera, Vector3 up, float deltaTime)
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new StackFrame());

            // Update the root frame (frame 0)
            var frame = m_FrameStack[0];
            var outgoingCamera = frame.Source.CamB;
            if (activeCamera != outgoingCamera)
            {
                bool backingOutOfBlend = false;

                // Do we need to create a game-play blend?
                if (LookupBlendDelegate != null && activeCamera != null && activeCamera.IsValid
                    && outgoingCamera != null && outgoingCamera.IsValid && deltaTime >= 0)
                {
                    // Create a blend (curve will be null if a cut)
                    var blendDef = LookupBlendDelegate(outgoingCamera, activeCamera);
                    if (blendDef.BlendCurve != null && blendDef.BlendTime > kEpsilon)
                    {
                        // Are we backing out of a blend-in-progress?
                        backingOutOfBlend = frame.Source.CamA == activeCamera && frame.Source.CamB == outgoingCamera;

                        frame.Source.CamA = outgoingCamera;
                        frame.Source.BlendCurve = blendDef.BlendCurve;
                    }
                    frame.Source.Duration = blendDef.BlendTime;
                    frame.Source.TimeInBlend = 0;
                }
                frame.Source.CamB = activeCamera;

                // Update the working blend:
                // Check the status of the working blend.  If not blending, no problem.
                // Otherwise, we need to do some work to chain the blends.
                var duration = frame.Source.Duration;
                if (frame.Blend.IsComplete)
                    frame.Blend.CamA = frame.GetSnapshotIfAppropriate(outgoingCamera, 0); // new blend
                else
                {
                    bool snapshot = (frame.Blend.State.BlendHint & CameraState.BlendHints.FreezeWhenBlendingOut) != 0;
                    
                    // Special check here: if incoming is InheritPosition and if it's already live
                    // in the outgoing blend, use a snapshot otherwise there could be a pop
                    if (!snapshot && activeCamera != null
                        && (activeCamera.State.BlendHint & CameraState.BlendHints.InheritPosition) != 0 
                        && frame.Blend.Uses(activeCamera))
                        snapshot = true;

                    var nbs = frame.Blend.CamA as NestedBlendSource;

                    // Special case: if backing out of a blend-in-progress
                    // with the same blend in reverse, adjust the blend time
                    // to cancel out the progress made in the opposite direction
                    if (backingOutOfBlend)
                    {
                        snapshot = true; // always use a snapshot for this to prevent pops
                        duration = frame.Blend.TimeInBlend;
                        if (nbs != null)
                            duration += nbs.Blend.Duration - nbs.Blend.TimeInBlend;
                        else if (frame.Blend.CamA is SnapshotBlendSource sbs)
                            duration += sbs.RemainingTimeInBlend;
                    }

                    // Avoid nesting too deeply
                    if (!snapshot && nbs != null && nbs.Blend.CamA is NestedBlendSource nbs2)
                        nbs2.Blend.CamA = new SnapshotBlendSource(nbs2.Blend.CamA);

                    // Chain to existing blend
                    frame.Blend.CamA = snapshot 
                        ? new SnapshotBlendSource(frame, frame.Blend.Duration - frame.Blend.TimeInBlend)
                        : new NestedBlendSource(new CinemachineBlend(frame.Blend));
                }
                frame.Blend.CamB = activeCamera;
                frame.Blend.BlendCurve = frame.Source.BlendCurve;
                frame.Blend.Duration = duration;
                frame.Blend.TimeInBlend = 0;
            }

            // Advance the working blend
            AdvanceBlend(frame.Blend, deltaTime);
            frame.UpdateCameraState(up, deltaTime);

            // local function
            static void AdvanceBlend(CinemachineBlend blend, float deltaTime)
            {
                if (blend.CamA != null)
                {
                    blend.TimeInBlend += (deltaTime >= 0) ? deltaTime : blend.Duration;
                    if (blend.IsComplete)
                    {
                        // No more blend
                        blend.CamA = null;
                        blend.BlendCurve = null;
                        blend.Duration = 0;
                        blend.TimeInBlend = 0;
                    }
                    else if (blend.CamA is NestedBlendSource bs)
                        AdvanceBlend(bs.Blend, deltaTime);
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
        public void ProcessOverrideFrames(
            ref CinemachineBlend outputBlend, int numTopLayersToExclude)
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new StackFrame());

            // Resolve the current working frame states in the stack
            int lastActive = 0;
            int topLayer = Mathf.Max(0, m_FrameStack.Count - numTopLayersToExclude);
            for (int i = 1; i < topLayer; ++i)
            {
                var frame = m_FrameStack[i];
                if (frame.Active)
                {
                    frame.Blend.CopyFrom(frame.Source);
                    if (frame.Source.CamA != null)
                        frame.Blend.CamA = frame.GetSnapshotIfAppropriate(
                            frame.Source.CamA, frame.Source.TimeInBlend);

                    // Handle cases where we're blending into or out of nothing
                    if (!frame.Blend.IsComplete)
                    {
                        frame.Blend.CamA ??= frame.GetSnapshotIfAppropriate(
                            m_FrameStack[lastActive], frame.Blend.TimeInBlend);
                        frame.Blend.CamB ??= m_FrameStack[lastActive];
                    }
                    lastActive = i;
                }
            }
            outputBlend.CopyFrom(m_FrameStack[lastActive].Blend);
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
                    m_FrameStack[0].Blend.CopyFrom(blend);
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

        /// <summary>
        /// Static source for blending. It's not really a virtual camera, but takes
        /// a CameraState and exposes it as a virtual camera for the purposes of blending.
        /// </summary>
        class SnapshotBlendSource : ICinemachineCamera
        {
            CameraState m_State;
            string m_Name;

            public float RemainingTimeInBlend { get; set; }

            public SnapshotBlendSource(ICinemachineCamera source = null, float remainingTimeInBlend = 0)
            {
                TakeSnapshot(source);
                RemainingTimeInBlend = remainingTimeInBlend;
            }

            public string Name => m_Name;
            public string Description => Name;
            public CameraState State => m_State;
            public bool IsValid => true;
            public ICinemachineMixer ParentCamera => null;
            public void UpdateCameraState(Vector3 worldUp, float deltaTime) {}
            public void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt) {}

            public void TakeSnapshot(ICinemachineCamera source)
            {
                m_State = source != null ? source.State : CameraState.Default; 
                m_State.BlendHint &= ~CameraState.BlendHints.FreezeWhenBlendingOut;
                m_Name ??= source == null ? "(null)" : "*" + source.Name;
            }
        }
    }
}
