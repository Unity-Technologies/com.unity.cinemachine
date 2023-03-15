#if HDRP_1_OR_NEWER
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public static class MaterialUpgradeHelper
    {
        public static List<MaterialUpgrader> GetHDRPMaterialUpgraders()
        {
            return new List<MaterialUpgrader>
            {
                new StandardsToHDLitMaterialUpgrader("Standard", "HDRP/Lit"),
                new StandardsToHDLitMaterialUpgrader("Standard (Specular setup)", "HDRP/Lit"),
                new StandardsToHDLitMaterialUpgrader("Standard (Roughness setup)", "HDRP/Lit"),
                new UnlitsToHDUnlitUpgrader("Unlit/Color", "HDRP/Unlit"),
                new UnlitsToHDUnlitUpgrader("Unlit/Texture", "HDRP/Unlit"),
                new UnlitsToHDUnlitUpgrader("Unlit/Transparent", "HDRP/Unlit"),
                new UnlitsToHDUnlitUpgrader("Unlit/Transparent Cutout", "HDRP/Unlit"),
            };
        }
    }
}
#endif