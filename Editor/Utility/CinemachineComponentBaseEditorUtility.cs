using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using NUnit.Framework;

namespace Cinemachine.Editor
{
    static class CinemachineComponentBaseEditorUtility
    {
        public static void DrawPopups(CmCamera vcam)
        {
            Assert.IsTrue(vcam.m_Components.Length == sStageData.Length);
            
            for (int i = 0; i < sStageData.Length; ++i)
            {
                int selected = GetSelectedComponent(i, vcam.m_Components);
                int newSelection = EditorGUILayout.Popup(
                    new GUIContent(sStageData[i].Name), selected, sStageData[i].PopupOptions);
                if (selected != newSelection)
                {
                    if (newSelection == 0)
                    {
                        Undo.DestroyObjectImmediate(vcam.m_Components[i]);
                        vcam.m_Components[i] = null;
                    }
                    else
                    {
                        var component = (CinemachineComponentBase) Undo.AddComponent(vcam.gameObject, sStageData[i].types[newSelection]);
                        vcam.m_Components[i] = component;
                    }
                }
            }
        }
        
        // Static state and caches - Call UpdateStaticData() to refresh this
        struct StageData
        {
            public string Name;
            public Type[] types;   // first entry is null
            public GUIContent[] PopupOptions;
        }
        static StageData[] sStageData = null;

        [InitializeOnLoad]
        static class EditorInitialize
        {
            // This code dynamically discovers eligible classes and builds the menu
            // data for the various component pipeline stages.
            static EditorInitialize()
            {
                sStageData = new StageData[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];

                var stageTypes = new List<Type>[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
                for (int i = 0; i < stageTypes.Length; ++i)
                {
                    sStageData[i].Name = ((CinemachineCore.Stage)i).ToString();
                    stageTypes[i] = new List<Type>();
                }

                // Get all ICinemachineComponents
                var allTypes
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                            (Type t) => typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract);

                foreach (var t in allTypes)
                {
                    var componentBase = (CinemachineComponentBase) Activator.CreateInstance(t);
                    stageTypes[(int)componentBase.Stage].Add(t);
                }

                // Create the static lists
                for (int i = 0; i < stageTypes.Length; ++i)
                {
                    stageTypes[i].Insert(0, null);  // first item is "none"
                    sStageData[i].types = stageTypes[i].ToArray();
                    GUIContent[] names = new GUIContent[sStageData[i].types.Length];
                    for (int n = 0; n < names.Length; ++n)
                    {
                        if (n == 0)
                        {
                            bool useSimple
                                = (i == (int)CinemachineCore.Stage.Aim)
                                    || (i == (int)CinemachineCore.Stage.Body);
                            names[n] = new GUIContent((useSimple) ? "Do nothing" : "none");
                        }
                        else
                            names[n] = new GUIContent(NicifyClassName(sStageData[i].types[n].Name));
                    }
                    sStageData[i].PopupOptions = names;
                }
            }
        }

        static int GetSelectedComponent(int i, CinemachineComponentBase[] components)
        {
            if (components[i] != null)
            {
                for (int j = 0; j < sStageData[i].PopupOptions.Length; ++j)
                {
                    if (sStageData[i].PopupOptions[j].text == NicifyClassName(components[i].GetType().Name))
                    {
                        return j;
                    }
                }
            }

            return 0;
        }
        
        public static string NicifyClassName(string name)
        {
            if (name.StartsWith("Cm"))
                name = name.Substring(2); // Trim the prefix
            return ObjectNames.NicifyVariableName(name);
        }
    }
}
