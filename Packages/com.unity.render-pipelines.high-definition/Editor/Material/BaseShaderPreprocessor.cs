using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public abstract class BaseShaderPreprocessor
    {
        // Common keyword list
        protected ShaderKeyword m_Transparent;
        protected ShaderKeyword m_DebugDisplay;
        protected ShaderKeyword m_TileLighting;
        protected ShaderKeyword m_ClusterLighting;
        protected ShaderKeyword m_LodFadeCrossFade;
        protected ShaderKeyword m_DecalsOFF;
        protected ShaderKeyword m_Decals3RT;
        protected ShaderKeyword m_Decals4RT;
        protected ShaderKeyword m_LightLayers;
        protected ShaderKeyword m_ShadowLow;
        protected ShaderKeyword m_ShadowMedium;
        protected ShaderKeyword m_ShadowHigh;
        protected ShaderKeyword m_WriteNormalBuffer;
        protected ShaderKeyword m_WriteMSAADepth;
        protected ShaderKeyword m_SubsurfaceScattering;

        protected Dictionary<HDShadowQuality, ShaderKeyword> m_ShadowVariants;

        public BaseShaderPreprocessor()
        {
            // NOTE: All these keyword should be automatically stripped so there's no need to handle them ourselves.
            // LIGHTMAP_ON, DIRLIGHTMAP_COMBINED, DYNAMICLIGHTMAP_ON, LIGHTMAP_SHADOW_MIXING, SHADOWS_SHADOWMASK
            // FOG_LINEAR, FOG_EXP, FOG_EXP2
            // STEREO_INSTANCING_ON, STEREO_MULTIVIEW_ON, STEREO_CUBEMAP_RENDER_ON, UNITY_SINGLE_PASS_STEREO
            // INSTANCING_ON
            m_Transparent = new ShaderKeyword("_SURFACE_TYPE_TRANSPARENT");
            m_DebugDisplay = new ShaderKeyword("DEBUG_DISPLAY");
            m_TileLighting = new ShaderKeyword("USE_FPTL_LIGHTLIST");
            m_ClusterLighting = new ShaderKeyword("USE_CLUSTERED_LIGHTLIST");
            m_LodFadeCrossFade = new ShaderKeyword("LOD_FADE_CROSSFADE");
            m_DecalsOFF = new ShaderKeyword("DECALS_OFF");
            m_Decals3RT = new ShaderKeyword("DECALS_3RT");
            m_Decals4RT = new ShaderKeyword("DECALS_4RT");
            m_LightLayers = new ShaderKeyword("LIGHT_LAYERS");
            m_ShadowLow = new ShaderKeyword("SHADOW_LOW");
            m_ShadowMedium = new ShaderKeyword("SHADOW_MEDIUM");
            m_ShadowHigh = new ShaderKeyword("SHADOW_HIGH");
            m_WriteNormalBuffer = new ShaderKeyword("WRITE_NORMAL_BUFFER");
            m_WriteMSAADepth = new ShaderKeyword("WRITE_MSAA_DEPTH");
            m_SubsurfaceScattering = new ShaderKeyword("OUTPUT_SPLIT_LIGHTING");

            m_ShadowVariants = new Dictionary<HDShadowQuality, ShaderKeyword>
            {
                {HDShadowQuality.Low, m_ShadowLow},
                {HDShadowQuality.Medium, m_ShadowMedium},
                {HDShadowQuality.High, m_ShadowHigh},
            };
        }

        public abstract bool ShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData);
    }
}
