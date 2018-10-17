using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public delegate Vector2Int ScaleFunc(Vector2Int size);

    public partial class RTHandleSystem : IDisposable
    {
        public enum ResizeMode
        {
            Auto,
            OnDemand
        }

        // Parameters for auto-scaled Render Textures
        bool                m_ScaledRTSupportsMSAA = false;
        MSAASamples         m_ScaledRTCurrentMSAASamples = MSAASamples.None;
        HashSet<RTHandle>   m_AutoSizedRTs;
        RTHandle[]          m_AutoSizedRTsArray; // For fast iteration
        HashSet<RTHandle>   m_ResizeOnDemandRTs;

        int m_MaxWidths = 0;
        int m_MaxHeights = 0;

        public RTHandleSystem()
        {
            m_AutoSizedRTs = new HashSet<RTHandle>();
            m_ResizeOnDemandRTs = new HashSet<RTHandle>();
            m_MaxWidths = 1;
            m_MaxHeights = 1;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // Call this once to set the initial size and allow msaa targets or not.
        public void Initialize(int width, int height, bool scaledRTsupportsMSAA, MSAASamples scaledRTMSAASamples)
        {
            Debug.Assert(m_AutoSizedRTs.Count == 0, "RTHandle.Initialize should only be called once before allocating any Render Texture.");

            m_MaxWidths = width;
            m_MaxHeights = height;

            m_ScaledRTSupportsMSAA = scaledRTsupportsMSAA;
            m_ScaledRTCurrentMSAASamples = scaledRTMSAASamples;
        }

        public void Release(RTHandle rth)
        {
            if (rth != null)
            {
                Assert.AreEqual(this, rth.m_Owner);
                rth.Release();
            }
        }

        public void SetReferenceSize(int width, int height, MSAASamples msaaSamples)
        {
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            bool sizeChanged = width > GetMaxWidth() || height > GetMaxHeight();
            bool msaaSamplesChanged = (msaaSamples != m_ScaledRTCurrentMSAASamples);

            if (sizeChanged || msaaSamplesChanged)
            {   
                Resize(width, height, msaaSamples, sizeChanged, msaaSamplesChanged);
            }
        }

        public void ResetReferenceSize(int width, int height, MSAASamples msaaSamples)
        {
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            bool sizeChanged = width > GetMaxWidth() || height > GetMaxHeight();
            bool msaaSamplesChanged = (msaaSamples != m_ScaledRTCurrentMSAASamples);

            if (sizeChanged || msaaSamplesChanged)
            {
                Resize(width, height, msaaSamples, sizeChanged, msaaSamplesChanged);
            }
        }

        public void SwitchResizeMode(RTHandle rth, ResizeMode mode)
        {
            switch (mode)
            {
                case ResizeMode.OnDemand:
                    m_AutoSizedRTs.Remove(rth);
                    m_ResizeOnDemandRTs.Add(rth);
                    break;
                case ResizeMode.Auto:
                    // Resize now so it is consistent with other auto resize RTHs
                    if (m_ResizeOnDemandRTs.Contains(rth))
                        DemandResize(rth);
                    m_ResizeOnDemandRTs.Remove(rth);
                    m_AutoSizedRTs.Add(rth);
                    break;
            }
        }

        public void DemandResize(RTHandle rth)
        {
            Assert.IsTrue(m_ResizeOnDemandRTs.Contains(rth), "The RTHandle is not an resize on demand handle in this RTHandleSystem. Please call SwitchToResizeOnDemand(rth, true) before resizing on demand.");

            // Grab the render texture
            var rt = rth.m_RT;
            rth.referenceSize = new Vector2Int(m_MaxWidths, m_MaxHeights);
            var scaledSize = rth.GetScaledSize(rth.referenceSize);
            scaledSize = Vector2Int.Max(Vector2Int.one, scaledSize);

            // Did the size change?
            var sizeChanged = rt.width != scaledSize.x || rt.height != scaledSize.y;
            // If this is an MSAA texture, did the sample count change?
            var msaaSampleChanged = rth.m_EnableMSAA && rt.antiAliasing != (int)m_ScaledRTCurrentMSAASamples;

            if (sizeChanged || msaaSampleChanged)
            {
                // Free this render texture
                rt.Release();

                // Update the antialiasing count
                if (rth.m_EnableMSAA)
                    rt.antiAliasing = (int)m_ScaledRTCurrentMSAASamples;

                // Update the size
                rt.width = scaledSize.x;
                rt.height = scaledSize.y;

                // Generate a new name
                rt.name = CoreUtils.GetRenderTargetAutoName(
                        rt.width,
                        rt.height,
                        rt.volumeDepth,
                        rt.format,
                        rth.m_Name,
                        mips: rt.useMipMap,
                        enableMSAA: rth.m_EnableMSAA,
                        msaaSamples: m_ScaledRTCurrentMSAASamples
                        );

                // Create the new texture
                rt.Create();
            }
        }

        public int GetMaxWidth() { return m_MaxWidths; }
        public int GetMaxHeight() { return m_MaxHeights; }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
                m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);
                for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
                {
                    var rt = m_AutoSizedRTsArray[i];
                    Release(rt);
                }
                m_AutoSizedRTs.Clear();

                Array.Resize(ref m_AutoSizedRTsArray, m_ResizeOnDemandRTs.Count);
                m_ResizeOnDemandRTs.CopyTo(m_AutoSizedRTsArray);
                for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
                {
                    var rt = m_AutoSizedRTsArray[i];
                    Release(rt);
                }
                m_ResizeOnDemandRTs.Clear();
                m_AutoSizedRTsArray = null;
            }
        }

        void Resize(int width, int height, MSAASamples msaaSamples, bool sizeChanged, bool msaaSampleChanged)
        {
            m_MaxWidths = Math.Max(width, m_MaxWidths);
            m_MaxHeights = Math.Max(height, m_MaxHeights);
            m_ScaledRTCurrentMSAASamples = msaaSamples;

            var maxSize = new Vector2Int(m_MaxWidths, m_MaxHeights);

            Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
            m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);

            for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
            {
                // Grab the RT Handle
                var rth = m_AutoSizedRTsArray[i];

                // If we are only processing MSAA sample count change, make sure this RT is an MSAA one
                if (!sizeChanged && msaaSampleChanged && !rth.m_EnableMSAA)
                {
                    continue;
                }

                // Force its new reference size
                rth.referenceSize = maxSize;

                // Grab the render texture
                var renderTexture = rth.m_RT;

                // Free the previous version
                renderTexture.Release();

                // Get the scaled size
                var scaledSize = rth.GetScaledSize(maxSize);

                renderTexture.width = Mathf.Max(scaledSize.x, 1);
                renderTexture.height = Mathf.Max(scaledSize.y, 1);

                // If this is a msaa texture, make sure to update its msaa count
                if (rth.m_EnableMSAA)
                {
                    renderTexture.antiAliasing = (int)m_ScaledRTCurrentMSAASamples;
                }

                // Regenerate the name
                renderTexture.name = CoreUtils.GetRenderTargetAutoName(renderTexture.width, renderTexture.height, renderTexture.volumeDepth, renderTexture.format, rth.m_Name, mips: renderTexture.useMipMap, enableMSAA: rth.m_EnableMSAA, msaaSamples: m_ScaledRTCurrentMSAASamples);

                // Create the render texture
                renderTexture.Create();
            }
        }

        // This method wraps around regular RenderTexture creation.
        // There is no specific logic applied to RenderTextures created this way.
        public RTHandle Alloc(
            int width,
            int height,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            RenderTextureFormat colorFormat = RenderTextureFormat.Default,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool sRGB = true,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            string name = ""
            )
        {
            bool enableMSAA = msaaSamples != MSAASamples.None;
            if (!enableMSAA && bindTextureMS == true)
            {
                Debug.LogWarning("RTHandle allocated without MSAA but with bindMS set to true, forcing bindMS to false.");
                bindTextureMS = false;
            }

            var rt = new RenderTexture(width, height, (int)depthBufferBits, colorFormat, sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
            {
                hideFlags = HideFlags.HideAndDontSave,
                volumeDepth = slices,
                filterMode = filterMode,
                wrapMode = wrapMode,
                dimension = dimension,
                enableRandomWrite = enableRandomWrite,
                useMipMap = useMipMap,
                autoGenerateMips = autoGenerateMips,
                anisoLevel = anisoLevel,
                mipMapBias = mipMapBias,
                antiAliasing = (int)msaaSamples,
                bindTextureMS = bindTextureMS,
                useDynamicScale = useDynamicScale,
                vrUsage = vrUsage,
                memorylessMode = memoryless,
                name = CoreUtils.GetRenderTargetAutoName(width, height, slices, colorFormat, name, mips: useMipMap, enableMSAA: enableMSAA, msaaSamples: msaaSamples)
            };
            rt.Create();

            RTCategory category = enableMSAA ? RTCategory.MSAA : RTCategory.Regular;
            var newRT = new RTHandle(this);
            newRT.SetRenderTexture(rt, category);
            newRT.useScaling = false;
            newRT.m_EnableRandomWrite = enableRandomWrite;
            newRT.m_EnableMSAA = enableMSAA;
            newRT.m_Name = name;

            newRT.referenceSize = new Vector2Int(width, height);

            return newRT;
        }

        // Next two methods are used to allocate RenderTexture that depend on the frame settings (resolution and msaa for now)
        // RenderTextures allocated this way are meant to be defined by a scale of camera resolution (full/half/quarter resolution for example).
        // The idea is that internally the system will scale up the size of all render texture so that it amortizes with time and not reallocate when a smaller size is required (which is what happens with TemporaryRTs).
        // Since MSAA cannot be changed on the fly for a given RenderTexture, a separate instance will be created if the user requires it. This instance will be the one used after the next call of SetReferenceSize if MSAA is required.
        public RTHandle Alloc(
            Vector2 scaleFactor,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            RenderTextureFormat colorFormat = RenderTextureFormat.Default,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool sRGB = true,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            bool enableMSAA = false,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            string name = ""
            )
        {
            // If an MSAA target is requested, make sure the support was on
            if (enableMSAA)
                Debug.Assert(m_ScaledRTSupportsMSAA);

            int width = Mathf.Max(Mathf.RoundToInt(scaleFactor.x * GetMaxWidth()), 1);
            int height = Mathf.Max(Mathf.RoundToInt(scaleFactor.y * GetMaxHeight()), 1);

            var rth = AllocAutoSizedRenderTexture(width,
                    height,
                    slices,
                    depthBufferBits,
                    colorFormat,
                    filterMode,
                    wrapMode,
                    dimension,
                    sRGB,
                    enableRandomWrite,
                    useMipMap,
                    autoGenerateMips,
                    anisoLevel,
                    mipMapBias,
                    enableMSAA,
                    bindTextureMS,
                    useDynamicScale,
                    vrUsage,
                    memoryless,
                    name
                    );

            rth.referenceSize = new Vector2Int(width, height);

            rth.scaleFactor = scaleFactor;
            return rth;
        }

        //
        // You can provide your own scaling function for advanced scaling schemes (e.g. scaling to
        // the next POT). The function takes a Vec2 as parameter that holds max width & height
        // values for the current manager context and returns a Vec2 of the final size in pixels.
        //
        // var rth = Alloc(
        //     size => new Vector2Int(size.x / 2, size.y),
        //     [...]
        // );
        //
        public RTHandle Alloc(
            ScaleFunc scaleFunc,
            int slices = 1,
            DepthBits depthBufferBits = DepthBits.None,
            RenderTextureFormat colorFormat = RenderTextureFormat.Default,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            TextureDimension dimension = TextureDimension.Tex2D,
            bool sRGB = true,
            bool enableRandomWrite = false,
            bool useMipMap = false,
            bool autoGenerateMips = true,
            int anisoLevel = 1,
            float mipMapBias = 0f,
            bool enableMSAA = false,
            bool bindTextureMS = false,
            bool useDynamicScale = false,
            VRTextureUsage vrUsage = VRTextureUsage.None,
            RenderTextureMemoryless memoryless = RenderTextureMemoryless.None,
            string name = ""
            )
        {
            var scaleFactor = scaleFunc(new Vector2Int(GetMaxWidth(), GetMaxHeight()));
            int width = Mathf.Max(scaleFactor.x, 1);
            int height = Mathf.Max(scaleFactor.y, 1);

            var rth = AllocAutoSizedRenderTexture(width,
                    height,
                    slices,
                    depthBufferBits,
                    colorFormat,
                    filterMode,
                    wrapMode,
                    dimension,
                    sRGB,
                    enableRandomWrite,
                    useMipMap,
                    autoGenerateMips,
                    anisoLevel,
                    mipMapBias,
                    enableMSAA,
                    bindTextureMS,
                    useDynamicScale,
                    vrUsage,
                    memoryless,
                    name
                    );

            rth.referenceSize = new Vector2Int(width, height);

            rth.scaleFunc = scaleFunc;
            return rth;
        }

        // Internal function
        RTHandle AllocAutoSizedRenderTexture(
            int width,
            int height,
            int slices,
            DepthBits depthBufferBits,
            RenderTextureFormat colorFormat,
            FilterMode filterMode,
            TextureWrapMode wrapMode,
            TextureDimension dimension,
            bool sRGB,
            bool enableRandomWrite,
            bool useMipMap,
            bool autoGenerateMips,
            int anisoLevel,
            float mipMapBias,
            bool enableMSAA,
            bool bindTextureMS,
            bool useDynamicScale,
            VRTextureUsage vrUsage,
            RenderTextureMemoryless memoryless,
            string name
            )
        {
            // Here user made a mistake in setting up msaa/bindMS, hence the warning
            if (!enableMSAA && bindTextureMS == true)
            {
                Debug.LogWarning("RTHandle allocated without MSAA but with bindMS set to true, forcing bindMS to false.");
                bindTextureMS = false;
            }

            bool allocForMSAA = m_ScaledRTSupportsMSAA ? enableMSAA : false;
            // Here we purposefully disable MSAA so we just force the bindMS param to false.
            if (!allocForMSAA)
            {
                bindTextureMS = false;
            }

            // MSAA Does not support random read/write.
            bool UAV = enableRandomWrite;
            if (allocForMSAA && (UAV == true))
            {
                Debug.LogWarning("RTHandle that is MSAA-enabled cannot allocate MSAA RT with 'enableRandomWrite = true'.");
                UAV = false;
            }

            int msaaSamples = allocForMSAA ? (int)m_ScaledRTCurrentMSAASamples : 1;
            RTCategory category = allocForMSAA ? RTCategory.MSAA : RTCategory.Regular;

            var rt = new RenderTexture(width, height, (int)depthBufferBits, colorFormat, sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
            {
                hideFlags = HideFlags.HideAndDontSave,
                volumeDepth = slices,
                filterMode = filterMode,
                wrapMode = wrapMode,
                dimension = dimension,
                enableRandomWrite = UAV,
                useMipMap = useMipMap,
                autoGenerateMips = autoGenerateMips,
                anisoLevel = anisoLevel,
                mipMapBias = mipMapBias,
                antiAliasing = msaaSamples,
                bindTextureMS = bindTextureMS,
                useDynamicScale = useDynamicScale,
                vrUsage = vrUsage,
                memorylessMode = memoryless,
                name = CoreUtils.GetRenderTargetAutoName(width, height, slices, colorFormat, name, mips: useMipMap, enableMSAA: allocForMSAA, msaaSamples: m_ScaledRTCurrentMSAASamples)
            };
            rt.Create();

            var rth = new RTHandle(this);
            rth.SetRenderTexture(rt, category);
            rth.m_EnableMSAA = enableMSAA;
            rth.m_EnableRandomWrite = enableRandomWrite;
            rth.useScaling = true;
            rth.m_Name = name;
            m_AutoSizedRTs.Add(rth);
            return rth;
        }

        public string DumpRTInfo()
        {
            string result = "";
            Array.Resize(ref m_AutoSizedRTsArray, m_AutoSizedRTs.Count);
            m_AutoSizedRTs.CopyTo(m_AutoSizedRTsArray);
            for (int i = 0, c = m_AutoSizedRTsArray.Length; i < c; ++i)
            {
                var rt = m_AutoSizedRTsArray[i].rt;
                result = string.Format("{0}\nRT ({1})\t Format: {2} W: {3} H {4}\n", result, i, rt.format, rt.width, rt.height);
            }

            return result;
        }
    }
}
