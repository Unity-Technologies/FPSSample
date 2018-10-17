using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DBufferManager : MRTBufferManager
    {
        public bool enableDecals { get; set; }

        RTHandleSystem.RTHandle m_HTile;

        // because number of render targets is not passed explicitly to SetRenderTarget, but rather deduces it from array size
        RenderTargetIdentifier[] m_RTIDs4;
        RenderTargetIdentifier[] m_RTIDs3;

        public DBufferManager()
            : base(Decal.GetMaterialDBufferCount())
        {
            Debug.Assert(m_BufferCount <= 4);
            m_RTIDs4 = new RenderTargetIdentifier[4];
            m_RTIDs3 = new RenderTargetIdentifier[3];
        }

        public override void CreateBuffers()
        {
            RenderTextureFormat[] rtFormat;
            bool[] sRGBFlags;
            Decal.GetMaterialDBufferDescription(out rtFormat, out sRGBFlags);

            for (int dbufferIndex = 0; dbufferIndex < m_BufferCount; ++dbufferIndex)
            {
                m_RTs[dbufferIndex] = RTHandles.Alloc(Vector2.one, colorFormat: rtFormat[dbufferIndex], sRGB: sRGBFlags[dbufferIndex], filterMode: FilterMode.Point, name: string.Format("DBuffer{0}", dbufferIndex));
                m_RTIDs[dbufferIndex] = m_RTs[dbufferIndex].nameID;
                m_TextureShaderIDs[dbufferIndex] = HDShaderIDs._DBufferTexture[dbufferIndex];
            }

            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            m_HTile = RTHandles.Alloc(size => new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8), filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.R8, sRGB: false, enableRandomWrite: true, name: "DBufferHTile"); // Enable UAV
        }

        override public void DestroyBuffers()
        {
            base.DestroyBuffers();
            RTHandles.Release(m_HTile);
        }

        public void ClearAndSetTargets(CommandBuffer cmd, HDCamera camera, bool rtCount4, RTHandleSystem.RTHandle cameraDepthStencilBuffer)
        {
            // for alpha compositing, color is cleared to 0, alpha to 1
            // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html

            // to avoid temporary allocations, because number of render targets is not passed explicitly to SetRenderTarget, but rather deduces it from array size
            RenderTargetIdentifier[] RTIDs = rtCount4 ? m_RTIDs4 : m_RTIDs3;
            
            // this clears the targets
            Color clearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            Color clearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
            Color clearColorAOSBlend = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            HDUtils.SetRenderTarget(cmd, camera, m_RTs[0], ClearFlag.Color, clearColor);
            HDUtils.SetRenderTarget(cmd, camera, m_RTs[1], ClearFlag.Color, clearColorNormal);
            HDUtils.SetRenderTarget(cmd, camera, m_RTs[2], ClearFlag.Color, clearColor);

            // names IDs have to be set every frame, because they can change
            RTIDs[0] = m_RTs[0].nameID;
            RTIDs[1] = m_RTs[1].nameID;
            RTIDs[2] = m_RTs[2].nameID;
            if (rtCount4)
            {
                HDUtils.SetRenderTarget(cmd, camera, m_RTs[3], ClearFlag.Color, clearColorAOSBlend);
                RTIDs[3] = m_RTs[3].nameID;
            }
            HDUtils.SetRenderTarget(cmd, camera, m_HTile, ClearFlag.Color, CoreUtils.clearColorAllBlack);

            // this actually sets the MRTs and HTile RWTexture, this is done separately because we do not have an api to clear MRTs to different colors
            HDUtils.SetRenderTarget(cmd, camera, RTIDs, cameraDepthStencilBuffer); // do not clear anymore
            cmd.SetRandomWriteTarget(rtCount4 ? 4 : 3, m_HTile);
        }

        public void UnSetHTile(CommandBuffer cmd)
        {
            cmd.ClearRandomWriteTargets();
        }

        public void SetHTileTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._DecalHTileTexture, m_HTile);
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.enableDecals)
            {
                cmd.SetGlobalInt(HDShaderIDs._EnableDecals, enableDecals ? 1 : 0);
                cmd.SetGlobalVector(HDShaderIDs._DecalAtlasResolution, new Vector2(HDUtils.hdrpSettings.decalSettings.atlasWidth, HDUtils.hdrpSettings.decalSettings.atlasHeight));
                BindBufferAsTextures(cmd);
            }
            else
            {
                cmd.SetGlobalInt(HDShaderIDs._EnableDecals, 0);
                // We still bind black textures to make sure that something is bound (can be a problem on some platforms)
                for (int i = 0; i < m_BufferCount; ++i)
                {
                    cmd.SetGlobalTexture(m_TextureShaderIDs[i], RuntimeUtilities.blackTexture);
                }
            }
        }
    }
}
