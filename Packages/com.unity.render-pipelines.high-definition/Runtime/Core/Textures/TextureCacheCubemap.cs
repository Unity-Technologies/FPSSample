using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public class TextureCacheCubemap : TextureCache
    {
        private CubemapArray m_Cache;

        const int k_NbFace = 6;

        // the member variables below are only in use when TextureCache.supportsCubemapArrayTextures is false
        private Texture2DArray m_CacheNoCubeArray;
        private RenderTexture[] m_StagingRTs;
        private int m_NumPanoMipLevels;
        private Material m_CubeBlitMaterial;
        private int m_CubeMipLevelPropName;
        private int m_cubeSrcTexPropName;

        public TextureCacheCubemap(string cacheName = "", int sliceSize = 1)
            : base(cacheName, sliceSize)
        {
        }

        override protected bool TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            if (!TextureCache.supportsCubemapArrayTextures)
                return TransferToPanoCache(cmd, sliceIndex, textureArray);
            else
            {
                // Make sure the array is not null or empty and that the first texture is a render-texture or a texture2D
                if (textureArray == null || textureArray.Length == 0)
                {
                    return false;
                }

                // First check here is to check if all the sub-texture have the same size
                for (int texIDx = 1; texIDx < textureArray.Length; ++texIDx)
                {
                    // We cannot update if the textures if they don't have the same size or not the right type
                    if (textureArray[texIDx].width != textureArray[0].width || textureArray[texIDx].height != textureArray[0].height)
                    {
                        Debug.LogWarning("All the sub-textures should have the same dimensions to be handled by the texture cache.");
                        return false;
                    }
                }

                var mismatch = (m_Cache.width != textureArray[0].width) || (m_Cache.height != textureArray[0].height);

                if (textureArray[0] is Cubemap)
                {
                    mismatch |= (m_Cache.format != (textureArray[0] as Cubemap).format);
                }

                for (int texIDx = 0; texIDx < textureArray.Length; ++texIDx)
                {
                    if (mismatch)
                    {
                        for (int f = 0; f < 6; f++)
                        {
                            cmd.ConvertTexture(textureArray[texIDx], f, m_Cache, 6 * (m_SliceSize * sliceIndex + texIDx) + f);
                        }
                    }
                    else
                    {
                        for (int f = 0; f < 6; f++)
                            cmd.CopyTexture(textureArray[texIDx], f, m_Cache, 6 * (m_SliceSize * sliceIndex + texIDx) + f);
                    }
                }

                return true;
            }
        }

        public override Texture GetTexCache()
        {
            return !TextureCache.supportsCubemapArrayTextures ? (Texture)m_CacheNoCubeArray : m_Cache;
        }

        public bool AllocTextureArray(int numCubeMaps, int width, TextureFormat format, bool isMipMapped, Material cubeBlitMaterial)
        {
            var res = AllocTextureArray(numCubeMaps);
            m_NumMipLevels = GetNumMips(width, width);      // will calculate same way whether we have cube array or not

            if (!TextureCache.supportsCubemapArrayTextures)
            {
                m_CubeBlitMaterial = cubeBlitMaterial;

                int panoWidthTop = 4 * width;
                int panoHeightTop = 2 * width;

                // create panorama 2D array. Hardcoding the render target for now. No convenient way atm to
                // map from TextureFormat to RenderTextureFormat and don't want to deal with sRGB issues for now.
                m_CacheNoCubeArray = new Texture2DArray(panoWidthTop, panoHeightTop, numCubeMaps, TextureFormat.RGBAHalf, isMipMapped)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0,
                    name = CoreUtils.GetTextureAutoName(panoWidthTop, panoHeightTop, format, TextureDimension.Tex2DArray, depth: numCubeMaps, name: m_CacheName)
                };

                m_NumPanoMipLevels = isMipMapped ? GetNumMips(panoWidthTop, panoHeightTop) : 1;
                m_StagingRTs = new RenderTexture[m_NumPanoMipLevels];
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_StagingRTs[m] = new RenderTexture(Mathf.Max(1, panoWidthTop >> m), Mathf.Max(1, panoHeightTop >> m), 0, RenderTextureFormat.ARGBHalf) { hideFlags = HideFlags.HideAndDontSave };
                    m_StagingRTs[m].name = CoreUtils.GetRenderTargetAutoName(Mathf.Max(1, panoWidthTop >> m), Mathf.Max(1, panoHeightTop >> m), 1, RenderTextureFormat.ARGBHalf, String.Format("PanaCache{0}", m));
                }

                if (m_CubeBlitMaterial)
                {
                    m_CubeMipLevelPropName = Shader.PropertyToID("_cubeMipLvl");
                    m_cubeSrcTexPropName = Shader.PropertyToID("_srcCubeTexture");
                }
            }
            else
            {
                m_Cache = new CubemapArray(width, numCubeMaps, format, isMipMapped)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0, // It is important to set 0 here, else unity force anisotropy filtering
                    name = CoreUtils.GetTextureAutoName(width, width, format, TextureDimension.CubeArray, depth: numCubeMaps, name: m_CacheName)
                };
            }

            return res;
        }

        public void Release()
        {
            if (m_CacheNoCubeArray)
            {
                CoreUtils.Destroy(m_CacheNoCubeArray);
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_StagingRTs[m].Release();
                }
                m_StagingRTs = null;
                CoreUtils.Destroy(m_CubeBlitMaterial);
            }

            CoreUtils.Destroy(m_Cache);
        }

        private bool TransferToPanoCache(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            for(int texIdx = 0; texIdx < textureArray.Length; ++texIdx)
            {
                m_CubeBlitMaterial.SetTexture(m_cubeSrcTexPropName, textureArray[texIdx]);
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_CubeBlitMaterial.SetInt(m_CubeMipLevelPropName, Mathf.Min(m_NumMipLevels - 1, m));
                    cmd.Blit(null, m_StagingRTs[m], m_CubeBlitMaterial, 0);
                }

                for (int m = 0; m < m_NumPanoMipLevels; m++)
                    cmd.CopyTexture(m_StagingRTs[m], 0, 0, m_CacheNoCubeArray, m_SliceSize * sliceIndex + texIdx, m);
            }
            return true;
        }

        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution, int sliceSize)
        {
            return (long)((long)nbElement * resolution * resolution * k_NbFace * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize);
        }

        internal static int GetMaxCacheSizeForWeightInByte(long weight, int resolution, int sliceSize)
        {
            int theoricalResult = Mathf.FloorToInt(weight / ((long)resolution * resolution * k_NbFace * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize));
            return Mathf.Clamp(theoricalResult, 1, k_MaxSupported);
        }
    }
}
