using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public class MipGenerator
    {
        RTHandle m_TempColorTarget;

        ComputeShader m_DepthPyramidCS;
        ComputeShader m_ColorPyramidCS;

        int m_DepthDownsampleKernel;
        int m_ColorDownsampleKernel;
        int m_ColorDownsampleKernelCopyMip0;
        int m_ColorGaussianKernel;

        int[] m_SrcOffset;
        int[] m_DstOffset;

        public MipGenerator(HDRenderPipelineAsset asset)
        {
            m_DepthPyramidCS = asset.renderPipelineResources.shaders.depthPyramidCS;
            m_ColorPyramidCS = asset.renderPipelineResources.shaders.colorPyramidCS;

            m_DepthDownsampleKernel = m_DepthPyramidCS.FindKernel("KDepthDownsample8DualUav");
            m_ColorDownsampleKernel = m_ColorPyramidCS.FindKernel("KColorDownsample");
            m_ColorDownsampleKernelCopyMip0 = m_ColorPyramidCS.FindKernel("KColorDownsampleCopyMip0");
            m_ColorGaussianKernel = m_ColorPyramidCS.FindKernel("KColorGaussian");
            m_SrcOffset = new int[4];
            m_DstOffset = new int[4];
        }

        public void Release()
        {
            RTHandles.Release(m_TempColorTarget);
            m_TempColorTarget = null;
        }

        // Generates an in-place depth pyramid
        // TODO: Mip-mapping depth is problematic for precision at lower mips, generate a packed atlas instead
        public void RenderMinDepthPyramid(CommandBuffer cmd, RenderTexture texture, HDUtils.PackedMipChainInfo info)
        {
            HDUtils.CheckRTCreated(texture);

            var cs     = m_DepthPyramidCS;
            int kernel = m_DepthDownsampleKernel;

            // TODO: Do it 1x MIP at a time for now. In the future, do 4x MIPs per pass, or even use a single pass.
            // Note: Gather() doesn't take a LOD parameter and we cannot bind an SRV of a MIP level,
            // and we don't support Min samplers either. So we are forced to perform 4x loads.
            for (int i = 1; i < info.mipLevelCount; i++)
            {
                Vector2Int dstSize   = info.mipLevelSizes[i];
                Vector2Int dstOffset = info.mipLevelOffsets[i];
                Vector2Int srcSize   = info.mipLevelSizes[i - 1];
                Vector2Int srcOffset = info.mipLevelOffsets[i - 1];
                Vector2Int srcLimit  = srcOffset + srcSize - Vector2Int.one;

                m_SrcOffset[0] = srcOffset.x;
                m_SrcOffset[1] = srcOffset.y;
                m_SrcOffset[2] = srcLimit.x;
                m_SrcOffset[3] = srcLimit.y;

                m_DstOffset[0] = dstOffset.x;
                m_DstOffset[1] = dstOffset.y;
                m_DstOffset[2] = 0;
                m_DstOffset[3] = 0;

                cmd.SetComputeIntParams(   cs,         HDShaderIDs._SrcOffsetAndLimit, m_SrcOffset);
                cmd.SetComputeIntParams(   cs,         HDShaderIDs._DstOffset,         m_DstOffset);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthMipChain,     texture);

                cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(dstSize.x, 8), HDUtils.DivRoundUp(dstSize.y, 8), 1);
            }
        }

        // Generates the gaussian pyramid of source into destination
        // We can't do it in place as the color pyramid has to be read while writing to the color
        // buffer in some cases (e.g. refraction, distortion)
        // Returns the number of mips
        public int RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size, Texture source, RenderTexture destination)
        {
            // Only create the temporary target on-demand in case the game doesn't actually need it
            if (m_TempColorTarget == null)
            {
                m_TempColorTarget = RTHandles.Alloc(
                    Vector2.one * 0.5f,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: RenderTextureFormat.ARGBHalf,
                    sRGB: false,
                    enableRandomWrite: true,
                    useMipMap: false,
                    enableMSAA: false,
                    name: "Temp Gaussian Pyramid Target"
                );
            }

            var cs = m_ColorPyramidCS;
            int downsampleKernel = m_ColorDownsampleKernel;
            int downsampleKernelMip0 = m_ColorDownsampleKernelCopyMip0;
            int gaussianKernel = m_ColorGaussianKernel;
            int srcMipLevel  = 0;
            int srcMipWidth  = size.x;
            int srcMipHeight = size.y;


            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            // TODO: Could be further optimized by merging the smaller mips to reduce the amount of dispatches
            // Specifically, levels 2x2 and 1x1 (or their variations, depending on the aspect ratio) should not be used.
            while (srcMipWidth >= 8 || srcMipHeight >= 8)
            {
                int dstMipWidth  = Mathf.Max(1, srcMipWidth  >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(srcMipWidth, srcMipHeight, 0f, 0f));

                // First dispatch also copies src to dst mip0
                if (srcMipLevel == 0)
                {
                    cmd.SetComputeTextureParam(cs, downsampleKernelMip0, HDShaderIDs._Source, source, 0);
                    cmd.SetComputeTextureParam(cs, downsampleKernelMip0, HDShaderIDs._Mip0, destination, 0);
                    cmd.SetComputeTextureParam(cs, downsampleKernelMip0, HDShaderIDs._Destination, m_TempColorTarget);
                    cmd.DispatchCompute(cs, downsampleKernelMip0, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);
                }
                else
                {
                    cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Source, destination, srcMipLevel);
                    cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Destination, m_TempColorTarget);
                    cmd.DispatchCompute(cs, downsampleKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);
                }

                cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(dstMipWidth, dstMipHeight, 0f, 0f));
                cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Source, m_TempColorTarget);
                cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Destination, destination, srcMipLevel + 1);
                cmd.DispatchCompute(cs, gaussianKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);

                srcMipLevel++;
                srcMipWidth  = srcMipWidth  >> 1;
                srcMipHeight = srcMipHeight >> 1;
            }

            return srcMipLevel + 1;
        }
    }
}
