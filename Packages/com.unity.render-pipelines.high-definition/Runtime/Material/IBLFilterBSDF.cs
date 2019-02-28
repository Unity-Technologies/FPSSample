using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    abstract public class IBLFilterBSDF
    {
        // Material that convolves the cubemap using the profile
        protected Material m_convolveMaterial;
        protected Matrix4x4[] m_faceWorldToViewMatrixMatrices = new Matrix4x4[6];

        // Input data
        protected RenderPipelineResources m_RenderPipelineResources;
        protected MipGenerator m_MipGenerator;

        abstract public bool IsInitialized();

        abstract public void Initialize(CommandBuffer cmd);

        abstract public void Cleanup();

        // Filters MIP map levels (other than 0) with GGX using BRDF importance sampling.
        abstract public void FilterCubemap(CommandBuffer cmd, Texture source, RenderTexture target);

        public void FilterPlanarTexture(CommandBuffer cmd, RenderTexture source, RenderTexture target)
        {
            m_MipGenerator.RenderColorGaussianPyramid(cmd, new Vector2Int(source.width, source.height), source, target);
        }

        public abstract void FilterCubemapMIS(CommandBuffer cmd, Texture source, RenderTexture target, RenderTexture conditionalCdf, RenderTexture marginalRowCdf);
    }
}
