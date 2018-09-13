using System;
using System.Collections.Generic;
using System.Linq;
using Spectator;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;


public class CinemachineStoryThreadTesterWindow : EditorWindow
{
    ReorderableList mThreadEditList;
    public List<StoryManager.StoryThread> mThreads = new List<StoryManager.StoryThread>();
    public List<StoryManager.StoryThread> mSortedThreads = new List<StoryManager.StoryThread>();

    GUIStyle mHelpBoxStyle;
    bool mSimulating;
/*
    GUIContent mUrgencyHeader;
    GUIContent mLastActiveHeader;
    GUIContent mDurationHeader;
    GUIContent mDeltaHeader;
    GUIContent mDecayTypeHeader;
    GUIContent mGrowTypeHeader;
*/
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

    private void OnGUI()
    {
        if (mHelpBoxStyle == null)
        {
            mHelpBoxStyle = new GUIStyle(GUI.skin.label);
            mHelpBoxStyle.richText = true;
        }

        if (mNameLabel == null)
        {
/*
            mUrgencyHeader = new GUIContent("Urgency", "Decays while on-screen, increases otherwise. Events and action states influence this heavily.");
            mLastActiveHeader = new GUIContent("Last Active", "The absolute time this thread was last active at.");
            mDurationHeader = new GUIContent("Duration", "The duration in seconds this thread was last active for.");
            mDeltaHeader = new GUIContent("Delta", "The rate by which the urgency is changing. A measure of growth or decay based on the state of the thread.");
            mDecayTypeHeader = new GUIContent("Decay type", "The mode by which to decay urgency when active. This decay should be balanced to ensure urgency does not float over time.");
            mGrowTypeHeader = new GUIContent("Grow type", "The mode by which to decay urgency when active. This growth should be balanced to ensure urgency does not float over time.");
*/
            mNameLabel = new GUIContent("Name");
            mGrowthLabel = new GUIContent("Growth");
            mInterestLabel = new GUIContent("Interest");
        }


        mThreads.Clear();
        mSortedThreads.Clear();
        if (StoryManager.Instance == null)
            return;

        for (int i = 0; i < StoryManager.Instance.NumThreads; ++i)
        {
            var th = StoryManager.Instance.GetThread(i);
            mThreads.Add(th);
            mSortedThreads.Add(th);
        }
        if (mSortedThreads.Count > 1)
            mSortedThreads.Sort((x, y) => x.Name.CompareTo(y.Name));

        if (mThreadEditList == null)
            RebuildReorderableList();

        mThreadEditList.DoLayoutList();

        if (!mSimulating && GUILayout.Button("Start Simulation"))
        {
            StoryManager.Instance.LiveThreads = null;
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
                GUILayout.Label("Urgency", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.Urgency.ToString("0.000"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("LastActive", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.TimeLastSeenStart.ToString("0.000"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Duration", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.LastOnScreenDuration.ToString("0.000"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Delta", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.UrgencyDerivative.ToString("00.000"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("CamPoints", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.m_cameraPoints.Count.ToString());
                }
            }

            GUI.color = Color.white;
        }

        StoryManager.Instance.m_MinimumThreadTime 
            = EditorGUILayout.Slider("Min story time (sec)", 
            StoryManager.Instance.m_MinimumThreadTime, 0.5f, 10f);
    }

    private Color GetThreadGUIColour(StoryManager.StoryThread thread)
    {
        if (StoryManager.Instance.ThreadIsLive(thread))
            return Color.green;
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
            StoryManager.Instance.TickStoryManager();
            if (StoryManager.Instance.NumThreads > 0)
                StoryManager.Instance.LiveThreads = new List<StoryManager.StoryThread> { StoryManager.Instance.GetThread(0) };
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
            StoryManager.StoryThread newThread = StoryManager.Instance.CreateStoryThread(
                new GameObject("Thread " + mThreads.Count).transform);
            newThread.InterestLevel = 1;
            newThread.Urgency = 0;
            newThread.TimeLastSeenStart = -1;
            newThread.TimeLastSeenStop = -1;
            newThread.UrgencyGrowthStrength = 1f;
        };

        mThreadEditList.onRemoveCallback += delegate(ReorderableList list)
        {
            StoryManager.StoryThread thread = mSortedThreads[list.index];
            Cinemachine.RuntimeUtility.DestroyObject(thread.TargetObject.gameObject);
            StoryManager.Instance.DestroyStoryThread(thread);
        };

        mThreadEditList.drawElementCallback += (rect, index, active, focused) =>
        {
            const float hSpace = 3;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 6f;
            float oldLabelWidth = EditorGUIUtility.labelWidth;

            StoryManager.StoryThread th = mSortedThreads[index];

            Rect r = rect;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(mNameLabel).x + hSpace;
            r.width = EditorGUIUtility.labelWidth + rect.width / 3;
            th.Name = EditorGUI.TextField(r, mNameLabel, th.Name);

            r.x += r.width + hSpace;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(mInterestLabel).x + hSpace;
            r.width = EditorGUIUtility.labelWidth + floatFieldWidth;
            th.InterestLevel = EditorGUI.FloatField(r, mInterestLabel, th.InterestLevel);

            EditorGUIUtility.labelWidth = oldLabelWidth;

            r.height -= 1;
            r.x += r.width + hSpace;
            r.width = rect.width - (r.x - rect.x);
            rect = r;
            Color bkg = new Color(0.27f, 0.27f, 0.27f); // ack! no better way than this?
            Color fg = Color.Lerp(Color.red, Color.yellow, 0.8f);
            fg = Color.Lerp(fg, bkg, 0.6f);
            if (!StoryManager.Instance.ThreadIsLive(th))
                fg = Color.Lerp(fg, bkg, 0.8f);

            float maxValue = 0;
            for (int i = 0; i < StoryManager.Instance.NumThreads; ++i)
                maxValue = Mathf.Max(maxValue, StoryManager.Instance.GetThread(i).Urgency);
            float u = (maxValue > 0.001f) ? (th.Urgency / maxValue) : 1;

            r.width = Mathf.Max(1, rect.width * u);
            EditorGUI.DrawRect(r, fg);

            r.x += r.width; r.width = rect.width - r.width;
            EditorGUI.DrawRect(r, bkg);

            EditorGUI.LabelField(rect, th.Urgency.ToString());
        };
    }
}

