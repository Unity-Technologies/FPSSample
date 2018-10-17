using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // The common shader stripper function 
    public class CommonShaderPreprocessor : BaseShaderPreprocessor
    {
        public CommonShaderPreprocessor() { }

        public override bool ShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // Strip every useless shadow configs
            var shadowInitParams = hdrpAsset.renderPipelineSettings.hdShadowInitParams;

            foreach (var punctualShadowVariant in m_PunctualShadowVariants)
            {
                if (punctualShadowVariant.Key != shadowInitParams.punctualShadowQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(punctualShadowVariant.Value))
                        return true;
            }

            foreach (var directionalShadowVariant in m_DirectionalShadowVariants)
            {
                if (directionalShadowVariant.Key != shadowInitParams.directionalShadowQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(directionalShadowVariant.Value))
                        return true;
            }

            bool isSceneSelectionPass = snippet.passName == "SceneSelectionPass";
            if (isSceneSelectionPass)
                return true;

            bool isMotionPass = snippet.passName == "Motion Vectors";
            if (!hdrpAsset.renderPipelineSettings.supportMotionVectors && isMotionPass)
                return true;

            //bool isForwardPass = (snippet.passName == "Forward") || (snippet.passName == "ForwardOnly");

            if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
            {
                // If we are transparent we use cluster lighting and not tile lighting
                if (inputData.shaderKeywordSet.IsEnabled(m_TileLighting))
                    return true;
            }
            else // Opaque
            {
                // Note: we can't assume anything regarding tile/cluster for opaque as multiple view could used different settings and it depends on MSAA
            }

            // TODO: If static lighting we can remove meta pass, but how to know that?

            // If we are in a release build, don't compile debug display variant
            // Also don't compile it if not requested by the render pipeline settings
            if ((/*!Debug.isDebugBuild || */ !hdrpAsset.renderPipelineSettings.supportRuntimeDebugDisplay) && inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_LodFadeCrossFade) && !hdrpAsset.renderPipelineSettings.supportDitheringCrossFade)
                return true;

            // Decal case

            // If decal support, remove unused variant
            if (hdrpAsset.renderPipelineSettings.supportDecals)
            {
                // Remove the no decal case
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalsOFF))
                    return true;

                // If decal but with 4RT remove 3RT variant and vice versa
                if (inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) && hdrpAsset.renderPipelineSettings.decalSettings.perChannelMask)
                    return true;

                if (inputData.shaderKeywordSet.IsEnabled(m_Decals4RT) && !hdrpAsset.renderPipelineSettings.decalSettings.perChannelMask)
                    return true;
            }
            else
            {
                // If no decal support, remove decal variant
                if (inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) || inputData.shaderKeywordSet.IsEnabled(m_Decals4RT))
                    return true;
            }


            if (inputData.shaderKeywordSet.IsEnabled(m_LightLayers) && !hdrpAsset.renderPipelineSettings.supportLightLayers)
                return true;

            return false;
        }
    }

    class HDRPreprocessShaders : IPreprocessShaders
    {
        HDRenderPipelineAsset m_CurrentHDRPAsset;

        // Track list of materials asking for specific preprocessor step
        List<BaseShaderPreprocessor> materialList;

        public HDRPreprocessShaders()
        {
            // Samplegame hack
            m_CurrentHDRPAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            materialList = HDEditorUtils.GetBaseShaderPreprocessorList();

            // EXISTING CODE; BROKEN WHEN BUILDING BATCHMOD
            /*
            // TODO: Grab correct configuration/quality asset.
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                m_CurrentHDRPAsset = hdPipeline.asset;

                materialList = HDEditorUtils.GetBaseShaderPreprocessorList();
            }
            */
        }

        public int callbackOrder { get { return 0; } }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> inputData)
        {
            // This test will also return if we are not using HDRenderPipelineAsset
            if (m_CurrentHDRPAsset == null || !m_CurrentHDRPAsset.allowShaderVariantStripping)
                return;

            int inputShaderVariantCount = inputData.Count;

            for (int i = 0; i < inputData.Count; ++i)
            {
                ShaderCompilerData input = inputData[i];

                bool removeInput = false;
                // Call list of strippers
                // Note that all strippers cumulate each other, so be aware of any conflict here
                foreach (BaseShaderPreprocessor material in materialList)
                {
                    if (material.ShadersStripper(m_CurrentHDRPAsset, shader, snippet, input))
                        removeInput = true;
                }

                if (removeInput)
                {
                    inputData.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
