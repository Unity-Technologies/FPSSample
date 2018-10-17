using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class UnlitsToHDUnlitUpgrader : MaterialUpgrader
    {
        string Unlit_Color = "Unlit/Color";
        //string Unlit_Texture = "Unlit/Texture";
        string Unlit_Transparent = "Unlit/Transparent";
        string Unlit_Cutout = "Unlit/Transparent Cutout";

        public UnlitsToHDUnlitUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);

            if (sourceShaderName == Unlit_Color)
                RenameColor("_Color", "_UnlitColor");
            else // all other unlit have a texture
                RenameTexture("_MainTex", "_UnlitColorMap");

            if (sourceShaderName == Unlit_Cutout)
            {
                RenameFloat("_Cutoff", "_AlphaCutoff");
                SetFloat("_AlphaCutoffEnable", 1f);
            }
            else
                SetFloat("_AlphaCutoffEnable", 0f);


            SetFloat("_SurfaceType", (sourceShaderName == Unlit_Transparent) ? 1f : 0f);
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            //dstMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;

            base.Convert(srcMaterial, dstMaterial);

            HDEditorUtils.ResetMaterialKeywords(dstMaterial);
        }
    }
}
