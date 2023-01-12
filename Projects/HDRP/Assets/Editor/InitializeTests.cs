using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using NUnit.Framework;


[InitializeOnLoad]
public class OnLoad
{

    static OnLoad()
    {
#if UNITY_2020
        if (GraphicsSettings.currentRenderPipeline)
        {
            if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition")){
                ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreHDRP2020", true);
            }
        }     
#endif
#if CINEMACHINE_HDRP
        ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreHDRPInstability", true);
#endif
    }
}

