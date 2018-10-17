using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    public class TextureCache2D : TextureCache
    {
        private Texture2DArray m_Cache;

        public TextureCache2D(string cacheName = "")
            : base(cacheName)
        {
        }

        bool TextureHasMipmaps(Texture texture)
        {
            if (texture is Texture2D)
                return ((Texture2D)texture).mipmapCount > 1;
            else if (texture is RenderTexture)
                return ((RenderTexture)texture).useMipMap;
            return false;
        }

        public override void TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture texture)
        {
            var mismatch = (m_Cache.width != texture.width) || (m_Cache.height != texture.height);

            if (texture is Texture2D)
            {
                mismatch |= (m_Cache.format != (texture as Texture2D).format);
            }

            if (mismatch)
            {
                cmd.ConvertTexture(texture, 0, m_Cache, sliceIndex);
            }
            else
            {
                if (TextureHasMipmaps(texture))
                    cmd.CopyTexture(texture, 0, m_Cache, sliceIndex);
                else
                    Debug.LogWarning("The texture '" + texture + "' should have mipmaps to be handeled by the cookie texture array");
            }
        }

        public override Texture GetTexCache()
        {
            return m_Cache;
        }

        public bool AllocTextureArray(int numTextures, int width, int height, TextureFormat format, bool isMipMapped)
        {
            var res = AllocTextureArray(numTextures);
            m_NumMipLevels = GetNumMips(width, height);

            m_Cache = new Texture2DArray(width, height, numTextures, format, isMipMapped)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetTextureAutoName(width, height, format, TextureDimension.Tex2DArray, depth: numTextures, name: m_CacheName)
            };

            return res;
        }

        public void Release()
        {
            CoreUtils.Destroy(m_Cache);
        }
        
        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution)
        {
            return (long)((long)nbElement * resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox);
        }

        internal static int GetMaxCacheSizeForWeightInByte(int weight, int resolution)
        {
            int theoricalResult = Mathf.FloorToInt(weight / ((long)resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox));
            return Mathf.Clamp(theoricalResult, 1, k_MaxSupported);
        }
    }

    public class TextureCacheCubemap : TextureCache
    {
        const int k_NbFace = 6;

        private CubemapArray m_Cache;

        // the member variables below are only in use when TextureCache.supportsCubemapArrayTextures is false
        private Texture2DArray m_CacheNoCubeArray;
        private RenderTexture[] m_StagingRTs;
        private int m_NumPanoMipLevels;
        private Material m_CubeBlitMaterial;
        private int m_CubeMipLevelPropName;
        private int m_cubeSrcTexPropName;

        public TextureCacheCubemap(string cacheName = "")
            : base(cacheName)
        {
        }

        public override void TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture texture)
        {
            if (!TextureCache.supportsCubemapArrayTextures)
                TransferToPanoCache(cmd, sliceIndex, texture);
            else
            {
                var mismatch = (m_Cache.width != texture.width) || (m_Cache.height != texture.height);

                if (texture is Cubemap)
                {
                    mismatch |= (m_Cache.format != (texture as Cubemap).format);
                }

                if (mismatch)
                {
                    for (int f = 0; f < 6; f++)
                    {
                        cmd.ConvertTexture(texture, f, m_Cache, 6 * sliceIndex + f);
                    }
                }
                else
                {
                    for (int f = 0; f < 6; f++)
                        cmd.CopyTexture(texture, f, m_Cache, 6 * sliceIndex + f);
                }
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

        private void TransferToPanoCache(CommandBuffer cmd, int sliceIndex, Texture texture)
        {
            m_CubeBlitMaterial.SetTexture(m_cubeSrcTexPropName, texture);
            for (int m = 0; m < m_NumPanoMipLevels; m++)
            {
                m_CubeBlitMaterial.SetInt(m_CubeMipLevelPropName, Mathf.Min(m_NumMipLevels - 1, m));
                cmd.Blit(null, m_StagingRTs[m], m_CubeBlitMaterial, 0);
            }

            for (int m = 0; m < m_NumPanoMipLevels; m++)
                cmd.CopyTexture(m_StagingRTs[m], 0, 0, m_CacheNoCubeArray, sliceIndex, m);
        }


        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution)
        {
            return (long)((long)nbElement * resolution * resolution * k_NbFace * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox);
        }

        internal static int GetMaxCacheSizeForWeightInByte(long weight, int resolution)
        {
            int theoricalResult = Mathf.FloorToInt(weight / ((long)resolution * resolution * k_NbFace * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox));
            return Mathf.Clamp(theoricalResult, 1, k_MaxSupported);
        }
    }


    public abstract class TextureCache
    {
        protected const int k_FP16SizeInByte = 2;
        protected const int k_NbChannel = 4;
        protected const float k_MipmapFactorApprox = 1.33f;
        internal const int k_MaxSupported = 250; //vary along hardware and cube/2D but 250 should be always safe 

        protected int m_NumMipLevels;
        protected string m_CacheName;

        public static bool isMobileBuildTarget
        {
            get
            {
    #if UNITY_EDITOR
                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.iOS:
                    case BuildTarget.Android:
                        return true;
                    default:
                        return false;
                }
    #else
                return Application.isMobilePlatform;
    #endif
            }
        }

        public static TextureFormat GetPreferredHDRCompressedTextureFormat
        {
            get
            {
                var format = TextureFormat.RGBAHalf;
                var probeFormat = TextureFormat.BC6H;

                if (SystemInfo.SupportsTextureFormat(probeFormat) && !UnityEngine.Rendering.GraphicsSettings.HasShaderDefine(UnityEngine.Rendering.BuiltinShaderDefine.UNITY_NO_DXT5nm))
                    format = probeFormat;

                return format;
            }
        }

        public static bool supportsCubemapArrayTextures
        {
            get
            {
                return !UnityEngine.Rendering.GraphicsSettings.HasShaderDefine(UnityEngine.Rendering.BuiltinShaderDefine.UNITY_NO_CUBEMAP_ARRAY);
            }
        }

        private struct SSliceEntry
        {
            public uint    texId;
            public uint    countLRU;
            public uint    sliceEntryHash;
        };

        private int m_NumTextures;
        private int[] m_SortedIdxArray;
        private SSliceEntry[] m_SliceArray;

        Dictionary<uint, int> m_LocatorInSliceArray;

        private static uint g_MaxFrameCount = unchecked((uint)(-1));
        private static uint g_InvalidTexID = (uint)0;


        public uint GetTextureHash(Texture texture)
        {
            uint textureHash  = texture.updateCount;
            // For baked probes in the editor we need to factor in the actual hash of texture because we can't increment the update count of a texture that's baked on the disk.
            // This code leaks logic from reflection probe baking into the texture cache which is not good... TODO: Find a way to do that outside of the texture cache.
#if UNITY_EDITOR
            textureHash += (uint)texture.imageContentsHash.GetHashCode();
#endif
            return textureHash;
        }

        public int ReserveSlice(Texture texture, out bool needUpdate)
        {
            needUpdate = false;
            if (texture == null)
                return -1;

            var texId = (uint)texture.GetInstanceID();
            if (texId == g_InvalidTexID)
                return -1;

            // search for existing copy
            var sliceIndex = -1;
            var foundIndex = -1;
            if (m_LocatorInSliceArray.TryGetValue(texId, out foundIndex))
            {
                sliceIndex = foundIndex;

                var textureHash  = GetTextureHash(texture);
                needUpdate |= (m_SliceArray[sliceIndex].sliceEntryHash != textureHash);

                Debug.Assert(m_SliceArray[sliceIndex].texId == texId);
            }

            // If no existing copy found in the array
            if (sliceIndex == -1)
            {
                // look for first non zero entry. Will by the least recently used entry
                // since the array was pre-sorted (in linear time) in NewFrame()
                var bFound = false;
                int j = 0, idx = 0;
                while ((!bFound) && j < m_NumTextures)
                {
                    idx = m_SortedIdxArray[j];
                    if (m_SliceArray[idx].countLRU == 0)
                        ++j;       // if entry already snagged by a new texture in this frame then ++j
                    else
                        bFound = true;
                }

                if (bFound)
                {
                    needUpdate = true;
                    // if we are replacing an existing entry delete it from m_locatorInSliceArray.
                    if (m_SliceArray[idx].texId != g_InvalidTexID)
                    {
                        m_LocatorInSliceArray.Remove(m_SliceArray[idx].texId);
                    }

                    m_LocatorInSliceArray.Add(texId, idx);
                    m_SliceArray[idx].texId = texId;

                    sliceIndex = idx;
                }
            }

            if (sliceIndex != -1)
            {
                m_SliceArray[sliceIndex].countLRU = 0;      // mark slice as in use this frame
            }

            return sliceIndex;
        }

        // In case the texture content with which we update the cache is not the input texture, we need to provide the right update count.
        public void UpdateSlice(CommandBuffer cmd, int sliceIndex, Texture content, uint textureHash)
        {
            // transfer new slice to sliceIndex from source texture
            SetSliceHash(sliceIndex, textureHash);
            TransferToSlice(cmd, sliceIndex, content);
        }

        public void SetSliceHash(int sliceIndex, uint hash)
        {
            // transfer new slice to sliceIndex from source texture
            m_SliceArray[sliceIndex].sliceEntryHash = hash;
        }

        public void UpdateSlice(CommandBuffer cmd, int sliceIndex, Texture content)
        {
            UpdateSlice(cmd, sliceIndex, content, GetTextureHash(content));
        }

        public int FetchSlice(CommandBuffer cmd, Texture texture, bool forceReinject = false)
        {
            bool needUpdate = false;
            var sliceIndex = ReserveSlice(texture, out needUpdate);

            var bSwapSlice = forceReinject || needUpdate;

            // wrap up
            Debug.Assert(sliceIndex != -1, "The texture cache doesn't have enough space to store all textures. Please either increase the size of the texture cache, or use fewer unique textures.");
            if (sliceIndex != -1 && bSwapSlice)
            {
                UpdateSlice(cmd, sliceIndex, texture);
            }

            return sliceIndex;
        }

        private static List<int> s_TempIntList = new List<int>();
        public void NewFrame()
        {
            var numNonZeros = 0;
            s_TempIntList.Clear();
            for (int i = 0; i < m_NumTextures; i++)
            {
                s_TempIntList.Add(m_SortedIdxArray[i]);     // copy buffer
                if (m_SliceArray[m_SortedIdxArray[i]].countLRU != 0) ++numNonZeros;
            }
            int nonZerosBase = 0, zerosBase = 0;
            for (int i = 0; i < m_NumTextures; i++)
            {
                if (m_SliceArray[s_TempIntList[i]].countLRU == 0)
                {
                    m_SortedIdxArray[zerosBase + numNonZeros] = s_TempIntList[i];
                    ++zerosBase;
                }
                else
                {
                    m_SortedIdxArray[nonZerosBase] = s_TempIntList[i];
                    ++nonZerosBase;
                }
            }

            for (int i = 0; i < m_NumTextures; i++)
            {
                if (m_SliceArray[i].countLRU < g_MaxFrameCount) ++m_SliceArray[i].countLRU;     // next frame
            }

            //for(int q=1; q<m_numTextures; q++)
            //    assert(m_SliceArray[m_SortedIdxArray[q-1]].CountLRU>=m_SliceArray[m_SortedIdxArray[q]].CountLRU);
        }

        protected TextureCache(string cacheName)
        {
            m_CacheName = cacheName;
            m_NumTextures = 0;
            m_NumMipLevels = 0;
        }

        public virtual void TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture texture)
        {
        }

        public virtual Texture GetTexCache()
        {
            return null;
        }

        protected bool AllocTextureArray(int numTextures)
        {
            if (numTextures > 0)
            {
                m_SliceArray = new SSliceEntry[numTextures];
                m_SortedIdxArray = new int[numTextures];
                m_LocatorInSliceArray = new Dictionary<uint, int>();

                m_NumTextures = numTextures;
                for (int i = 0; i < m_NumTextures; i++)
                {
                    m_SliceArray[i].countLRU = g_MaxFrameCount;         // never used before
                    m_SliceArray[i].texId = g_InvalidTexID;
                    m_SortedIdxArray[i] = i;
                }
            }

            //return m_SliceArray != NULL && m_SortedIdxArray != NULL && numTextures > 0;
            return numTextures > 0;
        }

        // should not really be used in general. Assuming lights are culled properly entries will automatically be replaced efficiently.
        public void RemoveEntryFromSlice(Texture texture)
        {
            var texId = (uint)texture.GetInstanceID();

            //assert(TexID!=g_InvalidTexID);
            if (texId == g_InvalidTexID) return;

            // search for existing copy
            if (!m_LocatorInSliceArray.ContainsKey(texId))
                return;

            var sliceIndex = m_LocatorInSliceArray[texId];

            //assert(m_SliceArray[sliceIndex].TexID==TexID);

            // locate entry sorted by uCountLRU in m_pSortedIdxArray
            var foundIdxSortLRU = false;
            var i = 0;
            while ((!foundIdxSortLRU) && i < m_NumTextures)
            {
                if (m_SortedIdxArray[i] == sliceIndex) foundIdxSortLRU = true;
                else ++i;
            }

            if (!foundIdxSortLRU)
                return;

            // relocate sliceIndex to front of m_pSortedIdxArray since uCountLRU will be set to maximum.
            for (int j = 0; j < i; j++)
            {
                m_SortedIdxArray[j + 1] = m_SortedIdxArray[j];
            }
            m_SortedIdxArray[0] = sliceIndex;

            // delete from m_locatorInSliceArray and m_pSliceArray.
            m_LocatorInSliceArray.Remove(texId);
            m_SliceArray[sliceIndex].countLRU = g_MaxFrameCount;            // never used before
            m_SliceArray[sliceIndex].texId = g_InvalidTexID;
        }

        protected int GetNumMips(int width, int height)
        {
            return GetNumMips(width > height ? width : height);
        }

        protected int GetNumMips(int dim)
        {
            var uDim = (uint)dim;
            var iNumMips = 0;
            while (uDim > 0)
            { ++iNumMips; uDim >>= 1; }
            return iNumMips;
        }
    }
}
