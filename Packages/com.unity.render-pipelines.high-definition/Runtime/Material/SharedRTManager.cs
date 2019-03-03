using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SharedRTManager
    {
        // The render target used when we do not support MSAA
        RTHandleSystem.RTHandle m_NormalRT = null;
        RTHandleSystem.RTHandle m_VelocityRT = null;
        RTHandleSystem.RTHandle m_CameraDepthStencilBuffer = null;
        RTHandleSystem.RTHandle m_CameraDepthBufferMipChain;
        RTHandleSystem.RTHandle m_CameraStencilBufferCopy;
        HDUtils.PackedMipChainInfo m_CameraDepthBufferMipChainInfo; // This is metadata

        // The two render targets that should be used when we render in MSAA
        RTHandleSystem.RTHandle m_NormalMSAART = null;
        RTHandleSystem.RTHandle m_VelocityMSAART = null;
        // This texture must be used because reading directly from an MSAA Depth buffer is way to expensive. The solution that we went for is writing the depth in an additional color buffer (10x cheaper to solve on ps4)
        RTHandleSystem.RTHandle m_DepthAsColorMSAART = null;
        RTHandleSystem.RTHandle m_CameraDepthStencilMSAABuffer;
        // This texture stores a set of depth values that are required for evaluating a bunch of effects in MSAA mode (R = Samples Max Depth, G = Samples Min Depth, G =  Samples Average Depth)
        RTHandleSystem.RTHandle m_CameraDepthValuesBuffer = null;

        // MSAA resolve materials
        Material m_DepthResolveMaterial  = null;
        Material m_ColorResolveMaterial = null;

        // Flags that defines if we are using a local texture or external
        bool m_ReuseGBufferMemory = false;
        bool m_VelocitySupport = false;
        bool m_MSAASupported = false;
        MSAASamples m_MSAASamples = MSAASamples.None;

        // Arrays of RTIDs that are used to set render targets (when MSAA and when not MSAA)
        protected RenderTargetIdentifier[] m_RTIDs1 = new RenderTargetIdentifier[1];
        protected RenderTargetIdentifier[] m_RTIDs2 = new RenderTargetIdentifier[2];
        protected RenderTargetIdentifier[] m_RTIDs3 = new RenderTargetIdentifier[3];

        // Property block used for the resolves
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        public SharedRTManager()
        {
        }

        public void InitSharedBuffers(GBufferManager gbufferManager, RenderPipelineSettings settings, RenderPipelineResources resources)
        {
            // Set the flags
            m_MSAASupported = settings.supportMSAA;
            m_MSAASamples = m_MSAASupported ? settings.msaaSampleCount : MSAASamples.None;
            m_VelocitySupport = settings.supportMotionVectors;
            m_ReuseGBufferMemory = settings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly;

            // Create the depth/stencil buffer
            m_CameraDepthStencilBuffer = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth32, colorFormat: RenderTextureFormat.Depth, filterMode: FilterMode.Point, name: "CameraDepthStencil");

            // Create the mip chain buffer
            m_CameraDepthBufferMipChainInfo = new HDUtils.PackedMipChainInfo();
            m_CameraDepthBufferMipChainInfo.Allocate();
            m_CameraDepthBufferMipChain = RTHandles.Alloc(ComputeDepthBufferMipChainSize, colorFormat: RenderTextureFormat.RFloat, filterMode: FilterMode.Point, sRGB: false, enableRandomWrite: true, name: "CameraDepthBufferMipChain");

            // Technically we won't need this buffer in some cases, but nothing that we can determine at init time.
            m_CameraStencilBufferCopy = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.None, colorFormat: RenderTextureFormat.R8, sRGB: false, filterMode: FilterMode.Point, enableRandomWrite: true, name: "CameraStencilCopy"); // DXGI_FORMAT_R8_UINT is not supported by Unity

            if (m_VelocitySupport)
            {
                m_VelocityRT = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: Builtin.GetVelocityBufferFormat(), sRGB: Builtin.GetVelocityBufferSRGBFlag(), name: "Velocity");
                if (m_MSAASupported)
                {
                    m_VelocityMSAART = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: Builtin.GetVelocityBufferFormat(), sRGB: Builtin.GetVelocityBufferSRGBFlag(), enableMSAA: true, bindTextureMS: true, name: "VelocityMSAA");
                }
            }

            // Allocate the additional textures only if MSAA is supported
            if (m_MSAASupported)
            {
                // Let's create the MSAA textures
                m_CameraDepthStencilMSAABuffer = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth24, colorFormat: RenderTextureFormat.Depth, filterMode: FilterMode.Point, bindTextureMS: true, enableMSAA: true, name: "CameraDepthStencilMSAA");
                m_CameraDepthValuesBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBFloat, sRGB: false, name: "DepthValuesBuffer");
                m_DepthAsColorMSAART = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RFloat, sRGB: false, bindTextureMS: true, enableMSAA: true, name: "DepthAsColorMSAA");

                // We need to allocate this texture as long as msaa is supported because on both mode, one of the cameras can be forward only using the framesettings
                m_NormalMSAART = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGB32, sRGB: false, enableMSAA: true, bindTextureMS: true, name: "NormalBufferMSAA");

                // Create the required resolve materials
                m_DepthResolveMaterial = CoreUtils.CreateEngineMaterial(resources.shaders.depthValuesPS);
                m_ColorResolveMaterial = CoreUtils.CreateEngineMaterial(resources.shaders.colorResolvePS);
            }

            // If we are in the forward only mode
            if (!m_ReuseGBufferMemory)
            {
                // In case of full forward we must allocate the render target for normal buffer (or reuse one already existing)
                // TODO: Provide a way to reuse a render target
                m_NormalRT = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGB32, sRGB: false, enableRandomWrite: true, name: "NormalBuffer");
            }
            else
            {
                // When not forward only we should are using the normal buffer of the gbuffer
                // In case of deferred, we must be in sync with NormalBuffer.hlsl and lit.hlsl files and setup the correct buffers
                m_NormalRT = gbufferManager.GetNormalBuffer(0); // Normal + Roughness
            }
        }

        public bool IsConsolePlatform()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation4 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOne ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12;
        }

        // Function that will return the set of buffers required for the prepass (depending on if msaa is enabled or not)
        public RenderTargetIdentifier[] GetPrepassBuffersRTI(FrameSettings frameSettings)
        {
            if (frameSettings.enableMSAA)
            {
                Debug.Assert(m_MSAASupported);
                m_RTIDs2[0] = m_NormalMSAART.nameID;
                m_RTIDs2[1] = m_DepthAsColorMSAART.nameID;
                return m_RTIDs2;
            }
            else
            {
                m_RTIDs1[0] = m_NormalRT.nameID;
                return m_RTIDs1;
            }
        }

        // Function that will return the set of buffers required for the motion vector pass
        public RenderTargetIdentifier[] GetVelocityPassBuffersRTI(FrameSettings frameSettings)
        {
            Debug.Assert(m_VelocitySupport);
            if (frameSettings.enableMSAA)
            {
                Debug.Assert(m_MSAASupported);
                m_RTIDs3[0] = m_VelocityMSAART.nameID;
                m_RTIDs3[1] = m_NormalMSAART.nameID;
                m_RTIDs3[2] = m_DepthAsColorMSAART.nameID;
                return m_RTIDs3;
            }
            else
            {
                Debug.Assert(m_VelocitySupport);
                m_RTIDs2[0] = m_VelocityRT.nameID;
                m_RTIDs2[1] = m_NormalRT.nameID;
                return m_RTIDs2;
            }
        }

        // Request the normal buffer (MSAA or not)
        public RTHandleSystem.RTHandle GetNormalBuffer(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_NormalMSAART;
            }
            else
            {
                return m_NormalRT;
            }
        }

        // Request the velocity buffer (MSAA or not)
        public RTHandleSystem.RTHandle GetVelocityBuffer(bool isMSAA = false)
        {
            Debug.Assert(m_VelocitySupport);
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_VelocityMSAART;
            }
            else
            {
                return m_VelocityRT;
            }
        }

        // Request the depth stencil buffer (MSAA or not)
        public RTHandleSystem.RTHandle GetDepthStencilBuffer(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_CameraDepthStencilMSAABuffer;
            }
            else
            {
                return m_CameraDepthStencilBuffer;
            }
        }

        // Request the depth texture (MSAA or not)
        public RTHandleSystem.RTHandle GetDepthTexture(bool isMSAA = false)
        {
            if (isMSAA)
            {
                Debug.Assert(m_MSAASupported);
                return m_DepthAsColorMSAART;
            }
            else
            {
                return m_CameraDepthBufferMipChain;
            }
        }

        public RTHandleSystem.RTHandle GetDepthValuesTexture()
        {
            Debug.Assert(m_MSAASupported);
            return m_CameraDepthValuesBuffer;
        }

        public void SetNumMSAASamples(MSAASamples msaaSamples)
        {
            m_MSAASamples = msaaSamples;
        }

        public RTHandleSystem.RTHandle GetStencilBufferCopy()
        {
            return m_CameraStencilBufferCopy;
        }

        public Vector2Int ComputeDepthBufferMipChainSize(Vector2Int screenSize)
        {
            m_CameraDepthBufferMipChainInfo.ComputePackedMipChainInfo(screenSize);
            return m_CameraDepthBufferMipChainInfo.textureSize;
        }

        public HDUtils.PackedMipChainInfo GetDepthBufferMipChainInfo()
        {
            return m_CameraDepthBufferMipChainInfo;
        }

        public void Build(HDRenderPipelineAsset hdAsset)
        {
        }

        public void Cleanup()
        {
            if (!m_ReuseGBufferMemory)
            {
                RTHandles.Release(m_NormalRT);
            }

            if (m_VelocitySupport)
            {
                RTHandles.Release(m_VelocityRT);
                if (m_MSAASupported)
                {
                    RTHandles.Release(m_VelocityMSAART);
                }
            }

            RTHandles.Release(m_CameraDepthStencilBuffer);
            RTHandles.Release(m_CameraDepthBufferMipChain);
            RTHandles.Release(m_CameraStencilBufferCopy);

            if (m_MSAASupported)
            {
                RTHandles.Release(m_CameraDepthStencilMSAABuffer);
                RTHandles.Release(m_CameraDepthValuesBuffer);

                RTHandles.Release(m_NormalMSAART);
                RTHandles.Release(m_DepthAsColorMSAART);

                 // Do not forget to release the materials
                CoreUtils.Destroy(m_DepthResolveMaterial);
                CoreUtils.Destroy(m_ColorResolveMaterial);
            }
        }

        public static int SampleCountToPassIndex(MSAASamples samples)
        {
            switch (samples)
            {
                case MSAASamples.None:
                    return 0;
                case MSAASamples.MSAA2x:
                    return 1;
                case MSAASamples.MSAA4x:
                    return 2;
                case MSAASamples.MSAA8x:
                    return 3;
            };
            return 0;
        }


        // Bind the normal buffer that is needed
        public void BindNormalBuffer(CommandBuffer cmd, bool isMSAA = false)
        {
            // NormalBuffer can be access in forward shader, so need to set global texture
            cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture, GetNormalBuffer(isMSAA));
        }

        public void ResolveSharedRT(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.enableMSAA)
            {
                Debug.Assert(m_MSAASupported);
                using (new ProfilingSample(cmd, "ComputeDepthValues", CustomSamplerId.VolumeUpdate.GetSampler()))
                {
                    // Grab the RTIs and set the output render targets
                    m_RTIDs2[0] = m_CameraDepthValuesBuffer.nameID;
                    m_RTIDs2[1] = m_NormalRT.nameID;
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_RTIDs2, m_CameraDepthStencilBuffer);

                    // Set the input textures
                    Shader.SetGlobalTexture(HDShaderIDs._NormalTextureMS, m_NormalMSAART);
                    Shader.SetGlobalTexture(HDShaderIDs._DepthTextureMS, m_DepthAsColorMSAART);

                    // Resolve the depth and normal buffers
                    cmd.DrawProcedural(Matrix4x4.identity, m_DepthResolveMaterial, SampleCountToPassIndex(m_MSAASamples), MeshTopology.Triangles, 3, 1);
                }
            }
        }
        public void ResolveMSAAColor(CommandBuffer cmd, HDCamera hdCamera, RTHandleSystem.RTHandle msaaTarget, RTHandleSystem.RTHandle simpleTarget)
        {
            if (hdCamera.frameSettings.enableMSAA)
            {
                Debug.Assert(m_MSAASupported);
                using (new ProfilingSample(cmd, "ResolveColor", CustomSamplerId.VolumeUpdate.GetSampler()))
                {
                    // Grab the RTIs and set the output render targets
                    HDUtils.SetRenderTarget(cmd, hdCamera, simpleTarget);

                    // Set the input textures
                    m_PropertyBlock.SetTexture(HDShaderIDs._ColorTextureMS, msaaTarget);

                    // Resolve the depth and normal buffers
                    cmd.DrawProcedural(Matrix4x4.identity, m_ColorResolveMaterial, SampleCountToPassIndex(m_MSAASamples), MeshTopology.Triangles, 3, 1, m_PropertyBlock);
                }
            }
        }
    }
}
