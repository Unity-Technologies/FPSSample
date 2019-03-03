using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class PlanarReflectionProbeCache
    {
        internal static readonly int s_InputTexID = Shader.PropertyToID("_InputTex");
        internal static readonly int s_LoDID = Shader.PropertyToID("_LoD");
        internal static readonly int s_FaceIndexID = Shader.PropertyToID("_FaceIndex");

        enum ProbeFilteringState
        {
            Convolving,
            Ready
        }

        int                     m_ProbeSize;
        int                     m_CacheSize;
        IBLFilterGGX            m_IBLFilterGGX;
        TextureCache2D          m_TextureCache;
        RenderTexture           m_TempRenderTexture;
        RenderTexture           m_ConvolutionTargetTexture;
        ProbeFilteringState[]   m_ProbeBakingState;
        Material                m_ConvertTextureMaterial;
        MaterialPropertyBlock   m_ConvertTextureMPB;
        bool                    m_PerformBC6HCompression;

        public PlanarReflectionProbeCache(HDRenderPipelineAsset hdAsset, IBLFilterGGX iblFilter, int cacheSize, int probeSize, TextureFormat probeFormat, bool isMipmaped)
        {
            m_ConvertTextureMaterial = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.blitCubeTextureFacePS);
            m_ConvertTextureMPB = new MaterialPropertyBlock();

            // BC6H requires CPP feature not yet available
            probeFormat = TextureFormat.RGBAHalf;

            Debug.Assert(probeFormat == TextureFormat.BC6H || probeFormat == TextureFormat.RGBAHalf, "Reflection Probe Cache format for HDRP can only be BC6H or FP16.");

            m_ProbeSize = probeSize;
            m_CacheSize = cacheSize;
            m_TextureCache = new TextureCache2D("PlanarReflectionProbe");
            m_TextureCache.AllocTextureArray(cacheSize, probeSize, probeSize, probeFormat, isMipmaped);
            m_IBLFilterGGX = iblFilter;

            m_PerformBC6HCompression = probeFormat == TextureFormat.BC6H;

            InitializeProbeBakingStates();
        }

        void Initialize()
        {
            if (m_TempRenderTexture == null)
            {
                // Temporary RT used for convolution and compression
                m_TempRenderTexture = new RenderTexture(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf);
                m_TempRenderTexture.hideFlags = HideFlags.HideAndDontSave;
                m_TempRenderTexture.dimension = TextureDimension.Tex2D;
                m_TempRenderTexture.useMipMap = true;
                m_TempRenderTexture.autoGenerateMips = false;
                m_TempRenderTexture.name = CoreUtils.GetRenderTargetAutoName(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf, "PlanarReflectionTemp", mips: true);
                m_TempRenderTexture.Create();

                m_ConvolutionTargetTexture = new RenderTexture(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf);
                m_ConvolutionTargetTexture.hideFlags = HideFlags.HideAndDontSave;
                m_ConvolutionTargetTexture.dimension = TextureDimension.Tex2D;
                m_ConvolutionTargetTexture.useMipMap = true;
                m_ConvolutionTargetTexture.autoGenerateMips = false;
                m_ConvolutionTargetTexture.name = CoreUtils.GetRenderTargetAutoName(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf, "PlanarReflectionConvolution", mips: true);
                m_ConvolutionTargetTexture.enableRandomWrite = true;
                m_ConvolutionTargetTexture.Create();

                InitializeProbeBakingStates();
            }
        }

        void InitializeProbeBakingStates()
        {
            m_ProbeBakingState = new ProbeFilteringState[m_CacheSize];
            for (int i = 0; i < m_CacheSize; ++i)
                m_ProbeBakingState[i] = ProbeFilteringState.Convolving;
        }

        public void Release()
        {
            m_TextureCache.Release();
            CoreUtils.Destroy(m_TempRenderTexture);
            CoreUtils.Destroy(m_ConvolutionTargetTexture);

            m_ProbeBakingState = null;

            CoreUtils.Destroy(m_ConvertTextureMaterial);
        }

        public void NewFrame()
        {
            Initialize();
            m_TextureCache.NewFrame();
        }

        // This method is used to convert inputs that are either compressed or not of the right size.
        // We can't use Graphics.ConvertTexture here because it does not work with a RenderTexture as destination.
        void ConvertTexture(CommandBuffer cmd, Texture input, RenderTexture target)
        {
            m_ConvertTextureMPB.SetTexture(s_InputTexID, input);
            m_ConvertTextureMPB.SetFloat(s_LoDID, 0.0f); // We want to convert mip 0 to whatever the size of the destination cache is.
            CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, Color.black, 0, 0);
            CoreUtils.DrawFullScreen(cmd, m_ConvertTextureMaterial, m_ConvertTextureMPB);
        }

        Texture ConvolveProbeTexture(CommandBuffer cmd, Texture texture)
        {
            // Probes can be either Cubemaps (for baked probes) or RenderTextures (for realtime probes)
            Texture2D texture2D = texture as Texture2D;
            RenderTexture renderTexture = texture as RenderTexture;

            RenderTexture convolutionSourceTexture = null;
            if (texture2D != null)
            {
                // if the size if different from the cache probe size or if the input texture format is compressed, we need to convert it
                // 1) to a format for which we can generate mip maps
                // 2) to the proper reflection probe cache size
                var sizeMismatch = texture2D.width != m_ProbeSize || texture2D.height != m_ProbeSize;
                var formatMismatch = texture2D.format != TextureFormat.RGBAHalf; // Temporary RT for convolution is always FP16
                if (formatMismatch || sizeMismatch)
                {
                    if (sizeMismatch)
                    {
                        Debug.LogWarningFormat("Baked Planar Reflection Probe {0} does not match HDRP Planar Reflection Probe Cache size of {1}. Consider baking it at the same size for better loading performance.", texture.name, m_ProbeSize);
                    }
                    else if (texture2D.format == TextureFormat.BC6H)
                    {
                        Debug.LogWarningFormat("Baked Planar Reflection Probe {0} is compressed but the HDRP Planar Reflection Probe Cache is not. Consider removing compression from the input texture for better quality.", texture.name);
                    }
                    ConvertTexture(cmd, texture2D, m_TempRenderTexture);
                }
                else
                    cmd.CopyTexture(texture2D, 0, 0, m_TempRenderTexture, 0, 0);

                // Ideally if input is not compressed and has mipmaps, don't do anything here. Problem is, we can't know if mips have been already convolved offline...
                cmd.GenerateMips(m_TempRenderTexture);
                convolutionSourceTexture = m_TempRenderTexture;
            }
            else
            {
                Debug.Assert(renderTexture != null);
                if (renderTexture.dimension != TextureDimension.Tex2D)
                {
                    Debug.LogError("Planar Realtime reflection probe should always be a 2D RenderTexture.");
                    return null;
                }

                // TODO: Do a different case for downsizing, in this case, instead of doing ConvertTexture just use the relevant mipmaps.
                var sizeMismatch = renderTexture.width != m_ProbeSize || renderTexture.height != m_ProbeSize;
                if (sizeMismatch)
                {
                    ConvertTexture(cmd, renderTexture, m_TempRenderTexture);
                    convolutionSourceTexture = m_TempRenderTexture;
                }
                else
                    convolutionSourceTexture = renderTexture;
                // Generate unfiltered mipmaps as a base for convolution
                // TODO: Make sure that we don't first convolve everything on the GPU with the legacy code path executed after rendering the probe.
                cmd.GenerateMips(convolutionSourceTexture);
            }

            m_IBLFilterGGX.FilterPlanarTexture(cmd, convolutionSourceTexture, m_ConvolutionTargetTexture);

            return m_ConvolutionTargetTexture;
        }

        public int FetchSlice(CommandBuffer cmd, Texture texture)
        {
            bool needUpdate;
            var sliceIndex = m_TextureCache.ReserveSlice(texture, out needUpdate);
            if (sliceIndex != -1)
            {
                if (needUpdate || m_ProbeBakingState[sliceIndex] != ProbeFilteringState.Ready)
                {
                    using (new ProfilingSample(cmd, "Convolve Planar Reflection Probe"))
                    {
                        // For now baking is done directly but will be time sliced in the future. Just preparing the code here.
                        m_ProbeBakingState[sliceIndex] = ProbeFilteringState.Convolving;

                        Texture result = ConvolveProbeTexture(cmd, texture);
                        if (result == null)
                            return -1;

                        if (m_PerformBC6HCompression)
                        {
                            throw new NotImplementedException("BC6H Support not implemented for PlanarReflectionProbeCache");
                        }
                        else
                        {
                            m_TextureCache.UpdateSlice(cmd, sliceIndex, result, m_TextureCache.GetTextureHash(texture)); // Be careful to provide the update count from the input texture, not the temporary one used for convolving.
                        }

                        m_ProbeBakingState[sliceIndex] = ProbeFilteringState.Ready;
                    }
                }
            }

            return sliceIndex;
        }

        public Texture GetTexCache()
        {
            return m_TextureCache.GetTexCache();
        }

        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution, int sliceSize)
        {
            return TextureCache2D.GetApproxCacheSizeInByte(nbElement, resolution, sliceSize);
        }

        internal static int GetMaxCacheSizeForWeightInByte(int weight, int resolution, int sliceSize)
        {
            return TextureCache2D.GetMaxCacheSizeForWeightInByte(weight, resolution, sliceSize);
        }
    }
}
