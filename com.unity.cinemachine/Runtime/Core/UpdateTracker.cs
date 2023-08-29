//#define DEBUG_LOG_NAME

using UnityEngine;
using System.Collections.Generic;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Attempt to track on what clock transforms get updated
    /// </summary>
    internal class UpdateTracker
    {
        public enum UpdateClock { Fixed, Late }

        class UpdateStatus
        {
            const int kWindowSize = 30;
            int windowStart;
            int numWindowLateUpdateMoves;
            int numWindowFixedUpdateMoves;
            int numWindows;
            int lastFrameUpdated;
            Matrix4x4 lastPos;
#if DEBUG_LOG_NAME
            string name;
#endif
            public UpdateClock PreferredUpdate { get; private set; }

#if DEBUG_LOG_NAME
            public UpdateStatus(string targetName, int currentFrame, Matrix4x4 pos)
            {
                name = targetName;
#else
            public UpdateStatus(int currentFrame, Matrix4x4 pos)
            {
#endif
                windowStart = currentFrame;
                lastFrameUpdated = Time.frameCount;
                PreferredUpdate = UpdateClock.Late;
                lastPos = pos;
            }

            public void OnUpdate(int currentFrame, UpdateClock currentClock, Matrix4x4 pos)
            {
                if (lastPos == pos)
                    return;

                if (currentClock == UpdateClock.Late)
                    ++numWindowLateUpdateMoves;
                else if (lastFrameUpdated != currentFrame) // only count 1 per rendered frame
                    ++numWindowFixedUpdateMoves;
                lastPos = pos;

                UpdateClock choice;
                if (numWindowFixedUpdateMoves > 3 && numWindowLateUpdateMoves < numWindowFixedUpdateMoves / 3)
                    choice = UpdateClock.Fixed;
                else
                    choice =  UpdateClock.Late;
                if (numWindows == 0)
                    PreferredUpdate = choice;
 
                if (windowStart + kWindowSize <= currentFrame)
                {
#if DEBUG_LOG_NAME
                    Debug.Log(name + ": Window " + numWindows + ": Late=" + numWindowLateUpdateMoves + ", Fixed=" + numWindowFixedUpdateMoves);
#endif
                    PreferredUpdate = choice;
                    ++numWindows;
                    windowStart = currentFrame;
                    numWindowLateUpdateMoves = (PreferredUpdate == UpdateClock.Late) ? 1 : 0;
                    numWindowFixedUpdateMoves = (PreferredUpdate == UpdateClock.Fixed) ? 1 : 0;
                }
            }
        }
        static Dictionary<Transform, UpdateStatus> m_UpdateStatus  = new();

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule() => m_UpdateStatus.Clear();
        
        static List<Transform> s_ToDelete = new();
        static void UpdateTargets(UpdateClock currentClock)
        {
            // Update the registry for all known targets
            int now = Time.frameCount;
            var iter = m_UpdateStatus.GetEnumerator();
            while (iter.MoveNext())
            {
                var current = iter.Current;
                if (current.Key == null)
                    s_ToDelete.Add(current.Key); // target was deleted
                else
                    current.Value.OnUpdate(now, currentClock, current.Key.localToWorldMatrix);
            }
            for (int i = s_ToDelete.Count-1; i >= 0; --i)
                m_UpdateStatus.Remove(s_ToDelete[i]);
            s_ToDelete.Clear();
            iter.Dispose();
        }

        public static UpdateClock GetPreferredUpdate(Transform target)
        {
            if (Application.isPlaying && target != null)
            {
                if (m_UpdateStatus.TryGetValue(target, out var status))
                    return status.PreferredUpdate;

                // Add the target to the registry
#if DEBUG_LOG_NAME
                status = new UpdateStatus(target.name, Time.frameCount, target.localToWorldMatrix);
#else
                status = new UpdateStatus(Time.frameCount, target.localToWorldMatrix);
#endif
                m_UpdateStatus.Add(target, status);
            }
            return UpdateClock.Late;
        }

        static float m_LastUpdateTime;
        public static void OnUpdate(UpdateClock currentClock)
        {
            // Do something only if we are the first controller processing this frame
            float now = CinemachineCore.CurrentTime;
            if (now != m_LastUpdateTime)
            {
                m_LastUpdateTime = now;
                UpdateTargets(currentClock);
            }
        }
    }
}
