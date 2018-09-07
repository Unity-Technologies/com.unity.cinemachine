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

    GUIStyle mHelpBoxStyle;
    bool mSimulating;

    GUIContent mUrgencyHeader;
    GUIContent mLastActiveHeader;
    GUIContent mDurationHeader;
    GUIContent mDeltaHeader;
    GUIContent mInterestHeader;
    GUIContent mDecayTypeHeader;
    GUIContent mGrowTypeHeader;

    [MenuItem("Cinemachine/Open thread tester window")]
    private static void OpenWindow()
    {
        CinemachineStoryThreadTesterWindow window = EditorWindow.GetWindow<CinemachineStoryThreadTesterWindow>();
        window.titleContent = new GUIContent("Thread tester");

        window.Show();
    }

    private void OnEnable()
    {

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
        }


        EditorGUI.BeginChangeCheck();
        mStoryManager = EditorGUILayout.ObjectField("Story Manager", mStoryManager, typeof(StoryManager), true) as StoryManager;
        if (EditorGUI.EndChangeCheck())
        {
            RebuildReorderableList();
        }

        if (mStoryManager == null)
        {
            return;
        }

        if (mThreadEditList == null)
        {
            RebuildReorderableList();
        }

        EditorGUILayout.LabelField("Edit initial threads");
        GUI.enabled = !mSimulating;
        mThreadEditList.DoLayoutList();
        GUI.enabled = true;

        if (!mSimulating && GUILayout.Button("Start Simulation"))
        {
            mStoryManager.LiveThread = null;
            foreach (var thread in mThreads)
            {
                thread.Urgency = thread.InterestLevel;
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
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label(mInterestHeader, EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.color = GetThreadGUIColour(thread);

                        EditorGUI.BeginChangeCheck();
                        thread.InterestLevel = EditorGUILayout.DelayedFloatField(thread.InterestLevel, GUILayout.ExpandWidth(false));//GUILayout.Label(thread.InterestLevel.ToString("000.0"), GUILayout.ExpandWidth(false)));
                        thread.InterestLevel = GUILayout.HorizontalSlider(thread.InterestLevel, 0f, 100f, GUILayout.ExpandWidth(true), GUILayout.MinWidth(100f));

                        if (EditorGUI.EndChangeCheck())
                        {
                            thread.InterestLevel = (float)Math.Round(thread.InterestLevel, 1, MidpointRounding.AwayFromZero);
                        }
                    }
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
        Color guiColour = Color.white;

        if (thread == mStoryManager.LiveThread)
        {
            guiColour = Color.green;
        }
        else if (thread == mStoryManager.NextLiveThread)
        {
            guiColour = Color.yellow;
        }

        return guiColour;
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
        }
    }

    private void RebuildReorderableList()
    {
        mThreads.Clear();

        for (int i = 0; i < mStoryManager.NumThreads; ++i)
        {
            mThreads.Add(mStoryManager.GetThread(i));
        }

        mThreadEditList = new ReorderableList(mThreads, typeof(StoryManager.StoryThread), false, true, true, true);
        mThreadEditList.drawHeaderCallback += delegate(Rect rect)
        {
            EditorGUI.LabelField(rect, "Threads");
        };
        mThreadEditList.onAddCallback += list =>
        {
            StoryManager.StoryThread newThread = mStoryManager.CreateStoryThread("New Thread");
            newThread.InterestLevel = 1;
            newThread.Urgency = 0;
            newThread.TimeLastSeenStart = -1;
            newThread.TimeLastSeenStop = -1;
            newThread.RateModifier = 1f;
            mThreads.Add(newThread);
        };

        mThreadEditList.onRemoveCallback += delegate(ReorderableList list)
        {
            StoryManager.StoryThread thread = mThreads[list.index];
            mThreads.RemoveAt(list.index);

            mStoryManager.DestroyStoryThread(thread);
        };

        mThreadEditList.elementHeightCallback += index => mThreadEditList.elementHeight * 2f;

        mThreadEditList.drawElementBackgroundCallback += (rect, index, active, focused) => {
            if (active)
            {
                GUI.color = Color.cyan;
                GUI.Box(rect, string.Empty);
            }
            else if (index % 2 == 0)
            {
                GUI.Box(rect, "");
            }

            GUI.color = Color.white;
        };


        mThreadEditList.drawElementCallback += (rect, index, active, focused) =>
        {
            float height = mThreadEditList.elementHeight;
            rect.height = height;

            GUIStyle lableStyle = GUI.skin.label;

            StoryManager.StoryThread thread = mThreads[index];

            float labelMin, labelMax;
            const string kLabelForName = "Name";
            const float kLabelPadding = 16f;
            lableStyle.CalcMinMaxWidth(new GUIContent(kLabelForName), out labelMin, out labelMax);
            labelMax += kLabelPadding;

            float oldWidth = rect.width;

            Rect labelRect = rect;
            labelRect.width = labelMax;
            EditorGUI.LabelField(labelRect, kLabelForName);

            rect.xMin = labelMax;
            rect.width = oldWidth - labelMax;
            thread.Name = EditorGUI.TextField(rect, thread.Name);

            const float kIndentSize = 20f;

            oldWidth -= kIndentSize;
            rect.xMin = kIndentSize;
            rect.width = oldWidth;
            rect.yMin += height;
            rect.height = height;

            const string kLabelForGrowth = "Growth Rate";
            lableStyle.CalcMinMaxWidth(new GUIContent(kLabelForGrowth), out labelMin, out labelMax);
            labelMax += kLabelPadding;

            oldWidth = rect.width;

            labelRect = rect;
            labelRect.width = labelMax;
            EditorGUI.LabelField(labelRect, kLabelForGrowth);

            rect.xMin = labelMax + kIndentSize;
            rect.width = oldWidth - labelMax;
            thread.RateModifier = EditorGUI.FloatField(rect, thread.RateModifier);
        };
    }
}

