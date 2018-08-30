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

        // Time when last on-screen
        public float TimeLastSeenStart;
        public float TimeLastSeenStop;

        public float InterestDerivative;

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
    ImportanceMode mDecayImportanceMode = ImportanceMode.Linear;
    ImportanceMode mGrowImportanceMode = ImportanceMode.Logarithmic10;

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

            const string kLabelForUrgency = "Urgency";
            lableStyle.CalcMinMaxWidth(new GUIContent(kLabelForUrgency), out labelMin, out labelMax);
            labelMax += kLabelPadding;

            oldWidth = rect.width;

            labelRect = rect;
            labelRect.width = labelMax;
            EditorGUI.LabelField(labelRect, kLabelForUrgency);

            rect.xMin = labelMax + kIndentSize;
            rect.width = oldWidth - labelMax;
            thread.Urgency = EditorGUI.FloatField(rect, thread.Urgency);
        };
    }

    private void OnGUI()
    {
        if (mHelpBoxStyle == null)
        {
            mHelpBoxStyle = new GUIStyle(GUI.skin.label);
            mHelpBoxStyle.richText = true;
        }

        EditorGUILayout.LabelField("Edit initial threads");
        GUI.enabled = !mSimulating;
        mThreadEditList.DoLayoutList();
        GUI.enabled = true;

        if (!mSimulating && GUILayout.Button("Start Simulation"))
        {
            foreach (var thread in mThreads)
            {
                thread.InterestLevel = thread.Urgency;
                thread.TimeLastSeenStart = -1;
                thread.TimeLastSeenStop = -1;
            }

            SetActiveThread(mThreads.OrderByDescending(t => t.InterestLevel).First());
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
                    GUILayout.Label(thread.Urgency.ToString("0.0"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Last Active", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.TimeLastSeenStart.ToString("0.0"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Duration", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.LastOnScreenDuration.ToString("0.0"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Interest", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.InterestLevel.ToString("0.0"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Delta", EditorStyles.boldLabel);
                foreach (var thread in mThreads)
                {
                    GUI.color = GetThreadGUIColour(thread);
                    GUILayout.Label(thread.InterestDerivative.ToString("00.00"));
                }
            }

            GUI.color = Color.white;
            using (new GUILayout.VerticalScope(GUILayout.MinWidth(60f)))
            {
                GUILayout.Label("Add Interest", EditorStyles.boldLabel);
                GUI.enabled = mSimulating;
                foreach (var thread in mThreads)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("0.1", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)))
                        {
                            thread.InterestAccumulator += 0.1f;
                        }

                        if (GUILayout.Button("0.5", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)))
                        {
                            thread.InterestAccumulator += 0.5f;
                        }

                        if (GUILayout.Button("1.0", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)))
                        {
                            thread.InterestAccumulator += 1f;
                        }

                        if (GUILayout.Button("5.0", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)))
                        {
                            thread.InterestAccumulator += 5f;
                        }
                    }
                }

                GUI.enabled = true;
            }
        }

        mThreadHysteresisSeconds = EditorGUILayout.Slider("Min story time (sec)", mThreadHysteresisSeconds, 0.5f, 10f);
        mDecayImportanceMode = (ImportanceMode)EditorGUILayout.EnumPopup("Decay type", mDecayImportanceMode);
        mGrowImportanceMode = (ImportanceMode)EditorGUILayout.EnumPopup("Grow type", mGrowImportanceMode);

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
            mThreads[mActiveThreadIndex].InterestLevel = Mathf.Max(mThreads[mActiveThreadIndex].Urgency, Mathf.Log10(mThreads[mActiveThreadIndex].InterestLevel));
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

    private void DecayStoryThreadImportance(StoryThread thread, float deltaTime)
    {
        switch (mDecayImportanceMode)
        {
            case ImportanceMode.Logarithmic10:
                thread.InterestLevel = thread.InterestLevel - deltaTime * 1f / Mathf.Log10(thread.Urgency + 1f);
                break;

            case ImportanceMode.Logarithmic2:
                thread.InterestLevel = thread.InterestLevel - deltaTime * 1f / Mathf.Log(thread.Urgency + 1f);
                break;

            case ImportanceMode.Linear:
                thread.InterestLevel = thread.InterestLevel - deltaTime * 1f / thread.Urgency;
                break;
        }

        thread.InterestLevel = Mathf.Max(0f, thread.InterestLevel);
    }

    private void GrowStoryThreadImportance(StoryThread thread, float deltaTime)
    {
        float interestDelta = 0f;
        switch (mGrowImportanceMode)
        {
            case ImportanceMode.Logarithmic10:
                interestDelta = Mathf.Log10(thread.Urgency + 1f) * deltaTime;
                break;

            case ImportanceMode.Logarithmic2:
                interestDelta = Mathf.Log(thread.Urgency + 1f) * deltaTime;
                break;

            case ImportanceMode.Linear:
                interestDelta = thread.Urgency * deltaTime;
                break;
        }

        thread.InterestLevel += interestDelta;
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
                    StoryThread nextThreadCandidate = mThreads.OrderByDescending(t => t.InterestLevel).First();
                    if (nextThreadCandidate != thread)
                    {
                        SetActiveThread(nextThreadCandidate);
                    }
                }
            }


            for (int i = 0; i < mThreads.Count; ++i)
            {
                StoryThread thread = mThreads[i];
                float startImportance = thread.InterestLevel;

                thread.InterestLevel += thread.InterestAccumulator;
                thread.InterestAccumulator = 0f;

                if (i == mActiveThreadIndex)
                {
                    DecayStoryThreadImportance(thread, deltaTime);
                }
                else
                {
                    GrowStoryThreadImportance(thread, deltaTime);
                }

                thread.InterestDerivative = (thread.InterestLevel - startImportance) / deltaTime;
            }

            StoryThread activeThread = mActiveThreadIndex != -1 ? mThreads[mActiveThreadIndex] : null;
            mHighestUrgencyIndex = mThreads.IndexOf(mThreads.OrderByDescending(t => (activeThread == t && activeThread.LastOnScreenDuration > mThreadHysteresisSeconds) ? 0 : t.InterestLevel).First());
        }
    }
}

