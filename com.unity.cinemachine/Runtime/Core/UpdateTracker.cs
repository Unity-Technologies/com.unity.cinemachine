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
        public enum UpdateClock { Fixed = 1, Late = 2}

        class UpdateStatus
        {
            const int kWindowSize = 30;
            int m_WindowStart;
            int m_NumWindowLateUpdateMoves;
            int m_NumWindowFixedUpdateMoves;
            int m_NumWindows;
            int m_LastFrameUpdated;
            Matrix4x4 m_LastPos;
#if DEBUG_LOG_NAME
            string m_Name;
#endif
            public UpdateClock PreferredUpdate { get; private set; }

#if DEBUG_LOG_NAME
            public UpdateStatus(string targetName, int currentFrame, Matrix4x4 pos)
            {
                m_Name = targetName;
#else
            public UpdateStatus(int currentFrame, Matrix4x4 pos)
            {
#endif
                m_WindowStart = currentFrame;
                m_LastFrameUpdated = Time.frameCount;
                PreferredUpdate = UpdateClock.Late;
                m_LastPos = pos;
            }

            public void OnUpdate(int currentFrame, UpdateClock currentClock, Matrix4x4 pos)
            {
                if (m_LastPos == pos)
                    return;

                if (currentClock == UpdateClock.Late)
                    ++m_NumWindowLateUpdateMoves;
                else if (m_LastFrameUpdated != currentFrame) // only count 1 per rendered frame
                    ++m_NumWindowFixedUpdateMoves;
                m_LastPos = pos;

                UpdateClock choice = UpdateClock.Late;
                if (m_NumWindowFixedUpdateMoves > 3 && m_NumWindowLateUpdateMoves < m_NumWindowFixedUpdateMoves / 3)
                    choice = UpdateClock.Fixed;
                if (m_NumWindows == 0)
                    PreferredUpdate = choice;

                if (m_WindowStart + kWindowSize <= currentFrame)
                {
#if DEBUG_LOG_NAME
                    Debug.Log(m_Name + ": Window " + m_NumWindows + ": Late=" + m_NumWindowLateUpdateMoves + ", Fixed=" + m_NumWindowFixedUpdateMoves + ", currentClock=" + currentClock);
#endif
                    PreferredUpdate = choice;
                    ++m_NumWindows;
                    m_WindowStart = currentFrame;
                    m_NumWindowLateUpdateMoves = (PreferredUpdate == UpdateClock.Late) ? 1 : 0;
                    m_NumWindowFixedUpdateMoves = (PreferredUpdate == UpdateClock.Fixed) ? 1 : 0;
                }
            }
        }
        static Dictionary<Transform, UpdateStatus> s_UpdateStatus  = new();

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule() => s_UpdateStatus.Clear();

        static List<Transform> s_ToDelete = new();
        static void UpdateTargets(UpdateClock currentClock)
        {
            // Update the registry for all known targets
            int now = Time.frameCount;
            var iter = s_UpdateStatus.GetEnumerator();
            while (iter.MoveNext())
            {
                var current = iter.Current;
                if (current.Key == null)
                    s_ToDelete.Add(current.Key); // target was deleted
                else
                    current.Value.OnUpdate(now, currentClock, current.Key.localToWorldMatrix);
            }
            for (int i = s_ToDelete.Count-1; i >= 0; --i)
                s_UpdateStatus.Remove(s_ToDelete[i]);
            s_ToDelete.Clear();
            iter.Dispose();
        }

        public static UpdateClock GetPreferredUpdate(Transform target)
        {
            if (Application.isPlaying && target != null)
            {
                if (s_UpdateStatus.TryGetValue(target, out var status))
                    return status.PreferredUpdate;

                // Add the target to the registry
#if DEBUG_LOG_NAME
                status = new UpdateStatus(target.name, Time.frameCount, target.localToWorldMatrix);
#else
                status = new UpdateStatus(Time.frameCount, target.localToWorldMatrix);
#endif
                s_UpdateStatus.Add(target, status);
            }
            return UpdateClock.Late;
        }

        static object s_LastUpdateContext;
        public static void OnUpdate(UpdateClock currentClock, object context)
        {
            // Do something only if we are the first controller processing this frame
            if (s_LastUpdateContext == null || s_LastUpdateContext == context)
            {
                s_LastUpdateContext = context;
                UpdateTargets(currentClock);
            }
        }

        public static void ForgetContext(object context)
        {
            if (s_LastUpdateContext == context)
                s_LastUpdateContext = null;
        }
    }
}
