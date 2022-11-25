using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using NUnit.Framework;

#if UNITY_2020
[InitializeOnLoad]
public class OnLoad
{
    static OnLoad()
    {
        if (GraphicsSettings.currentRenderPipeline)
        {
            if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition")){
                ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreHDRP2020", true);
            }
        }           
    }
}
#endif
