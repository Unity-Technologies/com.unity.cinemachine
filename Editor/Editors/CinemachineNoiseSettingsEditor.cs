using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(NoiseSettings))]
    internal sealed class NoiseSettingsEditor : BaseEditor<NoiseSettings>
    {
        private const float vSpace = 2;
        private const float hSpace = 3;

        // Needed for accessing string names of fields
        private NoiseSettings.TransformNoiseParams tpDef;
        private NoiseSettings.NoiseParams npDef;

        private static float mPreviewTime = 1;
        private static float mPreviewHeight = 5;

        private ReorderableList[] mPosChannels;
        private ReorderableList[] mRotChannels;
        private static GUIContent[] mPoslabels = new GUIContent[] 
        { 
            new GUIContent("Position X"), 
            new GUIContent("Position Y"), 
            new GUIContent("Position Z") 
        };
        private static GUIContent[] mRotlabels = new GUIContent[] 
        { 
            new GUIContent("Rotation X"), 
            new GUIContent("Rotation Y"), 
            new GUIContent("Rotation Z") 
        };
        private static bool[] mPosExpanded = new bool[3];
        private static bool[] mRotExpanded = new bool[3];

        protected override List<string> GetExcludedPropertiesInInspector()
        {
            var excluded = base.GetExcludedPropertiesInInspector();
            excluded.Add(FieldPath(x => Target.PositionNoise));
            excluded.Add(FieldPath(x => Target.OrientationNoise));
            return excluded;
        }

        public override void OnInspectorGUI()
        {
            if (mPosChannels == null)
                mPosChannels = SetupReorderableLists(
                    serializedObject.FindProperty(() => Target.PositionNoise), mPoslabels);
            if (mRotChannels == null)
                mRotChannels = SetupReorderableLists(
                    serializedObject.FindProperty(() => Target.OrientationNoise), mRotlabels);

            BeginInspector();

            Rect r = EditorGUILayout.GetControlRect();
            mPreviewTime = EditorGUI.Slider(r, "Preview Time", mPreviewTime, 0.01f, 10f);
            r = EditorGUILayout.GetControlRect();
            mPreviewHeight = EditorGUI.Slider(r, "Preview Height", mPreviewHeight, 1f, 10f);
            EditorGUILayout.Separator();

            r = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(r, "Position Noise", EditorStyles.boldLabel);
            r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
            DrawNoisePreview(r, Target.PositionNoise, 7);
            for (int i = 0; i < mPosChannels.Length; ++i)
            {
                r = EditorGUILayout.GetControlRect();
                mPosExpanded[i] = EditorGUI.Foldout(r, mPosExpanded[i], mPoslabels[i]);
                if (mPosExpanded[i])
                {
                    r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
                    DrawNoisePreview(r, Target.PositionNoise, 1 << i);
                    mPosChannels[i].DoLayoutList();
                }
            }
            EditorGUILayout.Separator();

            r = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(r, "Rotation Noise", EditorStyles.boldLabel);
            r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
            DrawNoisePreview(r, Target.OrientationNoise, 7);
            for (int i = 0; i < mPosChannels.Length; ++i)
            {
                r = EditorGUILayout.GetControlRect();
                mRotExpanded[i] = EditorGUI.Foldout(r, mRotExpanded[i], mRotlabels[i]);
                if (mRotExpanded[i])
                {
                    r = EditorGUILayout.GetControlRect(true, mPreviewHeight * EditorGUIUtility.singleLineHeight);
                    DrawNoisePreview(r, Target.OrientationNoise, 1 << i);
                    mRotChannels[i].DoLayoutList();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private List<Vector3> mSampleCurveX = new List<Vector3>();
        private List<Vector3> mSampleCurveY = new List<Vector3>();
        private List<Vector3> mSampleCurveZ = new List<Vector3>();
        private List<Vector3> mSampleNoise = new List<Vector3>();

        private void GetSampleCurve(
            Rect r, NoiseSettings.TransformNoiseParams[] signal, 
            int numSamples)
        {
            float maxVal = 0;
            mSampleNoise.Clear(); 
            for (int i = 0; i < numSamples; ++i)
            {
                float t = (float)i / (numSamples - 1);
                Vector3 p = NoiseSettings.GetCombinedFilterResults(signal, t * mPreviewTime, Vector3.zero);
                maxVal = Mathf.Max(maxVal, Mathf.Abs(p.x));
                maxVal = Mathf.Max(maxVal, Mathf.Abs(p.y));
                maxVal = Mathf.Max(maxVal, Mathf.Abs(p.z));
                mSampleNoise.Add(p);
            }
            mSampleCurveX.Clear(); 
            mSampleCurveY.Clear(); 
            mSampleCurveZ.Clear(); 
            float halfHeight = r.height / 2;
            float yOffset = r.y + halfHeight;
            for (int i = 0; i < numSamples; ++i)
            {
                float t = (float)i / (numSamples - 1);
                Vector3 p = mSampleNoise[i];
                mSampleCurveX.Add(new Vector3(r.width * t + r.x, halfHeight * (-p.x / maxVal) + yOffset, 0));
                mSampleCurveY.Add(new Vector3(r.width * t + r.x, halfHeight * (-p.y / maxVal) + yOffset, 0));
                mSampleCurveZ.Add(new Vector3(r.width * t + r.x, halfHeight * (-p.z / maxVal) + yOffset, 0));
            }
        }

        private void DrawNoisePreview(
            Rect r, NoiseSettings.TransformNoiseParams[] signal, int channelMask)
        {
            EditorGUI.DrawRect(r, Color.black);
            GetSampleCurve(r, signal, (int)(r.width / 2));
            if ((channelMask & 1) != 0)
            {
                Handles.color = new Color(1, 0.5f, 0, 0.8f); 
                Handles.DrawAAPolyLine(mSampleCurveX.ToArray());
            }
            if ((channelMask & 2) != 0)
            {
                Handles.color = new Color(0, 1, 0, 0.8f); 
                Handles.DrawAAPolyLine(mSampleCurveY.ToArray());
            }
            if ((channelMask & 4) != 0)
            {
                Handles.color = new Color(0, 0.5f, 1, 0.8f); 
                Handles.DrawAAPolyLine(mSampleCurveZ.ToArray());
            }
        }
        
        private ReorderableList[] SetupReorderableLists(
            SerializedProperty property, GUIContent[] titles)
        {
            ReorderableList[] lists = new ReorderableList[3];
            for (int i = 0; i < 3; ++i)
                lists[i] = SetupReorderableList(property, i, new GUIContent("Components"));
            return lists;
        }

        private ReorderableList SetupReorderableList(
            SerializedProperty property, int channel, GUIContent title)
        {
            ChannelList list = new ChannelList(
                property.serializedObject, property, channel, title);

            GUIContent steadyLabel = new GUIContent("(constant amplitude if checked)");
            float steadyLabelWidth = GUI.skin.label.CalcSize(steadyLabel).x;
            NoiseSettings.TransformNoiseParams[] signalArray = new NoiseSettings.TransformNoiseParams[1];

            list.drawHeaderCallback = (Rect rect) =>
                {
                    Rect r = rect;
                    EditorGUI.LabelField(r, list.mTitle);
                    r.x = rect.x + rect.width - steadyLabelWidth; r.width = steadyLabelWidth;
                    EditorGUI.LabelField(r, steadyLabel);
                };

            list.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                    switch (list.mChannel)
                    {
                        case 0: DrawNoiseChannel(rect, element.FindPropertyRelative(() => tpDef.X)); break;
                        case 1: DrawNoiseChannel(rect, element.FindPropertyRelative(() => tpDef.Y)); break;
                        case 2: DrawNoiseChannel(rect, element.FindPropertyRelative(() => tpDef.Z)); break;
                        default: break;
                    }
                };

            list.onAddCallback = (ReorderableList l) =>
                {
                    var index = l.serializedProperty.arraySize;
                    ++l.serializedProperty.arraySize;
                    SerializedProperty p = l.serializedProperty.GetArrayElementAtIndex(index);
                    ClearComponent(p.FindPropertyRelative(() => tpDef.X));
                    ClearComponent(p.FindPropertyRelative(() => tpDef.Y));
                    ClearComponent(p.FindPropertyRelative(() => tpDef.Z));
                };

            list.onRemoveCallback = (ReorderableList l) =>
                {
                    // Can't just delete because the component arrays are connected
                    SerializedProperty p = l.serializedProperty.GetArrayElementAtIndex(l.index);
                    bool IsClear 
                        =  (list.mChannel == 0 || IsClearComponent(p.FindPropertyRelative(() => tpDef.X)))
                        && (list.mChannel == 1 || IsClearComponent(p.FindPropertyRelative(() => tpDef.Y)))
                        && (list.mChannel == 2 || IsClearComponent(p.FindPropertyRelative(() => tpDef.Z)));
                    if (IsClear)
                        l.serializedProperty.DeleteArrayElementAtIndex(l.index);
                    else switch (list.mChannel)
                    {
                        case 0: ClearComponent(p.FindPropertyRelative(() => tpDef.X)); break;
                        case 1: ClearComponent(p.FindPropertyRelative(() => tpDef.Y)); break;
                        case 2: ClearComponent(p.FindPropertyRelative(() => tpDef.Z)); break;
                        default: break;
                    }
                };

            return list;
        }

        class ChannelList : ReorderableList
        {
            public int mChannel;
            public GUIContent mTitle;

            public ChannelList(
                SerializedObject serializedObject, 
                SerializedProperty elements, 
                int channel, GUIContent title)
            : base(serializedObject, elements, true, true, true, true)
            {
                mChannel = channel;
                mTitle = title;
            }
        };
        
        private GUIContent steadyLabel;
        private float steadyLabelWidth;
        private GUIContent freqLabel;
        private float freqLabelWidth;
        private GUIContent ampLabel;
        private float ampLabelWidth;

        private void InitializeLabels(SerializedProperty property)
        {
            if (steadyLabel == null)
            {
                SerializedProperty p = property.FindPropertyRelative(() => npDef.Constant);
                steadyLabel = new GUIContent(p.displayName, p.tooltip) { text = " " };
                steadyLabelWidth = GUI.skin.label.CalcSize(steadyLabel).x;
            }
            if (freqLabel == null)
            {
                SerializedProperty p = property.FindPropertyRelative(() => npDef.Frequency);
                freqLabel = new GUIContent(p.displayName, p.tooltip);
                freqLabelWidth = GUI.skin.label.CalcSize(freqLabel).x;
            }
            if (ampLabel == null)
            {
                SerializedProperty p = property.FindPropertyRelative(() => npDef.Amplitude);
                ampLabel = new GUIContent(p.displayName, p.tooltip);
                ampLabelWidth = GUI.skin.label.CalcSize(ampLabel).x;
            }
        }

        private void DrawNoiseChannel(Rect rect, SerializedProperty property)
        {
            InitializeLabels(property);

            // Needed for accessing string names of fields
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            Rect r = rect;
            r.height -= vSpace;
            r.width -= EditorGUIUtility.singleLineHeight + hSpace;
            r.width /= 2;

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = freqLabelWidth;
            EditorGUI.PropertyField(r, property.FindPropertyRelative(() => npDef.Frequency), freqLabel);

            r.x += r.width + hSpace;
            EditorGUIUtility.labelWidth = ampLabelWidth;
            EditorGUI.PropertyField(r, property.FindPropertyRelative(() => npDef.Amplitude), ampLabel);

            r.y -= 1;
            r.x += r.width + hSpace; r.width = EditorGUIUtility.singleLineHeight + hSpace;
            EditorGUIUtility.labelWidth = hSpace;
            EditorGUI.PropertyField(r, property.FindPropertyRelative(() => npDef.Constant), steadyLabel);

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        // SerializedProperty is a NoiseSettings.NoiseParam
        void ClearComponent(SerializedProperty p)
        {
            p.FindPropertyRelative(() => npDef.Amplitude).floatValue = 0;
            p.FindPropertyRelative(() => npDef.Frequency).floatValue = 0;
            p.FindPropertyRelative(() => npDef.Constant).boolValue = false;
        }

        // SerializedProperty is a NoiseSettings.NoiseParam
        bool IsClearComponent(SerializedProperty p)
        {
            return p.FindPropertyRelative(() => npDef.Amplitude).floatValue == 0
                && p.FindPropertyRelative(() => npDef.Frequency).floatValue == 0;
        }
    }
}
