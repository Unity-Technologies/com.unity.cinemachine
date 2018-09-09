using System;
using System.Collections.Generic;
using System.Linq;
using Spectator;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class CinemachineStoryThreadTesterWindow : EditorWindow
{

    StoryManager mStoryManager;

    ReorderableList mThreadEditList;
    public List<StoryManager.StoryThread> mThreads = new List<StoryManager.StoryThread>();
    public List<StoryManager.StoryThread> mSortedThreads = new List<StoryManager.StoryThread>();

    GUIStyle mHelpBoxStyle;
    bool mSimulating;

    GUIContent mUrgencyHeader;
    GUIContent mLastActiveHeader;
    GUIContent mDurationHeader;
    GUIContent mDeltaHeader;
    GUIContent mInterestHeader;
    GUIContent mDecayTypeHeader;
    GUIContent mGrowTypeHeader;

    GUIContent mNameLabel;
    GUIContent mGrowthLabel;
    GUIContent mInterestLabel;

    [MenuItem("Cinemachine/Open thread tester window")]
    private static void OpenWindow()
    {
        CinemachineStoryThreadTesterWindow window = EditorWindow.GetWindow<CinemachineStoryThreadTesterWindow>();
        window.titleContent = new GUIContent("Thread tester");

        window.Show();
    }

    private void OnEnable()
    {
        mStoryManager = StoryManager.Instance;
    }

    private void OnGUI()
    {
        if (mHelpBoxStyle == null)
        {
            mHelpBoxStyle = new GUIStyle(GUI.skin.label);
            mHelpBoxStyle.richText = true;
        }

        if (mUrgencyHeader == null)
        {
            mUrgencyHeader = new GUIContent("Urgency", "Decays while on-screen, increases otherwise. Events and action states influence this heavily.");
            mLastActiveHeader = new GUIContent("Last Active", "The absolute time this thread was last active at.");
            mDurationHeader = new GUIContent("Duration", "The duration in seconds this thread was last active for.");
            mDeltaHeader = new GUIContent("Delta", "The rate by which the urgency is changing. A measure of growth or decay based on the state of the thread.");
            mInterestHeader = new GUIContent("Interest", "Interest level - Controlled by the dev, to focus on a specific thread he judges relevant.");
            mDecayTypeHeader = new GUIContent("Decay type", "The mode by which to decay urgency when active. This decay should be balanced to ensure urgency does not float over time.");
            mGrowTypeHeader = new GUIContent("Grow type", "The mode by which to decay urgency when active. This growth should be balanced to ensure urgency does not float over time.");

            mNameLabel = new GUIContent("Name");
            mGrowthLabel = new GUIContent("Growth");
            mInterestLabel = new GUIContent("Interest");
        }


        EditorGUI.BeginChangeCheck();
        mStoryManager = EditorGUILayout.ObjectField("Story Manager", mStoryManager, typeof(StoryManager), true) as StoryManager;
        if (EditorGUI.EndChangeCheck())
            mThreadEditList = null;

        mThreads.Clear();
        mSortedThreads.Clear();
        if (mStoryManager == null)
            return;

        for (int i = 0; i < mStoryManager.NumThreads; ++i)
        {
            mThreads.Add(mStoryManager.GetThread(i));
            mSortedThreads.Add(mStoryManager.GetThread(i));
        }
        if (mSortedThreads.Count > 1)
            mSortedThreads.Sort((x, y) => x.Name.CompareTo(y.Name));

        if (mThreadEditList == null)
            RebuildReorderableList();

        EditorGUILayout.LabelField("Edit initial threads");
        mThreadEditList.DoLayoutList();

        if (!mSimulating && GUILayout.Button("Start Simulation"))
        {
            mStoryManager.LiveThread = null;
            foreach (var thread in mThreads)
            {
                thread.TimeLastSeenStart = -1;
                thread.TimeLastSeenStop = -1;
            }
            mSimulating = true;
        }
        else if (mSimulating && GUILayout.Button("Stop simulation"))
        {
            mSimulating = false;
        }

        using (new GUILayout.HorizontalScope())
        {
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Name", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.Name);
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label(mUrgencyHeader, EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.Urgency.ToString("0.0"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label(mLastActiveHeader, EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.TimeLastSeenStart.ToString("0.0"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label(mDurationHeader, EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.LastOnScreenDuration.ToString("0.0"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label(mDeltaHeader, EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.UrgencyDerivative.ToString("00.00"));
                }
            }

            GUI.color = Color.white;
        }

        mStoryManager.m_MinimumThreadTime = EditorGUILayout.Slider("Min story time (sec)", mStoryManager.m_MinimumThreadTime, 0.5f, 10f);
        mStoryManager.DecayUrgencyeMode = (StoryManager.ImportanceMode)EditorGUILayout.EnumPopup(mDecayTypeHeader, mStoryManager.DecayUrgencyeMode);
        mStoryManager.GrowUrgencyMode = (StoryManager.ImportanceMode)EditorGUILayout.EnumPopup(mGrowTypeHeader, mStoryManager.GrowUrgencyMode);

        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            GUILayout.Label("<b>LEGEND</b>\n<color=yellow>YELLOW</color>: Queued camera\n<color=#00FF00>GREEN</color>: Active Camera", mHelpBoxStyle);
        }
    }

    private Color GetThreadGUIColour(StoryManager.StoryThread thread)
    {
        if (thread == mStoryManager.LiveThread)
            return Color.green;
        if (mStoryManager.NumThreads > 1 && thread == mStoryManager.GetThread(1))
            return Color.yellow;
        return Color.white;
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            Repaint();
            return;
        }

        if (mSimulating)
        {
            Repaint();
            mStoryManager.TickStoryManagerExternal();
            if (mStoryManager.NumThreads > 0)
                mStoryManager.LiveThread = mStoryManager.GetThread(0);
        }
    }

    private void RebuildReorderableList()
    {
        mThreadEditList = new ReorderableList(mThreads, typeof(StoryManager.StoryThread), false, true, true, true);
        mThreadEditList.drawHeaderCallback += delegate(Rect rect)
        {
            EditorGUI.LabelField(rect, "Threads");
        };
        mThreadEditList.onAddCallback += list =>
        {
            StoryManager.StoryThread newThread = mStoryManager.CreateStoryThread("Thread " + mThreads.Count);
            newThread.InterestLevel = 1;
            newThread.Urgency = 0;
            newThread.TimeLastSeenStart = -1;
            newThread.TimeLastSeenStop = -1;
            newThread.UrgencyGrowthStrength = 1f;
        };

        mThreadEditList.onRemoveCallback += delegate(ReorderableList list)
        {
            StoryManager.StoryThread thread = mSortedThreads[list.index];
            mStoryManager.DestroyStoryThread(thread);
        };

        mThreadEditList.drawElementCallback += (rect, index, active, focused) =>
        {
            const float hSpace = 2;

            StoryManager.StoryThread thread = mSortedThreads[index];

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            rect.width /= 3; rect.width -= hSpace;

            Rect r = rect;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(mNameLabel).x + hSpace;
            thread.Name = EditorGUI.TextField(r, mNameLabel, thread.Name);

            rect.x += rect.width + hSpace;
            r = rect;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(mGrowthLabel).x + hSpace;
            thread.UrgencyGrowthStrength = EditorGUI.FloatField(r, mGrowthLabel, thread.UrgencyGrowthStrength);

            rect.x += rect.width + hSpace;
            r = rect;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(mInterestLabel).x + hSpace;
            thread.InterestLevel = EditorGUI.FloatField(r, mInterestLabel, thread.InterestLevel);

            EditorGUIUtility.labelWidth = oldLabelWidth;
        };
    }
}

