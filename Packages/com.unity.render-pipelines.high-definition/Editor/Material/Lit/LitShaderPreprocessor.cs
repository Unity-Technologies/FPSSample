using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class LitShaderPreprocessor : BaseShaderPreprocessor
    {
        public LitShaderPreprocessor() {}

        public override bool ShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            bool isGBufferPass = snippet.passName == "GBuffer";
            bool isForwardPass = snippet.passName == "Forward";
            bool isDepthOnlyPass = snippet.passName == "DepthOnly";
            bool isTransparentPrepass = snippet.passName == "TransparentDepthPrepass";
            bool isTransparentPostpass = snippet.passName == "TransparentDepthPostpass";
            bool isTransparentBackface = snippet.passName == "TransparentBackface";
            bool isDistortionPass = snippet.passName == "DistortionVectors";
            bool isTransparentForwardPass = isTransparentPostpass || isTransparentBackface || isTransparentPrepass;

            // Using Contains to include the Tessellation variants
            bool isBuiltInLit = shader.name.Contains("HDRenderPipeline/Lit") || shader.name.Contains("HDRenderPipeline/LayeredLit") || shader.name.Contains("HDRenderPipeline/TerrainLit");

            if (isDistortionPass && !hdrpAsset.renderPipelineSettings.supportDistortion)
                return true;

            if (isTransparentBackface && !hdrpAsset.renderPipelineSettings.supportTransparentBackface)
                return true;

            if (isTransparentPrepass && !hdrpAsset.renderPipelineSettings.supportTransparentDepthPrepass)
                return true;

            if (isTransparentPostpass && !hdrpAsset.renderPipelineSettings.supportTransparentDepthPostpass)
                return true;

            // When using forward only, we never need GBuffer pass (only Forward)
            if (hdrpAsset.renderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly && isGBufferPass)
                return true;

            if(isBuiltInLit)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
                {

                    // If transparent, we never need GBuffer pass.
                    if (isGBufferPass)
                        return true;

                    // If transparent we don't need the depth only pass
                    if (isDepthOnlyPass)
                        return true;
                }
                else // Opaque
                {
                    // If opaque, we never need transparent specific passes (even in forward only mode)
                    if (isTransparentForwardPass)
                        return true;

                    if (hdrpAsset.renderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
                    {
                        // When we are in deferred, we only support tile lighting
                        if (inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting))
                            return true;

                        // If we use deferred only, MSAA is not supported.
                        if (inputData.shaderKeywordSet.IsEnabled(m_WriteMSAADepth))
                            return true;

                        if (isForwardPass && !inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                            return true;

                        if (inputData.shaderKeywordSet.IsEnabled(m_WriteNormalBuffer))
                            return true;
                    }

                    if (isDepthOnlyPass)
                    {
                        // When we are full forward, we don't have depth prepass without writeNormalBuffer
                        if (hdrpAsset.renderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly && !inputData.shaderKeywordSet.IsEnabled(m_WriteNormalBuffer))
                            return true;
                    }
                }
            }



            // TODO: Tests for later
            // We need to find a way to strip useless shader features for passes/shader stages that don't need them (example, vertex shaders won't ever need SSS Feature flag)
            // This causes several problems:
            // - Runtime code that "finds" shader variants based on feature flags might not find them anymore... thus fall backing to the "let's give a score to variant" code path that may find the wrong variant.
            // - Another issue is that if a feature is declared without a "_" fall-back, if we strip the other variants, none may be left to use! This needs to be changed on our side.
            //if (snippet.shaderType == ShaderType.Vertex && inputData.shaderKeywordSet.IsEnabled(m_FeatureSSS))
            //    return true;

            return false;
        }
    }
}
