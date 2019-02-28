using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class IBLFilterCharlie : IBLFilterBSDF
    {
        public IBLFilterCharlie(RenderPipelineResources renderPipelineResources, MipGenerator mipGenerator)
        {
            m_RenderPipelineResources = renderPipelineResources;
            m_MipGenerator = mipGenerator;
        }

        public override bool IsInitialized()
        {
            return m_convolveMaterial != null;
        }

        public override void Initialize(CommandBuffer cmd)
        {
            if (!m_convolveMaterial)
            {
                m_convolveMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelineResources.shaders.charlieConvolvePS);
            }

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                m_faceWorldToViewMatrixMatrices[i] = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...
            }
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_convolveMaterial);
            m_convolveMaterial = null;
        }

        void FilterCubemapCommon(CommandBuffer cmd,
            Texture source, RenderTexture target,
            Matrix4x4[] worldToViewMatrices)
        {
            int mipCount = 1 + (int)Mathf.Log(source.width, 2.0f);
            if (mipCount < ((int)EnvConstants.SpecCubeLodStep + 1))
            {
                Debug.LogWarning("RenderCubemapCharlieConvolution: Cubemap size is too small for Charlie convolution, needs at least " + ((int)EnvConstants.SpecCubeLodStep + 1) + " mip levels");
                return;
            }

            // Solid angle associated with a texel of the cubemap.
            float invOmegaP = (6.0f * source.width * source.width) / (4.0f * Mathf.PI);
            
            // Copy the first mip
            using (new ProfilingSample(cmd, "Copy Original Mip"))
            {
                for (int f = 0; f < 6; f++)
                {
                    cmd.CopyTexture(source, f, 0, target, f, 0);
                }
            }

            var props = new MaterialPropertyBlock();
            props.SetTexture("_MainTex", source);
            props.SetFloat("_InvOmegaP", invOmegaP);

            for (int mip = 0; mip < ((int)EnvConstants.SpecCubeLodStep + 1); ++mip)
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

        override public void FilterCubemap(CommandBuffer cmd, Texture source, RenderTexture target)
        {
            FilterCubemapCommon(cmd, source, target, m_faceWorldToViewMatrixMatrices);
        }

        public override void FilterCubemapMIS(CommandBuffer cmd, Texture source, RenderTexture target, RenderTexture conditionalCdf, RenderTexture marginalRowCdf)
        {
        }
    }
}
