using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class IBLFilterGGX : IBLFilterBSDF
    {
        RenderTexture m_GgxIblSampleData;
        int           m_GgxIblMaxSampleCount          = TextureCache.isMobileBuildTarget ? 34 : 89;   // Width
        const int     k_GgxIblMipCountMinusOne        = 6;    // Height (UNITY_SPECCUBE_LOD_STEPS)

        ComputeShader m_ComputeGgxIblSampleDataCS;
        int           m_ComputeGgxIblSampleDataKernel = -1;

        ComputeShader m_BuildProbabilityTablesCS;
        int           m_ConditionalDensitiesKernel    = -1;
        int           m_MarginalRowDensitiesKernel    = -1;

        public IBLFilterGGX(RenderPipelineResources renderPipelineResources, MipGenerator mipGenerator)
        {
            m_RenderPipelineResources = renderPipelineResources;
            m_MipGenerator = mipGenerator;
        }

        public override bool IsInitialized()
        {
            return m_GgxIblSampleData != null;
        }

        public override void Initialize(CommandBuffer cmd)
        {
            if (!m_ComputeGgxIblSampleDataCS)
            {
                m_ComputeGgxIblSampleDataCS     = m_RenderPipelineResources.shaders.computeGgxIblSampleDataCS;
                m_ComputeGgxIblSampleDataKernel = m_ComputeGgxIblSampleDataCS.FindKernel("ComputeGgxIblSampleData");
            }

            if (!m_BuildProbabilityTablesCS)
            {
                m_BuildProbabilityTablesCS   = m_RenderPipelineResources.shaders.buildProbabilityTablesCS;
                m_ConditionalDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
                m_MarginalRowDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");
            }

            if (!m_convolveMaterial)
            {
                m_convolveMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelineResources.shaders.GGXConvolvePS);
            }

            if (!m_GgxIblSampleData)
            {
                m_GgxIblSampleData = new RenderTexture(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_GgxIblSampleData.useMipMap = false;
                m_GgxIblSampleData.autoGenerateMips = false;
                m_GgxIblSampleData.enableRandomWrite = true;
                m_GgxIblSampleData.filterMode = FilterMode.Point;
                m_GgxIblSampleData.name = CoreUtils.GetRenderTargetAutoName(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 1, RenderTextureFormat.ARGBHalf, "GGXIblSampleData");
                m_GgxIblSampleData.hideFlags = HideFlags.HideAndDontSave;
                m_GgxIblSampleData.Create();

                InitializeGgxIblSampleData(cmd);
            }

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                m_faceWorldToViewMatrixMatrices[i] = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...
            }
        }

        void InitializeGgxIblSampleData(CommandBuffer cmd)
        {
            m_ComputeGgxIblSampleDataCS.SetTexture(m_ComputeGgxIblSampleDataKernel, "output", m_GgxIblSampleData);

            using (new ProfilingSample(cmd, "Compute GGX IBL Sample Data"))
            {
                cmd.DispatchCompute(m_ComputeGgxIblSampleDataCS, m_ComputeGgxIblSampleDataKernel, 1, 1, 1);
            }
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_convolveMaterial);
            CoreUtils.Destroy(m_GgxIblSampleData);
        }

        void FilterCubemapCommon(CommandBuffer cmd,
            Texture source, RenderTexture target,
            Matrix4x4[] worldToViewMatrices)
        {
            int mipCount = 1 + (int)Mathf.Log(source.width, 2.0f);
            if (mipCount < ((int)EnvConstants.SpecCubeLodStep + 1))
            {
                Debug.LogWarning("RenderCubemapGGXConvolution: Cubemap size is too small for GGX convolution, needs at least " + ((int)EnvConstants.SpecCubeLodStep + 1) + " mip levels");
                return;
            }

            // Copy the first mip
            using (new ProfilingSample(cmd, "Copy Original Mip"))
            {
                for (int f = 0; f < 6; f++)
                {
                    cmd.CopyTexture(source, f, 0, target, f, 0);
                }
            }

            // Solid angle associated with a texel of the cubemap.
            float invOmegaP = (6.0f * source.width * source.width) / (4.0f * Mathf.PI);

            if (!m_GgxIblSampleData.IsCreated())
            {
                m_GgxIblSampleData.Create();
                InitializeGgxIblSampleData(cmd);
            }

            m_convolveMaterial.SetTexture("_GgxIblSamples", m_GgxIblSampleData);

            var props = new MaterialPropertyBlock();
            props.SetTexture("_MainTex", source);
            props.SetFloat("_InvOmegaP", invOmegaP);

            for (int mip = 1; mip < ((int)EnvConstants.SpecCubeLodStep + 1); ++mip)
            {
                props.SetFloat("_Level", mip);

                using (new ProfilingSample(cmd, "Filter Cubemap Mip {0}", mip))
                {
                    for (int face = 0; face < 6; ++face)
                    {
                        var faceSize = new Vector4(source.width >> mip, source.height >> mip, 1.0f / (source.width >> mip), 1.0f / (source.height >> mip));
                        var transform = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, faceSize, worldToViewMatrices[face], true);

                        props.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, transform);

                        CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, mip, (CubemapFace)face);
                        CoreUtils.DrawFullScreen(cmd, m_convolveMaterial, props);
                    }
                }
            }
        }

        // Filters MIP map levels (other than 0) with GGX using multiple importance sampling.
        override public void FilterCubemapMIS(CommandBuffer cmd,
            Texture source, RenderTexture target,
            RenderTexture conditionalCdf, RenderTexture marginalRowCdf)
        {
            // Bind the input cubemap.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "envMap", source);

            // Bind the outputs.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "conditionalDensities", conditionalCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "marginalRowDensities", marginalRowCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_MarginalRowDensitiesKernel, "marginalRowDensities", marginalRowCdf);

            int numRows = conditionalCdf.height;

            using (new ProfilingSample(cmd, "Build Probability Tables"))
            {
                cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_ConditionalDensitiesKernel, numRows, 1, 1);
                cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_MarginalRowDensitiesKernel, 1, 1, 1);
            }

            m_convolveMaterial.EnableKeyword("USE_MIS");
            m_convolveMaterial.SetTexture("_ConditionalDensities", conditionalCdf);
            m_convolveMaterial.SetTexture("_MarginalRowDensities", marginalRowCdf);

            FilterCubemapCommon(cmd, source, target, m_faceWorldToViewMatrixMatrices);
        }
        override public void FilterCubemap(CommandBuffer cmd, Texture source, RenderTexture target)
        {
            FilterCubemapCommon(cmd, source, target, m_faceWorldToViewMatrixMatrices);
        }
    }
}
