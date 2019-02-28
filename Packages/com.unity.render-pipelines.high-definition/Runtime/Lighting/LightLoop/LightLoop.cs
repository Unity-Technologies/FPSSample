using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.HDPipeline.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class VisibleLightExtensionMethods
    {
        public static Vector3 GetPosition(this VisibleLight value)
        {
            return value.localToWorld.GetColumn(3);
        }

        public static Vector3 GetForward(this VisibleLight value)
        {
            return value.localToWorld.GetColumn(2);
        }

        public static Vector3 GetUp(this VisibleLight value)
        {
            return value.localToWorld.GetColumn(1);
        }

        public static Vector3 GetRight(this VisibleLight value)
        {
            return value.localToWorld.GetColumn(0);
        }
    }

    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    [GenerateHLSL]
    public enum LightVolumeType
    {
        Cone,
        Sphere,
        Box,
        Count
    }

    [GenerateHLSL]
    public enum LightCategory
    {
        Punctual,
        Area,
        Env,
        Decal,
        DensityVolume,
        Count
    }

    [GenerateHLSL]
    public enum LightFeatureFlags
    {
        // Light bit mask must match LightDefinitions.s_LightFeatureMaskFlags value
        Punctual    = 1 << 12,
        Area        = 1 << 13,
        Directional = 1 << 14,
        Env         = 1 << 15,
        Sky         = 1 << 16,
        SSRefraction = 1 << 17,
        SSReflection = 1 << 18
            // If adding more light be sure to not overflow LightDefinitions.s_LightFeatureMaskFlags
    }

    [GenerateHLSL]
    public class LightDefinitions
    {
        public static int s_MaxNrBigTileLightsPlusOne = 512;      // may be overkill but the footprint is 2 bits per pixel using uint16.
        public static float s_ViewportScaleZ = 1.0f;
        public static int s_UseLeftHandCameraSpace = 1;

        public static int s_TileSizeFptl = 16;
        public static int s_TileSizeClustered = 32;
        public static int s_TileSizeBigTile = 64;

        // feature variants
        public static int s_NumFeatureVariants = 27;

        // Following define the maximum number of bits use in each feature category.
        public static uint s_LightFeatureMaskFlags = 0xFFF000;
        public static uint s_LightFeatureMaskFlagsOpaque = 0xFFF000 & ~((uint)LightFeatureFlags.SSRefraction); // Opaque don't support screen space refraction
        public static uint s_LightFeatureMaskFlagsTransparent = 0xFFF000 & ~((uint)LightFeatureFlags.SSReflection); // Transparent don't support screen space reflection
        public static uint s_MaterialFeatureMaskFlags = 0x000FFF;   // don't use all bits just to be safe from signed and/or float conversions :/
    }

    [GenerateHLSL]
    public struct SFiniteLightBound
    {
        public Vector3 boxAxisX;
        public Vector3 boxAxisY;
        public Vector3 boxAxisZ;
        public Vector3 center;        // a center in camera space inside the bounding volume of the light source.
        public Vector2 scaleXY;
        public float radius;
    };

    [GenerateHLSL]
    public struct LightVolumeData
    {
        public Vector3 lightPos;
        public uint lightVolume;

        public Vector3 lightAxisX;
        public uint lightCategory;

        public Vector3 lightAxisY;
        public float radiusSq;

        public Vector3 lightAxisZ;      // spot +Z axis
        public float cotan;

        public Vector3 boxInnerDist;
        public uint featureFlags;

        public Vector3 boxInvRange;
        public float unused2;
    };

    public class LightLoop
    {
        public enum TileClusterDebug : int
        {
            None,
            Tile,
            Cluster,
            MaterialFeatureVariants
        };

        public enum LightVolumeDebug : int
        {
            Gradient,
            ColorAndEdge
        };

        public enum TileClusterCategoryDebug : int
        {
            Punctual = 1,
            Area = 2,
            AreaAndPunctual = 3,
            Environment = 4,
            EnvironmentAndPunctual = 5,
            EnvironmentAndArea = 6,
            EnvironmentAndAreaAndPunctual = 7,
            Decal = 8,
            DensityVolumes = 16
        };

        internal const int k_MaxCacheSize = 2000000000; //2 GigaByte
        public const int k_MaxDirectionalLightsOnScreen = 16;
        public const int k_MaxPunctualLightsOnScreen    = 512;
        public const int k_MaxAreaLightsOnScreen        = 64;
        public const int k_MaxDecalsOnScreen = 512;
        public const int k_MaxLightsOnScreen = k_MaxDirectionalLightsOnScreen + k_MaxPunctualLightsOnScreen + k_MaxAreaLightsOnScreen + k_MaxEnvLightsOnScreen;
        public const int k_MaxEnvLightsOnScreen = 64;
        public const int k_MaxStereoEyes = 2;
        public static readonly Vector3 k_BoxCullingExtentThreshold = Vector3.one * 0.01f;

        public int m_MaxDirectionalLightsOnScreen;
        public int m_MaxPunctualLightsOnScreen;
        public int m_MaxAreaLightsOnScreen;
        public int m_MaxDecalsOnScreen;
        public int m_MaxLightsOnScreen;
        public int m_MaxEnvLightsOnScreen;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        ComputeBuffer m_DirectionalLightDatas = null;
        ComputeBuffer m_LightDatas = null;
        ComputeBuffer m_EnvLightDatas = null;
        ComputeBuffer m_DecalDatas = null;

        Texture2DArray  m_DefaultTexture2DArray;
        Cubemap         m_DefaultTextureCube;

        PlanarReflectionProbeCache m_ReflectionPlanarProbeCache;
        ReflectionProbeCache m_ReflectionProbeCache;
        TextureCache2D m_CookieTexArray;
        TextureCacheCubemap m_CubeCookieTexArray;
        List<Matrix4x4> m_Env2DCaptureVP = new List<Matrix4x4>();

        // For now we don't use shadow cascade borders.
        static public readonly bool s_UseCascadeBorders = false;

        // Keep sorting array around to avoid garbage
        uint[] m_SortKeys = null;

        void UpdateSortKeysArray(int count)
        {
            if (m_SortKeys == null ||count > m_SortKeys.Length)
            {
                m_SortKeys = new uint[count];
            }
        }

        // Matrix used for Light list building
        // Keep them around to avoid allocations
        Matrix4x4[] m_LightListProjMatrices = new Matrix4x4[2];
        Matrix4x4[] m_LightListProjscrMatrices = new Matrix4x4[2];
        Matrix4x4[] m_LightListInvProjscrMatrices = new Matrix4x4[2];

        Matrix4x4[] m_LightListProjHMatrices = new Matrix4x4[2];
        Matrix4x4[] m_LightListInvProjHMatrices = new Matrix4x4[2];

        public class LightList
        {
            public List<DirectionalLightData> directionalLights;
            public List<LightData> lights;
            public List<EnvLightData> envLights;

            public List<SFiniteLightBound> bounds;
            public List<LightVolumeData> lightVolumes;
            public List<SFiniteLightBound> rightEyeBounds;
            public List<LightVolumeData> rightEyeLightVolumes;

            public void Clear()
            {
                directionalLights.Clear();
                lights.Clear();
                envLights.Clear();

                bounds.Clear();
                lightVolumes.Clear();
                rightEyeBounds.Clear();
                rightEyeLightVolumes.Clear();
            }

            public void Allocate()
            {
                directionalLights = new List<DirectionalLightData>();
                lights = new List<LightData>();
                envLights = new List<EnvLightData>();

                bounds = new List<SFiniteLightBound>();
                lightVolumes = new List<LightVolumeData>();

                rightEyeBounds = new List<SFiniteLightBound>();
                rightEyeLightVolumes = new List<LightVolumeData>();
            }
        }

        LightList m_lightList;
        int m_punctualLightCount = 0;
        int m_areaLightCount = 0;
        int m_lightCount = 0;
        int m_densityVolumeCount = 0;
        bool m_enableBakeShadowMask = false; // Track if any light require shadow mask. In this case we will need to enable the keyword shadow mask

        private ComputeShader buildScreenAABBShader { get { return m_Resources.shaders.buildScreenAABBCS; } }
        private ComputeShader buildPerTileLightListShader { get { return m_Resources.shaders.buildPerTileLightListCS; } }
        private ComputeShader buildPerBigTileLightListShader { get { return m_Resources.shaders.buildPerBigTileLightListCS; } }
        private ComputeShader buildPerVoxelLightListShader { get { return m_Resources.shaders.buildPerVoxelLightListCS; } }

        private ComputeShader buildMaterialFlagsShader { get { return m_Resources.shaders.buildMaterialFlagsCS; } }
        private ComputeShader buildDispatchIndirectShader { get { return m_Resources.shaders.buildDispatchIndirectCS; } }
        private ComputeShader clearDispatchIndirectShader { get { return m_Resources.shaders.clearDispatchIndirectCS; } }
        private ComputeShader deferredComputeShader { get { return m_Resources.shaders.deferredCS; } }
        private ComputeShader screenSpaceShadowComputeShader { get { return m_Resources.shaders.screenSpaceShadowCS; } }


        static int s_GenAABBKernel;
        static int s_GenAABBKernel_Oblique;
        static int s_GenListPerTileKernel;
        static int s_GenListPerTileKernel_Oblique;
        static int s_GenListPerVoxelKernel;
        static int s_GenListPerVoxelKernelOblique;
        static int s_ClearVoxelAtomicKernel;
        static int s_ClearDispatchIndirectKernel;
        static int s_BuildDispatchIndirectKernel;
        static int s_BuildMaterialFlagsWriteKernel;
        static int s_BuildMaterialFlagsOrKernel;

        static int s_shadeOpaqueDirectFptlKernel;
        static int s_shadeOpaqueDirectFptlDebugDisplayKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel;

        static int[] s_shadeOpaqueIndirectFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];
        static int[] s_shadeOpaqueIndirectShadowMaskFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];

        static int s_deferredContactShadowKernel;
        static int s_deferredContactShadowKernelMSAA;

        static ComputeBuffer s_LightVolumeDataBuffer = null;
        static ComputeBuffer s_ConvexBoundsBuffer = null;
        static ComputeBuffer s_AABBBoundsBuffer = null;
        static ComputeBuffer s_LightList = null;
        static ComputeBuffer s_TileList = null;
        static ComputeBuffer s_TileFeatureFlags = null;
        static ComputeBuffer s_DispatchIndirectBuffer = null;

        static ComputeBuffer s_BigTileLightList = null;        // used for pre-pass coarse culling on 64x64 tiles
        static int s_GenListPerBigTileKernel;

        const bool k_UseDepthBuffer = true;      // only has an impact when EnableClustered is true (requires a depth-prepass)

        const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6. NumClusters is 1<<g_iLog2NumClusters
        const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
        float m_ClustScale;
        static ComputeBuffer s_PerVoxelLightLists = null;
        static ComputeBuffer s_PerVoxelOffset = null;
        static ComputeBuffer s_PerTileLogBaseTweak = null;
        static ComputeBuffer s_GlobalLightListAtomic = null;

        static DebugLightVolumes s_lightVolumes = null;

        public enum ClusterPrepassSource : int
        {
            None = 0,
            BigTile = 1,
            Count = 2,
        }

        public enum ClusterDepthSource : int
        {
            NoDepth = 0,
            Depth = 1,
            MSAA_Depth = 2,
            Count = 3,
        }

        static string[,] s_ClusterKernelNames = new string[(int)ClusterPrepassSource.Count, (int)ClusterDepthSource.Count]
        {
            { "TileLightListGen_NoDepthRT", "TileLightListGen_DepthRT", "TileLightListGen_DepthRT_MSAA" },
            { "TileLightListGen_NoDepthRT_SrcBigTile", "TileLightListGen_DepthRT_SrcBigTile", "TileLightListGen_DepthRT_MSAA_SrcBigTile" }
        };
        static string[,] s_ClusterObliqueKernelNames = new string[(int)ClusterPrepassSource.Count, (int)ClusterDepthSource.Count]
        {
            { "TileLightListGen_NoDepthRT ", "TileLightListGen_DepthRT_Oblique", "TileLightListGen_DepthRT_MSAA_Oblique" },
            { "TileLightListGen_NoDepthRT_SrcBigTile", "TileLightListGen_DepthRT_SrcBigTile_Oblique", "TileLightListGen_DepthRT_MSAA_SrcBigTile_Oblique" }
        };
        // clustered light list specific buffers and data end

        static int[] s_TempScreenDimArray = new int[2]; // Used to avoid GC stress when calling SetComputeIntParams

        FrameSettings m_FrameSettings = null;
        RenderPipelineResources m_Resources = null;

        ContactShadows m_ContactShadows = null;
        bool m_EnableContactShadow = false;

        IndirectLightingController m_indirectLightingController = null;

        // Following is an array of material of size eight for all combination of keyword: OUTPUT_SPLIT_LIGHTING - LIGHTLOOP_TILE_PASS - SHADOWS_SHADOWMASK - USE_FPTL_LIGHTLIST/USE_CLUSTERED_LIGHTLIST - DEBUG_DISPLAY
        Material[] m_deferredLightingMaterial;
        Material m_DebugViewTilesMaterial;
        Material m_DebugHDShadowMapMaterial;
        Material m_CubeToPanoMaterial;

        Light m_CurrentSunLight;
        int m_CurrentShadowSortedSunLightIndex = -1;

        // Used to get the current dominant casting shadow light on screen (the one which takes the biggest part of the screen)
        int m_DominantLightIndex = -1;
        float m_DominantLightValue;
        // Store the dominant light to give to ScreenSpaceShadow.compute (null is the dominant light is directional)
        LightData m_DominantLightData;

        public Light GetCurrentSunLight() { return m_CurrentSunLight; }

        // shadow related stuff
        HDShadowManager                     m_ShadowManager;
        HDShadowInitParameters              m_ShadowInitParameters;

        // Used to shadow shadow maps with use selection enabled in the debug menu
        int m_DebugSelectedLightShadowIndex;
        int m_DebugSelectedLightShadowCount;

        public ComputeBuffer GetBigTileLightList()
        {
            return s_BigTileLightList;
        }
        public int GetNumTileBigTileX(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.x, LightDefinitions.s_TileSizeBigTile);
        }

        public int GetNumTileBigTileY(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.y, LightDefinitions.s_TileSizeBigTile);
        }

        public int GetNumTileFtplX(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.x, LightDefinitions.s_TileSizeFptl);
        }

        void InitShadowSystem(HDRenderPipelineAsset hdAsset)
        {
            m_ShadowInitParameters = hdAsset.GetRenderPipelineSettings().hdShadowInitParams;
            m_ShadowManager = new HDShadowManager(
                m_ShadowInitParameters.shadowAtlasResolution,
                m_ShadowInitParameters.shadowAtlasResolution,
                m_ShadowInitParameters.maxShadowRequests,
                m_ShadowInitParameters.shadowMapsDepthBits,
                hdAsset.renderPipelineResources.shaders.shadowClearPS
            );
        }

        void DeinitShadowSystem()
        {
            m_ShadowManager.Dispose();
            m_ShadowManager = null;
        }

        int GetNumTileFtplY(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.y, LightDefinitions.s_TileSizeFptl);
        }

        int GetNumTileClusteredX(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.x, LightDefinitions.s_TileSizeClustered);
        }

        int GetNumTileClusteredY(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.y, LightDefinitions.s_TileSizeClustered);
        }

        public bool GetFeatureVariantsEnabled()
        {
            return m_FrameSettings.shaderLitMode == LitShaderMode.Deferred && m_FrameSettings.lightLoopSettings.isFptlEnabled && m_FrameSettings.lightLoopSettings.enableComputeLightEvaluation &&
                (m_FrameSettings.lightLoopSettings.enableComputeLightVariants || m_FrameSettings.lightLoopSettings.enableComputeMaterialVariants);
        }

        public LightLoop()
        {}

        int GetDeferredLightingMaterialIndex(int outputSplitLighting, int lightLoopTilePass, int shadowMask, int debugDisplay)
        {
            return (outputSplitLighting) | (lightLoopTilePass << 1) | (shadowMask << 2) | (debugDisplay << 3);
        }

        public void Build(HDRenderPipelineAsset hdAsset, IBLFilterBSDF[] iBLFilterBSDFArray)
        {
            m_Resources = hdAsset.renderPipelineResources;
            var lightLoopSettings = hdAsset.renderPipelineSettings.lightLoopSettings;

            m_DebugViewTilesMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.debugViewTilesPS);
            m_DebugHDShadowMapMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.debugHDShadowMapPS);
            m_CubeToPanoMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.cubeToPanoPS);

            m_MaxDirectionalLightsOnScreen = lightLoopSettings.maxDirectionalLightsOnScreen;
            m_MaxPunctualLightsOnScreen = lightLoopSettings.maxPunctualLightsOnScreen;
            m_MaxAreaLightsOnScreen = lightLoopSettings.maxAreaLightsOnScreen;
            m_MaxDecalsOnScreen = lightLoopSettings.maxDecalsOnScreen;
            m_MaxEnvLightsOnScreen = lightLoopSettings.maxEnvLightsOnScreen;
            m_MaxLightsOnScreen = m_MaxDirectionalLightsOnScreen + m_MaxPunctualLightsOnScreen + m_MaxAreaLightsOnScreen + m_MaxEnvLightsOnScreen;

            m_lightList = new LightList();
            m_lightList.Allocate();
            m_Env2DCaptureVP.Clear();
            for (int i = 0, c = Mathf.Max(1, lightLoopSettings.planarReflectionProbeCacheSize); i < c; ++i)
                m_Env2DCaptureVP.Add(Matrix4x4.identity);

            m_DirectionalLightDatas = new ComputeBuffer(m_MaxDirectionalLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
            m_LightDatas = new ComputeBuffer(m_MaxPunctualLightsOnScreen + m_MaxAreaLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
            m_EnvLightDatas = new ComputeBuffer(m_MaxEnvLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
            m_DecalDatas = new ComputeBuffer(m_MaxDecalsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DecalData)));

            GlobalLightLoopSettings gLightLoopSettings = hdAsset.GetRenderPipelineSettings().lightLoopSettings;
            m_CookieTexArray = new TextureCache2D("Cookie");
            int coockieSize = gLightLoopSettings.cookieTexArraySize;
            int coockieResolution = (int)gLightLoopSettings.cookieSize;
            if (TextureCache2D.GetApproxCacheSizeInByte(coockieSize, coockieResolution, 1) > k_MaxCacheSize)
                coockieSize = TextureCache2D.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, coockieResolution, 1);
            m_CookieTexArray.AllocTextureArray(coockieSize, coockieResolution, coockieResolution, TextureFormat.RGBA32, true);
            m_CubeCookieTexArray = new TextureCacheCubemap("Cookie");
            int coockieCubeSize = gLightLoopSettings.cubeCookieTexArraySize;
            int coockieCubeResolution = (int)gLightLoopSettings.pointCookieSize;
            if (TextureCacheCubemap.GetApproxCacheSizeInByte(coockieCubeSize, coockieCubeResolution, 1) > k_MaxCacheSize)
                coockieCubeSize = TextureCacheCubemap.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, coockieCubeResolution, 1);
            m_CubeCookieTexArray.AllocTextureArray(coockieCubeSize, coockieCubeResolution, TextureFormat.RGBA32, true, m_CubeToPanoMaterial);

            // For regular reflection probes, we need to convolve with all the BSDF functions
            TextureFormat probeCacheFormat = gLightLoopSettings.reflectionCacheCompressed ? TextureFormat.BC6H : TextureFormat.RGBAHalf;
            int reflectionCubeSize = gLightLoopSettings.reflectionProbeCacheSize;
            int reflectionCubeResolution = (int)gLightLoopSettings.reflectionCubemapSize;
            if (ReflectionProbeCache.GetApproxCacheSizeInByte(reflectionCubeSize, reflectionCubeResolution, iBLFilterBSDFArray.Length) > k_MaxCacheSize)
                reflectionCubeSize = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, reflectionCubeResolution, iBLFilterBSDFArray.Length);
            m_ReflectionProbeCache = new ReflectionProbeCache(hdAsset, iBLFilterBSDFArray, reflectionCubeSize, reflectionCubeResolution, probeCacheFormat, true);

            // For planar reflection we only convolve with the GGX filter, otherwise it would be too expensive
            TextureFormat planarProbeCacheFormat = gLightLoopSettings.planarReflectionCacheCompressed ? TextureFormat.BC6H : TextureFormat.RGBAHalf;
            int reflectionPlanarSize = gLightLoopSettings.planarReflectionProbeCacheSize;
            int reflectionPlanarResolution = (int)gLightLoopSettings.planarReflectionTextureSize;
            if (ReflectionProbeCache.GetApproxCacheSizeInByte(reflectionPlanarSize, reflectionPlanarResolution, 1) > k_MaxCacheSize)
                reflectionPlanarSize = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, reflectionPlanarResolution, 1);
            m_ReflectionPlanarProbeCache = new PlanarReflectionProbeCache(hdAsset, (IBLFilterGGX)iBLFilterBSDFArray[0], reflectionPlanarSize, reflectionPlanarResolution, planarProbeCacheFormat, true);

            s_GenAABBKernel = buildScreenAABBShader.FindKernel("ScreenBoundsAABB");
            s_GenAABBKernel_Oblique = buildScreenAABBShader.FindKernel("ScreenBoundsAABB_Oblique");

            // The bounds and light volumes are view-dependent, and AABB is additionally projection dependent.
            // The view and proj matrices are per eye in stereo. This means we have to double the size of these buffers.
            // TODO: Maybe in stereo, we will only support half as many lights total, in order to minimize buffer size waste.
            // Alternatively, we could re-size these buffers if any stereo camera is active, instead of unilaterally increasing buffer size.
            // TODO: I don't think k_MaxLightsOnScreen corresponds to the actual correct light count for cullable light types (punctual, area, env, decal)
            s_AABBBoundsBuffer = new ComputeBuffer(k_MaxStereoEyes * 2 * m_MaxLightsOnScreen, 4 * sizeof(float));
            s_ConvexBoundsBuffer = new ComputeBuffer(k_MaxStereoEyes * m_MaxLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
            s_LightVolumeDataBuffer = new ComputeBuffer(k_MaxStereoEyes * m_MaxLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolumeData)));
            s_DispatchIndirectBuffer = new ComputeBuffer(LightDefinitions.s_NumFeatureVariants * 3, sizeof(uint), ComputeBufferType.IndirectArguments);

            // Cluster
            {
                s_ClearVoxelAtomicKernel = buildPerVoxelLightListShader.FindKernel("ClearAtomic");
                s_GlobalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
            }

            s_GenListPerBigTileKernel = buildPerBigTileLightListShader.FindKernel("BigTileLightListGen");

            s_BuildDispatchIndirectKernel = buildDispatchIndirectShader.FindKernel("BuildDispatchIndirect");
            s_ClearDispatchIndirectKernel = clearDispatchIndirectShader.FindKernel("ClearDispatchIndirect");

            s_BuildMaterialFlagsOrKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Or");
            s_BuildMaterialFlagsWriteKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Write");

            s_shadeOpaqueDirectFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl");
            s_shadeOpaqueDirectFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl_DebugDisplay");

            s_shadeOpaqueDirectShadowMaskFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_ShadowMask_Fptl");
            s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_ShadowMask_Fptl_DebugDisplay");

            s_deferredContactShadowKernel = screenSpaceShadowComputeShader.FindKernel("DeferredContactShadow");
            s_deferredContactShadowKernelMSAA = screenSpaceShadowComputeShader.FindKernel("DeferredContactShadowMSAA");

            for (int variant = 0; variant < LightDefinitions.s_NumFeatureVariants; variant++)
            {
                s_shadeOpaqueIndirectFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_Fptl_Variant" + variant);
                s_shadeOpaqueIndirectShadowMaskFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_ShadowMask_Fptl_Variant" + variant);
            }

            s_LightList = null;
            s_TileList = null;
            s_TileFeatureFlags = null;

            // OUTPUT_SPLIT_LIGHTING - LIGHTLOOP_TILE_PASS - SHADOWS_SHADOWMASK - DEBUG_DISPLAY
            m_deferredLightingMaterial = new Material[16];

            for (int outputSplitLighting = 0; outputSplitLighting < 2; ++outputSplitLighting)
            {
                for (int lightLoopTilePass = 0; lightLoopTilePass < 2; ++lightLoopTilePass)
                {
                    for (int shadowMask = 0; shadowMask < 2; ++shadowMask)
                    {
                        for (int debugDisplay = 0; debugDisplay < 2; ++debugDisplay)
                        {
                            int index = GetDeferredLightingMaterialIndex(outputSplitLighting, lightLoopTilePass, shadowMask, debugDisplay);

                            m_deferredLightingMaterial[index] = CoreUtils.CreateEngineMaterial(m_Resources.shaders.deferredPS);
                            m_deferredLightingMaterial[index].name = string.Format("{0}_{1}", m_Resources.shaders.deferredPS.name, index);
                            CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "OUTPUT_SPLIT_LIGHTING", outputSplitLighting == 1);
                            CoreUtils.SelectKeyword(m_deferredLightingMaterial[index], "LIGHTLOOP_TILE_PASS", "LIGHTLOOP_SINGLE_PASS", lightLoopTilePass == 1);
                            CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "SHADOWS_SHADOWMASK", shadowMask == 1);
                            CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "DEBUG_DISPLAY", debugDisplay == 1);

                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilRef, outputSplitLighting == 1 ? (int)StencilLightingUsage.SplitLighting : (int)StencilLightingUsage.RegularLighting);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._SrcBlend, (int)BlendMode.One);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._DstBlend, (int)BlendMode.Zero);
                        }
                    }
                }
            }

            m_DefaultTexture2DArray = new Texture2DArray(1, 1, 1, TextureFormat.ARGB32, false);
            m_DefaultTexture2DArray.hideFlags = HideFlags.HideAndDontSave;
            m_DefaultTexture2DArray.name = CoreUtils.GetTextureAutoName(1, 1, TextureFormat.ARGB32, depth: 1, dim: TextureDimension.Tex2DArray, name: "LightLoopDefault");
            m_DefaultTexture2DArray.SetPixels32(new Color32[1] { new Color32(128, 128, 128, 128) }, 0);
            m_DefaultTexture2DArray.Apply();

            m_DefaultTextureCube = new Cubemap(16, TextureFormat.ARGB32, false);
            m_DefaultTextureCube.Apply();

            // Setup shadow algorithms
            var shadowParams = hdAsset.renderPipelineSettings.hdShadowInitParams;
            var shadowKeywords = new[]{"SHADOW_LOW", "SHADOW_MEDIUM", "SHADOW_HIGH"};
            foreach (var p in shadowKeywords)
                Shader.DisableKeyword(p);
            Shader.EnableKeyword(shadowKeywords[(int)shadowParams.shadowQuality]);

            InitShadowSystem(hdAsset);

            s_lightVolumes = new DebugLightVolumes();
            s_lightVolumes.InitData(m_Resources);
        }

        public void Cleanup()
        {
            s_lightVolumes.ReleaseData();

            DeinitShadowSystem();

            CoreUtils.Destroy(m_DefaultTexture2DArray);
            CoreUtils.Destroy(m_DefaultTextureCube);

            CoreUtils.SafeRelease(m_DirectionalLightDatas);
            CoreUtils.SafeRelease(m_LightDatas);
            CoreUtils.SafeRelease(m_EnvLightDatas);
            CoreUtils.SafeRelease(m_DecalDatas);

            if (m_ReflectionProbeCache != null)
            {
                m_ReflectionProbeCache.Release();
                m_ReflectionProbeCache = null;
            }
            if (m_ReflectionPlanarProbeCache != null)
            {
                m_ReflectionPlanarProbeCache.Release();
                m_ReflectionPlanarProbeCache = null;
            }
            if (m_CookieTexArray != null)
            {
                m_CookieTexArray.Release();
                m_CookieTexArray = null;
            }
            if (m_CubeCookieTexArray != null)
            {
                m_CubeCookieTexArray.Release();
                m_CubeCookieTexArray = null;
            }

            ReleaseResolutionDependentBuffers();

            CoreUtils.SafeRelease(s_AABBBoundsBuffer);
            CoreUtils.SafeRelease(s_ConvexBoundsBuffer);
            CoreUtils.SafeRelease(s_LightVolumeDataBuffer);
            CoreUtils.SafeRelease(s_DispatchIndirectBuffer);

            // enableClustered
            CoreUtils.SafeRelease(s_GlobalLightListAtomic);

            for (int outputSplitLighting = 0; outputSplitLighting < 2; ++outputSplitLighting)
            {
                for (int lightLoopTilePass = 0; lightLoopTilePass < 2; ++lightLoopTilePass)
                {
                    for (int shadowMask = 0; shadowMask < 2; ++shadowMask)
                    {
                        for (int debugDisplay = 0; debugDisplay < 2; ++debugDisplay)
                        {
                            int index = GetDeferredLightingMaterialIndex(outputSplitLighting, lightLoopTilePass, shadowMask, debugDisplay);
                            CoreUtils.Destroy(m_deferredLightingMaterial[index]);
                        }
                    }
                }
            }

            CoreUtils.Destroy(m_DebugViewTilesMaterial);
            CoreUtils.Destroy(m_DebugHDShadowMapMaterial);
            CoreUtils.Destroy(m_CubeToPanoMaterial);
        }

        public void NewFrame(FrameSettings frameSettings)
        {
            m_FrameSettings = frameSettings;

            m_ContactShadows = VolumeManager.instance.stack.GetComponent<ContactShadows>();
            m_EnableContactShadow = m_FrameSettings.enableContactShadows && m_ContactShadows.enable && m_ContactShadows.length > 0;
            m_indirectLightingController = VolumeManager.instance.stack.GetComponent<IndirectLightingController>();

            // Cluster
            {
                var clustPrepassSourceIdx = m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? ClusterPrepassSource.BigTile : ClusterPrepassSource.None;
                var clustDepthSourceIdx = ClusterDepthSource.NoDepth;
                if (k_UseDepthBuffer)
                {
                    if (m_FrameSettings.enableMSAA)
                        clustDepthSourceIdx = ClusterDepthSource.MSAA_Depth;
                    else
                        clustDepthSourceIdx = ClusterDepthSource.Depth;
                }
                var kernelName = s_ClusterKernelNames[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx];
                var kernelObliqueName = s_ClusterObliqueKernelNames[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx];

                s_GenListPerVoxelKernel = buildPerVoxelLightListShader.FindKernel(kernelName);
                s_GenListPerVoxelKernelOblique = buildPerVoxelLightListShader.FindKernel(kernelObliqueName);
            }

            if (GetFeatureVariantsEnabled())
            {
                s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile_FeatureFlags" : "TileLightListGen_FeatureFlags");
                s_GenListPerTileKernel_Oblique = buildPerTileLightListShader.FindKernel(m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile_FeatureFlags_Oblique" : "TileLightListGen_FeatureFlags_Oblique");

            }
            else
            {
                s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile" : "TileLightListGen");
                s_GenListPerTileKernel_Oblique = buildPerTileLightListShader.FindKernel(m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile_Oblique" : "TileLightListGen_Oblique");
            }

            m_CookieTexArray.NewFrame();
            m_CubeCookieTexArray.NewFrame();
            m_ReflectionProbeCache.NewFrame();
            m_ReflectionPlanarProbeCache.NewFrame();
        }

        public bool NeedResize()
        {
            return s_LightList == null || s_TileList == null || s_TileFeatureFlags == null ||
                (s_BigTileLightList == null && m_FrameSettings.lightLoopSettings.enableBigTilePrepass) ||
                (s_PerVoxelLightLists == null);
        }

        public void ReleaseResolutionDependentBuffers()
        {
            CoreUtils.SafeRelease(s_LightList);
            CoreUtils.SafeRelease(s_TileList);
            CoreUtils.SafeRelease(s_TileFeatureFlags);

            // enableClustered
            CoreUtils.SafeRelease(s_PerVoxelLightLists);
            CoreUtils.SafeRelease(s_PerVoxelOffset);
            CoreUtils.SafeRelease(s_PerTileLogBaseTweak);

            // enableBigTilePrepass
            CoreUtils.SafeRelease(s_BigTileLightList);
        }

        int NumLightIndicesPerClusteredTile()
        {
            return 32 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
        }

        public void AllocResolutionDependentBuffers(int width, int height, bool stereoEnabled)
        {
            var nrStereoLayers = stereoEnabled ? 2 : 1;

            var nrTilesX = (width + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
            var nrTilesY = (height + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
            var nrTiles = nrTilesX * nrTilesY * nrStereoLayers;
            const int capacityUShortsPerTile = 32;
            const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

            s_LightList = new ComputeBuffer((int)LightCategory.Count * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display
            s_TileList = new ComputeBuffer((int)LightDefinitions.s_NumFeatureVariants * nrTiles, sizeof(uint));
            s_TileFeatureFlags = new ComputeBuffer(nrTiles, sizeof(uint));

            // Cluster
            {
                var nrClustersX = (width + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                var nrClustersY = (height + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                var nrClusterTiles = nrClustersX * nrClustersY * nrStereoLayers;

                s_PerVoxelOffset = new ComputeBuffer((int)LightCategory.Count * (1 << k_Log2NumClusters) * nrClusterTiles, sizeof(uint));
                s_PerVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrClusterTiles, sizeof(uint));

                if (k_UseDepthBuffer)
                {
                    s_PerTileLogBaseTweak = new ComputeBuffer(nrClusterTiles, sizeof(float));
                }
            }

            if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
            {
                var nrBigTilesX = (width + 63) / 64;
                var nrBigTilesY = (height + 63) / 64;
                var nrBigTiles = nrBigTilesX * nrBigTilesY * nrStereoLayers;
                s_BigTileLightList = new ComputeBuffer(LightDefinitions.s_MaxNrBigTileLightsPlusOne * nrBigTiles, sizeof(uint));
            }
        }

        public static Matrix4x4 WorldToCamera(Camera camera)
        {
            // camera.worldToCameraMatrix is RHS and Unity's transforms are LHS
            // We need to flip it to work with transforms
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * camera.worldToCameraMatrix;
        }

        static Matrix4x4 WorldToViewStereo(Camera camera, Camera.StereoscopicEye eyeIndex)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * camera.GetStereoViewMatrix(eyeIndex);
        }

        // For light culling system, we need non oblique projection matrices
        static Matrix4x4 CameraProjectionNonObliqueLHS(HDCamera camera)
        {
            // camera.projectionMatrix expect RHS data and Unity's transforms are LHS
            // We need to flip it to work with transforms
            return camera.nonObliqueProjMatrix * Matrix4x4.Scale(new Vector3(1, 1, -1));
        }

        static Matrix4x4 CameraProjectionStereoLHS(Camera camera, Camera.StereoscopicEye eyeIndex)
        {
            return camera.GetStereoProjectionMatrix(eyeIndex) * Matrix4x4.Scale(new Vector3(1, 1, -1));
        }

        public Vector3 GetLightColor(VisibleLight light)
        {
            return new Vector3(light.finalColor.r, light.finalColor.g, light.finalColor.b);
        }

        bool GetDominantLightWithShadows(AdditionalShadowData additionalShadowData, VisibleLight light, Light lightComponent, int lightIndex = -1)
        {
            // Can happen for particle lights (where we don't support shadows anyway)
            if (lightComponent == null)
                return false;

            // Ratio of the size of the light on screen and its intensity, gives a value used to compare light importance
            float lightDominanceValue = light.screenRect.size.magnitude * lightComponent.intensity;;

            if (additionalShadowData == null || !additionalShadowData.contactShadows)
                return false;
            if (lightDominanceValue <= m_DominantLightValue || m_DominantLightValue == Single.PositiveInfinity)
                return false;

            // Directional lights are always considered first.
            if (light.lightType == LightType.Directional)
                m_DominantLightValue = Single.PositiveInfinity;
            else
            {
                m_DominantLightData = m_lightList.lights[lightIndex];
                m_DominantLightIndex = lightIndex;
                m_DominantLightValue = lightDominanceValue;
            }
            return true;
        }

        public bool GetDirectionalLightData(CommandBuffer cmd, GPULightType gpuLightType, VisibleLight light, Light lightComponent, HDAdditionalLightData additionalLightData, AdditionalShadowData additionalShadowData, int lightIndex, int shadowIndex, DebugDisplaySettings debugDisplaySettings, int sortedIndex)
        {
            // Clamp light list to the maximum allowed lights on screen to avoid ComputeBuffer overflow
            if (m_lightList.directionalLights.Count >= m_MaxDirectionalLightsOnScreen)
                return false;

            bool contributesToLighting = ((additionalLightData.lightDimmer > 0) && (additionalLightData.affectDiffuse || additionalLightData.affectSpecular)) || (additionalLightData.volumetricDimmer > 0);

            if (!contributesToLighting)
                return false;

            // Discard light if disabled in debug display settings
            if (!debugDisplaySettings.lightingDebugSettings.showDirectionalLight)
                return false;

            var lightData = new DirectionalLightData();

            lightData.lightLayers = additionalLightData.GetLightLayers();

            // Light direction for directional is opposite to the forward direction
            lightData.forward = light.GetForward();
            // Rescale for cookies and windowing.
            lightData.right      = light.GetRight() * 2 / Mathf.Max(additionalLightData.shapeWidth, 0.001f);
            lightData.up         = light.GetUp() * 2 / Mathf.Max(additionalLightData.shapeHeight, 0.001f);
            lightData.positionRWS = light.GetPosition();
            lightData.color = GetLightColor(light);

            // Caution: This is bad but if additionalData == HDUtils.s_DefaultHDAdditionalLightData it mean we are trying to promote legacy lights, which is the case for the preview for example, so we need to multiply by PI as legacy Unity do implicit divide by PI for direct intensity.
            // So we expect that all light with additionalData == HDUtils.s_DefaultHDAdditionalLightData are currently the one from the preview, light in scene MUST have additionalData
            lightData.color *= (HDUtils.s_DefaultHDAdditionalLightData == additionalLightData) ? Mathf.PI : 1.0f;

            lightData.lightDimmer           = additionalLightData.lightDimmer;
            lightData.diffuseDimmer         = additionalLightData.affectDiffuse  ? additionalLightData.lightDimmer * m_FrameSettings.diffuseGlobalDimmer  : 0;
            lightData.specularDimmer        = additionalLightData.affectSpecular ? additionalLightData.lightDimmer * m_FrameSettings.specularGlobalDimmer : 0;
            lightData.volumetricLightDimmer = additionalLightData.volumetricDimmer;

            lightData.shadowIndex = lightData.cookieIndex = -1;

            if (lightComponent != null && lightComponent.cookie != null)
            {
                lightData.tileCookie = lightComponent.cookie.wrapMode == TextureWrapMode.Repeat ? 1 : 0;
                lightData.cookieIndex = m_CookieTexArray.FetchSlice(cmd, lightComponent.cookie);
            }

            if (additionalShadowData)
            {
                lightData.shadowDimmer           = additionalShadowData.shadowDimmer;
                lightData.volumetricShadowDimmer = additionalShadowData.volumetricShadowDimmer;
            }
            else
            {
                lightData.shadowDimmer           = 1.0f;
                lightData.volumetricShadowDimmer = 1.0f;
            }

            // fix up shadow information
            lightData.shadowIndex = shadowIndex;
            if (shadowIndex != -1)
            {
                m_CurrentSunLight = lightComponent;
                m_CurrentShadowSortedSunLightIndex = sortedIndex;
            }

            // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
            lightData.minRoughness = Mathf.Max((1.0f - additionalLightData.maxSmoothness) * (1.0f - additionalLightData.maxSmoothness));

            lightData.shadowMaskSelector = Vector4.zero;

            if (IsBakedShadowMaskLight(lightComponent))
            {
                lightData.shadowMaskSelector[lightComponent.bakingOutput.occlusionMaskChannel] = 1.0f;
                lightData.nonLightMappedOnly = lightComponent.lightShadowCasterMode == LightShadowCasterMode.NonLightmappedOnly ? 1 : 0;
            }
            else
            {
                // use -1 to say that we don't use shadow mask
                lightData.shadowMaskSelector.x = -1.0f;
                lightData.nonLightMappedOnly = 0;
            }

            // Sun disk.
            {
                var sunDiskAngle = additionalLightData.sunDiskSize;
                var sunHaloSize  = additionalLightData.sunHaloSize;

                var cosConeInnerHalfAngle = Mathf.Clamp(Mathf.Cos(sunDiskAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                var cosConeOuterHalfAngle = Mathf.Clamp(Mathf.Cos(sunDiskAngle * 0.5f * (1 + sunHaloSize) * Mathf.Deg2Rad), 0.0f, 1.0f);

                var val = Mathf.Max(0.0001f, (cosConeInnerHalfAngle - cosConeOuterHalfAngle));
                lightData.angleScale = 1.0f / val;
                lightData.angleOffset = -cosConeOuterHalfAngle * lightData.angleScale;

            }

            // Fallback to the first non shadow casting directional light.
            m_CurrentSunLight = m_CurrentSunLight == null ? lightComponent : m_CurrentSunLight;

            lightData.contactShadowIndex = -1;

            // The first directional light with contact shadow enabled is always taken as dominant light
            if (GetDominantLightWithShadows(additionalShadowData, light, lightComponent))
                lightData.contactShadowIndex = 0;

            m_lightList.directionalLights.Add(lightData);

            return true;
        }

        void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, out float scale, out float bias)
        {
            // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
            float distanceFadeNear = 0.9f * fadeDistance;
            scale = 1.0f / (fadeDistance - distanceFadeNear);
            bias = -distanceFadeNear / (fadeDistance - distanceFadeNear);
        }

        float ComputeLinearDistanceFade(float distanceToCamera, float fadeDistance)
        {
            float scale;
            float bias;
            GetScaleAndBiasForLinearDistanceFade(fadeDistance, out scale, out bias);

            return 1.0f - Mathf.Clamp01(distanceToCamera * scale + bias);
        }

        public bool GetLightData(CommandBuffer cmd, HDShadowSettings shadowSettings, Camera camera, GPULightType gpuLightType,
            VisibleLight light, Light lightComponent, HDAdditionalLightData additionalLightData, AdditionalShadowData additionalShadowData,
            int lightIndex, int shadowIndex, ref Vector3 lightDimensions, DebugDisplaySettings debugDisplaySettings)
        {
            // Clamp light list to the maximum allowed lights on screen to avoid ComputeBuffer overflow
            if (m_lightList.lights.Count >= m_MaxPunctualLightsOnScreen + m_MaxAreaLightsOnScreen)
                return false;

            // Both of these positions are non-camera-relative.
            float distanceToCamera  = (light.GetPosition() - camera.transform.position).magnitude;
            float lightDistanceFade = ComputeLinearDistanceFade(distanceToCamera, additionalLightData.fadeDistance);

            bool contributesToLighting = ((additionalLightData.lightDimmer > 0) && (additionalLightData.affectDiffuse || additionalLightData.affectSpecular)) || (additionalLightData.volumetricDimmer > 0);
                 contributesToLighting = contributesToLighting && (lightDistanceFade > 0);

            if (!contributesToLighting)
                return false;

            var lightData = new LightData();

            lightData.lightLayers = additionalLightData.GetLightLayers();

            lightData.lightType = gpuLightType;

            lightData.positionRWS = light.GetPosition();

            bool applyRangeAttenuation = additionalLightData.applyRangeAttenuation && (gpuLightType != GPULightType.ProjectorBox);

            // Discard light if disabled in debug display settings
            if (lightData.lightType.IsAreaLight())
            {
                if (!debugDisplaySettings.lightingDebugSettings.showAreaLight)
                    return false;
            }
            else
            {
                if (!debugDisplaySettings.lightingDebugSettings.showPunctualLight)
                    return false;
            }

            lightData.range = light.range;

            if (applyRangeAttenuation)
            {
                lightData.rangeAttenuationScale = 1.0f / (light.range * light.range);
                lightData.rangeAttenuationBias  = 1.0f;

                if (lightData.lightType == GPULightType.Rectangle)
                {
                    // Rect lights are currently a special case because they use the normalized
                    // [0, 1] attenuation range rather than the regular [0, r] one.
                    lightData.rangeAttenuationScale = 1.0f;
                }
            }
            else // Don't apply any attenuation but do a 'step' at range
            {
                // Solve f(x) = b - (a * x)^2 where x = (d/r)^2.
                // f(0) = huge -> b = huge.
                // f(1) = 0    -> huge - a^2 = 0 -> a = sqrt(huge).
                const float hugeValue = 16777216.0f;
                const float sqrtHuge  = 4096.0f;
                lightData.rangeAttenuationScale = sqrtHuge / (light.range * light.range);
                lightData.rangeAttenuationBias  = hugeValue;

                if (lightData.lightType == GPULightType.Rectangle)
                {
                    // Rect lights are currently a special case because they use the normalized
                    // [0, 1] attenuation range rather than the regular [0, r] one.
                    lightData.rangeAttenuationScale = sqrtHuge;
                }
            }

            lightData.color = GetLightColor(light);

            lightData.forward = light.GetForward();
            lightData.up = light.GetUp();
            lightData.right = light.GetRight();

            lightDimensions.x = additionalLightData.shapeWidth;
            lightDimensions.y = additionalLightData.shapeHeight;
            lightDimensions.z = light.range;

            if (lightData.lightType == GPULightType.ProjectorBox)
            {
                // Rescale for cookies and windowing.
                lightData.right *= 2.0f / Mathf.Max(additionalLightData.shapeWidth, 0.001f);
                lightData.up    *= 2.0f / Mathf.Max(additionalLightData.shapeHeight, 0.001f);
            }
            else if (lightData.lightType == GPULightType.ProjectorPyramid)
            {
                // Get width and height for the current frustum
                var spotAngle = light.spotAngle;

                float frustumWidth, frustumHeight;

                if (additionalLightData.aspectRatio >= 1.0f)
                {
                    frustumHeight = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                    frustumWidth = frustumHeight * additionalLightData.aspectRatio;
                }
                else
                {
                    frustumWidth = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                    frustumHeight = frustumWidth / additionalLightData.aspectRatio;
                }

                // Adjust based on the new parametrization.
                lightDimensions.x = frustumWidth;
                lightDimensions.y = frustumHeight;

                // Rescale for cookies and windowing.
                lightData.right *= 2.0f / frustumWidth;
                lightData.up *= 2.0f / frustumHeight;
            }

            if (lightData.lightType == GPULightType.Spot)
            {
                var spotAngle = light.spotAngle;

                var innerConePercent = additionalLightData.GetInnerSpotPercent01();
                var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                var sinSpotOuterHalfAngle = Mathf.Sqrt(1.0f - cosSpotOuterHalfAngle * cosSpotOuterHalfAngle);
                var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                var val = Mathf.Max(0.0001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                lightData.angleScale = 1.0f / val;
                lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;

                // Rescale for cookies and windowing.
                float cotOuterHalfAngle = cosSpotOuterHalfAngle / sinSpotOuterHalfAngle;
                lightData.up    *= cotOuterHalfAngle;
                lightData.right *= cotOuterHalfAngle;
            }
            else
            {
                // These are the neutral values allowing GetAngleAnttenuation in shader code to return 1.0
                lightData.angleScale = 0.0f;
                lightData.angleOffset = 1.0f;
            }

            if (lightData.lightType != GPULightType.Directional && lightData.lightType != GPULightType.ProjectorBox)
            {
                // Store the squared radius of the light to simulate a fill light.
                lightData.size = new Vector2(additionalLightData.shapeRadius * additionalLightData.shapeRadius, 0);
            }

            if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Tube)
            {
                lightData.size = new Vector2(additionalLightData.shapeWidth, additionalLightData.shapeHeight);
            }

            lightData.lightDimmer           = lightDistanceFade * (additionalLightData.lightDimmer);
            lightData.diffuseDimmer         = lightDistanceFade * (additionalLightData.affectDiffuse  ? additionalLightData.lightDimmer * m_FrameSettings.diffuseGlobalDimmer  : 0);
            lightData.specularDimmer        = lightDistanceFade * (additionalLightData.affectSpecular ? additionalLightData.lightDimmer * m_FrameSettings.specularGlobalDimmer : 0);
            lightData.volumetricLightDimmer = lightDistanceFade * (additionalLightData.volumetricDimmer);

            lightData.cookieIndex = -1;
            lightData.shadowIndex = -1;

            if (lightComponent != null && lightComponent.cookie != null)
            {
                // TODO: add texture atlas support for cookie textures.
                switch (light.lightType)
                {
                    case LightType.Spot:
                        lightData.cookieIndex = m_CookieTexArray.FetchSlice(cmd, lightComponent.cookie);
                        break;
                    case LightType.Point:
                        lightData.cookieIndex = m_CubeCookieTexArray.FetchSlice(cmd, lightComponent.cookie);
                        break;
                }
            }
            else if (light.lightType == LightType.Spot && additionalLightData.spotLightShape != SpotLightShape.Cone)
            {
                // Projectors lights must always have a cookie texture.
                // As long as the cache is a texture array and not an atlas, the 4x4 white texture will be rescaled to 128
                lightData.cookieIndex = m_CookieTexArray.FetchSlice(cmd, Texture2D.whiteTexture);
            }

            if (additionalShadowData)
            {
                float shadowDistanceFade         = ComputeLinearDistanceFade(distanceToCamera, Mathf.Min(shadowSettings.maxShadowDistance, additionalShadowData.shadowFadeDistance));
                lightData.shadowDimmer           = shadowDistanceFade * additionalShadowData.shadowDimmer;
                lightData.volumetricShadowDimmer = shadowDistanceFade * additionalShadowData.volumetricShadowDimmer;
            }
            else
            {
                lightData.shadowDimmer           = 1.0f;
                lightData.volumetricShadowDimmer = 1.0f;
            }

            // fix up shadow information
            lightData.shadowIndex = shadowIndex;

            // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
            lightData.minRoughness = (1.0f - additionalLightData.maxSmoothness) * (1.0f - additionalLightData.maxSmoothness);

            lightData.shadowMaskSelector = Vector4.zero;

            if (IsBakedShadowMaskLight(lightComponent))
            {
                lightData.shadowMaskSelector[lightComponent.bakingOutput.occlusionMaskChannel] = 1.0f;
                lightData.nonLightMappedOnly = lightComponent.lightShadowCasterMode == LightShadowCasterMode.NonLightmappedOnly ? 1 : 0;
            }
            else
            {
                // use -1 to say that we don't use shadow mask
                lightData.shadowMaskSelector.x = -1.0f;
                lightData.nonLightMappedOnly = 0;
            }

            lightData.contactShadowIndex = -1;

            m_lightList.lights.Add(lightData);

            // Check if the current light is dominant and store it's index to change it's property later,
            // as we can't know which one will be dominant before checking all the lights
            GetDominantLightWithShadows(additionalShadowData, light, lightComponent, m_lightList.lights.Count -1);

            return true;
        }

        // TODO: we should be able to do this calculation only with LightData without VisibleLight light, but for now pass both
        public void GetLightVolumeDataAndBound(LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType,
            VisibleLight light, LightData lightData, Vector3 lightDimensions, Matrix4x4 worldToView,
            Camera.StereoscopicEye eyeIndex = Camera.StereoscopicEye.Left)
        {
            // Then Culling side
            var range = lightDimensions.z;
            var lightToWorld = light.localToWorld;
            Vector3 positionWS = lightData.positionRWS;
            Vector3 positionVS = worldToView.MultiplyPoint(positionWS);

            Matrix4x4 lightToView = worldToView * lightToWorld;
            Vector3   xAxisVS     = lightToView.GetColumn(0);
            Vector3   yAxisVS     = lightToView.GetColumn(1);
            Vector3   zAxisVS     = lightToView.GetColumn(2);

            // Fill bounds
            var bound = new SFiniteLightBound();
            var lightVolumeData = new LightVolumeData();

            lightVolumeData.lightCategory = (uint)lightCategory;
            lightVolumeData.lightVolume = (uint)lightVolumeType;

            if (gpuLightType == GPULightType.Spot || gpuLightType == GPULightType.ProjectorPyramid)
            {
                Vector3 lightDir = lightToWorld.GetColumn(2);

                // represents a left hand coordinate system in world space since det(worldToView)<0
                Vector3 vx = xAxisVS;
                Vector3 vy = yAxisVS;
                Vector3 vz = zAxisVS;

                const float pi = 3.1415926535897932384626433832795f;
                const float degToRad = (float)(pi / 180.0);

                var sa = light.spotAngle;
                var cs = Mathf.Cos(0.5f * sa * degToRad);
                var si = Mathf.Sin(0.5f * sa * degToRad);

                if (gpuLightType == GPULightType.ProjectorPyramid)
                {
                    Vector3 lightPosToProjWindowCorner = (0.5f * lightDimensions.x) * vx + (0.5f * lightDimensions.y) * vy + 1.0f * vz;
                    cs = Vector3.Dot(vz, Vector3.Normalize(lightPosToProjWindowCorner));
                    si = Mathf.Sqrt(1.0f - cs * cs);
                }

                const float FltMax = 3.402823466e+38F;
                var ta = cs > 0.0f ? (si / cs) : FltMax;
                var cota = si > 0.0f ? (cs / si) : FltMax;

                //const float cotasa = l.GetCotanHalfSpotAngle();

                // apply nonuniform scale to OBB of spot light
                var squeeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                var fS = squeeze ? ta : si;
                bound.center = worldToView.MultiplyPoint(positionWS + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                // scale axis to match box or base of pyramid
                bound.boxAxisX = (fS * range) * vx;
                bound.boxAxisY = (fS * range) * vy;
                bound.boxAxisZ = (0.5f * range) * vz;

                // generate bounding sphere radius
                var fAltDx = si;
                var fAltDy = cs;
                fAltDy = fAltDy - 0.5f;
                //if(fAltDy<0) fAltDy=-fAltDy;

                fAltDx *= range; fAltDy *= range;

                // Handle case of pyramid with this select (currently unused)
                var altDist = Mathf.Sqrt(fAltDy * fAltDy + (true ? 1.0f : 2.0f) * fAltDx * fAltDx);
                bound.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                bound.scaleXY = squeeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                lightVolumeData.lightAxisX = vx;
                lightVolumeData.lightAxisY = vy;
                lightVolumeData.lightAxisZ = vz;
                lightVolumeData.lightPos = positionVS;
                lightVolumeData.radiusSq = range * range;
                lightVolumeData.cotan = cota;
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else if (gpuLightType == GPULightType.Point)
            {
                Vector3 vx = xAxisVS;
                Vector3 vy = yAxisVS;
                Vector3 vz = zAxisVS;

                bound.center   = positionVS;
                bound.boxAxisX = vx * range;
                bound.boxAxisY = vy * range;
                bound.boxAxisZ = vz * range;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = range;

                // fill up ldata
                lightVolumeData.lightAxisX = vx;
                lightVolumeData.lightAxisY = vy;
                lightVolumeData.lightAxisZ = vz;
                lightVolumeData.lightPos = bound.center;
                lightVolumeData.radiusSq = range * range;
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else if (gpuLightType == GPULightType.Tube)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, 2 * range, 2 * range); // Omni-directional
                Vector3 extents = 0.5f * dimensions;

                bound.center = positionVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = extents.magnitude;

                lightVolumeData.lightPos = positionVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInnerDist = new Vector3(lightDimensions.x, 0, 0);
                lightVolumeData.boxInvRange.Set(1.0f / range, 1.0f / range, 1.0f / range);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.Rectangle)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, lightDimensions.y + 2 * range, range); // One-sided
                Vector3 extents = 0.5f * dimensions;
                Vector3 centerVS = positionVS + extents.z * zAxisVS;

                bound.center = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = extents.magnitude;

                lightVolumeData.lightPos     = centerVS;
                lightVolumeData.lightAxisX   = xAxisVS;
                lightVolumeData.lightAxisY   = yAxisVS;
                lightVolumeData.lightAxisZ   = zAxisVS;
                lightVolumeData.boxInnerDist = extents;
                lightVolumeData.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.ProjectorBox)
            {
                Vector3 dimensions  = new Vector3(lightDimensions.x, lightDimensions.y, range);  // One-sided
                Vector3 extents = 0.5f * dimensions;
                Vector3 centerVS = positionVS + extents.z * zAxisVS;

                bound.center   = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.radius   = extents.magnitude;
                bound.scaleXY.Set(1.0f, 1.0f);

                lightVolumeData.lightPos     = centerVS;
                lightVolumeData.lightAxisX   = xAxisVS;
                lightVolumeData.lightAxisY   = yAxisVS;
                lightVolumeData.lightAxisZ   = zAxisVS;
                lightVolumeData.boxInnerDist = extents;
                lightVolumeData.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else
            {
                Debug.Assert(false, "TODO: encountered an unknown GPULightType.");
            }

            if (eyeIndex == Camera.StereoscopicEye.Left)
            {
                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(lightVolumeData);
            }
            else
            {
                m_lightList.rightEyeBounds.Add(bound);
                m_lightList.rightEyeLightVolumes.Add(lightVolumeData);
            }
        }

        public bool GetEnvLightData(CommandBuffer cmd, HDCamera hdCamera, HDProbe probe, DebugDisplaySettings debugDisplaySettings)
        {
            Camera camera = hdCamera.camera;

            // For now we won't display real time probe when rendering one.
            // TODO: We may want to display last frame result but in this case we need to be careful not to update the atlas before all realtime probes are rendered (for frame coherency).
            // Unfortunately we don't have this information at the moment.
            if (probe.mode == ReflectionProbeMode.Realtime && camera.cameraType == CameraType.Reflection)
                return false;

            // Discard probe if disabled in debug menu
            if (!debugDisplaySettings.lightingDebugSettings.showReflectionProbe)
                return false;

            var capturePosition = Vector3.zero;
            var influenceToWorld = probe.influenceToWorld;

            // 31 bits index, 1 bit cache type
            var envIndex = -1;
            if (hdCamera.frameSettings.enableRealtimePlanarReflection && probe is PlanarReflectionProbe)
            {
                PlanarReflectionProbe planarProbe = probe as PlanarReflectionProbe;
                var fetchIndex = m_ReflectionPlanarProbeCache.FetchSlice(cmd, planarProbe.currentTexture);
                envIndex = (fetchIndex << 1) | (int)EnvCacheType.Texture2D;

                float nearClipPlane, farClipPlane, aspect, fov;
                Color backgroundColor;
                CameraClearFlags clearFlags;
                Quaternion captureRotation;
                Matrix4x4 worldToCamera, projection;

                ReflectionSystem.CalculateCaptureCameraProperties(
                    planarProbe,
                    out nearClipPlane, out farClipPlane,
                    out aspect, out fov, out clearFlags, out backgroundColor,
                    out worldToCamera, out projection, out capturePosition, out captureRotation,
                    camera);

                var gpuProj = GL.GetGPUProjectionMatrix(projection, true); // Had to change this from 'false'
                var gpuView = worldToCamera;

                // We transform it to object space by translating the capturePosition
                var vp = gpuProj * gpuView * Matrix4x4.Translate(capturePosition);
                m_Env2DCaptureVP[fetchIndex] = vp;
            }
            else if (probe is HDAdditionalReflectionData)
            {
                HDAdditionalReflectionData cubeProbe = probe as HDAdditionalReflectionData;
                envIndex = m_ReflectionProbeCache.FetchSlice(cmd, probe.currentTexture);
                envIndex = envIndex << 1 | (int)EnvCacheType.Cubemap;
                capturePosition = cubeProbe.capturePosition;
            }
            // -1 means that the texture is not ready yet (ie not convolved/compressed yet)
            if (envIndex == -1)
                return false;

            // Build light data
            var envLightData = new EnvLightData();

            InfluenceVolume influence = probe.influenceVolume;
            envLightData.lightLayers = probe.GetLightLayers();
            envLightData.influenceShapeType = influence.envShape;
            envLightData.weight = probe.weight;
            envLightData.multiplier = probe.multiplier * m_indirectLightingController.indirectSpecularIntensity;
            envLightData.influenceExtents = influence.extends;
            switch (influence.envShape)
            {
                case EnvShapeType.Box:
                    envLightData.blendNormalDistancePositive = influence.boxBlendNormalDistancePositive;
                    envLightData.blendNormalDistanceNegative = influence.boxBlendNormalDistanceNegative;
                    envLightData.blendDistancePositive = influence.boxBlendDistancePositive;
                    envLightData.blendDistanceNegative = influence.boxBlendDistanceNegative;
                    envLightData.boxSideFadePositive = influence.boxSideFadePositive;
                    envLightData.boxSideFadeNegative = influence.boxSideFadeNegative;
                    break;
                case EnvShapeType.Sphere:
                    envLightData.blendNormalDistancePositive.x = influence.sphereBlendNormalDistance;
                    envLightData.blendDistancePositive.x = influence.sphereBlendDistance;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown EnvShapeType");
            }

            envLightData.influenceRight = influenceToWorld.GetColumn(0).normalized;
            envLightData.influenceUp = influenceToWorld.GetColumn(1).normalized;
            envLightData.influenceForward = influenceToWorld.GetColumn(2).normalized;
            envLightData.capturePositionRWS = capturePosition;
            envLightData.influencePositionRWS = influenceToWorld.GetColumn(3);

            envLightData.envIndex = envIndex;

            // Proxy data
            var proxyToWorld = probe.proxyToWorld;
            envLightData.proxyExtents = probe.proxyExtents;
            envLightData.minProjectionDistance = probe.infiniteProjection ? 65504f : 0;
            envLightData.proxyRight = proxyToWorld.GetColumn(0).normalized;
            envLightData.proxyUp = proxyToWorld.GetColumn(1).normalized;
            envLightData.proxyForward = proxyToWorld.GetColumn(2).normalized;
            envLightData.proxyPositionRWS = proxyToWorld.GetColumn(3);

            m_lightList.envLights.Add(envLightData);
            return true;
        }

        public void GetEnvLightVolumeDataAndBound(HDProbe probe, LightVolumeType lightVolumeType, Matrix4x4 worldToView, Camera.StereoscopicEye eyeIndex = Camera.StereoscopicEye.Left)
        {
            var bound = new SFiniteLightBound();
            var lightVolumeData = new LightVolumeData();

            // C is reflection volume center in world space (NOT same as cube map capture point)
            var influenceExtents = probe.influenceExtents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);

            var influenceToWorld = probe.influenceToWorld;

            // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
            var influenceRightVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(0).normalized);
            var influenceUpVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(1).normalized);
            var influenceForwardVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(2).normalized);
            var influencePositionVS = worldToView.MultiplyPoint(influenceToWorld.GetColumn(3));

            lightVolumeData.lightCategory = (uint)LightCategory.Env;
            lightVolumeData.lightVolume = (uint)lightVolumeType;
            lightVolumeData.featureFlags = (uint)LightFeatureFlags.Env;

            switch (lightVolumeType)
            {
                case LightVolumeType.Sphere:
                {
                    lightVolumeData.lightPos = influencePositionVS;
                    lightVolumeData.radiusSq = influenceExtents.x * influenceExtents.x;
                    lightVolumeData.lightAxisX = influenceRightVS;
                    lightVolumeData.lightAxisY = influenceUpVS;
                    lightVolumeData.lightAxisZ = influenceForwardVS;

                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceRightVS * influenceExtents.x;
                    bound.boxAxisY = influenceUpVS * influenceExtents.x;
                    bound.boxAxisZ = influenceForwardVS * influenceExtents.x;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = influenceExtents.x;
                    break;
                }
                case LightVolumeType.Box:
                {
                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceExtents.x * influenceRightVS;
                    bound.boxAxisY = influenceExtents.y * influenceUpVS;
                    bound.boxAxisZ = influenceExtents.z * influenceForwardVS;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = influenceExtents.magnitude;

                    // The culling system culls pixels that are further
                    //   than a threshold to the box influence extents.
                    // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
                    lightVolumeData.lightPos = influencePositionVS;
                    lightVolumeData.lightAxisX = influenceRightVS;
                    lightVolumeData.lightAxisY = influenceUpVS;
                    lightVolumeData.lightAxisZ = influenceForwardVS;
                    lightVolumeData.boxInnerDist = influenceExtents - k_BoxCullingExtentThreshold;
                    lightVolumeData.boxInvRange.Set(1.0f / k_BoxCullingExtentThreshold.x, 1.0f / k_BoxCullingExtentThreshold.y, 1.0f / k_BoxCullingExtentThreshold.z);
                    break;
                }
            }

            if (eyeIndex == Camera.StereoscopicEye.Left)
            {
                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(lightVolumeData);
            }
            else
            {
                m_lightList.rightEyeBounds.Add(bound);
                m_lightList.rightEyeLightVolumes.Add(lightVolumeData);
            }
        }

        public void AddBoxVolumeDataAndBound(OrientedBBox obb, LightCategory category, LightFeatureFlags featureFlags, Matrix4x4 worldToView)
        {
            var bound      = new SFiniteLightBound();
            var volumeData = new LightVolumeData();

            // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
            var positionVS = worldToView.MultiplyPoint(obb.center);
            var rightVS    = worldToView.MultiplyVector(obb.right);
            var upVS       = worldToView.MultiplyVector(obb.up);
            var forwardVS  = Vector3.Cross(upVS, rightVS);
            var extents    = new Vector3(obb.extentX, obb.extentY, obb.extentZ);

            volumeData.lightVolume   = (uint)LightVolumeType.Box;
            volumeData.lightCategory = (uint)category;
            volumeData.featureFlags  = (uint)featureFlags;

            bound.center   = positionVS;
            bound.boxAxisX = obb.extentX * rightVS;
            bound.boxAxisY = obb.extentY * upVS;
            bound.boxAxisZ = obb.extentZ * forwardVS;
            bound.radius   = extents.magnitude;
            bound.scaleXY.Set(1.0f, 1.0f);

            // The culling system culls pixels that are further
            //   than a threshold to the box influence extents.
            // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
            volumeData.lightPos     = positionVS;
            volumeData.lightAxisX   = rightVS;
            volumeData.lightAxisY   = upVS;
            volumeData.lightAxisZ   = forwardVS;
            volumeData.boxInnerDist = extents - k_BoxCullingExtentThreshold; // We have no blend range, but the culling code needs a small EPS value for some reason???
            volumeData.boxInvRange.Set(1.0f / k_BoxCullingExtentThreshold.x, 1.0f / k_BoxCullingExtentThreshold.y, 1.0f / k_BoxCullingExtentThreshold.z);

            m_lightList.bounds.Add(bound);
            m_lightList.lightVolumes.Add(volumeData);
        }

        public int GetCurrentShadowCount()
        {
            return m_ShadowManager.GetShadowRequestCount();
        }

        public void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams)
        {
            m_ShadowManager.UpdateCullingParameters(ref cullingParams);

            // In HDRP we don't need per object light/probe info so we disable the native code that handles it.
            cullingParams.cullingFlags |= CullFlag.DisablePerObjectCulling;
        }

        public bool IsBakedShadowMaskLight(Light light)
        {
            // This can happen for particle lights.
            if (light == null)
                return false;

            return light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask &&
                light.bakingOutput.occlusionMaskChannel != -1;     // We need to have an occlusion mask channel assign, else we have no shadow mask
        }

        HDProbe SelectProbe(VisibleReflectionProbe probe, PlanarReflectionProbe planarProbe)
        {
            if (probe.probe != null)
            {
                var add = probe.probe.GetComponent<HDAdditionalReflectionData>();
                if (add == null)
                {
                    add = HDUtils.s_DefaultHDAdditionalReflectionData;
                    if (add.influenceVolume == null)
                    {
                        add.Awake(); // We need to init the 'default' data if it isn't
                    }
                    Vector3 distance = Vector3.one * probe.blendDistance;
                    add.influenceVolume.boxBlendDistancePositive = distance;
                    add.influenceVolume.boxBlendDistanceNegative = distance;
                    add.influenceVolume.shape = InfluenceShape.Box;
                }
                return add;
            }
            if (planarProbe != null)
                return planarProbe;

            throw new ArgumentException();
        }

        // Return true if BakedShadowMask are enabled
        public bool PrepareLightsForGPU(CommandBuffer cmd, HDCamera hdCamera, CullResults cullResults,
            ReflectionProbeCullResults reflectionProbeCullResults, DensityVolumeList densityVolumes, DebugDisplaySettings debugDisplaySettings)
        {
            using (new ProfilingSample(cmd, "Prepare Lights For GPU"))
            {
                Camera camera = hdCamera.camera;

                // If any light require it, we need to enabled bake shadow mask feature
                m_enableBakeShadowMask = false;

                m_lightList.Clear();

                // We need to properly reset this here otherwise if we go from 1 light to no visible light we would keep the old reference active.
                m_CurrentSunLight = null;
                m_CurrentShadowSortedSunLightIndex = -1;
                m_DominantLightIndex = -1;
                m_DominantLightValue = 0;
                m_DebugSelectedLightShadowIndex = -1;

                int decalDatasCount = Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen);

                var stereoEnabled = hdCamera.camera.stereoEnabled;

                var hdShadowSettings = VolumeManager.instance.stack.GetComponent<HDShadowSettings>();

                Vector3 camPosWS = camera.transform.position;

                var worldToView = WorldToCamera(camera);
                var rightEyeWorldToView = Matrix4x4.identity;
                if (stereoEnabled)
                {
                    worldToView = WorldToViewStereo(camera, Camera.StereoscopicEye.Left);
                    rightEyeWorldToView = WorldToViewStereo(camera, Camera.StereoscopicEye.Right);
                }

                // We must clear the shadow requests before checking if they are any visible light because we would have requests from the last frame executed in the case where we don't see any lights
                m_ShadowManager.Clear();

                // Note: Light with null intensity/Color are culled by the C++, no need to test it here
                if (cullResults.visibleLights.Count != 0 || cullResults.visibleReflectionProbes.Count != 0)
                {
                    // 1. Count the number of lights and sort all lights by category, type and volume - This is required for the fptl/cluster shader code
                    // If we reach maximum of lights available on screen, then we discard the light.
                    // Lights are processed in order, so we don't discards light based on their importance but based on their ordering in visible lights list.
                    int directionalLightcount = 0;
                    int punctualLightcount = 0;
                    int areaLightCount = 0;

                    int lightCount = Math.Min(cullResults.visibleLights.Count, m_MaxLightsOnScreen);
                    UpdateSortKeysArray(lightCount);
                    int sortCount = 0;
                    for (int lightIndex = 0, numLights = cullResults.visibleLights.Count; (lightIndex < numLights) && (sortCount < lightCount); ++lightIndex)
                    {
                        var light = cullResults.visibleLights[lightIndex];
                        var lightComponent = light.light;

                        // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
                        var additionalData = GetHDAdditionalLightData(lightComponent);

                        // Reserve shadow map resolutions and check if light needs to render shadows
                        additionalData.ReserveShadows(camera, m_ShadowManager, m_ShadowInitParameters, cullResults, m_FrameSettings, lightIndex);

                        LightCategory lightCategory = LightCategory.Count;
                        GPULightType gpuLightType = GPULightType.Point;
                        LightVolumeType lightVolumeType = LightVolumeType.Count;

                        if (additionalData.lightTypeExtent == LightTypeExtent.Punctual)
                        {
                            lightCategory = LightCategory.Punctual;

                            switch (light.lightType)
                            {
                                case LightType.Spot:
                                    if (punctualLightcount >= m_MaxPunctualLightsOnScreen)
                                        continue;
                                    switch (additionalData.spotLightShape)
                                    {
                                        case SpotLightShape.Cone:
                                            gpuLightType = GPULightType.Spot;
                                            lightVolumeType = LightVolumeType.Cone;
                                            break;
                                        case SpotLightShape.Pyramid:
                                            gpuLightType = GPULightType.ProjectorPyramid;
                                            lightVolumeType = LightVolumeType.Cone;
                                            break;
                                        case SpotLightShape.Box:
                                            gpuLightType = GPULightType.ProjectorBox;
                                            lightVolumeType = LightVolumeType.Box;
                                            break;
                                        default:
                                            Debug.Assert(false, "Encountered an unknown SpotLightShape.");
                                            break;
                                    }
                                    break;

                                case LightType.Directional:
                                    if (directionalLightcount >= m_MaxDirectionalLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Directional;
                                    // No need to add volume, always visible
                                    lightVolumeType = LightVolumeType.Count; // Count is none
                                    break;

                                case LightType.Point:
                                    if (punctualLightcount >= m_MaxPunctualLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Point;
                                    lightVolumeType = LightVolumeType.Sphere;
                                    break;

                                default:
                                    Debug.Assert(false, "Encountered an unknown LightType.");
                                    break;
                            }
                        }
                        else
                        {
                            lightCategory = LightCategory.Area;

                            switch (additionalData.lightTypeExtent)
                            {
                                case LightTypeExtent.Rectangle:
                                    if (areaLightCount >= m_MaxAreaLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Rectangle;
                                    lightVolumeType = LightVolumeType.Box;
                                    break;

                                case LightTypeExtent.Tube:
                                    if (areaLightCount >= m_MaxAreaLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Tube;
                                    lightVolumeType = LightVolumeType.Box;
                                    break;

                                default:
                                    Debug.Assert(false, "Encountered an unknown LightType.");
                                    break;
                            }
                        }

                        // 5 bit (0x1F) light category, 5 bit (0x1F) GPULightType, 5 bit (0x1F) lightVolume, 1 bit for shadow casting, 16 bit index
                        m_SortKeys[sortCount++] = (uint)lightCategory << 27 | (uint)gpuLightType << 22 | (uint)lightVolumeType << 17 | (uint)lightIndex;
                    }

                    CoreUnsafeUtils.QuickSort(m_SortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    // Now that all the lights have requested a shadow resolution, we can layout them in the atlas
                    // And if needed rescale the whole atlas
                    m_ShadowManager.LayoutShadowMaps(debugDisplaySettings.lightingDebugSettings);

                    // TODO: Refactor shadow management
                    // The good way of managing shadow:
                    // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
                    // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
                    // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
                    // will be use...)
                    // The lightLoop is in charge, not the shadow pass.
                    // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet

                    // 2. Go through all lights, convert them to GPU format.
                    // Simultaneously create data for culling (LightVolumeData and SFiniteLightBound)

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = m_SortKeys[sortIndex];
                        LightCategory lightCategory = (LightCategory)((sortKey >> 27) & 0x1F);
                        GPULightType gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
                        LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x1F);
                        int lightIndex = (int)(sortKey & 0xFFFF);

                        var light = cullResults.visibleLights[lightIndex];
                        var lightComponent = light.light;

                        m_enableBakeShadowMask = m_enableBakeShadowMask || IsBakedShadowMaskLight(lightComponent);

                        // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
                        var additionalLightData = GetHDAdditionalLightData(lightComponent);
                        var additionalShadowData = lightComponent != null ? lightComponent.GetComponent<AdditionalShadowData>() : null; // Can be null

                        int shadowIndex = -1;
                        // Manage shadow requests
                        if (additionalLightData.WillRenderShadows())
                        {
                            int shadowRequestCount;
                            shadowIndex = additionalLightData.UpdateShadowRequest(hdCamera, m_ShadowManager, light, cullResults, lightIndex, out shadowRequestCount);

#if UNITY_EDITOR
                            if ((debugDisplaySettings.lightingDebugSettings.shadowDebugUseSelection
                                    || debugDisplaySettings.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                                && UnityEditor.Selection.activeGameObject == lightComponent.gameObject)
                            {
                                m_DebugSelectedLightShadowIndex = shadowIndex;
                                m_DebugSelectedLightShadowCount = shadowRequestCount;
                            }
#endif
                        }

                        // Directional rendering side, it is separated as it is always visible so no volume to handle here
                        if (gpuLightType == GPULightType.Directional)
                        {
                            if (GetDirectionalLightData(cmd, gpuLightType, light, lightComponent, additionalLightData, additionalShadowData, lightIndex, shadowIndex, debugDisplaySettings, directionalLightcount))
                            {
                                directionalLightcount++;

                                // We make the light position camera-relative as late as possible in order
                                // to allow the preceding code to work with the absolute world space coordinates.
                                if (ShaderConfig.s_CameraRelativeRendering != 0)
                                {
                                    // Caution: 'DirectionalLightData.positionWS' is camera-relative after this point.
                                    int last = m_lightList.directionalLights.Count - 1;
                                    DirectionalLightData lightData = m_lightList.directionalLights[last];
                                    lightData.positionRWS -= camPosWS;
                                    m_lightList.directionalLights[last] = lightData;
                                }
                            }
                            continue;
                        }

                        Vector3 lightDimensions = new Vector3(); // X = length or width, Y = height, Z = range (depth)

                        // Punctual, area, projector lights - the rendering side.
                        if (GetLightData(cmd, hdShadowSettings, camera, gpuLightType, light, lightComponent, additionalLightData, additionalShadowData, lightIndex, shadowIndex, ref lightDimensions, debugDisplaySettings))
                        {
                            switch (lightCategory)
                            {
                                case LightCategory.Punctual:
                                    punctualLightcount++;
                                    break;
                                case LightCategory.Area:
                                    areaLightCount++;
                                    break;
                                default:
                                    Debug.Assert(false, "TODO: encountered an unknown LightCategory.");
                                    break;
                            }

                            // Then culling side. Must be call in this order as we pass the created Light data to the function
                            GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], lightDimensions, worldToView);
                            if (stereoEnabled)
                                GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], lightDimensions, rightEyeWorldToView, Camera.StereoscopicEye.Right);

                            // We make the light position camera-relative as late as possible in order
                            // to allow the preceding code to work with the absolute world space coordinates.
                            if (ShaderConfig.s_CameraRelativeRendering != 0)
                            {
                                // Caution: 'LightData.positionWS' is camera-relative after this point.
                                int last = m_lightList.lights.Count - 1;
                                LightData lightData = m_lightList.lights[last];
                                lightData.positionRWS -= camPosWS;
                                m_lightList.lights[last] = lightData;
                            }
                        }
                    }

                    // Update the compute buffer with the shadow request datas
                    m_ShadowManager.PrepareGPUShadowDatas(cullResults, camera);

                    //Activate contact shadows on dominant light
                    if (m_DominantLightIndex != -1)
                    {
                        m_DominantLightData =  m_lightList.lights[m_DominantLightIndex];
                        m_DominantLightData.contactShadowIndex = 0;
                        m_lightList.lights[m_DominantLightIndex] = m_DominantLightData;
                    }

                    // Sanity check
                    Debug.Assert(m_lightList.directionalLights.Count == directionalLightcount);
                    Debug.Assert(m_lightList.lights.Count == areaLightCount + punctualLightcount);

                    m_punctualLightCount = punctualLightcount;
                    m_areaLightCount = areaLightCount;

                    // Redo everything but this time with envLights
                    Debug.Assert(m_MaxEnvLightsOnScreen <= 256); //for key construction
                    int envLightCount = 0;

                    var totalProbes = cullResults.visibleReflectionProbes.Count + reflectionProbeCullResults.visiblePlanarReflectionProbeCount;
                    int probeCount = Math.Min(totalProbes, m_MaxEnvLightsOnScreen);
                    UpdateSortKeysArray(probeCount);
                    sortCount = 0;

                    for (int probeIndex = 0, numProbes = totalProbes; (probeIndex < numProbes) && (sortCount < probeCount); probeIndex++)
                    {
                        if (probeIndex < cullResults.visibleReflectionProbes.Count)
                        {
                            VisibleReflectionProbe probe = cullResults.visibleReflectionProbes[probeIndex];
                            HDAdditionalReflectionData additional = probe.probe.GetComponent<HDAdditionalReflectionData>();

                            // probe.texture can be null when we are adding a reflection probe in the editor
                            if (probe.texture == null || envLightCount >= m_MaxEnvLightsOnScreen)
                                continue;

                            // Work around the culling issues. TODO: fix culling in C++.
                            if (probe.probe == null || !probe.probe.isActiveAndEnabled)
                                continue;

                            // Work around the data issues.
                            if (probe.localToWorld.determinant == 0)
                            {
                                Debug.LogError("Reflection probe " + probe.probe.name + " has an invalid local frame and needs to be fixed.");
                                continue;
                            }

                            LightVolumeType lightVolumeType = LightVolumeType.Box;
                            if (additional != null && additional.influenceVolume.shape == InfluenceShape.Sphere)
                                lightVolumeType = LightVolumeType.Sphere;
                            ++envLightCount;

                            var logVolume = CalculateProbeLogVolume(probe.bounds);

                            m_SortKeys[sortCount++] = PackProbeKey(logVolume, lightVolumeType, 0u, probeIndex); // Sort by volume
                        }
                        else
                        {
                            var planarProbeIndex = probeIndex - cullResults.visibleReflectionProbes.Count;
                            var probe = reflectionProbeCullResults.visiblePlanarReflectionProbes[planarProbeIndex];

                            // probe.texture can be null when we are adding a reflection probe in the editor
                            if (probe.currentTexture == null || envLightCount >= m_MaxEnvLightsOnScreen)
                                continue;

                            var lightVolumeType = LightVolumeType.Box;
                            if (probe.influenceVolume.shape == InfluenceShape.Sphere)
                                lightVolumeType = LightVolumeType.Sphere;
                            ++envLightCount;

                            var logVolume = CalculateProbeLogVolume(probe.bounds);

                            m_SortKeys[sortCount++] = PackProbeKey(logVolume, lightVolumeType, 1u, planarProbeIndex); // Sort by volume
                        }
                    }

                    // Not necessary yet but call it for future modification with sphere influence volume
                    CoreUnsafeUtils.QuickSort(m_SortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = m_SortKeys[sortIndex];
                        LightVolumeType lightVolumeType;
                        int probeIndex;
                        int listType;
                        UnpackProbeSortKey(sortKey, out lightVolumeType, out probeIndex, out listType);

                        PlanarReflectionProbe planarProbe = null;
                        VisibleReflectionProbe probe = default(VisibleReflectionProbe);
                        if (listType == 0)
                            probe = cullResults.visibleReflectionProbes[probeIndex];
                        else
                            planarProbe = reflectionProbeCullResults.visiblePlanarReflectionProbes[probeIndex];

                        var probeWrapper = SelectProbe(probe, planarProbe);

                        if (GetEnvLightData(cmd, hdCamera, probeWrapper, debugDisplaySettings))
                        {
                            GetEnvLightVolumeDataAndBound(probeWrapper, lightVolumeType, worldToView);
                            if (stereoEnabled)
                                GetEnvLightVolumeDataAndBound(probeWrapper, lightVolumeType, rightEyeWorldToView, Camera.StereoscopicEye.Right);

                            // We make the light position camera-relative as late as possible in order
                            // to allow the preceding code to work with the absolute world space coordinates.
                            if (ShaderConfig.s_CameraRelativeRendering != 0)
                            {
                                // Caution: 'EnvLightData.positionRWS' is camera-relative after this point.
                                int last = m_lightList.envLights.Count - 1;
                                EnvLightData envLightData = m_lightList.envLights[last];
                                envLightData.capturePositionRWS -= camPosWS;
                                envLightData.influencePositionRWS -= camPosWS;
                                envLightData.proxyPositionRWS -= camPosWS;
                                m_lightList.envLights[last] = envLightData;
                            }
                        }
                    }
                }

                if (decalDatasCount > 0)
                {
                    for (int i = 0; i < decalDatasCount; i++)
                    {
                        m_lightList.bounds.Add(DecalSystem.m_Bounds[i]);
                        m_lightList.lightVolumes.Add(DecalSystem.m_LightVolumes[i]);
                    }
                }

                // Inject density volumes into the clustered data structure for efficient look up.
                m_densityVolumeCount = densityVolumes.bounds != null ? densityVolumes.bounds.Count : 0;

                Matrix4x4 worldToViewCR = worldToView;

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    // The OBBs are camera-relative, the matrix is not. Fix it.
                    worldToViewCR.SetColumn(3, new Vector4(0, 0, 0, 1));
                }

                for (int i = 0, n = m_densityVolumeCount; i < n; i++)
                {
                    // Density volumes are not lights and therefore should not affect light classification.
                    LightFeatureFlags featureFlags = 0;
                    AddBoxVolumeDataAndBound(densityVolumes.bounds[i], LightCategory.DensityVolume, featureFlags, worldToViewCR);
                }

                m_lightCount = m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount + m_densityVolumeCount;
                Debug.Assert(m_lightCount == m_lightList.bounds.Count);
                Debug.Assert(m_lightCount == m_lightList.lightVolumes.Count);

                if (stereoEnabled)
                {
                    // TODO: Proper decal + stereo cull management

                    Debug.Assert(m_lightList.rightEyeBounds.Count == m_lightCount);
                    Debug.Assert(m_lightList.rightEyeLightVolumes.Count == m_lightCount);

                    // TODO: GC considerations?
                    m_lightList.bounds.AddRange(m_lightList.rightEyeBounds);
                    m_lightList.lightVolumes.AddRange(m_lightList.rightEyeLightVolumes);
                }

                UpdateDataBuffers();

                cmd.SetGlobalInt(HDShaderIDs._EnvLightIndexShift, m_lightList.lights.Count);
                cmd.SetGlobalInt(HDShaderIDs._DecalIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count);
                cmd.SetGlobalInt(HDShaderIDs._DensityVolumeIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount);
            }

            m_enableBakeShadowMask = m_enableBakeShadowMask && hdCamera.frameSettings.enableShadowMask;

            // We push this parameter here because we know that normal/deferred shadows are not yet rendered
            if (debugDisplaySettings.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
            {
                int shadowIndex = (int)debugDisplaySettings.lightingDebugSettings.shadowMapIndex;

                if (debugDisplaySettings.lightingDebugSettings.shadowDebugUseSelection)
                    shadowIndex = m_DebugSelectedLightShadowIndex;
                cmd.SetGlobalInt(HDShaderIDs._DebugSingleShadowIndex, shadowIndex);
            }

            return m_enableBakeShadowMask;
        }

        static float CalculateProbeLogVolume(Bounds bounds)
        {
            //Notes:
            // - 1+ term is to prevent having negative values in the log result
            // - 1000* is too keep 3 digit after the dot while we truncate the result later
            // - 1048575 is 2^20-1 as we pack the result on 20bit later
            float boxVolume = 8f* bounds.extents.x * bounds.extents.y * bounds.extents.z;
            float logVolume = Mathf.Clamp(Mathf.Log(1 + boxVolume, 1.05f)*1000, 0, 1048575);
            return logVolume;
        }

        static void UnpackProbeSortKey(uint sortKey, out LightVolumeType lightVolumeType, out int probeIndex, out int listType)
        {
            lightVolumeType = (LightVolumeType)((sortKey >> 9) & 0x3);
            probeIndex = (int)(sortKey & 0xFF);
            listType = (int)((sortKey >> 8) & 1);
        }

        static uint PackProbeKey(float logVolume, LightVolumeType lightVolumeType, uint listType, int probeIndex)
        {
            // 20 bit volume, 3 bit LightVolumeType, 1 bit list type, 8 bit index
            return (uint)logVolume << 12 | (uint)lightVolumeType << 9 | listType << 8 | ((uint)probeIndex & 0xFF);
        }

        void VoxelLightListGeneration(CommandBuffer cmd, HDCamera hdCamera, Matrix4x4[] projscrArr, Matrix4x4[] invProjscrArr, RenderTargetIdentifier cameraDepthBufferRT)
        {
            Camera camera = hdCamera.camera;

            var isProjectionOblique = GeometryUtils.IsProjectionMatrixOblique(camera.projectionMatrix);

            // clear atomic offset index
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, s_GlobalLightListAtomic);
            cmd.DispatchCompute(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

            bool isOrthographic = camera.orthographic;
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);
            cmd.SetComputeMatrixArrayParam(buildPerVoxelLightListShader, HDShaderIDs.g_mScrProjectionArr, projscrArr);
            cmd.SetComputeMatrixArrayParam(buildPerVoxelLightListShader, HDShaderIDs.g_mInvScrProjectionArr, invProjscrArr);

            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_iLog2NumClusters, k_Log2NumClusters);

            cmd.SetComputeVectorParam(buildPerVoxelLightListShader, HDShaderIDs.g_screenSize, hdCamera.screenSize);
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_iNumSamplesMSAA, (int)hdCamera.msaaSamples);

            //Vector4 v2_near = invProjscr * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            //Vector4 v2_far = invProjscr * new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            //float nearPlane2 = -(v2_near.z/v2_near.w);
            //float farPlane2 = -(v2_far.z/v2_far.w);
            var nearPlane = camera.nearClipPlane;
            var farPlane = camera.farClipPlane;
            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fNearPlane, nearPlane);
            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fFarPlane, farPlane);

            const float C = (float)(1 << k_Log2NumClusters);
            var geomSeries = (1.0 - Mathf.Pow(k_ClustLogBase, C)) / (1 - k_ClustLogBase);        // geometric series: sum_k=0^{C-1} base^k
            m_ClustScale = (float)(geomSeries / (farPlane - nearPlane));

            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fClustScale, m_ClustScale);
            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fClustBase, k_ClustLogBase);

            var genListPerVoxelKernel = isProjectionOblique ? s_GenListPerVoxelKernelOblique : s_GenListPerVoxelKernel;

            cmd.SetComputeTextureParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_depth_tex, cameraDepthBufferRT);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_vLayeredLightList, s_PerVoxelLightLists);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_LayeredOffset, s_PerVoxelOffset);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, s_GlobalLightListAtomic);
            if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_vBigTileLightList, s_BigTileLightList);

            if (k_UseDepthBuffer)
            {
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_logBaseBuffer, s_PerTileLogBaseTweak);
            }

            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs._LightVolumeData, s_LightVolumeDataBuffer);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, genListPerVoxelKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);

            var numTilesX = GetNumTileClusteredX(hdCamera);
            var numTilesY = GetNumTileClusteredY(hdCamera);

            cmd.DispatchCompute(buildPerVoxelLightListShader, genListPerVoxelKernel, numTilesX, numTilesY, (int)hdCamera.numEyes);
        }

        public void BuildGPULightListsCommon(HDCamera hdCamera, CommandBuffer cmd, RenderTargetIdentifier cameraDepthBufferRT, RenderTargetIdentifier stencilTextureRT, bool skyEnabled)
        {
            var camera = hdCamera.camera;
            cmd.BeginSample("Build Light List");

            var w = (int)hdCamera.screenSize.x;
            var h = (int)hdCamera.screenSize.y;
            s_TempScreenDimArray[0] = w;
            s_TempScreenDimArray[1] = h;
            var numBigTilesX = (w + 63) / 64;
            var numBigTilesY = (h + 63) / 64;

            var temp = new Matrix4x4();
            temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
            temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            bool isOrthographic = camera.orthographic;

            // camera to screen matrix (and it's inverse)
            if (camera.stereoEnabled)
            {
                // XRTODO: If possible, we could generate a non-oblique stereo projection
                // matrix.  It's ok if it's not the exact same matrix, as long as it encompasses
                // the same FOV as the original projection matrix (which would mean padding each half
                // of the frustum with the max half-angle). We don't need the light information in
                // real projection space.  We just use screen space to figure out what is proximal
                // to a cluster or tile.
                // Once we generate this non-oblique projection matrix, it can be shared across both eyes (un-array)
                for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                {
                    m_LightListProjMatrices[eyeIndex] = CameraProjectionStereoLHS(hdCamera.camera, (Camera.StereoscopicEye)eyeIndex);
                    m_LightListProjscrMatrices[eyeIndex] = temp * m_LightListProjMatrices[eyeIndex];
                    m_LightListInvProjscrMatrices[eyeIndex] = m_LightListProjscrMatrices[eyeIndex].inverse;

                }
            }
            else
            {
                m_LightListProjMatrices[0] = GeometryUtils.GetProjectionMatrixLHS(hdCamera.camera);
                m_LightListProjscrMatrices[0] = temp * m_LightListProjMatrices[0];
                m_LightListInvProjscrMatrices[0] = m_LightListProjscrMatrices[0].inverse;
            }
            var isProjectionOblique = GeometryUtils.IsProjectionMatrixOblique(m_LightListProjMatrices[0]);

            // generate screen-space AABBs (used for both fptl and clustered).
            if (m_lightCount != 0)
            {
                temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                if (camera.stereoEnabled)
                {
                    for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                    {
                        m_LightListProjHMatrices[eyeIndex] = temp * m_LightListProjMatrices[eyeIndex];
                        m_LightListInvProjHMatrices[eyeIndex] = m_LightListProjHMatrices[eyeIndex].inverse;
                    }
                }
                else
                {
                    m_LightListProjHMatrices[0] = temp * m_LightListProjMatrices[0];
                    m_LightListInvProjHMatrices[0] = m_LightListProjHMatrices[0].inverse;
                }

                var genAABBKernel = isProjectionOblique ? s_GenAABBKernel_Oblique : s_GenAABBKernel;

                cmd.SetComputeIntParam(buildScreenAABBShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);

                // In the stereo case, we have two sets of light bounds to iterate over (bounds are in per-eye view space)
                cmd.SetComputeIntParam(buildScreenAABBShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);
                cmd.SetComputeBufferParam(buildScreenAABBShader, genAABBKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);

                cmd.SetComputeMatrixArrayParam(buildScreenAABBShader, HDShaderIDs.g_mProjectionArr, m_LightListProjHMatrices);
                cmd.SetComputeMatrixArrayParam(buildScreenAABBShader, HDShaderIDs.g_mInvProjectionArr, m_LightListInvProjHMatrices);

                // In stereo, we output two sets of AABB bounds
                cmd.SetComputeBufferParam(buildScreenAABBShader, genAABBKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);

                int tgY = (int)hdCamera.numEyes;
                cmd.DispatchCompute(buildScreenAABBShader, genAABBKernel, (m_lightCount + 7) / 8, tgY, 1);
            }

            // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
            if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
            {
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(buildPerBigTileLightListShader, HDShaderIDs.g_viDimensions, s_TempScreenDimArray);

                // TODO: These two aren't actually used...
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs._EnvLightIndexShift, m_lightList.lights.Count);
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs._DecalIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count);

                cmd.SetComputeMatrixArrayParam(buildPerBigTileLightListShader, HDShaderIDs.g_mScrProjectionArr, m_LightListProjscrMatrices);
                cmd.SetComputeMatrixArrayParam(buildPerBigTileLightListShader, HDShaderIDs.g_mInvScrProjectionArr, m_LightListInvProjscrMatrices);

                cmd.SetComputeFloatParam(buildPerBigTileLightListShader, HDShaderIDs.g_fNearPlane, camera.nearClipPlane);
                cmd.SetComputeFloatParam(buildPerBigTileLightListShader, HDShaderIDs.g_fFarPlane, camera.farClipPlane);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs.g_vLightList, s_BigTileLightList);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs._LightVolumeData, s_LightVolumeDataBuffer);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);

                int tgZ = (int)hdCamera.numEyes;
                cmd.DispatchCompute(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, numBigTilesX, numBigTilesY, tgZ);
            }

            var numTilesX = GetNumTileFtplX(hdCamera);
            var numTilesY = GetNumTileFtplY(hdCamera);
            var numTiles = numTilesX * numTilesY;
            bool enableFeatureVariants = GetFeatureVariantsEnabled();

            // optimized for opaques only
            if (m_FrameSettings.lightLoopSettings.isFptlEnabled)
            {
                var genListPerTileKernel = isProjectionOblique ? s_GenListPerTileKernel_Oblique : s_GenListPerTileKernel;

                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(buildPerTileLightListShader, HDShaderIDs.g_viDimensions, s_TempScreenDimArray);
                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs._EnvLightIndexShift, m_lightList.lights.Count);
                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs._DecalIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count);
                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);

                cmd.SetComputeBufferParam(buildPerTileLightListShader, genListPerTileKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);
                cmd.SetComputeBufferParam(buildPerTileLightListShader, genListPerTileKernel, HDShaderIDs._LightVolumeData, s_LightVolumeDataBuffer);
                cmd.SetComputeBufferParam(buildPerTileLightListShader, genListPerTileKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);

                cmd.SetComputeMatrixParam(buildPerTileLightListShader, HDShaderIDs.g_mScrProjection, m_LightListProjscrMatrices[0]);
                cmd.SetComputeMatrixParam(buildPerTileLightListShader, HDShaderIDs.g_mInvScrProjection, m_LightListInvProjscrMatrices[0]);

                cmd.SetComputeTextureParam(buildPerTileLightListShader, genListPerTileKernel, HDShaderIDs.g_depth_tex, cameraDepthBufferRT);
                cmd.SetComputeBufferParam(buildPerTileLightListShader, genListPerTileKernel, HDShaderIDs.g_vLightList, s_LightList);
                if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, genListPerTileKernel, HDShaderIDs.g_vBigTileLightList, s_BigTileLightList);

                if (enableFeatureVariants)
                {
                    uint baseFeatureFlags = 0;
                    if (m_lightList.directionalLights.Count > 0)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Directional;
                    }
                    if (skyEnabled)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Sky;
                    }
                    if (!m_FrameSettings.lightLoopSettings.enableComputeMaterialVariants)
                    {
                        baseFeatureFlags |= LightDefinitions.s_MaterialFeatureMaskFlags;
                    }
                    cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs.g_BaseFeatureFlags, (int)baseFeatureFlags);
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, genListPerTileKernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);
                }

                cmd.DispatchCompute(buildPerTileLightListShader, genListPerTileKernel, numTilesX, numTilesY, 1);
            }

            // Cluster
            VoxelLightListGeneration(cmd, hdCamera, m_LightListProjscrMatrices, m_LightListInvProjscrMatrices, cameraDepthBufferRT);

            if (enableFeatureVariants)
            {
                // material classification
                if (m_FrameSettings.lightLoopSettings.enableComputeMaterialVariants)
                {
                    int buildMaterialFlagsKernel = s_BuildMaterialFlagsOrKernel;

                    uint baseFeatureFlags = 0;
                    if (!m_FrameSettings.lightLoopSettings.enableComputeLightVariants)
                    {
                        buildMaterialFlagsKernel = s_BuildMaterialFlagsWriteKernel;
                        baseFeatureFlags |= LightDefinitions.s_LightFeatureMaskFlags;
                    }

                    cmd.SetComputeIntParam(buildMaterialFlagsShader, HDShaderIDs.g_BaseFeatureFlags, (int)baseFeatureFlags);
                    cmd.SetComputeIntParams(buildMaterialFlagsShader, HDShaderIDs.g_viDimensions, s_TempScreenDimArray);
                    cmd.SetComputeBufferParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);

                    cmd.SetComputeTextureParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, stencilTextureRT);

                    cmd.DispatchCompute(buildMaterialFlagsShader, buildMaterialFlagsKernel, numTilesX, numTilesY, 1);
                }

                // clear dispatch indirect buffer
                cmd.SetComputeBufferParam(clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, s_DispatchIndirectBuffer);
                cmd.DispatchCompute(clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, 1, 1, 1);

                // add tiles to indirect buffer
                cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, s_DispatchIndirectBuffer);
                cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_TileList, s_TileList);
                cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);
                cmd.SetComputeIntParam(buildDispatchIndirectShader, HDShaderIDs.g_NumTiles, numTiles);
                cmd.SetComputeIntParam(buildDispatchIndirectShader, HDShaderIDs.g_NumTilesX, numTilesX);
                cmd.DispatchCompute(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, (numTiles + 63) / 64, 1, 1);
            }

            cmd.EndSample("Build Light List");
        }

        public void BuildGPULightLists(HDCamera hdCamera, CommandBuffer cmd, RenderTargetIdentifier cameraDepthBufferRT, RenderTargetIdentifier stencilTextureRT, bool skyEnabled)
        {
            cmd.SetRenderTarget(BuiltinRenderTextureType.None);

            BuildGPULightListsCommon(hdCamera, cmd, cameraDepthBufferRT, stencilTextureRT, skyEnabled);
            PushGlobalParams(hdCamera, cmd);
        }
        void UpdateDataBuffers()
        {
            m_DirectionalLightDatas.SetData(m_lightList.directionalLights);
            m_LightDatas.SetData(m_lightList.lights);
            m_EnvLightDatas.SetData(m_lightList.envLights);
            m_DecalDatas.SetData(DecalSystem.m_DecalDatas, 0, 0, Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen)); // don't add more than the size of the buffer

            // These two buffers have been set in Rebuild()
            s_ConvexBoundsBuffer.SetData(m_lightList.bounds);
            s_LightVolumeDataBuffer.SetData(m_lightList.lightVolumes);
        }

        HDAdditionalLightData GetHDAdditionalLightData(Light light)
        {
            // Light reference can be null for particle lights.
            var add = light != null ? light.GetComponent<HDAdditionalLightData>() : null;
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalLightData;
            }
            return add;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", CustomSamplerId.TPPushGlobalParameters.GetSampler()))
            {
                Camera camera = hdCamera.camera;

                // Shadows
                m_ShadowManager.SyncData();
                m_ShadowManager.BindResources(cmd);

                cmd.SetGlobalTexture(HDShaderIDs._CookieTextures, m_CookieTexArray.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._CookieCubeTextures, m_CubeCookieTexArray.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._EnvCubemapTextures, m_ReflectionProbeCache.GetTexCache());
                cmd.SetGlobalInt(HDShaderIDs._EnvSliceSize, m_ReflectionProbeCache.GetEnvSliceSize());
                cmd.SetGlobalTexture(HDShaderIDs._Env2DTextures, m_ReflectionPlanarProbeCache.GetTexCache());
                cmd.SetGlobalMatrixArray(HDShaderIDs._Env2DCaptureVP, m_Env2DCaptureVP);

                cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, m_DirectionalLightDatas);
                cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, m_lightList.directionalLights.Count);
                cmd.SetGlobalBuffer(HDShaderIDs._LightDatas, m_LightDatas);
                cmd.SetGlobalInt(HDShaderIDs._PunctualLightCount, m_punctualLightCount);
                cmd.SetGlobalInt(HDShaderIDs._AreaLightCount, m_areaLightCount);
                cmd.SetGlobalBuffer(HDShaderIDs._EnvLightDatas, m_EnvLightDatas);
                cmd.SetGlobalInt(HDShaderIDs._EnvLightCount, m_lightList.envLights.Count);
                cmd.SetGlobalBuffer(HDShaderIDs._DecalDatas, m_DecalDatas);
                cmd.SetGlobalInt(HDShaderIDs._DecalCount, DecalSystem.m_DecalDatasCount);

                cmd.SetGlobalInt(HDShaderIDs._NumTileBigTileX, GetNumTileBigTileX(hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileBigTileY, GetNumTileBigTileY(hdCamera));

                cmd.SetGlobalInt(HDShaderIDs._NumTileFtplX, GetNumTileFtplX(hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileFtplY, GetNumTileFtplY(hdCamera));

                cmd.SetGlobalInt(HDShaderIDs._NumTileClusteredX, GetNumTileClusteredX(hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileClusteredY, GetNumTileClusteredY(hdCamera));

                cmd.SetGlobalInt(HDShaderIDs._EnableSSRefraction, hdCamera.frameSettings.enableRoughRefraction ? 1 : 0);

                if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vBigTileLightList, s_BigTileLightList);

                // Cluster
                {
                    cmd.SetGlobalFloat(HDShaderIDs.g_fClustScale, m_ClustScale);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fClustBase, k_ClustLogBase);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fNearPlane, camera.nearClipPlane);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fFarPlane, camera.farClipPlane);
                    cmd.SetGlobalInt(HDShaderIDs.g_iLog2NumClusters, k_Log2NumClusters);

                    cmd.SetGlobalInt(HDShaderIDs.g_isLogBaseBufferEnabled, k_UseDepthBuffer ? 1 : 0);

                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, s_PerVoxelOffset);
                    if (k_UseDepthBuffer)
                    {
                        cmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, s_PerTileLogBaseTweak);
                    }

                    // Set up clustered lighting for volumetrics.
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, s_PerVoxelLightLists);
                }

                // Push global params
                AdditionalShadowData sunShadowData = m_CurrentSunLight != null ? m_CurrentSunLight.GetComponent<AdditionalShadowData>() : null;
                bool sunLightShadow = sunShadowData != null && m_CurrentShadowSortedSunLightIndex >= 0;
                if (sunLightShadow)
                {
                    cmd.SetGlobalInt(HDShaderIDs._DirectionalShadowIndex, m_CurrentShadowSortedSunLightIndex);
                }
                else
                {
                    cmd.SetGlobalInt(HDShaderIDs._DirectionalShadowIndex, -1);
                }


            }
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults)
        {
            // kick off the shadow jobs here
            m_ShadowManager.RenderShadows(renderContext, cmd, cullResults);
        }

        public struct LightingPassOptions
        {
            public bool outputSplitLighting;
        }

        public void SetScreenSpaceShadowsTexture(HDCamera hdCamera, RTHandleSystem.RTHandle deferredShadowRT, CommandBuffer cmd)
        {
            AdditionalShadowData sunShadowData = m_CurrentSunLight != null ? m_CurrentSunLight.GetComponent<AdditionalShadowData>() : null;
            bool needsContactShadows = (m_CurrentSunLight != null && sunShadowData != null && sunShadowData.contactShadows) || m_DominantLightIndex != -1;
            if (!m_EnableContactShadow || !needsContactShadows)
            {
                cmd.SetGlobalTexture(HDShaderIDs._DeferredShadowTexture, RuntimeUtilities.blackTexture);
                return;
            }
            cmd.SetGlobalTexture(HDShaderIDs._DeferredShadowTexture, deferredShadowRT);
        }

        public void RenderScreenSpaceShadows(HDCamera hdCamera, RTHandleSystem.RTHandle deferredShadowRT, RenderTargetIdentifier depthTexture, int firstMipOffsetY, CommandBuffer cmd)
        {
            AdditionalShadowData sunShadowData = m_CurrentSunLight != null ? m_CurrentSunLight.GetComponent<AdditionalShadowData>() : null;
            // if there is no need to compute contact shadows, we just quit
            bool needsContactShadows = (m_CurrentSunLight != null && sunShadowData != null && sunShadowData.contactShadows) || m_DominantLightIndex != -1;
            if (!m_EnableContactShadow || !needsContactShadows)
            {
                return;
            }

            using (new ProfilingSample(cmd, "Screen Space Shadow", CustomSamplerId.TPScreenSpaceShadows.GetSampler()))
            {
                Vector4         lightDirection = Vector4.zero;
                Vector4         lightPosition = Vector4.zero;
                int             kernel;

                // Pick the adequate kenel
                kernel = hdCamera.frameSettings.enableMSAA ? s_deferredContactShadowKernelMSAA : s_deferredContactShadowKernel;

                // We use the .w component of the direction/position vectors to choose in the shader the
                // light direction of the contact shadows (direction light direction or (pixel position - light position))
                if (m_CurrentSunLight != null)
                {
                    lightDirection = -m_CurrentSunLight.transform.forward;
                    lightDirection.w = 1;
                }
                if (m_DominantLightIndex != -1)
                {
                    lightPosition = m_DominantLightData.positionRWS;
                    lightPosition.w = 1;
                    lightDirection.w = 0;
                }

                m_ShadowManager.BindResources(cmd);

                float contactShadowRange = Mathf.Clamp(m_ContactShadows.fadeDistance, 0.0f, m_ContactShadows.maxDistance);
                float contactShadowFadeEnd = m_ContactShadows.maxDistance;
                float contactShadowOneOverFadeRange = 1.0f / Math.Max(1e-6f, contactShadowRange);
                Vector4 contactShadowParams = new Vector4(m_ContactShadows.length, m_ContactShadows.distanceScaleFactor, contactShadowFadeEnd, contactShadowOneOverFadeRange);
                Vector4 contactShadowParams2 = new Vector4(m_ContactShadows.opacity, firstMipOffsetY, 0.0f, 0.0f);
                cmd.SetComputeVectorParam(screenSpaceShadowComputeShader, HDShaderIDs._ContactShadowParamsParameters, contactShadowParams);
                cmd.SetComputeVectorParam(screenSpaceShadowComputeShader, HDShaderIDs._ContactShadowParamsParameters2, contactShadowParams2);
                cmd.SetComputeIntParam(screenSpaceShadowComputeShader, HDShaderIDs._DirectionalContactShadowSampleCount, m_ContactShadows.sampleCount);
                cmd.SetComputeVectorParam(screenSpaceShadowComputeShader, HDShaderIDs._DirectionalLightDirection, lightDirection);
                cmd.SetComputeVectorParam(screenSpaceShadowComputeShader, HDShaderIDs._PunctualLightPosition, lightPosition);

                // Inject the texture in the adequate slot
                cmd.SetComputeTextureParam(screenSpaceShadowComputeShader, kernel, hdCamera.frameSettings.enableMSAA ? HDShaderIDs._CameraDepthValuesTexture : HDShaderIDs._CameraDepthTexture, depthTexture);

                cmd.SetComputeTextureParam(screenSpaceShadowComputeShader, kernel, HDShaderIDs._DeferredShadowTextureUAV, deferredShadowRT);

                int deferredShadowTileSize = 16; // Must match DeferreDirectionalShadow.compute
                int numTilesX = hdCamera.camera.stereoEnabled ? ((hdCamera.actualWidth / 2) + (deferredShadowTileSize - 1)) / deferredShadowTileSize : (hdCamera.actualWidth + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
                int numTilesY = (hdCamera.actualHeight + (deferredShadowTileSize - 1)) / deferredShadowTileSize;

                for (int eye = 0; eye < hdCamera.numEyes; eye++)
                {
                    cmd.SetGlobalInt(HDShaderIDs._ComputeEyeIndex, (int)eye);
                    cmd.DispatchCompute(screenSpaceShadowComputeShader, kernel, numTilesX, numTilesY, 1);
                }
            }
        }

        public void RenderDeferredLighting(HDCamera hdCamera, CommandBuffer cmd, DebugDisplaySettings debugDisplaySettings,
            RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthStencilBuffer, RenderTargetIdentifier depthTexture,
            LightingPassOptions options)
        {
            cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, s_LightList);

            if (m_FrameSettings.lightLoopSettings.enableTileAndCluster && m_FrameSettings.lightLoopSettings.enableComputeLightEvaluation && options.outputSplitLighting)
            {
                // The CS is always in the MRT mode. Do not execute the same shader twice.
                return;
            }


            // Predeclared to reduce GC pressure
            string tilePassName = "TilePass - Deferred Lighting Pass";
            string tilePassMRTName = "TilePass - Deferred Lighting Pass MRT";
            string singlePassName = "SinglePass - Deferred Lighting Pass";
            string SinglePassMRTName = "SinglePass - Deferred Lighting Pass MRT";

            string sLabel = m_FrameSettings.lightLoopSettings.enableTileAndCluster ?
                (options.outputSplitLighting ? tilePassMRTName : tilePassName) :
                (options.outputSplitLighting ? SinglePassMRTName : singlePassName);

            using (new ProfilingSample(cmd, sLabel, CustomSamplerId.TPRenderDeferredLighting.GetSampler()))
            {
                // Compute path
                if (m_FrameSettings.lightLoopSettings.enableTileAndCluster && m_FrameSettings.lightLoopSettings.enableComputeLightEvaluation)
                {
                    int w = hdCamera.actualWidth;
                    int h = hdCamera.actualHeight;
                    int numTilesX = hdCamera.camera.stereoEnabled ? ((w / 2) + 15) / 16 : (w + 15) / 16;
                    int numTilesY = (h + 15) / 16;
                    int numTiles = numTilesX * numTilesY;

                    bool enableFeatureVariants = GetFeatureVariantsEnabled() && !debugDisplaySettings.IsDebugDisplayEnabled() && !hdCamera.camera.stereoEnabled; // TODO VR: Reenable later

                    int numVariants = 1;
                    if (enableFeatureVariants)
                        numVariants = LightDefinitions.s_NumFeatureVariants;

                    for (int variant = 0; variant < numVariants; variant++)
                    {
                        int kernel;

                        if (enableFeatureVariants)
                        {
                            if (m_enableBakeShadowMask)
                                kernel = s_shadeOpaqueIndirectShadowMaskFptlKernels[variant];
                            else
                                kernel = s_shadeOpaqueIndirectFptlKernels[variant];
                        }
                        else
                        {
                            if (m_enableBakeShadowMask)
                            {
                                kernel = debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel : s_shadeOpaqueDirectShadowMaskFptlKernel;
                            }
                            else
                            {
                                kernel = debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectFptlDebugDisplayKernel : s_shadeOpaqueDirectFptlKernel;
                            }
                        }

                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, HDShaderIDs._CameraDepthTexture, depthTexture);

                        // TODO: Is it possible to setup this outside the loop ? Can figure out how, get this: Property (specularLightingUAV) at kernel index (21) is not set
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, HDShaderIDs.specularLightingUAV, colorBuffers[0]);
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, HDShaderIDs.diffuseLightingUAV,  colorBuffers[1]);

                        // always do deferred lighting in blocks of 16x16 (not same as tiled light size)

                        if (enableFeatureVariants)
                        { // TODO VR: variants support (solve how g_TileListOffset and surrounding math should work with stereo)
                            cmd.SetComputeBufferParam(deferredComputeShader, kernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);
                            cmd.SetComputeIntParam(deferredComputeShader, HDShaderIDs.g_TileListOffset, variant * numTiles);
                            cmd.SetComputeBufferParam(deferredComputeShader, kernel, HDShaderIDs.g_TileList, s_TileList);
                            cmd.SetGlobalInt(HDShaderIDs._ComputeEyeIndex, 0);
                            cmd.DispatchCompute(deferredComputeShader, kernel, s_DispatchIndirectBuffer, (uint)variant * 3 * sizeof(uint));
                        }
                        else
                        {
                            for (int eye = 0; eye < hdCamera.numEyes; eye++)
                            {
                                cmd.SetGlobalInt(HDShaderIDs._ComputeEyeIndex, eye);
                                cmd.DispatchCompute(deferredComputeShader, kernel, numTilesX, numTilesY, 1);
                            }
                        }
                    }
                }
                else // Pixel shader evaluation
                {
                    int index = GetDeferredLightingMaterialIndex(options.outputSplitLighting ? 1 : 0,
                            m_FrameSettings.lightLoopSettings.enableTileAndCluster ? 1 : 0,
                            m_enableBakeShadowMask ? 1 : 0,
                            debugDisplaySettings.IsDebugDisplayEnabled() ? 1 : 0);

                    Material currentLightingMaterial = m_deferredLightingMaterial[index];

                    if (options.outputSplitLighting)
                    {
                        CoreUtils.DrawFullScreen(cmd, currentLightingMaterial, colorBuffers, depthStencilBuffer);
                    }
                    else
                    {
                        // If SSS is disable, do lighting for both split lighting and no split lighting
                        // This is for debug purpose, so fine to use immediate material mode here to modify render state
                        if (!m_FrameSettings.enableSubsurfaceScattering)
                        {
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.NotEqual);
                        }
                        else
                        {
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.RegularLighting);
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                        }

                        CoreUtils.DrawFullScreen(cmd, currentLightingMaterial, colorBuffers[0], depthStencilBuffer);
                    }
                }
            } // End profiling
        }

        public void RenderForward(Camera camera, CommandBuffer cmd, bool renderOpaque)
        {
            // Note: SHADOWS_SHADOWMASK keyword is enabled in HDRenderPipeline.cs ConfigureForShadowMask

            // Note: if we use render opaque with deferred tiling we need to render a opaque depth pass for these opaque objects
            if (!m_FrameSettings.lightLoopSettings.enableTileAndCluster)
            {
                using (new ProfilingSample(cmd, "Forward pass", CustomSamplerId.TPForwardPass.GetSampler()))
                {
                    cmd.EnableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                    cmd.DisableShaderKeyword("LIGHTLOOP_TILE_PASS");
                }
            }
            else
            {
                // Only opaques can use FPTL, transparent must use clustered!
                bool useFptl = renderOpaque && m_FrameSettings.lightLoopSettings.enableFptlForForwardOpaque;

                using (new ProfilingSample(cmd, useFptl ? "Forward Tiled pass" : "Forward Clustered pass", CustomSamplerId.TPForwardTiledClusterpass.GetSampler()))
                {
                    // say that we want to use tile of single loop
                    cmd.EnableShaderKeyword("LIGHTLOOP_TILE_PASS");
                    cmd.DisableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                    CoreUtils.SetKeyword(cmd, "USE_FPTL_LIGHTLIST", useFptl);
                    CoreUtils.SetKeyword(cmd, "USE_CLUSTERED_LIGHTLIST", !useFptl);
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, useFptl ? s_LightList : s_PerVoxelLightLists);
                }
            }
        }

        public void RenderDebugOverlay(HDCamera hdCamera, CommandBuffer cmd, DebugDisplaySettings debugDisplaySettings, ref float x, ref float y, float overlaySize, float width, CullResults cullResults)
        {
            LightingDebugSettings lightingDebug = debugDisplaySettings.lightingDebugSettings;

            using (new ProfilingSample(cmd, "Tiled/cluster Lighting Debug", CustomSamplerId.TPTiledLightingDebug.GetSampler()))
            {
                if (lightingDebug.tileClusterDebug != LightLoop.TileClusterDebug.None)
                {
                    int w = hdCamera.actualWidth;
                    int h = hdCamera.actualHeight;
                    int numTilesX = (w + 15) / 16;
                    int numTilesY = (h + 15) / 16;
                    int numTiles = numTilesX * numTilesY;

                    // Debug tiles
                    if (lightingDebug.tileClusterDebug == LightLoop.TileClusterDebug.MaterialFeatureVariants)
                    {
                        if (GetFeatureVariantsEnabled())
                        {
                            // featureVariants
                            m_DebugViewTilesMaterial.SetInt(HDShaderIDs._NumTiles, numTiles);
                            m_DebugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                            m_DebugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                            m_DebugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                            m_DebugViewTilesMaterial.SetBuffer(HDShaderIDs.g_TileList, s_TileList);
                            m_DebugViewTilesMaterial.SetBuffer(HDShaderIDs.g_DispatchIndirectBuffer, s_DispatchIndirectBuffer);
                            m_DebugViewTilesMaterial.EnableKeyword("USE_FPTL_LIGHTLIST");
                            m_DebugViewTilesMaterial.DisableKeyword("USE_CLUSTERED_LIGHTLIST");
                            m_DebugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                            m_DebugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                            cmd.DrawProcedural(Matrix4x4.identity, m_DebugViewTilesMaterial, 0, MeshTopology.Triangles, numTiles * 6);
                        }
                    }
                    else // tile or cluster
                    {
                        bool bUseClustered = lightingDebug.tileClusterDebug == LightLoop.TileClusterDebug.Cluster;

                        // lightCategories
                        m_DebugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                        m_DebugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                        m_DebugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                        m_DebugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListGlobal, bUseClustered ? s_PerVoxelLightLists : s_LightList);
                        m_DebugViewTilesMaterial.EnableKeyword(bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        m_DebugViewTilesMaterial.DisableKeyword(!bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        m_DebugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                        m_DebugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");

                        CoreUtils.DrawFullScreen(cmd, m_DebugViewTilesMaterial, 0);
                    }
                }
            }

            using (new ProfilingSample(cmd, "Display Shadows", CustomSamplerId.TPDisplayShadows.GetSampler()))
            {
                if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    int startShadowIndex = (int)lightingDebug.shadowMapIndex;
                    int shadowRequestCount = 1;

#if UNITY_EDITOR
                    if (lightingDebug.shadowDebugUseSelection && m_DebugSelectedLightShadowIndex != -1)
                    {
                        startShadowIndex = m_DebugSelectedLightShadowIndex;
                        shadowRequestCount = m_DebugSelectedLightShadowCount;
                    }
#endif

                    for (int shadowIndex = startShadowIndex; shadowIndex < startShadowIndex + shadowRequestCount; shadowIndex++)
                    {
                        m_ShadowManager.DisplayShadowMap(shadowIndex, cmd, m_DebugHDShadowMapMaterial, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, hdCamera.camera.cameraType != CameraType.SceneView);
                        HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                    }
                }
                else if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.VisualizeAtlas)
                {
                    m_ShadowManager.DisplayShadowAtlas(cmd, m_DebugHDShadowMapMaterial, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, hdCamera.camera.cameraType != CameraType.SceneView);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                    m_ShadowManager.DisplayShadowCascadeAtlas(cmd, m_DebugHDShadowMapMaterial, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, hdCamera.camera.cameraType != CameraType.SceneView);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                }
            }

            if (lightingDebug.displayLightVolumes)
            {
                s_lightVolumes.RenderLightVolumes(cmd, hdCamera, cullResults, lightingDebug);
            }
        }
    }
}
