using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class CinemachineStoryThreadTesterWindow : EditorWindow
{
    [Serializable]
    public class StoryThread
    {
        public string Name;

        // Interest level - Controlled by the dev, to focus on a specific thread he judges relevant.
        // This is used as part of the weighting algorithm when calculating urgency
        public float InterestLevel;


        public float RateModifier = 1;

        // Time when last on-screen
        public float TimeLastSeenStart;
        public float TimeLastSeenStop;

        public float UrgencyDerivative;

        // Last on-screen duration
        public float LastOnScreenDuration { get { return TimeLastSeenStop - TimeLastSeenStart; } }

        // Urgency - decays while on-screen, increases otherwise.
        // Events and action states influence this heavily.
        // The Urgency evolution function is non-trivial and is configurable
        public float Urgency;

        public float InterestAccumulator;
    }

    private enum ImportanceMode
    {
        Linear = 0,
        Logarithmic10 = 1,
        Logarithmic2 = 2,
    }

    ReorderableList mThreadEditList;
    public List<StoryThread> mThreads = new List<StoryThread>();

    GUIStyle mHelpBoxStyle;
    int mActiveThreadIndex = -1;
    int mHighestUrgencyIndex = -1;
    bool mSimulating;
    float mThreadHysteresisSeconds = 3f;
    ImportanceMode mDecayUrgencyeMode = ImportanceMode.Logarithmic10;
    ImportanceMode mGrowUrgencyMode = ImportanceMode.Logarithmic10;

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
        mThreadEditList = new ReorderableList(mThreads, typeof(StoryThread), false, true, true, true);
        mThreadEditList.drawHeaderCallback += delegate(Rect rect)
        {
            EditorGUI.LabelField(rect, "Threads");
        };
        mThreadEditList.onAddCallback += list =>
        {
            mThreads.Add(new StoryThread()
            {
                Name = "New thread",
                InterestLevel = 1,
                Urgency = 0,
                RateModifier = 1f,
                TimeLastSeenStart = -1,
                TimeLastSeenStop = -1
            });
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

            StoryThread thread = mThreads[index];

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


        EditorGUILayout.LabelField("Edit initial threads");
        GUI.enabled = !mSimulating;
        mThreadEditList.DoLayoutList();
        GUI.enabled = true;

        if (!mSimulating && GUILayout.Button("Start Simulation"))
        {
            foreach (var thread in mThreads)
            {
                thread.Urgency = thread.InterestLevel;
                thread.TimeLastSeenStart = -1;
                thread.TimeLastSeenStop = -1;
            }

            SetActiveThread(mThreads.OrderByDescending(t => t.Urgency).First());
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

        mThreadHysteresisSeconds = EditorGUILayout.Slider("Min story time (sec)", mThreadHysteresisSeconds, 0.5f, 10f);
        mDecayUrgencyeMode = (ImportanceMode)EditorGUILayout.EnumPopup(mDecayTypeHeader, mDecayUrgencyeMode);
        mGrowUrgencyMode = (ImportanceMode)EditorGUILayout.EnumPopup(mGrowTypeHeader, mGrowUrgencyMode);

        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            GUILayout.Label("<b>LEGEND</b>\n<color=yellow>YELLOW</color>: Queued camera\n<color=#00FF00>GREEN</color>: Active Camera", mHelpBoxStyle);
        }
    }

    private void SetActiveThread(StoryThread toThread)
    {
        if (mActiveThreadIndex != -1)
        {
            mThreads[mActiveThreadIndex].TimeLastSeenStop = (float)EditorApplication.timeSinceStartup;
            //Decay here based on temporal importance: if this one generated a lot of interest so reduce its interest level but not completely
            mThreads[mActiveThreadIndex].Urgency = Mathf.Max(mThreads[mActiveThreadIndex].Urgency, Mathf.Log10(mThreads[mActiveThreadIndex].InterestLevel));
        }

        mActiveThreadIndex = mThreads.IndexOf(toThread);
        toThread.TimeLastSeenStart = (float)EditorApplication.timeSinceStartup;
    }

    private Color GetThreadGUIColour(StoryThread thread)
    {
        int threadIdx = mThreads.IndexOf(thread);

        Color guiColour = Color.white;

        if (threadIdx == mActiveThreadIndex)
        {
            guiColour = Color.green;
        }
        else if (threadIdx == mHighestUrgencyIndex)
        {
            guiColour = Color.yellow;
        }

        return guiColour;
    }

    private void DecayStoryThreadUrgency(StoryThread thread, float deltaTime)
    {
        switch (mDecayUrgencyeMode)
        {
            case ImportanceMode.Logarithmic10:
                thread.Urgency = thread.Urgency - deltaTime * 1f / Mathf.Log10(thread.InterestLevel + 2f);
                break;

            case ImportanceMode.Logarithmic2:
                thread.Urgency = thread.Urgency - deltaTime * 1f / Mathf.Log(thread.InterestLevel + 2f);
                break;

            case ImportanceMode.Linear:
                thread.Urgency = thread.Urgency - deltaTime * 1f / thread.InterestLevel;
                break;
        }

        thread.Urgency = Mathf.Max(0f, thread.Urgency);
    }

    private void GrowStoryThreadUrgency(StoryThread thread, float deltaTime)
    {
        float urgencyDelta = 0f;
        switch (mGrowUrgencyMode)
        {
            case ImportanceMode.Logarithmic10:
                urgencyDelta = Mathf.Log10(thread.InterestLevel + 1f) * deltaTime;
                break;

            case ImportanceMode.Logarithmic2:
                urgencyDelta = Mathf.Log(thread.InterestLevel + 1f) * deltaTime;
                break;

            case ImportanceMode.Linear:
                urgencyDelta = thread.InterestLevel * deltaTime;
                break;
        }

        thread.Urgency += urgencyDelta * thread.RateModifier;
    }


    private void Update()
    {
        if (mSimulating)
        {
            Repaint();
            float deltaTime = Time.deltaTime;
            if (mActiveThreadIndex != -1)
            {
                StoryThread thread = mThreads[mActiveThreadIndex];
                thread.TimeLastSeenStop = (float)EditorApplication.timeSinceStartup;

                if (thread.LastOnScreenDuration > mThreadHysteresisSeconds)
                {
                    StoryThread nextThreadCandidate = mThreads.OrderByDescending(t => t.Urgency).First();
                    if (nextThreadCandidate != thread)
                    {
                        SetActiveThread(nextThreadCandidate);
                    }
                }
            }


            for (int i = 0; i < mThreads.Count; ++i)
            {
                StoryThread thread = mThreads[i];
                float startUrgency = thread.Urgency;

                if (i == mActiveThreadIndex)
                {
                    DecayStoryThreadUrgency(thread, deltaTime);
                }
                else
                {
                    GrowStoryThreadUrgency(thread, deltaTime);
                }

                thread.UrgencyDerivative = (thread.Urgency - startUrgency) / deltaTime;
            }

            StoryThread activeThread = mActiveThreadIndex != -1 ? mThreads[mActiveThreadIndex] : null;
            mHighestUrgencyIndex = mThreads.IndexOf(mThreads.OrderByDescending(t => (activeThread == t && activeThread.LastOnScreenDuration > mThreadHysteresisSeconds) ? 0 : t.Urgency).First());
        }
    }
}

