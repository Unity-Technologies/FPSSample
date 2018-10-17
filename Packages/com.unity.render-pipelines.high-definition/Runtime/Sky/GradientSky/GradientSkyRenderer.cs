using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class GradientSkyRenderer : SkyRenderer
    {
        Material m_GradientSkyMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        MaterialPropertyBlock m_PropertyBlock;
        GradientSky m_GradientSkyParams;

        readonly int _GradientBottom = Shader.PropertyToID("_GradientBottom");
        readonly int _GradientMiddle = Shader.PropertyToID("_GradientMiddle");
        readonly int _GradientTop = Shader.PropertyToID("_GradientTop");
        readonly int _GradientDiffusion = Shader.PropertyToID("_GradientDiffusion");

        public GradientSkyRenderer(GradientSky GradientSkyParams)
        {
            m_GradientSkyParams = GradientSkyParams;
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        public override void Build()
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            m_GradientSkyMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.gradientSkyPS);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_GradientSkyMaterial);
        }

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer);
            }
            else
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer, builtinParams.depthBuffer);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            m_GradientSkyMaterial.SetColor(_GradientBottom, m_GradientSkyParams.bottom);
            m_GradientSkyMaterial.SetColor(_GradientMiddle, m_GradientSkyParams.middle);
            m_GradientSkyMaterial.SetColor(_GradientTop, m_GradientSkyParams.top);
            m_GradientSkyMaterial.SetFloat(_GradientDiffusion, m_GradientSkyParams.gradientDiffusion);

            // This matrix needs to be updated at the draw call frequency.
            m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_GradientSkyMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
        }

        public override bool IsValid()
        {
            return m_GradientSkyParams != null && m_GradientSkyMaterial != null;
        }
    }
}
