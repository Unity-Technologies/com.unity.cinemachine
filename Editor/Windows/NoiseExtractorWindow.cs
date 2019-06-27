using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    public class NoiseExtractorWindow : EditorWindow
    {
        AnimationClip cameraMotion;
        AnimationClip targetMotion;
        CinemachineFixedSignal fixedSignal;

        float sampleFPS = 60;
        float startOffsetA;
        float startOffsetB;

        struct BindingInfo
        {
            public EditorCurveBinding[] bindings;
            public GUIContent[] bindingLabels;
            public int[] bindingIndices;
            public int selected;
        }
        BindingInfo bindingsA = new BindingInfo();
        BindingInfo bindingsB = new BindingInfo();

        // Add menu to the Window menu
        [MenuItem("Cinemachine/Tools/Noise Extractor")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            NoiseExtractorWindow window = (NoiseExtractorWindow)GetWindow(typeof(NoiseExtractorWindow));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Source", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            cameraMotion = EditorGUILayout.ObjectField(
                new GUIContent("Camera Motion"), cameraMotion,
                typeof(AnimationClip), false) as AnimationClip;
            if (bindingsA.bindings == null || EditorGUI.EndChangeCheck())
                BuildBindingInfo(cameraMotion, ref bindingsA);
//            bindingsA.selected = EditorGUILayout.Popup(
//                new GUIContent("Object"), bindingsA.selected, bindingsA.bindingLabels);

            float lenA = cameraMotion == null ? 0 : cameraMotion.length;
            EditorGUILayout.LabelField(new GUIContent($"Length = { lenA }"));
            startOffsetA = EditorGUILayout.FloatField(new GUIContent("Start Offset"), startOffsetA);

            EditorGUI.BeginChangeCheck();
            targetMotion = EditorGUILayout.ObjectField(
                new GUIContent("Target Motion"), targetMotion,
                typeof(AnimationClip), false) as AnimationClip;
            if (bindingsB.bindings == null || EditorGUI.EndChangeCheck())
                BuildBindingInfo(targetMotion, ref bindingsB);
//            bindingsB.selected = EditorGUILayout.Popup(
//                new GUIContent("Object"), bindingsB.selected, bindingsA.bindingLabels);

            float lenB = targetMotion == null ? 0 : targetMotion.length;
            EditorGUILayout.LabelField(new GUIContent($"Length = { lenB }"));
            startOffsetB = EditorGUILayout.FloatField(new GUIContent("Start Offset"), startOffsetB);

            GUILayout.Label("Output", EditorStyles.boldLabel);
            fixedSignal = EditorGUILayout.ObjectField(
                new GUIContent("Result"), fixedSignal,
                typeof(CinemachineFixedSignal), false) as CinemachineFixedSignal;
            sampleFPS = EditorGUILayout.FloatField(new GUIContent("Sample FPS"), sampleFPS);

            EditorGUILayout.GetControlRect();
            Rect r = EditorGUILayout.GetControlRect();
            GUI.enabled = fixedSignal != null
                && sampleFPS > 1
                && lenA > 0 && lenB > 0
                && bindingsA.selected >= 0 && bindingsB.selected >= 0;
            if (GUI.Button(r, new GUIContent("Generate")))
            {
                ComputeDeltas();
                AssetDatabase.SaveAssets();
            }
            GUI.enabled  = true;
        }

        void BuildBindingInfo(AnimationClip clip, ref BindingInfo info)
        {
            info.bindings = clip == null ? new EditorCurveBinding[0] : AnimationUtility.GetCurveBindings(clip);
            List<GUIContent> labels = new List<GUIContent>();
            List<int> indices = new List<int>();
            for (int i = 0; i < info.bindings.Length; ++i)
            {
                //Debug.Log(info.bindings[i].path + ": " + info.bindings[i].propertyName + " " + info.bindings[i].type.ToString());
                labels.Add(new GUIContent(info.bindings[i].path));
                indices.Add(i);
            }
            info.bindingLabels = labels.ToArray();
            info.bindingIndices = indices.ToArray();
            info.selected = labels.Count == 0 ? -1 : 0;
        }

        struct Curve3
        {
            public AnimationCurve x;
            public AnimationCurve y;
            public AnimationCurve z;

            public Vector3 Sample(float t)
            {
                return new Vector3(
                    (x == null || t < 0) ? 0 : x.Evaluate(t),
                    (y == null || t < 0) ? 0 : y.Evaluate(t),
                    (z == null || t < 0) ? 0 : z.Evaluate(t));
            }
        }

        static float NormalizeRot(float a, float prev)
        {
            if (Mathf.Abs(a - prev) < 180)
                return a;
            return (a < prev) ? a + 380 : a - 360;
        }

        Curve3 Curve3FromClip(AnimationClip clip, string path, string p)
        {
            var c = new Curve3();
            c.x = AnimationUtility.GetEditorCurve(clip, new EditorCurveBinding
                { path = path, propertyName = p + ".x", type = typeof(Transform) });
            c.y = AnimationUtility.GetEditorCurve(clip, new EditorCurveBinding
                { path = path, propertyName = p + ".y", type = typeof(Transform) });
            c.z = AnimationUtility.GetEditorCurve(clip, new EditorCurveBinding
                { path = path, propertyName = p + ".z", type = typeof(Transform) });
            return c;
        }

        void ComputeDeltas()
        {
            Curve3 posA = Curve3FromClip(cameraMotion, bindingsA.bindings[bindingsA.selected].path, "m_LocalPosition");
            Curve3 rotA = Curve3FromClip(cameraMotion, bindingsA.bindings[bindingsA.selected].path, "localEulerAnglesRaw");
            Curve3 posB = Curve3FromClip(targetMotion, bindingsB.bindings[bindingsB.selected].path, "m_LocalPosition");

            float step = 1 / sampleFPS;
            float length = cameraMotion.length - startOffsetA;
            int numKeys = Mathf.CeilToInt(length * sampleFPS) + 1;

            List<Keyframe> kX = new List<Keyframe>(numKeys);
            List<Keyframe> kY = new List<Keyframe>(numKeys);
            List<Keyframe> kZ = new List<Keyframe>(numKeys);

            Vector3 rPrev = Vector3.zero;
            for (float t = 0; t <= length; t += step)
            {
                var pA = posA.Sample(t + startOffsetA);
                var pB = posB.Sample(t + startOffsetB);

                var qA = Quaternion.Euler(rotA.Sample(t + startOffsetA));
                var fwdA = qA * Vector3.forward;
                var fwdB = pB - pA;
                if (fwdB.sqrMagnitude < 0.01f)
                    fwdB = fwdA;
                var r = Quaternion.FromToRotation(fwdB, fwdA).eulerAngles;
                if (t > 0)
                {
                    r.x = NormalizeRot(r.x, rPrev.x);
                    r.y = NormalizeRot(r.y, rPrev.y);
                    r.z = NormalizeRot(r.z, rPrev.z);
                }
                rPrev = r;
                kX.Add(new Keyframe { time = t, value = r.x });
                kY.Add(new Keyframe { time = t, value = r.y });
                kZ.Add(new Keyframe { time = t, value = r.z });
            }
            numKeys = kX.Count;
            AnimationCurve cX = new AnimationCurve { keys = kX.ToArray() };
            AnimationCurve cY = new AnimationCurve { keys = kY.ToArray() };
            AnimationCurve cZ = new AnimationCurve { keys = kZ.ToArray() };
            for (int i = 0; i < numKeys; ++i)
            {
                AnimationUtility.SetKeyLeftTangentMode(cX, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(cX, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyLeftTangentMode(cY, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(cY, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyLeftTangentMode(cZ, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(cZ, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            fixedSignal.m_XCurve = cX;
            fixedSignal.m_YCurve = cY;
            fixedSignal.m_ZCurve = cZ;
        }
    }
}
