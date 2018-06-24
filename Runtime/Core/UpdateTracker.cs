using UnityEngine;
using System.Collections.Generic;

namespace Cinemachine
{
    /// <summary>
    /// Attempt to track on what clock transforms get updated
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.Undoc)]
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

            public UpdateClock PreferredUpdate { get; private set; }

            public UpdateStatus(int currentFrame, Matrix4x4 pos)
            {
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
                    //Debug.Log("Window " + numWindows + ": Late=" + numWindowLateUpdateMoves + ", Fixed=" + numWindowFixedUpdateMoves);
                    PreferredUpdate = choice;
                    ++numWindows;
                    windowStart = currentFrame;
                    numWindowLateUpdateMoves = (PreferredUpdate == UpdateClock.Late) ? 1 : 0;
                    numWindowFixedUpdateMoves = (PreferredUpdate == UpdateClock.Fixed) ? 1 : 0;
                }
            }
        }
        static Dictionary<Transform, UpdateStatus> mUpdateStatus 
            = new Dictionary<Transform, UpdateStatus>();

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule() { mUpdateStatus.Clear(); }
        
        static List<Transform> sToDelete = new List<Transform>();
        static void UpdateTargets(UpdateClock currentClock)
        {
            // Update the registry for all known targets
            int now = Time.frameCount;
            var iter = mUpdateStatus.GetEnumerator();
            while (iter.MoveNext())
            {
                var current = iter.Current;
                if (current.Key == null)
                    sToDelete.Add(current.Key); // target was deleted
                else
                    current.Value.OnUpdate(now, currentClock, current.Key.localToWorldMatrix);
            }
            for (int i = sToDelete.Count-1; i >= 0; --i)
                mUpdateStatus.Remove(sToDelete[i]);
            sToDelete.Clear();
        }

        public static UpdateClock GetPreferredUpdate(Transform target)
        {
            if (Application.isPlaying && target != null)
            {
                UpdateStatus status;
                if (mUpdateStatus.TryGetValue(target, out status))
                    return status.PreferredUpdate;

                // Add the target to the registry
                status = new UpdateStatus(Time.frameCount, target.localToWorldMatrix);
                mUpdateStatus.Add(target, status);
            }
            return UpdateClock.Late;
        }

        static float mLastUpdateTime;
        public static void OnUpdate(UpdateClock currentClock)
        {
            // Do something only if we are the first controller processing this frame
            float now = Time.time;
            if (now != mLastUpdateTime)
            {
                mLastUpdateTime = now;
                UpdateTargets(currentClock);
            }
        }
    }
}
