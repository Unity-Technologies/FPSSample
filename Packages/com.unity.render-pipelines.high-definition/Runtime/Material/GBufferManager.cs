using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This enum allow to identify which render target is used for a specific feature
    public enum GBufferUsage
    {
        None,
        SubsurfaceScattering,
        Normal,
        LightLayers,
        ShadowMask
    }

    public class GBufferManager : MRTBufferManager
    {
        RenderPipelineMaterial m_DeferredMaterial;
        // This contain the usage for all allocated buffer
        protected GBufferUsage[] m_GBufferUsage;
        // This is the index of the gbuffer use for shadowmask and lightlayers, if any
        protected int m_ShadowMaskIndex = -1;
        protected int m_LightLayers = -1;
        protected HDRenderPipelineAsset m_asset;
        // We need to store current set of RT to bind exactly, as we can have dynamic RT (LightLayers, ShadowMask), we allocated an array for each possible size (to avoid gardbage collect pressure)
        protected RenderTargetIdentifier[][] m_RTIDsArray = new RenderTargetIdentifier[8][];

        public GBufferManager(HDRenderPipelineAsset asset, RenderPipelineMaterial deferredMaterial)
            : base(deferredMaterial.GetMaterialGBufferCount(asset))
        {
            Debug.Assert(m_BufferCount <= 8);

            for (int i = 0; i < m_BufferCount; ++i)
            {
                m_RTIDsArray[i] = new RenderTargetIdentifier[i + 1];
            }

            m_DeferredMaterial = deferredMaterial;
            m_asset = asset;
        }

        public override void CreateBuffers()
        {
            RenderTextureFormat[] rtFormat;
            bool[] sRGBFlags;
            bool[] enableWrite;
            m_DeferredMaterial.GetMaterialGBufferDescription(m_asset, out rtFormat, out sRGBFlags, out m_GBufferUsage, out enableWrite);

            for (int gbufferIndex = 0; gbufferIndex < m_BufferCount; ++gbufferIndex)
            {
                m_RTs[gbufferIndex] = RTHandles.Alloc(Vector2.one, colorFormat: rtFormat[gbufferIndex], sRGB: sRGBFlags[gbufferIndex], filterMode: FilterMode.Point, name: string.Format("GBuffer{0}", gbufferIndex), enableRandomWrite: enableWrite[gbufferIndex]); 
                m_RTIDs[gbufferIndex] = m_RTs[gbufferIndex].nameID;
                m_TextureShaderIDs[gbufferIndex] = HDShaderIDs._GBufferTexture[gbufferIndex];

                if (m_GBufferUsage[gbufferIndex] == GBufferUsage.ShadowMask)
                    m_ShadowMaskIndex = gbufferIndex;
                else if (m_GBufferUsage[gbufferIndex] == GBufferUsage.LightLayers)
                    m_LightLayers = gbufferIndex;
            }
        }

        public override void BindBufferAsTextures(CommandBuffer cmd)
        {
            for (int i = 0; i < m_BufferCount; ++i)
            {
                cmd.SetGlobalTexture(m_TextureShaderIDs[i], m_RTs[i]);
            }

            // Bind alias for gbuffer usage to simplify shader code (not need to check which gbuffer is the shadowmask or lightlayers)
            if (m_ShadowMaskIndex >= 0)
                cmd.SetGlobalTexture(HDShaderIDs._ShadowMaskTexture, m_RTs[m_ShadowMaskIndex]);
            if (m_LightLayers >= 0)
                cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, m_RTs[m_LightLayers]);
            else
                cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, RuntimeUtilities.whiteTexture); // This is never use but need to be bind as the read is inside a if
        }

        // This function will setup the required render target array. This take into account if shadow mask and light layers are enabled or not.
        // Note for the future: Setup works fine as we don't have change per object (like velocity for example). If in the future it is the case
        // the changing per object buffer MUST be the last one so the shader can decide if it write to it or not
        public RenderTargetIdentifier[] GetBuffersRTI(FrameSettings frameSettings)
        {
            // We do two loop to avoid to have to allocate an array every frame
            // Do a first step to know how many RT we require
            int gbufferIndex = 0;
            for (int i = 0; i < m_BufferCount; ++i)
            {
                if (m_GBufferUsage[i] == GBufferUsage.ShadowMask && !frameSettings.enableShadowMask)
                    continue; // Skip

                if (m_GBufferUsage[i] == GBufferUsage.LightLayers && !frameSettings.enableLightLayers)
                    continue; // Skip

                gbufferIndex++;
            }

            // Now select the correct array and do another loop to fill the array
            RenderTargetIdentifier[] m_RTIDsArrayCurrent = m_RTIDsArray[gbufferIndex - 1];

            gbufferIndex = 0;
            // nameID can change from one frame to another depending on the msaa flag so so we need to update this array to be sure it's up to date.
            for (int i = 0; i < m_BufferCount; ++i)
            {
                if (m_GBufferUsage[i] == GBufferUsage.ShadowMask && !frameSettings.enableShadowMask)
                    continue; // Skip

                if (m_GBufferUsage[i] == GBufferUsage.LightLayers && !frameSettings.enableLightLayers)
                    continue; // Skip

                m_RTIDsArrayCurrent[gbufferIndex] = m_RTs[i].nameID;
                gbufferIndex++;
            }

            return m_RTIDsArrayCurrent;
        }

        public RTHandleSystem.RTHandle GetNormalBuffer(int index)
        {
            int currentIndex = 0;
            for (int gbufferIndex = 0; gbufferIndex < m_BufferCount; ++gbufferIndex)
            {
                if (m_GBufferUsage[gbufferIndex] == GBufferUsage.Normal)
                {
                    if (currentIndex == index)
                        return m_RTs[gbufferIndex];

                    // This is not the index we are looking for, find the next one
                    currentIndex++;
                }                    
            }

            return null;
        }

        public RTHandleSystem.RTHandle GetSubsurfaceScatteringBuffer(int index)
        {
            int currentIndex = 0;
            for (int gbufferIndex = 0; gbufferIndex < m_BufferCount; ++gbufferIndex)
            {
                if (m_GBufferUsage[gbufferIndex] == GBufferUsage.SubsurfaceScattering)
                {
                    if (currentIndex == index)
                        return m_RTs[gbufferIndex];

                    // This is not the index we are looking for, find the next one
                    currentIndex++;
                }
            }

            return null;
        }
    }
}
