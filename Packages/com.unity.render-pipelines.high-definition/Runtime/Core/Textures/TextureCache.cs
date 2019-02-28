using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    // This class allows us to map each of texture to an internal Slice structure. The set of slices that have been produced are then stored into a child-class specific structure
    public abstract class TextureCache
    {
        // Name that identifies the texture cache (Mainly used to generate the storage texture name)
        protected string m_CacheName;
        // The number of mipmap that is deduced from the maximal resolution
        protected int m_NumMipLevels;
        // In the texture cache, a given texture/texture hash can request more than one single slot in the cache. The set of slots that match a single texture hash is thus called a "slice"
        protected int m_SliceSize;
        // Counter of input texture that have been fed to this structure
        private int m_NumTextures;
        // Array that maps input textureIDs into the slices indexes
        Dictionary<uint, int> m_LocatorInSliceDictionnary;

        // This structure defines the mapping between an input texture and the internal structure
        private struct SliceEntry
        {
            // ID of the internal structure
            public uint texId;
            // This counter tracks the number of frames since this slice was requested. The mechanic behind this is due to the fact that the number storage of the cache is limited
            public uint countLRU;
            // Hash that tracks the version of the input texture (allows us to know if it needs an update)  
            public uint sliceEntryHash;
        }
        // The array of slices  that the cache holds
        private SliceEntry[] m_SliceArray;

        // Array with the slices sorted according to their countLRU
        private int[] m_SortedIdxArray;

        // Array used when we use the texture as itself's representative in the slices
        private Texture[] m_autoContentArray = new Texture[1];

        // Constant values
        private static uint g_MaxFrameCount = unchecked((uint)(-1));
        private static uint g_InvalidTexID = (uint)0;

        protected const int k_FP16SizeInByte = 2;
        protected const int k_NbChannel = 4;
        protected const float k_MipmapFactorApprox = 1.33f;
        internal const int k_MaxSupported = 250; //vary along hardware and cube/2D but 250 should be always safe 
        
        protected TextureCache(string cacheName, int sliceSize = 1)
        {
            m_CacheName = cacheName;
            m_SliceSize = sliceSize;
            m_NumTextures = 0;
            m_NumMipLevels = 0;
        }

        // Function that initialize the texture cache with a maximal number of textures in the cache
        protected bool AllocTextureArray(int numTextures)
        {
            if (numTextures >= m_SliceSize)
            {
                m_SliceArray = new SliceEntry[numTextures];
                m_SortedIdxArray = new int[numTextures];
                m_LocatorInSliceDictionnary = new Dictionary<uint, int>();

                m_NumTextures = numTextures / m_SliceSize;
                for (int i = 0; i < m_NumTextures; i++)
                {
                    m_SliceArray[i].countLRU = g_MaxFrameCount;         // never used before
                    m_SliceArray[i].texId = g_InvalidTexID;
                    m_SortedIdxArray[i] = i;
                }
            }
            return numTextures >= m_SliceSize;
        }

        // This function returns the internal storage texture of the cache. It is specified by the child class.
        abstract public Texture GetTexCache();

        // Function that allows us to do the mapping between a texture value and an identifier
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

        // Function that reserves a slice using a texture and it returns a update flag that tells if the stored value matches the input one
        public int ReserveSlice(Texture texture, out bool needUpdate)
        {
            // Reset the update flag
            needUpdate = false;

            // Check the validity of the input texture
            if (texture == null)
                return -1;
            var texId = (uint)texture.GetInstanceID();
            if (texId == g_InvalidTexID)
                return -1;

            // Search for existing copy in the texId to slice index dictionary
            var sliceIndex = -1;
            if (m_LocatorInSliceDictionnary.TryGetValue(texId, out sliceIndex))
            {
                // Compute the new hash of the texture
                var textureHash  = GetTextureHash(texture);

                // We need to update the texture if the hash does not match the one in the slice
                needUpdate |= (m_SliceArray[sliceIndex].sliceEntryHash != textureHash);

                Debug.Assert(m_SliceArray[sliceIndex].texId == texId);
            }
            else
            {
                // This texture was not in the slice array. We need to look for first non zero entry.
                // Will by the least recently used entry since the array was pre-sorted (in linear time) in NewFrame()
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
                        m_LocatorInSliceDictionnary.Remove(m_SliceArray[idx].texId);
                    }

                    m_LocatorInSliceDictionnary.Add(texId, idx);
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
        public bool UpdateSlice(CommandBuffer cmd, int sliceIndex, Texture[] contentArray, uint textureHash)
        {
            // Make sure the content matches the size of the texture cache
            Debug.Assert(contentArray.Length == m_SliceSize);

            // Update the hash
            SetSliceHash(sliceIndex, textureHash);

            // transfer new slice to sliceIndex from source texture
            return TransferToSlice(cmd, sliceIndex, contentArray);
        }

        public bool UpdateSlice(CommandBuffer cmd, int sliceIndex, Texture texture, uint textureHash)
        {
            // Make sure the content matches the size of the texture cache
            Debug.Assert(m_SliceSize == 1);

            // Update the hash
            SetSliceHash(sliceIndex, textureHash);

            // transfer new slice to sliceIndex from source texture
            m_autoContentArray[0] = texture;
            return TransferToSlice(cmd, sliceIndex, m_autoContentArray);
        }

        public void SetSliceHash(int sliceIndex, uint hash)
        {
            // transfer new slice to sliceIndex from source texture
            m_SliceArray[sliceIndex].sliceEntryHash = hash;
        }

        // Push the content to the internal target slice. Should be overridden by the child class. It will return fals if it fails to update (mainly sub-textures's size do not match)
        protected abstract bool TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture[] textureArray);

        public int FetchSlice(CommandBuffer cmd, Texture texture, bool forceReinject = false)
        {
            bool needUpdate = false;
            var sliceIndex = ReserveSlice(texture, out needUpdate);

            var bSwapSlice = forceReinject || needUpdate;

            // wrap up
            Debug.Assert(sliceIndex != -1, "The texture cache doesn't have enough space to store all textures. Please either increase the size of the texture cache, or use fewer unique textures.");
            if (sliceIndex != -1 && bSwapSlice)
            {
                m_autoContentArray[0] = texture;
                UpdateSlice(cmd, sliceIndex, m_autoContentArray, GetTextureHash(texture));
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

        // should not really be used in general. Assuming lights are culled properly entries will automatically be replaced efficiently.
        public void RemoveEntryFromSlice(Texture texture)
        {
            var texId = (uint)texture.GetInstanceID();

            //assert(TexID!=g_InvalidTexID);
            if (texId == g_InvalidTexID) return;

            // search for existing copy
            if (!m_LocatorInSliceDictionnary.ContainsKey(texId))
                return;

            var sliceIndex = m_LocatorInSliceDictionnary[texId];

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
            m_LocatorInSliceDictionnary.Remove(texId);
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
    }
}
