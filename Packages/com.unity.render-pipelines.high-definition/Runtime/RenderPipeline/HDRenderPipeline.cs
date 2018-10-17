using System.Collections.Generic;
using UnityEngine.Rendering;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.GlobalIllumination;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDRenderPipeline : RenderPipeline
    {
        // SampleGame Change BEGIN
        public delegate void RenderCallback(HDCamera hdCamera, CommandBuffer cmd);
        public RenderCallback DebugLayer2DCallback;
        public RenderCallback DebugLayer3DCallback;
        // SampleGame Change END

        enum ForwardPass
        {
            Opaque,
            PreRefraction,
            Transparent
        }

        static readonly string[] k_ForwardPassDebugName =
        {
            "Forward Opaque Debug",
            "Forward PreRefraction Debug",
            "Forward Transparent Debug"
        };

        static readonly string[] k_ForwardPassName =
        {
            "Forward Opaque",
            "Forward PreRefraction",
            "Forward Transparent"
        };

        readonly HDRenderPipelineAsset m_Asset;
        public HDRenderPipelineAsset asset { get { return m_Asset; } }

        DiffusionProfileSettings m_InternalSSSAsset;
        public DiffusionProfileSettings diffusionProfileSettings
        {
            get
            {
                // If no SSS asset is set, build / reuse an internal one for simplicity
                var asset = m_Asset.diffusionProfileSettings;

                if (asset == null)
                {
                    if (m_InternalSSSAsset == null)
                        m_InternalSSSAsset = ScriptableObject.CreateInstance<DiffusionProfileSettings>();

                    asset = m_InternalSSSAsset;
                }

                return asset;
            }
        }
        public RenderPipelineSettings renderPipelineSettings { get { return m_Asset.renderPipelineSettings; } }

        public bool IsInternalDiffusionProfile(DiffusionProfileSettings profile)
        {
            return m_InternalSSSAsset == profile;
        }

        readonly RenderPipelineMaterial m_DeferredMaterial;
        readonly List<RenderPipelineMaterial> m_MaterialList = new List<RenderPipelineMaterial>();

        readonly GBufferManager m_GbufferManager;
        readonly DBufferManager m_DbufferManager;
        readonly SubsurfaceScatteringManager m_SSSBufferManager = new SubsurfaceScatteringManager();
        readonly SharedRTManager m_SharedRTManager = new SharedRTManager();

        // Renderer Bake configuration can vary depends on if shadow mask is enabled or no
        RendererConfiguration m_currentRendererConfigurationBakedLighting = HDUtils.k_RendererConfigurationBakedLighting;
        Material m_CopyStencilForNoLighting;
        Material m_CopyDepth;
        GPUCopy m_GPUCopy;
        MipGenerator m_MipGenerator;

        IBLFilterGGX m_IBLFilterGGX = null;

        ComputeShader m_ScreenSpaceReflectionsCS { get { return m_Asset.renderPipelineResources.shaders.screenSpaceReflectionsCS; } }
        int m_SsrTracingKernel      = -1;
        int m_SsrReprojectionKernel = -1;

        ComputeShader m_applyDistortionCS { get { return m_Asset.renderPipelineResources.shaders.applyDistortionCS; } }
        int m_applyDistortionKernel;

        Material m_CameraMotionVectorsMaterial;
        Material m_DecalNormalBufferMaterial;

        // Debug material
        Material m_DebugViewMaterialGBuffer;
        Material m_DebugViewMaterialGBufferShadowMask;
        Material m_currentDebugViewMaterialGBuffer;
        Material m_DebugDisplayLatlong;
        Material m_DebugFullScreen;
        Material m_DebugColorPicker;
        Material m_Blit;
        Material m_ErrorMaterial;

        RenderTargetIdentifier[] m_MRTCache2 = new RenderTargetIdentifier[2];

        // 'm_CameraColorBuffer' does not contain diffuse lighting of SSS materials until the SSS pass. It is stored within 'm_CameraSssDiffuseLightingBuffer'.
        RTHandleSystem.RTHandle m_CameraColorBuffer;
        RTHandleSystem.RTHandle m_CameraColorBufferMipChain;
        RTHandleSystem.RTHandle m_CameraSssDiffuseLightingBuffer;

        RTHandleSystem.RTHandle m_ScreenSpaceShadowsBuffer;
        RTHandleSystem.RTHandle m_AmbientOcclusionBuffer;
        RTHandleSystem.RTHandle m_MultiAmbientOcclusionBuffer;
        RTHandleSystem.RTHandle m_DistortionBuffer;

        // TODO: remove me, I am just a temporary debug texture. :-)
        // RTHandleSystem.RTHandle m_SsrDebugTexture;
        RTHandleSystem.RTHandle m_SsrHitPointTexture;
        RTHandleSystem.RTHandle m_SsrLightingTexture;
        // MSAA Versions of regular textures
        RTHandleSystem.RTHandle m_CameraColorMSAABuffer;
        RTHandleSystem.RTHandle m_CameraSssDiffuseLightingMSAABuffer;

        // Temporary hack post process output for multi camera setup
        static int _TempPostProcessOutputTexture = Shader.PropertyToID("_TempPostProcessOutputTexture");
        static RenderTargetIdentifier _TempPostProcessOutputTextureID = new RenderTargetIdentifier(_TempPostProcessOutputTexture);

        // The current MSAA count
        MSAASamples m_MSAASamples;

        // AO resolve property block
        MaterialPropertyBlock m_AOPropertyBlock = new MaterialPropertyBlock();
        Material m_AOResolveMaterial = null;

        // The pass "SRPDefaultUnlit" is a fall back to legacy unlit rendering and is required to support unity 2d + unity UI that render in the scene.
        ShaderPassName[] m_ForwardAndForwardOnlyPassNames = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName };
        ShaderPassName[] m_ForwardOnlyPassNames = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllTransparentPassNames = {  HDShaderPassNames.s_TransparentBackfaceName,
                                                        HDShaderPassNames.s_ForwardOnlyName,
                                                        HDShaderPassNames.s_ForwardName,
                                                        HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_AllForwardOpaquePassNames = {    HDShaderPassNames.s_ForwardOnlyName,
                                                            HDShaderPassNames.s_ForwardName,
                                                            HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderPassName[] m_DepthOnlyAndDepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_DepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName };
        ShaderPassName[] m_DepthOnlyPassNames = { HDShaderPassNames.s_DepthOnlyName };
        ShaderPassName[] m_TransparentDepthPrepassNames = { HDShaderPassNames.s_TransparentDepthPrepassName };
        ShaderPassName[] m_TransparentDepthPostpassNames = { HDShaderPassNames.s_TransparentDepthPostpassName };
        ShaderPassName[] m_ForwardErrorPassNames = { HDShaderPassNames.s_AlwaysName, HDShaderPassNames.s_ForwardBaseName, HDShaderPassNames.s_DeferredName, HDShaderPassNames.s_PrepassBaseName, HDShaderPassNames.s_VertexName, HDShaderPassNames.s_VertexLMRGBMName, HDShaderPassNames.s_VertexLMName };
        ShaderPassName[] m_SinglePassName = new ShaderPassName[1];

        // Stencil usage in HDRenderPipeline.
        // Currently we use only 2 bits to identify the kind of lighting that is expected from the render pipeline
        // Usage is define in LightDefinitions.cs
        [Flags]
        public enum StencilBitMask
        {
            Clear                           = 0,    // 0x0
            LightingMask                    = 7,    // 0x7  - 3 bit
            Decals                          = 8,    // 0x8  - 1 bit
            DecalsForwardOutputNormalBuffer = 16,   // 0x10  - 1 bit
            ObjectVelocity                  = 128,  // 0x80 - 1 bit
            All                             = 255   // 0xFF - 8 bit
        }

        RenderStateBlock m_DepthStateOpaque;

        // Detect when windows size is changing
        int m_CurrentWidth;
        int m_CurrentHeight;

        // Use to detect frame changes
        uint  m_FrameCount;
        float m_LastTime, m_Time;

        public int GetCurrentShadowCount() { return m_LightLoop.GetCurrentShadowCount(); }
        public int GetDecalAtlasMipCount()
        {
            int highestDim = Math.Max(renderPipelineSettings.decalSettings.atlasWidth, renderPipelineSettings.decalSettings.atlasHeight);
            return (int)Math.Log(highestDim, 2);
        }

        readonly SkyManager m_SkyManager = new SkyManager();
        readonly LightLoop m_LightLoop = new LightLoop();
        readonly VolumetricLightingSystem m_VolumetricLightingSystem = new VolumetricLightingSystem();

        // Debugging
        MaterialPropertyBlock m_SharedPropertyBlock = new MaterialPropertyBlock();
        DebugDisplaySettings m_DebugDisplaySettings = new DebugDisplaySettings();
        public DebugDisplaySettings debugDisplaySettings { get { return m_DebugDisplaySettings; } }
        static DebugDisplaySettings s_NeutralDebugDisplaySettings = new DebugDisplaySettings();
        DebugDisplaySettings m_CurrentDebugDisplaySettings;
        RTHandleSystem.RTHandle         m_DebugColorPickerBuffer;
        RTHandleSystem.RTHandle         m_DebugFullScreenTempBuffer;
        bool                            m_FullScreenDebugPushed;
        bool                            m_ValidAPI; // False by default mean we render normally, true mean we don't render anything
        bool                            m_IsDepthBufferCopyValid;

        RenderTargetIdentifier[] m_MRTWithSSS;
        string m_ForwardPassProfileName;

        Vector2Int m_PyramidSizeV2I = new Vector2Int();
        Vector4 m_PyramidSizeV4F = new Vector4();
        Vector4 m_PyramidScaleLod = new Vector4();
        Vector4 m_PyramidScale = new Vector4();

        public Material GetBlitMaterial() { return m_Blit; }

        ComputeBuffer m_DepthPyramidMipLevelOffsetsBuffer = null;


        public HDRenderPipeline(HDRenderPipelineAsset asset)
        {
            m_Asset = asset;

            DebugManager.instance.RefreshEditor();

            m_ValidAPI = true;

            if (!SetRenderingFeatures())
            {
                m_ValidAPI = false;

                return;
            }

            // Upgrade the resources (re-import every references in RenderPipelineResources) if the resource version mismatches
            // It's done here because we know every HDRP assets have been imported before
            UpgradeResourcesIfNeeded();


            // Initial state of the RTHandle system.
            // Tells the system that we will require MSAA or not so that we can avoid wasteful render texture allocation.
            // TODO: Might want to initialize to at least the window resolution to avoid un-necessary re-alloc in the player
            RTHandles.Initialize(1, 1, m_Asset.renderPipelineSettings.supportMSAA, m_Asset.renderPipelineSettings.msaaSampleCount);

            m_GPUCopy = new GPUCopy(asset.renderPipelineResources.shaders.copyChannelCS);

            m_MipGenerator = new MipGenerator(m_Asset);

            EncodeBC6H.DefaultInstance = EncodeBC6H.DefaultInstance ?? new EncodeBC6H(asset.renderPipelineResources.shaders.encodeBC6HCS);

            m_ReflectionProbeCullResults = new ReflectionProbeCullResults(asset.reflectionSystemParameters);
            ReflectionSystem.SetParameters(asset.reflectionSystemParameters);

            // Scan material list and assign it
            m_MaterialList = HDUtils.GetRenderPipelineMaterialList();
            // Find first material that have non 0 Gbuffer count and assign it as deferredMaterial
            m_DeferredMaterial = null;
            foreach (var material in m_MaterialList)
            {
                if (material.IsDefferedMaterial())
                    m_DeferredMaterial = material;
            }

            // TODO: Handle the case of no Gbuffer material
            // TODO: I comment the assert here because m_DeferredMaterial for whatever reasons contain the correct class but with a "null" in the name instead of the real name and then trigger the assert
            // whereas it work. Don't know what is happening, DebugDisplay use the same code and name is correct there.
            // Debug.Assert(m_DeferredMaterial != null);

            m_GbufferManager = new GBufferManager(asset, m_DeferredMaterial);
            m_DbufferManager = new DBufferManager();

            m_SSSBufferManager.Build(asset);
            m_SharedRTManager.Build(asset);

            // Initialize various compute shader resources
            m_applyDistortionKernel = m_applyDistortionCS.FindKernel("KMain");
            m_SsrTracingKernel      = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsTracing");
            m_SsrReprojectionKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsReprojection");

            // General material
            m_CopyStencilForNoLighting = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.copyStencilBufferPS);
            m_CopyStencilForNoLighting.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
            m_CopyStencilForNoLighting.SetInt(HDShaderIDs._StencilMask, (int)StencilBitMask.LightingMask);
            m_CameraMotionVectorsMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.cameraMotionVectorsPS);
            m_DecalNormalBufferMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.decalNormalBufferPS);

            m_CopyDepth = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.copyDepthBufferPS);

            InitializeDebugMaterials();

            m_MaterialList.ForEach(material => material.Build(asset));

            m_IBLFilterGGX = new IBLFilterGGX(asset.renderPipelineResources, m_MipGenerator);

            m_LightLoop.Build(asset, m_IBLFilterGGX);

            m_SkyManager.Build(asset, m_IBLFilterGGX);

            m_VolumetricLightingSystem.Build(asset);

            m_DebugDisplaySettings.RegisterDebug();
#if UNITY_EDITOR
            // We don't need the debug of Scene View at runtime (each camera have its own debug settings)
            FrameSettings.RegisterDebug("Scene View", m_Asset.GetFrameSettings());
#endif

            m_DepthPyramidMipLevelOffsetsBuffer = new ComputeBuffer(15, sizeof(int) * 2);

            InitializeRenderTextures();

            // For debugging
            MousePositionDebug.instance.Build();

            InitializeRenderStateBlocks();

            // Init the MSAA AO resolve material
            m_AOResolveMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.aoResolvePS);

            // Keep track of the original msaa sample value
            m_MSAASamples = m_Asset ? m_Asset.renderPipelineSettings.msaaSampleCount : MSAASamples.None;

            // Propagate it to the debug menu
            m_DebugDisplaySettings.msaaSamples = m_MSAASamples;

            m_MRTWithSSS = new RenderTargetIdentifier[2 + m_SSSBufferManager.sssBufferCount];
        }

        void UpgradeResourcesIfNeeded()
        {
#if UNITY_EDITOR
            m_Asset.renderPipelineResources.UpgradeIfNeeded();
#endif
        }


        void InitializeRenderTextures()
        {
            RenderPipelineSettings settings = m_Asset.renderPipelineSettings;

            if (!settings.supportOnlyForward)
                m_GbufferManager.CreateBuffers();

            if (settings.supportDecals)
                m_DbufferManager.CreateBuffers();

            m_SSSBufferManager.InitSSSBuffers(m_GbufferManager, m_Asset.renderPipelineSettings);
            m_SharedRTManager.InitSharedBuffers(m_GbufferManager, m_Asset.renderPipelineSettings, m_Asset.renderPipelineResources);

            m_CameraColorBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, enableRandomWrite: true, useMipMap: false, name: "CameraColor");
            m_CameraSssDiffuseLightingBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RGB111110Float, sRGB: false, enableRandomWrite: true, name: "CameraSSSDiffuseLighting");
            m_CameraColorBufferMipChain = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, name: "CameraColorBufferMipChain");

            if (settings.supportSSAO)
            {
                m_AmbientOcclusionBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Bilinear, colorFormat: RenderTextureFormat.R8, sRGB: false, enableRandomWrite: true, name: "AmbientOcclusion");
            }

            m_DistortionBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: Builtin.GetDistortionBufferFormat(), sRGB: Builtin.GetDistortionBufferSRGBFlag(), name: "Distortion");

            // TODO: For MSAA, we'll need to add a Draw path in order to support MSAA properlye
            // Use RG16 as we only have one deferred directional and one screen space shadow light currently
            m_ScreenSpaceShadowsBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.R16, sRGB: false, enableRandomWrite: true, name: "ScreenSpaceShadowsBuffer");

            if (settings.supportSSR)
            {
                // m_SsrDebugTexture    = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBFloat, sRGB: false, enableRandomWrite: true, name: "SSR_Debug_Texture");
                m_SsrHitPointTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RG32,      sRGB: false, enableRandomWrite: true, name: "SSR_Hit_Point_Texture");
                m_SsrLightingTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf,  sRGB: false, enableRandomWrite: true, name: "SSR_Lighting_Texture");
            }

            if (Debug.isDebugBuild)
            {
                m_DebugColorPickerBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, name: "DebugColorPicker");
                m_DebugFullScreenTempBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, name: "DebugFullScreen");
            }

            // Let's create the MSAA textures
            if (m_Asset.renderPipelineSettings.supportMSAA)
            {
                // MSAA versions of classic texture
                if (m_Asset.renderPipelineSettings.supportSSAO)
                {
                    m_MultiAmbientOcclusionBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Bilinear, colorFormat: RenderTextureFormat.RG16, sRGB: false, enableRandomWrite: true, name: "AmbientOcclusionMSAA");
                }
                m_CameraColorMSAABuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, bindTextureMS: true, enableMSAA: true, name: "CameraColorMSAA");
                m_CameraSssDiffuseLightingMSAABuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RGB111110Float, sRGB: false, bindTextureMS: true, enableMSAA: true, name: "CameraSSSDiffuseLightingMSAA");
            }
        }

        void DestroyRenderTextures()
        {
            m_GbufferManager.DestroyBuffers();
            m_DbufferManager.DestroyBuffers();
            m_MipGenerator.Release();

            RTHandles.Release(m_CameraColorBuffer);
            RTHandles.Release(m_CameraColorBufferMipChain);
            RTHandles.Release(m_CameraSssDiffuseLightingBuffer);

            RTHandles.Release(m_AmbientOcclusionBuffer);
            RTHandles.Release(m_DistortionBuffer);
            RTHandles.Release(m_ScreenSpaceShadowsBuffer);

            // RTHandles.Release(m_SsrDebugTexture);
            RTHandles.Release(m_SsrHitPointTexture);
            RTHandles.Release(m_SsrLightingTexture);

            RTHandles.Release(m_DebugColorPickerBuffer);
            RTHandles.Release(m_DebugFullScreenTempBuffer);

            RTHandles.Release(m_CameraColorMSAABuffer);
            RTHandles.Release(m_MultiAmbientOcclusionBuffer);
            RTHandles.Release(m_CameraSssDiffuseLightingMSAABuffer);

            HDCamera.ClearAll();
        }

        bool SetRenderingFeatures()
        {
            // Set sub-shader pipeline tag
            Shader.globalRenderPipeline = "HDRenderPipeline";

            // HD use specific GraphicsSettings
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.useScriptableRenderPipelineBatching = m_Asset.enableSRPBatcher;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeSupportFlags = SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation,
                defaultMixedLightingMode = SupportedRenderingFeatures.LightmapMixedBakeMode.IndirectOnly,
                supportedMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeMode.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeMode.Shadowmask,
                supportedLightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                supportedLightmapsModes = LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional,
                rendererSupportsLightProbeProxyVolumes = true,
                rendererSupportsMotionVectors = true,
                rendererSupportsReceiveShadows = false,
                rendererSupportsReflectionProbes = true,
                rendererSupportsRendererPriority = true,
                rendererOverridesEnvironmentLighting = true,
                rendererOverridesFog = true,
                rendererOverridesOtherLightingSettings = true
            };

            Lightmapping.SetDelegate(GlobalIlluminationUtils.hdLightsDelegate);

#if UNITY_EDITOR
            SceneViewDrawMode.SetupDrawMode();

            if (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                Debug.LogError("High Definition Render Pipeline doesn't support Gamma mode, change to Linear mode");
            }
#endif

            if (!IsSupportedPlatform())
            {
                CoreUtils.DisplayUnsupportedAPIMessage();

                // Display more information to the users when it should have use Metal instead of OpenGL
                if (SystemInfo.graphicsDeviceType.ToString().StartsWith("OpenGL"))
                {
                    if (SystemInfo.operatingSystem.StartsWith("Mac"))
                        CoreUtils.DisplayUnsupportedMessage("Use Metal API instead.");
                    else if (SystemInfo.operatingSystem.StartsWith("Windows"))
                        CoreUtils.DisplayUnsupportedMessage("Use Vulkan API instead.");
                }

                return false;
            }

            return true;
        }

        bool IsSupportedPlatform()
        {
            // Note: If you add new platform in this function, think about adding support when building the player to in HDRPCustomBuildProcessor.cs

            if (!SystemInfo.supportsComputeShaders)
                return false;

// If we are in the editor, we have to take the current target build platform as the graphic device type is always the same
#if UNITY_EDITOR
            if (HDUtils.IsSupportedBuildTarget(UnityEditor.EditorUserBuildSettings.activeBuildTarget))
                return true;
#else
            if (HDUtils.IsSupportedGraphicDevice(SystemInfo.graphicsDeviceType))
                return true;
#endif

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                string os = SystemInfo.operatingSystem;

                // Metal support depends on OS version:
                // macOS 10.11.x doesn't have tessellation / earlydepthstencil support, early driver versions were buggy in general
                // macOS 10.12.x should usually work with AMD, but issues with Intel/Nvidia GPUs. Regardless of the GPU, there are issues with MTLCompilerService crashing with some shaders
                // macOS 10.13.x is expected to work, and if it's a driver/shader compiler issue, there's still hope on getting it fixed to next shipping OS patch release
                //
                // Has worked experimentally with iOS in the past, but it's not currently supported
                //

                if (os.StartsWith("Mac"))
                {
                    // TODO: Expose in C# version number, for now assume "Mac OS X 10.10.4" format with version 10 at least
                    int startIndex = os.LastIndexOf(" ");
                    var parts = os.Substring(startIndex + 1).Split('.');
                    int a = Convert.ToInt32(parts[0]);
                    int b = Convert.ToInt32(parts[1]);
                    // In case in the future there's a need to disable specific patch releases
                    // int c = Convert.ToInt32(parts[2]);

                    if (a >= 10 && b >= 13)
                        return true;
                }
            }

            return false;
        }

        void UnsetRenderingFeatures()
        {
            Shader.globalRenderPipeline = "";

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

            // Reset srp batcher state just in case
            GraphicsSettings.useScriptableRenderPipelineBatching = false;

            Lightmapping.ResetDelegate();
        }

        void InitializeDebugMaterials()
        {
            m_DebugViewMaterialGBuffer = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask.EnableKeyword("SHADOWS_SHADOWMASK");
            m_DebugDisplayLatlong = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugDisplayLatlongPS);
            m_DebugFullScreen = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugFullScreenPS);
            m_DebugColorPicker = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugColorPickerPS);
            m_Blit = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.blitPS);
            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");
        }

        void InitializeRenderStateBlocks()
        {
            m_DepthStateOpaque = new RenderStateBlock
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        public void OnSceneLoad()
        {
            // Recreate the textures which went NULL
            m_MaterialList.ForEach(material => material.Build(m_Asset));
        }

        public override void Dispose()
        {
            UnsetRenderingFeatures();

            if (!m_ValidAPI)
                return;

            base.Dispose();

            m_DebugDisplaySettings.UnregisterDebug();

            m_LightLoop.Cleanup();

            // For debugging
            MousePositionDebug.instance.Cleanup();

            DecalSystem.instance.Cleanup();

            m_MaterialList.ForEach(material => material.Cleanup());

            CoreUtils.Destroy(m_AOResolveMaterial);
            CoreUtils.Destroy(m_CopyStencilForNoLighting);
            CoreUtils.Destroy(m_CameraMotionVectorsMaterial);
            CoreUtils.Destroy(m_DecalNormalBufferMaterial);

            CoreUtils.Destroy(m_DebugViewMaterialGBuffer);
            CoreUtils.Destroy(m_DebugViewMaterialGBufferShadowMask);
            CoreUtils.Destroy(m_DebugDisplayLatlong);
            CoreUtils.Destroy(m_DebugFullScreen);
            CoreUtils.Destroy(m_DebugColorPicker);
            CoreUtils.Destroy(m_Blit);
            CoreUtils.Destroy(m_CopyDepth);
            CoreUtils.Destroy(m_ErrorMaterial);

            m_SSSBufferManager.Cleanup();
            m_SharedRTManager.Cleanup();
            m_SkyManager.Cleanup();
            m_VolumetricLightingSystem.Cleanup();
            m_IBLFilterGGX.Cleanup();

            HDCamera.ClearAll();

            DestroyRenderTextures();
            CullingGroupManager.instance.Cleanup();

            CoreUtils.SafeRelease(m_DepthPyramidMipLevelOffsetsBuffer);

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
            FrameSettings.UnRegisterDebug("Scene View");
#endif
        }

        void Resize(HDCamera hdCamera)
        {
            bool resolutionChanged = (hdCamera.actualWidth != m_CurrentWidth) || (hdCamera.actualHeight != m_CurrentHeight);

            if (resolutionChanged || m_LightLoop.NeedResize())
            {
                if (m_CurrentWidth > 0 && m_CurrentHeight > 0)
                    m_LightLoop.ReleaseResolutionDependentBuffers();

                m_LightLoop.AllocResolutionDependentBuffers((int)hdCamera.screenSize.x, (int)hdCamera.screenSize.y, hdCamera.frameSettings.enableStereo);
            }

            // update recorded window resolution
            m_CurrentWidth = hdCamera.actualWidth;
            m_CurrentHeight = hdCamera.actualHeight;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, DiffusionProfileSettings sssParameters)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", CustomSamplerId.PushGlobalParameters.GetSampler()))
            {
                // Set up UnityPerFrame CBuffer.
                m_SSSBufferManager.PushGlobalParams(hdCamera, cmd, sssParameters);

                m_DbufferManager.PushGlobalParams(hdCamera, cmd);

                m_VolumetricLightingSystem.PushGlobalParams(hdCamera, cmd, m_FrameCount);

                var ssRefraction = VolumeManager.instance.stack.GetComponent<ScreenSpaceRefraction>()
                    ?? ScreenSpaceRefraction.@default;
                ssRefraction.PushShaderParameters(cmd);
                var ssReflection = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>()
                    ?? ScreenSpaceReflection.@default;
                ssReflection.PushShaderParameters(cmd);

                // Set up UnityPerView CBuffer.
                hdCamera.SetupGlobalParams(cmd, m_Time, m_LastTime, m_FrameCount);

                cmd.SetGlobalVector(HDShaderIDs._IndirectLightingMultiplier, new Vector4(VolumeManager.instance.stack.GetComponent<IndirectLightingController>().indirectDiffuseIntensity, 0, 0, 0));

                PushGlobalRTHandle(
                    cmd,
                    m_SharedRTManager.GetDepthTexture(),
                    HDShaderIDs._DepthPyramidTexture,
                    HDShaderIDs._DepthPyramidSize,
                    HDShaderIDs._DepthPyramidScale
                );
                PushGlobalRTHandle(
                    cmd,
                    m_CameraColorBufferMipChain,
                    HDShaderIDs._ColorPyramidTexture,
                    HDShaderIDs._ColorPyramidSize,
                    HDShaderIDs._ColorPyramidScale
                );
                PushGlobalRTHandle(
                    cmd,
                    m_SharedRTManager.GetVelocityBuffer(),
                    HDShaderIDs._CameraMotionVectorsTexture,
                    HDShaderIDs._CameraMotionVectorsSize,
                    HDShaderIDs._CameraMotionVectorsScale
                );

                // Light loop stuff...
                if (hdCamera.frameSettings.enableSSR)
                    cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, m_SsrLightingTexture);
                else
                    cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, Texture2D.blackTexture);
            }
        }

        bool NeedStencilBufferCopy()
        {
            // Currently, Unity does not offer a way to bind the stencil buffer as a texture in a compute shader.
            // Therefore, it's manually copied using a pixel shader.
            return m_LightLoop.GetFeatureVariantsEnabled();
        }

        void CopyDepthBufferIfNeeded(CommandBuffer cmd)
        {
            if (!m_IsDepthBufferCopyValid)
            {
                using (new ProfilingSample(cmd, "Copy depth buffer", CustomSamplerId.CopyDepthBuffer.GetSampler()))
                {
                    // TODO: maybe we don't actually need the top MIP level?
                    // That way we could avoid making the copy, and build the MIP hierarchy directly.
                    // The downside is that our SSR tracing accuracy would decrease a little bit.
                    // But since we never render SSR at full resolution, this may be acceptable.

                    // TODO: reading the depth buffer with a compute shader will cause it to decompress in place.
                    // On console, to preserve the depth test performance, we must NOT decompress the 'm_CameraDepthStencilBuffer' in place.
                    // We should call decompressDepthSurfaceToCopy() and decompress it to 'm_CameraDepthBufferMipChain'.
                    m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, m_SharedRTManager.GetDepthStencilBuffer(), m_SharedRTManager.GetDepthTexture(), new RectInt(0, 0, m_CurrentWidth, m_CurrentHeight));
                }
                m_IsDepthBufferCopyValid = true;
            }
        }

        public void SetMicroShadowingSettings(CommandBuffer cmd)
        {
            MicroShadowing microShadowingSettings = VolumeManager.instance.stack.GetComponent<MicroShadowing>();
            cmd.SetGlobalFloat(HDShaderIDs._MicroShadowingOpacity, microShadowingSettings.enable ? microShadowingSettings.opacity : 0.0f);
        }

        public void ConfigureKeywords(bool enableBakeShadowMask, HDCamera hdCamera, CommandBuffer cmd)
        {
            // Globally enable (for GBuffer shader and forward lit (opaque and transparent) the keyword SHADOWS_SHADOWMASK
            CoreUtils.SetKeyword(cmd, "SHADOWS_SHADOWMASK", enableBakeShadowMask);
            // Configure material to use depends on shadow mask option
            m_currentRendererConfigurationBakedLighting = enableBakeShadowMask ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;
            m_currentDebugViewMaterialGBuffer = enableBakeShadowMask ? m_DebugViewMaterialGBufferShadowMask : m_DebugViewMaterialGBuffer;

            CoreUtils.SetKeyword(cmd, "LIGHT_LAYERS", hdCamera.frameSettings.enableLightLayers);
            cmd.SetGlobalInt(HDShaderIDs._EnableLightLayers, hdCamera.frameSettings.enableLightLayers ? 1 : 0);

            if (m_Asset.renderPipelineSettings.supportDecals)
            {
                CoreUtils.SetKeyword(cmd, "DECALS_OFF", false);
                CoreUtils.SetKeyword(cmd, "DECALS_3RT", !m_Asset.GetRenderPipelineSettings().decalSettings.perChannelMask);
                CoreUtils.SetKeyword(cmd, "DECALS_4RT", m_Asset.GetRenderPipelineSettings().decalSettings.perChannelMask);
            }
            else
            {
                CoreUtils.SetKeyword(cmd, "DECALS_OFF", true);
                CoreUtils.SetKeyword(cmd, "DECALS_3RT", false);
                CoreUtils.SetKeyword(cmd, "DECALS_4RT", false);
            }

            // Raise the normal buffer flag only if we are in forward rendering
            CoreUtils.SetKeyword(cmd, "WRITE_NORMAL_BUFFER", hdCamera.frameSettings.enableForwardRenderingOnly);

            // Raise or remove the depth msaa flag based on the frame setting
            CoreUtils.SetKeyword(cmd, "WRITE_MSAA_DEPTH", hdCamera.frameSettings.enableMSAA);
        }

        static bool CompareCamRT(Camera cam1, Camera cam2)
        {
            if (cam1.targetTexture == null)
                return false;
            else if (cam2.targetTexture == null)
                return true;
            else return  cam1.targetTexture.GetInstanceID() < cam2.targetTexture.GetInstanceID();
        }

        // We want the camera sorting to be stable and keep the sorting done by C++ internally.
        // Bubble sort is simple enough and will work fine for lists of camera that should stay small.
        static void SortCameraByRT(Camera[] cameras)
        {
            bool swap = true;
            while (swap)
            {
                swap = false;
                for (int i = 0; (i < cameras.Length - 1) ; ++i)
                {
                    var cam1 = cameras[i];
                    var cam2 = cameras[i + 1];
                    if (CompareCamRT(cam1, cam2))
                    {
                        cameras[i] = cam2;
                        cameras[i + 1] = cam1;
                        swap = true;
                    }
                }
            }
        }

        CullResults m_CullResults;
        ReflectionProbeCullResults m_ReflectionProbeCullResults;
        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            if (!m_ValidAPI)
                return;

            base.Render(renderContext, cameras);
            RenderPipeline.BeginFrameRendering(cameras);

            {
                // SRP.Render() can be called several times per frame.
                // Also, most Time variables do not consistently update in the Scene View.
                // This makes reliable detection of the start of the new frame VERY hard.
                // One of the exceptions is 'Time.realtimeSinceStartup'.
                // Therefore, outside of the Play Mode we update the time at 60 fps,
                // and in the Play Mode we rely on 'Time.frameCount'.
                float t = Time.realtimeSinceStartup;
                uint c = (uint)Time.frameCount;

                bool newFrame;

                if (Application.isPlaying)
                {
                    newFrame = m_FrameCount != c;

                    m_FrameCount = c;
                }
                else
                {
                    newFrame = (t - m_Time) > 0.0166f;

                    if (newFrame)
                        m_FrameCount++;
                }

                if (newFrame)
                {
                    HDCamera.CleanUnused();

                    // Make sure both are never 0.
                    m_LastTime = (m_Time > 0) ? m_Time : t;
                    m_Time = t;
                }
            }

            // TODO: Render only visible probes
            var isAnyCamerasAReflectionCamera = false;
            for (int i = 0; i < cameras.Length && !isAnyCamerasAReflectionCamera; ++i)
                isAnyCamerasAReflectionCamera |= cameras[i].cameraType == CameraType.Reflection;
            if(!isAnyCamerasAReflectionCamera)  //only pass here when rendering normal camera (prevent infinite loop)
            {
                ReflectionSystem.RenderAllRealtimeProbes();
            }

            // We first update the state of asset frame settings as they can be use by various camera
            // but we keep the dirty state to correctly reset other camera that use RenderingPath.Default.
            bool assetFrameSettingsIsDirty = m_Asset.frameSettingsIsDirty;
            m_Asset.UpdateDirtyFrameSettings();

            // We need to sort by target RenderTexture because we need to accumulate cameras rendering in the same RT.
            // In this case (and if there is more than one camera with the same target) we need to blit to the final target only for the last camera of the group.
            SortCameraByRT(cameras);

            for (int cameraIndex = 0; cameraIndex < cameras.Length; ++cameraIndex)
            {
                var camera = cameras[cameraIndex];

                if (camera == null)
                    continue;

                bool lastCameraFromGroup = true;
                if (cameraIndex < (cameras.Length - 1))
                    lastCameraFromGroup = (camera.targetTexture != cameras[cameraIndex + 1].targetTexture);

                RenderPipeline.BeginCameraRendering(camera);

                // First, get aggregate of frame settings base on global settings, camera frame settings and debug settings
                // Note: the SceneView camera will never have additionalCameraData
                var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();

                // Init effective frame settings of each camera
                // Each camera have its own debug frame settings control from the debug windows
                // debug frame settings can't be aggregate with frame settings (i.e we can't aggregate forward only control for example)
                // so debug settings (when use) are the effective frame settings
                // To be able to have this behavior we init effective frame settings with serialized frame settings and copy
                // debug settings change on top of it. Each time frame settings are change in the editor, we reset all debug settings
                // to stay in sync. The loop below allow to update all frame settings correctly and is required because
                // camera can rely on default frame settings from the HDRendeRPipelineAsset
                FrameSettings srcFrameSettings;
                if (additionalCameraData)
                {
                    if (camera.cameraType != CameraType.Reflection)
                    {
                        //reflection camera must already have their framesettings correctly set at this point
                        additionalCameraData.UpdateDirtyFrameSettings(assetFrameSettingsIsDirty, m_Asset.GetFrameSettings());
                    }
                    srcFrameSettings = additionalCameraData.GetFrameSettings();
                }
                else
                {
                    srcFrameSettings = m_Asset.GetFrameSettings();
                }

                FrameSettings currentFrameSettings = new FrameSettings();
                // Get the effective frame settings for this camera taking into account the global setting and camera type
                FrameSettings.InitializeFrameSettings(camera, m_Asset.GetRenderPipelineSettings(), srcFrameSettings, ref currentFrameSettings);

                // This is the main command buffer used for the frame.
                var cmd = CommandBufferPool.Get("");

                // Specific pass to simply display the content of the camera buffer if users have fill it themselves (like video player)
                if (additionalCameraData && additionalCameraData.renderingPath == HDAdditionalCameraData.RenderingPath.FullscreenPassthrough)
                {
                    renderContext.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                    renderContext.Submit();
                    continue;
                }

                // Don't render reflection in Preview, it prevent them to display
                if (currentFrameSettings.enableRealtimePlanarReflection && camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview
                    // Planar probes rendering is not currently supported for orthographic camera
                    // Avoid rendering to prevent error log spamming
                    && !camera.orthographic)
                    // TODO: Render only visible probes
                    ReflectionSystem.RenderAllRealtimeViewerDependentProbesFor(ReflectionProbeType.PlanarReflection, camera);

                // Init material if needed
                // TODO: this should be move outside of the camera loop but we have no command buffer, ask details to Tim or Julien to do this
                if (!m_IBLFilterGGX.IsInitialized())
                    m_IBLFilterGGX.Initialize(cmd);

                foreach (var material in m_MaterialList)
                    material.RenderInit(cmd);

                using (new ProfilingSample(cmd, "HDRenderPipeline::Render", CustomSamplerId.HDRenderPipelineRender.GetSampler()))
                {
                    // If we render a reflection view or a preview we should not display any debug information
                    // This need to be call before ApplyDebugDisplaySettings()
                    if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                    {
                        // Neutral allow to disable all debug settings
                        m_CurrentDebugDisplaySettings = s_NeutralDebugDisplaySettings;
                    }
                    else
                    {
                        m_CurrentDebugDisplaySettings = m_DebugDisplaySettings;

                        // Make sure we are in sync with the debug menu for the msaa count
                        m_MSAASamples = m_DebugDisplaySettings.msaaSamples;
                        m_SharedRTManager.SetNumMSAASamples(m_MSAASamples);
                    }

                    // Caution: Component.GetComponent() generate 0.6KB of garbage at each frame here !
                    var postProcessLayer = camera.GetComponent<PostProcessLayer>();

                    // Disable post process if we enable debug mode or if the post process layer is disabled
                    if (m_CurrentDebugDisplaySettings.IsDebugDisplayRemovePostprocess() || !HDUtils.IsPostProcessingActive(postProcessLayer))
                    {
                        currentFrameSettings.enablePostprocess = false;
                    }

                    // Disable SSS if luxmeter is enabled
                    if (debugDisplaySettings.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter)
                    {
                        currentFrameSettings.enableSubsurfaceScattering = false;
                    }

                    var hdCamera = HDCamera.Get(camera);

                    if (hdCamera == null)
                    {
                        hdCamera = HDCamera.Create(camera, m_VolumetricLightingSystem);
                    }

                    // From this point, we should only use frame settings from the camera
                    hdCamera.Update(currentFrameSettings, postProcessLayer, m_VolumetricLightingSystem, m_MSAASamples);

                    using (new ProfilingSample(cmd, "Volume Update", CustomSamplerId.VolumeUpdate.GetSampler()))
                    {
                        VolumeManager.instance.Update(hdCamera.volumeAnchor, hdCamera.volumeLayerMask);
                    }

                    if (additionalCameraData != null && additionalCameraData.hasCustomRender)
                    {
                        // Flush pending command buffer.
                        renderContext.ExecuteCommandBuffer(cmd);
                        CommandBufferPool.Release(cmd);

                        // Execute custom render
                        additionalCameraData.ExecuteCustomRender(renderContext, hdCamera);

                        renderContext.Submit();
                        continue;
                    }

                    // Do anything we need to do upon a new frame.
                    // The NewFrame must be after the VolumeManager update and before Resize because it uses properties set in NewFrame
                    m_LightLoop.NewFrame(currentFrameSettings);

                    Resize(hdCamera);

                    ApplyDebugDisplaySettings(hdCamera, cmd);
                    m_SkyManager.UpdateCurrentSkySettings(hdCamera);

                    ScriptableCullingParameters cullingParams;
                    if (!CullResults.GetCullingParameters(camera, hdCamera.frameSettings.enableStereo, out cullingParams))
                    {
                        renderContext.Submit();
                        continue;
                    }

                    if (camera.useOcclusionCulling)
                        cullingParams.cullingFlags |= CullFlag.OcclusionCull;

                    m_LightLoop.UpdateCullingParameters(ref cullingParams);
                    hdCamera.UpdateStereoDependentState(ref cullingParams);

#if UNITY_EDITOR
                    // emit scene view UI
                    if (camera.cameraType == CameraType.SceneView)
                    {
                        ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                    }
#endif

                    if (hdCamera.frameSettings.enableDecals)
                    {
                        // decal system needs to be updated with current camera, it needs it to set up culling and light list generation parameters
                        DecalSystem.instance.CurrentCamera = camera;
                        DecalSystem.instance.BeginCull();
                    }

                    ReflectionSystem.PrepareCull(camera, m_ReflectionProbeCullResults);

                    using (new ProfilingSample(cmd, "CullResults.Cull", CustomSamplerId.CullResultsCull.GetSampler()))
                    {
                        cullingParams.accurateOcclusionThreshold = 50.0f;
                        CullResults.Cull(ref cullingParams, renderContext, ref m_CullResults);
                    }

                    m_IsDepthBufferCopyValid = false; // this is a new render frame
                    m_ReflectionProbeCullResults.Cull();

                    m_DbufferManager.enableDecals = false;
                    if (hdCamera.frameSettings.enableDecals)
                    {
                        using (new ProfilingSample(cmd, "DBufferPrepareDrawData", CustomSamplerId.DBufferPrepareDrawData.GetSampler()))
                        {
// sample-game begin: adding profilers
                            Profiling.Profiler.BeginSample("DecalSystem.instance.EndCull");
                            DecalSystem.instance.EndCull();
                            Profiling.Profiler.EndSample();

                            m_DbufferManager.enableDecals = true;              // mesh decals are renderers managed by c++ runtime and we have no way to query if any are visible, so set to true
                            Profiling.Profiler.BeginSample("DecalSystem.instance.UpdateCachedMaterialData");
                            DecalSystem.instance.UpdateCachedMaterialData();    // textures, alpha or fade distances could've changed
                            Profiling.Profiler.EndSample();

                            Profiling.Profiler.BeginSample("DecalSystem.instance.CreateDrawData");
                            DecalSystem.instance.CreateDrawData();              // prepare data is separate from draw
                            Profiling.Profiler.EndSample();
// sample-game end

// sample-game begin: commenting out as no transparent decals
                            if(false)   //TODO: if(transparent_decals_enables) or whatever it is going to be called
                            { 
                                Profiling.Profiler.BeginSample("DecalSystem.instance.UpdateTextureAtlas");
                            DecalSystem.instance.UpdateTextureAtlas(cmd);       // as this is only used for transparent pass, would've been nice not to have to do this if no transparent renderers are visible, needs to happen after CreateDrawData
                                Profiling.Profiler.EndSample();
                            }
// sample-game end
                        }
                    }

                    renderContext.SetupCameraProperties(camera, hdCamera.frameSettings.enableStereo);

                    PushGlobalParams(hdCamera, cmd, diffusionProfileSettings);

                    // TODO: Find a correct place to bind these material textures
                    // We have to bind the material specific global parameters in this mode
                    m_MaterialList.ForEach(material => material.Bind());

                    // Frustum cull density volumes on the CPU. Can be performed as soon as the camera is set up.
                    DensityVolumeList densityVolumes = m_VolumetricLightingSystem.PrepareVisibleDensityVolumeList(hdCamera, cmd, m_Time);

                    // Note: Legacy Unity behave like this for ShadowMask
                    // When you select ShadowMask in Lighting panel it recompile shaders on the fly with the SHADOW_MASK keyword.
                    // However there is no C# function that we can query to know what mode have been select in Lighting Panel and it will be wrong anyway. Lighting Panel setup what will be the next bake mode. But until light is bake, it is wrong.
                    // Currently to know if you need shadow mask you need to go through all visible lights (of CullResult), check the LightBakingOutput struct and look at lightmapBakeType/mixedLightingMode. If one light have shadow mask bake mode, then you need shadow mask features (i.e extra Gbuffer).
                    // It mean that when we build a standalone player, if we detect a light with bake shadow mask, we generate all shader variant (with and without shadow mask) and at runtime, when a bake shadow mask light is visible, we dynamically allocate an extra GBuffer and switch the shader.
                    // So the first thing to do is to go through all the light: PrepareLightsForGPU
                    bool enableBakeShadowMask;
                    using (new ProfilingSample(cmd, "TP_PrepareLightsForGPU", CustomSamplerId.TPPrepareLightsForGPU.GetSampler()))
                    {
                        enableBakeShadowMask = m_LightLoop.PrepareLightsForGPU(cmd, hdCamera, m_CullResults, m_ReflectionProbeCullResults, densityVolumes, m_DebugDisplaySettings);
                    }
                    // Configure all the keywords
                    ConfigureKeywords(enableBakeShadowMask, hdCamera, cmd);

                    StartStereoRendering(cmd, renderContext, hdCamera);

                    ClearBuffers(hdCamera, cmd);

                    // TODO: Add stereo occlusion mask
                    RenderDepthPrepass(m_CullResults, hdCamera, renderContext, cmd);

                    // Now that all depths have been rendered, resolve the depth buffer
                    m_SharedRTManager.ResolveSharedRT(cmd, hdCamera);

                    // This will bind the depth buffer if needed for DBuffer)
                    RenderDBuffer(hdCamera, cmd, renderContext, m_CullResults);

                    RenderGBuffer(m_CullResults, hdCamera, renderContext, cmd);

                    // We can now bind the normal buffer to be use by any effect
                    m_SharedRTManager.BindNormalBuffer(cmd);

                    if (!hdCamera.frameSettings.enableMSAA) // MSAA not supported
                    {
                        using (new ProfilingSample(cmd, "DBuffer Normal (forward)", CustomSamplerId.DBufferNormal.GetSampler()))
                        {
                            int stencilMask;
                            int stencilRef;
                            if (hdCamera.frameSettings.enableForwardRenderingOnly) // in forward rendering all pixels that decals wrote into have to be composited
                            {
                                stencilMask = (int)StencilBitMask.Decals;
                                stencilRef = (int)StencilBitMask.Decals;
                            }
                            else // in deferred rendering only pixels affected by both forward materials and decals need to be composited
                            {
                                stencilMask = (int)StencilBitMask.Decals | (int)StencilBitMask.DecalsForwardOutputNormalBuffer;
                                stencilRef = (int)StencilBitMask.Decals | (int)StencilBitMask.DecalsForwardOutputNormalBuffer;
                            }

                            m_DecalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilReadMask, stencilMask);
                            m_DecalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilRef, stencilRef);

                            HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetDepthStencilBuffer());
                            cmd.SetRandomWriteTarget(1, m_SharedRTManager.GetNormalBuffer());
                            cmd.DrawProcedural(Matrix4x4.identity, m_DecalNormalBufferMaterial, 0, MeshTopology.Triangles, 3, 1);
                            cmd.ClearRandomWriteTargets();
                        }
                    }

                    // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
                    GenerateDepthPyramid(hdCamera, cmd, FullScreenDebugMode.DepthPyramid);
                    // Depth texture is now ready, bind it (Depth buffer could have been bind before if DBuffer is enable)
                    cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_SharedRTManager.GetDepthTexture());

                    // TODO: In the future we will render object velocity at the same time as depth prepass (we need C++ modification for this)
                    // Once the C++ change is here we will first render all object without motion vector then motion vector object
                    // We can't currently render object velocity after depth prepass because if there is no depth prepass we can have motion vector write that should have been rejected

                    // If objects velocity if enabled, this will render the rest of objects into the target buffers (in addition to the velocity buffer)
                    RenderObjectsVelocity(m_CullResults, hdCamera, renderContext, cmd);

                    RenderCameraVelocity(m_CullResults, hdCamera, renderContext, cmd);

                    StopStereoRendering(cmd, renderContext, hdCamera);
                    // Caution: We require sun light here as some skies use the sun light to render, it means that UpdateSkyEnvironment must be called after PrepareLightsForGPU.
                    // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
                    UpdateSkyEnvironment(hdCamera, cmd);


                    if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled())
                    {
                        RenderDebugViewMaterial(m_CullResults, hdCamera, renderContext, cmd);

                        PushColorPickerDebugTexture(cmd, m_CameraColorBuffer, hdCamera);
                    }
                    else
                    {
                        StartStereoRendering(cmd, renderContext, hdCamera);

                        // TODO: Everything here (SSAO, Shadow, Build light list, deferred shadow, material and light classification can be parallelize with Async compute)
                        RenderSSAO(cmd, hdCamera, renderContext, postProcessLayer);

                        // Needs the depth pyramid and motion vectors, as well as the render of the previous frame.
                        RenderSSR(hdCamera, cmd);

                        // Clear and copy the stencil texture needs to be moved to before we invoke the async light list build,
                        // otherwise the async compute queue can end up using that texture before the graphics queue is done with it.
                        // TODO: Move this code inside LightLoop
                        if (m_LightLoop.GetFeatureVariantsEnabled())
                        {
                            // For material classification we use compute shader and so can't read into the stencil, so prepare it.
                            using (new ProfilingSample(cmd, "Clear and copy stencil texture", CustomSamplerId.ClearAndCopyStencilTexture.GetSampler()))
                            {
                                HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetStencilBufferCopy(), ClearFlag.Color, CoreUtils.clearColorAllBlack);

                                // In the material classification shader we will simply test is we are no lighting
                                // Use ShaderPassID 1 => "Pass 1 - Write 1 if value different from stencilRef to output"
                                HDUtils.DrawFullScreen(cmd, hdCamera, m_CopyStencilForNoLighting, m_SharedRTManager.GetStencilBufferCopy(), m_SharedRTManager.GetDepthStencilBuffer(), null, 1);
                            }
                        }

                        StopStereoRendering(cmd, renderContext, hdCamera);

#if UNITY_2019_1_OR_NEWER
                        GraphicsFence buildGPULightListsCompleteFence = new GraphicsFence();
#else
                        GPUFence buildGPULightListsCompleteFence = new GPUFence();
#endif
                        if (hdCamera.frameSettings.enableAsyncCompute)
                        {
#if UNITY_2019_1_OR_NEWER
                            GraphicsFence startFence = cmd.CreateAsyncGraphicsFence();
#else
                            GPUFence startFence = cmd.CreateGPUFence();
#endif
                            renderContext.ExecuteCommandBuffer(cmd);
                            cmd.Clear();

                            buildGPULightListsCompleteFence = m_LightLoop.BuildGPULightListsAsyncBegin(hdCamera, renderContext, m_SharedRTManager.GetDepthStencilBuffer(), m_SharedRTManager.GetStencilBufferCopy(), startFence, m_SkyManager.IsLightingSkyValid());
                        }

                        using (new ProfilingSample(cmd, "Render shadows", CustomSamplerId.RenderShadows.GetSampler()))
                        {
                            // This call overwrites camera properties passed to the shader system.
                            m_LightLoop.RenderShadows(renderContext, cmd, m_CullResults);

                            // Overwrite camera properties set during the shadow pass with the original camera properties.
                            renderContext.SetupCameraProperties(camera, hdCamera.frameSettings.enableStereo);
                            hdCamera.SetupGlobalParams(cmd, m_Time, m_LastTime, m_FrameCount);
                        }

                        using (new ProfilingSample(cmd, "Screen space shadows", CustomSamplerId.ScreenSpaceShadows.GetSampler()))
                        {

                            StartStereoRendering(cmd, renderContext, hdCamera);
                            // When debug is enabled we need to clear otherwise we may see non-shadows areas with stale values.
                            if (m_CurrentDebugDisplaySettings.fullScreenDebugMode == FullScreenDebugMode.ContactShadows)
                            {
                                HDUtils.SetRenderTarget(cmd, hdCamera, m_ScreenSpaceShadowsBuffer, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                            }

                            HDUtils.CheckRTCreated(m_ScreenSpaceShadowsBuffer);

                            int firstMipOffsetY = m_SharedRTManager.GetDepthBufferMipChainInfo().mipLevelOffsets[1].y;
                            m_LightLoop.RenderScreenSpaceShadows(hdCamera, m_ScreenSpaceShadowsBuffer, hdCamera.frameSettings.enableMSAA ? m_SharedRTManager.GetDepthValuesTexture() : m_SharedRTManager.GetDepthTexture(), firstMipOffsetY, cmd);

                            PushFullScreenDebugTexture(hdCamera, cmd, m_ScreenSpaceShadowsBuffer, FullScreenDebugMode.ContactShadows);
                            StopStereoRendering(cmd, renderContext, hdCamera);
                        }

                        if (hdCamera.frameSettings.enableAsyncCompute)
                        {
                            m_LightLoop.BuildGPULightListAsyncEnd(hdCamera, cmd, buildGPULightListsCompleteFence);
                        }
                        else
                        {
                            using (new ProfilingSample(cmd, "Build Light list", CustomSamplerId.BuildLightList.GetSampler()))
                            {
                                m_LightLoop.BuildGPULightLists(hdCamera, cmd, m_SharedRTManager.GetDepthStencilBuffer(), m_SharedRTManager.GetStencilBufferCopy(), m_SkyManager.IsLightingSkyValid());
                            }
                        }

                        {
                            // Set fog parameters for volumetric lighting.
                            var visualEnv = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
                            visualEnv.PushFogShaderParameters(hdCamera, cmd);
                        }

                        // Perform the voxelization step which fills the density 3D texture.
                        // Requires the clustered lighting data structure to be built, and can run async.
                        m_VolumetricLightingSystem.VolumeVoxelizationPass(hdCamera, cmd, m_FrameCount, densityVolumes);

                        // Render the volumetric lighting.
                        // The pass requires the volume properties, the light list and the shadows, and can run async.
                        m_VolumetricLightingSystem.VolumetricLightingPass(hdCamera, cmd, m_FrameCount);

						SetMicroShadowingSettings(cmd);

						// Might float this higher if we enable stereo w/ deferred
                        StartStereoRendering(cmd, renderContext, hdCamera);

                        RenderDeferredLighting(hdCamera, cmd);


                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Opaque);

                        m_SharedRTManager.ResolveMSAAColor(cmd, hdCamera, m_CameraSssDiffuseLightingMSAABuffer, m_CameraSssDiffuseLightingBuffer);
                        m_SharedRTManager.ResolveMSAAColor(cmd, hdCamera, m_SSSBufferManager.GetSSSBufferMSAA(0), m_SSSBufferManager.GetSSSBuffer(0));

                        // SSS pass here handle both SSS material from deferred and forward
                        m_SSSBufferManager.SubsurfaceScatteringPass(hdCamera, cmd, diffusionProfileSettings, hdCamera.frameSettings.enableMSAA ? m_CameraColorMSAABuffer : m_CameraColorBuffer,
                            m_CameraSssDiffuseLightingBuffer, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA), m_SharedRTManager.GetDepthTexture());

                        RenderSky(hdCamera, cmd);

                        m_SharedRTManager.ResolveMSAAColor(cmd, hdCamera, m_CameraColorMSAABuffer, m_CameraColorBuffer);

                        RenderTransparentDepthPrepass(m_CullResults, hdCamera, renderContext, cmd);

                        // Render pre refraction objects
                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.PreRefraction);

			// SampleGame Change BEGIN
                        if(DebugLayer3DCallback != null)
                            DebugLayer3DCallback(hdCamera, cmd);
			// SampleGame Change END

                        RenderColorPyramid(hdCamera, cmd, true);

                        // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
                        RenderForward(m_CullResults, hdCamera, renderContext, cmd, ForwardPass.Transparent);

                        // Render All forward error
                        RenderForwardError(m_CullResults, hdCamera, renderContext, cmd);

                        // Fill depth buffer to reduce artifact for transparent object during postprocess
                        RenderTransparentDepthPostpass(m_CullResults, hdCamera, renderContext, cmd);

                        RenderColorPyramid(hdCamera, cmd, false);

                        AccumulateDistortion(m_CullResults, hdCamera, renderContext, cmd);
                        RenderDistortion(hdCamera, cmd);

                        StopStereoRendering(cmd, renderContext, hdCamera);

                        PushFullScreenDebugTexture(hdCamera, cmd, m_CameraColorBuffer, FullScreenDebugMode.NanTracker);
                        PushFullScreenLightingDebugTexture(hdCamera, cmd, m_CameraColorBuffer);
                        PushColorPickerDebugTexture(cmd, m_CameraColorBuffer, hdCamera);

                        // The final pass either postprocess of Blit will flip the screen (as it is reverse by default due to Unity openGL legacy)
                        // Postprocess system (that doesn't use cmd.Blit) handle it with configuration (and do not flip in SceneView) or it is automatically done in Blit

                        StartStereoRendering(cmd, renderContext, hdCamera);


                        // Final blit
                        if (hdCamera.frameSettings.enablePostprocess)
                        {
                            // when we have a group of multiple cameras rendering into the same render target, for every camera but the last of the group, we need to output the result into
                            // the camera color buffer so that the next camera can accumulate over it.
                            RenderPostProcess(hdCamera, cmd, postProcessLayer, !lastCameraFromGroup);
                        }
                        else
                        {
                            using (new ProfilingSample(cmd, "Blit to final RT", CustomSamplerId.BlitToFinalRT.GetSampler()))
                            {
                                // This Blit will flip the screen on anything other than openGL
                                if (srcFrameSettings.enableStereo && (XRGraphicsConfig.eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes))
                                {
                                    cmd.BlitFullscreenTriangle(m_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget); // If double-wide, only blit once (not once per-eye)
                                }
                                else
                                {
                                    HDUtils.BlitCameraTexture(cmd, hdCamera, m_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget);
                                }
                            }
                        }

                        StopStereoRendering(cmd, renderContext, hdCamera);
                        // Pushes to XR headset and/or display mirror
                        if (hdCamera.frameSettings.enableStereo)
                            renderContext.StereoEndRender(hdCamera.camera);
                    }

                    // Copy depth buffer if render texture has one as our depth buffer can be bigger than the one provided and we use our RT handle system.
                    // We need to copy only the corresponding portion
                    // (it's handled automatically by the copy shader because it uses a load in pixel coordinates based on the target).
                    // This copy will also have the effect of re-binding this depth buffer correctly for subsequent editor rendering (This allow to have correct Gizmo/Icons).
                    // TODO: If at some point we get proper render target aliasing, we will be able to use the provided depth texture directly with our RT handle system
                    bool copyDepth = hdCamera.camera.targetTexture != null ? hdCamera.camera.targetTexture.depth != 0 : false;

                    // NOTE: This needs to be done before the call to RenderDebug because debug overlays need to update the depth for the scene view as well.
                    // Make sure RenderDebug does not change the current Render Target
                    if (copyDepth)
                    {
                        using (new ProfilingSample(cmd, "Copy Depth in Target Texture", CustomSamplerId.CopyDepth.GetSampler()))
                        {
                            m_CopyDepth.SetTexture(HDShaderIDs._InputDepth, m_SharedRTManager.GetDepthStencilBuffer());
                            cmd.Blit(null, BuiltinRenderTextureType.CameraTarget, m_CopyDepth);
                        }
                    }

                    // Caution: RenderDebug need to take into account that we have flip the screen (so anything capture before the flip will be flipped)
                    RenderDebug(hdCamera, cmd, m_CullResults);

                    /// SampleGame Change BEGIN
                    if(DebugLayer2DCallback != null)
                        DebugLayer2DCallback(hdCamera, cmd);
                    /// SampleGame Change END

#if UNITY_EDITOR
                    // We need to make sure the viewport is correctly set for the editor rendering. It might have been changed by debug overlay rendering just before.
                    cmd.SetViewport(new Rect(0.0f, 0.0f, hdCamera.actualWidth, hdCamera.actualHeight));
#endif
                }

                // Caution: ExecuteCommandBuffer must be outside of the profiling bracket
                renderContext.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
                renderContext.Submit();

#if UNITY_EDITOR
                UnityEditor.Handles.DrawGizmos(camera);
#endif

            } // For each camera
        }

        void RenderOpaqueRenderList(CullResults cull,
            HDCamera hdCamera,
            ScriptableRenderContext renderContext,
            CommandBuffer cmd,
            ShaderPassName passName,
            RendererConfiguration rendererConfiguration = 0,
            RenderQueueRange? inRenderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null)
        {
            m_SinglePassName[0] = passName;
            RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, m_SinglePassName, rendererConfiguration, inRenderQueueRange, stateBlock, overrideMaterial);
        }

        void RenderOpaqueRenderList(CullResults cull,
            HDCamera hdCamera,
            ScriptableRenderContext renderContext,
            CommandBuffer cmd,
            ShaderPassName[] passNames,
            RendererConfiguration rendererConfiguration = 0,
            RenderQueueRange? inRenderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null,
            bool excludeMotionVector = false
            )
        {
            if (!hdCamera.frameSettings.enableOpaqueObjects)
                return;

            // This is done here because DrawRenderers API lives outside command buffers so we need to make call this before doing any DrawRenders
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var drawSettings = new DrawRendererSettings(hdCamera.camera, HDShaderPassNames.s_EmptyName)
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CanvasOrder | SortFlags.OptimizeStateChanges | SortFlags.RenderQueue | SortFlags.SortingLayer }  // Decals force a depth-prepass so there is no reason to break batches with QuantizedFrontToBack
            };

            for (int i = 0; i < passNames.Length; ++i)
            {
                drawSettings.SetShaderPassName(i, passNames[i]);
            }

            if (overrideMaterial != null)
                drawSettings.SetOverrideMaterial(overrideMaterial, 0);

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = inRenderQueueRange == null ? HDRenderQueue.k_RenderQueue_AllOpaque : inRenderQueueRange.Value,
                excludeMotionVectorObjects = excludeMotionVector
            };

            if (stateBlock == null)
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
            else
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings, stateBlock.Value);
        }

        void RenderTransparentRenderList(CullResults cull,
            HDCamera hdCamera,
            ScriptableRenderContext renderContext,
            CommandBuffer cmd,
            ShaderPassName passName,
            RendererConfiguration rendererConfiguration = 0,
            RenderQueueRange? inRenderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null,
            bool excludeMotionVectorObjects = false
            )
        {
            m_SinglePassName[0] = passName;
            RenderTransparentRenderList(cull, hdCamera, renderContext, cmd, m_SinglePassName,
                rendererConfiguration, inRenderQueueRange, stateBlock, overrideMaterial);
        }

        void RenderTransparentRenderList(CullResults cull,
            HDCamera hdCamera,
            ScriptableRenderContext renderContext,
            CommandBuffer cmd,
            ShaderPassName[] passNames,
            RendererConfiguration rendererConfiguration = 0,
            RenderQueueRange? inRenderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null,
            bool excludeMotionVectorObjects = false
            )
        {
            if (!hdCamera.frameSettings.enableTransparentObjects)
                return;

            // This is done here because DrawRenderers API lives outside command buffers so we need to make call this before doing any DrawRenders
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var drawSettings = new DrawRendererSettings(hdCamera.camera, HDShaderPassNames.s_EmptyName)
            {
                rendererConfiguration = rendererConfiguration,
                sorting = { flags = SortFlags.CommonTransparent | SortFlags.RendererPriority }
            };

            for (int i = 0; i < passNames.Length; ++i)
            {
                drawSettings.SetShaderPassName(i, passNames[i]);
            }

            if (overrideMaterial != null)
                drawSettings.SetOverrideMaterial(overrideMaterial, 0);

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = inRenderQueueRange == null ? HDRenderQueue.k_RenderQueue_AllTransparent : inRenderQueueRange.Value,
                excludeMotionVectorObjects = excludeMotionVectorObjects
            };

            if (stateBlock == null)
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);
            else
                renderContext.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings, stateBlock.Value);
        }

        void AccumulateDistortion(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.enableDistortion)
                return;

            using (new ProfilingSample(cmd, "Distortion", CustomSamplerId.Distortion.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, hdCamera, m_DistortionBuffer, m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, Color.clear);

                // Only transparent object can render distortion vectors
                RenderTransparentRenderList(cullResults, hdCamera, renderContext, cmd, HDShaderPassNames.s_DistortionVectorsName);
            }
        }

        void RenderDistortion(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.enableDistortion)
                return;

            using (new ProfilingSample(cmd, "ApplyDistortion", CustomSamplerId.ApplyDistortion.GetSampler()))
            {
                var size = new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);
                uint x, y, z;
                m_applyDistortionCS.GetKernelThreadGroupSizes(m_applyDistortionKernel, out x, out y, out z);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._DistortionTexture, m_DistortionBuffer);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._ColorPyramidTexture, m_CameraColorBufferMipChain);
                cmd.SetComputeTextureParam(m_applyDistortionCS, m_applyDistortionKernel, HDShaderIDs._Destination, m_CameraColorBuffer);
                cmd.SetComputeVectorParam(m_applyDistortionCS, HDShaderIDs._Size, size);

                cmd.DispatchCompute(m_applyDistortionCS, m_applyDistortionKernel, Mathf.CeilToInt(size.x / x), Mathf.CeilToInt(size.y / y), 1);
            }
        }

        // RenderDepthPrepass render both opaque and opaque alpha tested based on engine configuration.
        // Forward only renderer: We always render everything
        // Deferred renderer: We always render depth prepass for alpha tested (optimization), other object are render based on engine configuration.
        // Forward opaque with deferred renderer (DepthForwardOnly pass): We always render everything
        void RenderDepthPrepass(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // In case of deferred renderer, we can have forward opaque material. These materials need to be render in the depth buffer to correctly build the light list.
            // And they will tag the stencil to not be lit during the deferred lighting pass.

            // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
            // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
            // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.
            // If a forward material have no depth prepass, then lighting can be incorrect (deferred shadowing, SSAO), this may be acceptable depends on usage

            // Whatever the configuration we always render first the opaque object as opaque alpha tested are more costly to render and could be reject by early-z
            // (but not Hi-z as it is disable with clip instruction). This is handled automatically with the RenderQueue value (OpaqueAlphaTested have a different value and thus are sorted after Opaque)

            // Forward material always output normal buffer (unless they don't participate to shading)
            // Deferred material never output normal buffer

            // Note: Unlit object use a ForwardOnly pass and don't have normal, they will write 0 in the normal buffer. This should be safe
            // as they will not use the result of lighting anyway. However take care of effect that will try to filter normal buffer.
            // TODO: maybe we can use a stencil to tag when Forward unlit touch normal buffer

            // Additional guidelines when motion vector are enabled
            // To save drawcall we don't render in prepass the object that have object motion vector. We use the excludeMotion filter option of DrawRenderer to know that (only C++ can know if an object have object motion vector).
            // Thus during this prepass we will exclude all object that have object motion vector, mean that during the velocity pass they will also output normal buffer (like a regular prepass) if needed.
            // Combination of both depth prepass + motion vector pass provide the full depth buffer

            // In order to avoid rendering objects twice (once in the depth pre-pass and once in the motion vector pass, when the motion vector pass is enabled). We exclude the objects that have motion vectors.
            // TODO: Currently disable, require a C++ PR
            bool excludeMotion = false; // hdCamera.frameSettings.enableObjectMotionVectors;

            if (hdCamera.frameSettings.enableForwardRenderingOnly)
            {
                using (new ProfilingSample(cmd, "Depth Prepass (forward)", CustomSamplerId.DepthPrepass.GetSampler()))
                {
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetPrepassBuffersRTI(hdCamera.frameSettings), m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA));

                    XRUtils.DrawOcclusionMesh(cmd, hdCamera.camera, hdCamera.frameSettings.enableStereo);

                    // Full forward: Output normal buffer for both forward and forwardOnly
                    // Exclude object that render velocity (if motion vector are enabled)
                    RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, m_DepthOnlyAndDepthForwardOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque, excludeMotionVector : excludeMotion);
                }
            }
            // If we enable DBuffer, we need a full depth prepass
            else if (hdCamera.frameSettings.enableDepthPrepassWithDeferredRendering || m_DbufferManager.enableDecals)
            {
                using (new ProfilingSample(cmd, m_DbufferManager.enableDecals ? "Depth Prepass (deferred) force by Decals" : "Depth Prepass (deferred)", CustomSamplerId.DepthPrepass.GetSampler()))
                {
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetDepthStencilBuffer());

                    XRUtils.DrawOcclusionMesh(cmd, hdCamera.camera, hdCamera.frameSettings.enableStereo);

                    // First deferred material
                    RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, m_DepthOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque, excludeMotionVector: excludeMotion);

                    HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetPrepassBuffersRTI(hdCamera.frameSettings), m_SharedRTManager.GetDepthStencilBuffer());

                    // Then forward only material that output normal buffer
                    RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, m_DepthForwardOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque, excludeMotionVector: excludeMotion);
                }
            }
            else // Deferred with partial depth prepass
            {
                using (new ProfilingSample(cmd, "Depth Prepass (deferred incomplete)", CustomSamplerId.DepthPrepass.GetSampler()))
                {
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetDepthStencilBuffer());

                    XRUtils.DrawOcclusionMesh(cmd, hdCamera.camera, hdCamera.frameSettings.enableStereo);

                    // First deferred alpha tested materials. Alpha tested object have always a prepass even if enableDepthPrepassWithDeferredRendering is disabled
                    var renderQueueRange = new RenderQueueRange { min = (int)RenderQueue.AlphaTest, max = (int)RenderQueue.GeometryLast - 1 };
                    RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, m_DepthOnlyPassNames, 0, renderQueueRange, excludeMotionVector : excludeMotion);

                    HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetPrepassBuffersRTI(hdCamera.frameSettings), m_SharedRTManager.GetDepthStencilBuffer());

                    // Then forward only material that output normal buffer
                    RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, m_DepthForwardOnlyPassNames, 0, HDRenderQueue.k_RenderQueue_AllOpaque, excludeMotionVector : excludeMotion);
                }
            }
        }

        // RenderGBuffer do the gbuffer pass. This is solely call with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque apha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.enableForwardRenderingOnly)
                return;

            using (new ProfilingSample(cmd, m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ? "GBuffer Debug" : "GBuffer", CustomSamplerId.GBuffer.GetSampler()))
            {
                // setup GBuffer for rendering
                HDUtils.SetRenderTarget(cmd, hdCamera, m_GbufferManager.GetBuffersRTI(hdCamera.frameSettings), m_SharedRTManager.GetDepthStencilBuffer());
                RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, HDShaderPassNames.s_GBufferName, m_currentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_AllOpaque);

                m_GbufferManager.BindBufferAsTextures(cmd);
            }
        }

        void RenderDBuffer(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, CullResults cullResults)
        {
            if (!hdCamera.frameSettings.enableDecals)
                return;

            using (new ProfilingSample(cmd, "DBufferRender", CustomSamplerId.DBufferRender.GetSampler()))
            {
                // We need to copy depth buffer texture if we want to bind it at this stage
                CopyDepthBufferIfNeeded(cmd);

                bool rtCount4 = m_Asset.GetRenderPipelineSettings().decalSettings.perChannelMask;
                // Depth texture is now ready, bind it.
                cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_SharedRTManager.GetDepthTexture());
                m_DbufferManager.ClearAndSetTargets(cmd, hdCamera, rtCount4, m_SharedRTManager.GetDepthStencilBuffer());
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawRendererSettings drawSettings = new DrawRendererSettings(hdCamera.camera, HDShaderPassNames.s_EmptyName)
                {
                    rendererConfiguration = 0,
                    sorting = { flags = SortFlags.CommonOpaque }
                };

                if (rtCount4)
                {
                    drawSettings.SetShaderPassName(0, HDShaderPassNames.s_MeshDecalsMName);
                    drawSettings.SetShaderPassName(1, HDShaderPassNames.s_MeshDecalsAOName);
                    drawSettings.SetShaderPassName(2, HDShaderPassNames.s_MeshDecalsMAOName);
                    drawSettings.SetShaderPassName(3, HDShaderPassNames.s_MeshDecalsSName);
                    drawSettings.SetShaderPassName(4, HDShaderPassNames.s_MeshDecalsMSName);
                    drawSettings.SetShaderPassName(5, HDShaderPassNames.s_MeshDecalsAOSName);
                    drawSettings.SetShaderPassName(6, HDShaderPassNames.s_MeshDecalsMAOSName);
                }
                else
                {
                    drawSettings.SetShaderPassName(0, HDShaderPassNames.s_MeshDecals3RTName);
                }

                FilterRenderersSettings filterRenderersSettings = new FilterRenderersSettings(true)
                {
                    renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque
                };

                renderContext.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterRenderersSettings);
                DecalSystem.instance.RenderIntoDBuffer(cmd);
                m_DbufferManager.UnSetHTile(cmd);
                m_DbufferManager.SetHTileTexture(cmd);  // mask per 8x8 tile used for optimization when looking up dbuffer values
            }
        }

        void RenderDebugViewMaterial(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "DisplayDebug ViewMaterial", CustomSamplerId.DisplayDebugViewMaterial.GetSampler()))
            {
                if (m_CurrentDebugDisplaySettings.materialDebugSettings.IsDebugGBufferEnabled() && !hdCamera.frameSettings.enableForwardRenderingOnly)
                {
                    using (new ProfilingSample(cmd, "DebugViewMaterialGBuffer", CustomSamplerId.DebugViewMaterialGBuffer.GetSampler()))
                    {
                        HDUtils.DrawFullScreen(cmd, hdCamera, m_currentDebugViewMaterialGBuffer, m_CameraColorBuffer);
                    }
                }
                else
                {
                    // When rendering debug material we shouldn't rely on a depth prepass for optimizing the alpha clip test. As it is control on the material inspector side
                    // we must override the state here.

                    HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.All, CoreUtils.clearColorAllBlack);
                    // Render Opaque forward
                    RenderOpaqueRenderList(cull, hdCamera, renderContext, cmd, m_AllForwardOpaquePassNames, m_currentRendererConfigurationBakedLighting, stateBlock: m_DepthStateOpaque);

                    // Render forward transparent
                    RenderTransparentRenderList(cull, hdCamera, renderContext, cmd, m_AllTransparentPassNames, m_currentRendererConfigurationBakedLighting, stateBlock: m_DepthStateOpaque);
                }
            }

            // Last blit
            {
                using (new ProfilingSample(cmd, "Blit DebugView Material Debug", CustomSamplerId.BlitDebugViewMaterialDebug.GetSampler()))
                {
                    // This Blit will flip the screen anything other than openGL
                    HDUtils.BlitCameraTexture(cmd, hdCamera, m_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget);
                }
            }
        }

        void RenderSSAO(CommandBuffer cmd, HDCamera hdCamera, ScriptableRenderContext renderContext, PostProcessLayer postProcessLayer)
        {
            var camera = hdCamera.camera;

            // Apply SSAO from PostProcessLayer
            if (hdCamera.frameSettings.enableSSAO && postProcessLayer != null && postProcessLayer.enabled)
            {
                var settings = postProcessLayer.GetSettings<AmbientOcclusion>();

                if (settings.IsEnabledAndSupported(null))
                {
                    using (new ProfilingSample(cmd, "Render SSAO", CustomSamplerId.RenderSSAO.GetSampler()))
                    {
                        // In case we are in an MSAA frame, we need to feed both min and max depth of the pixel so that we compute ao values for both depths and resolve the AO afterwards
                        var aoTarget = hdCamera.frameSettings.enableMSAA ? m_MultiAmbientOcclusionBuffer : m_AmbientOcclusionBuffer;
                        var depthTexture = hdCamera.frameSettings.enableMSAA ? m_SharedRTManager.GetDepthValuesTexture() : m_SharedRTManager.GetDepthTexture();

                        HDUtils.CheckRTCreated(aoTarget.rt);
                        postProcessLayer.BakeMSVOMap(cmd, camera, aoTarget, depthTexture, true, hdCamera.frameSettings.enableMSAA);
                    }

                    if (hdCamera.frameSettings.enableMSAA)
                    {
                        using (new ProfilingSample(cmd, "Resolve AO Buffer", CustomSamplerId.BlitDebugViewMaterialDebug.GetSampler()))
                        {
                            HDUtils.SetRenderTarget(cmd, hdCamera, m_AmbientOcclusionBuffer);
                            m_AOPropertyBlock.SetTexture("_DepthValuesTexture", m_SharedRTManager.GetDepthValuesTexture());
                            m_AOPropertyBlock.SetTexture("_MultiAOTexture", m_MultiAmbientOcclusionBuffer);
                            cmd.DrawProcedural(Matrix4x4.identity, m_AOResolveMaterial, 0, MeshTopology.Triangles, 3, 1, m_AOPropertyBlock);
                        }
                    }

                    cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, m_AmbientOcclusionBuffer);
                    cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(settings.color.value.r, settings.color.value.g, settings.color.value.b, settings.directLightingStrength.value));
                    PushFullScreenDebugTexture(hdCamera, cmd, m_AmbientOcclusionBuffer, FullScreenDebugMode.SSAO);

                    return;
                }
            }

            // No AO applied - neutral is black, see the comment in the shaders
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, RuntimeUtilities.blackTexture);
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
        }

        void RenderDeferredLighting(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.enableForwardRenderingOnly)
                return;

            m_MRTCache2[0] = m_CameraColorBuffer;
            m_MRTCache2[1] = m_CameraSssDiffuseLightingBuffer;
            var depthTexture = m_SharedRTManager.GetDepthTexture();

            var options = new LightLoop.LightingPassOptions();

            if (hdCamera.frameSettings.enableSubsurfaceScattering)
            {
                // Output split lighting for materials asking for it (masked in the stencil buffer)
                options.outputSplitLighting = true;

                m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_SharedRTManager.GetDepthStencilBuffer(), depthTexture, options);
            }

            // Output combined lighting for all the other materials.
            options.outputSplitLighting = false;

            m_LightLoop.RenderDeferredLighting(hdCamera, cmd, m_CurrentDebugDisplaySettings, m_MRTCache2, m_SharedRTManager.GetDepthStencilBuffer(), depthTexture, options);
        }

        void UpdateSkyEnvironment(HDCamera hdCamera, CommandBuffer cmd)
        {
            m_SkyManager.UpdateEnvironment(hdCamera, m_LightLoop.GetCurrentSunLight(), cmd);
        }

        void RenderSky(HDCamera hdCamera, CommandBuffer cmd)
        {
            var colorBuffer = hdCamera.frameSettings.enableMSAA ? m_CameraColorMSAABuffer : m_CameraColorBuffer;
            var depthBuffer = m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA);

            var visualEnv = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            m_SkyManager.RenderSky(hdCamera, m_LightLoop.GetCurrentSunLight(), colorBuffer, depthBuffer, m_CurrentDebugDisplaySettings, cmd);

            if (visualEnv.fogType.value != FogType.None)
                m_SkyManager.RenderOpaqueAtmosphericScattering(cmd, colorBuffer, depthBuffer, hdCamera.frameSettings.enableMSAA);
        }

        public Texture2D ExportSkyToTexture()
        {
            return m_SkyManager.ExportSkyToTexture();
        }

        // Render forward is use for both transparent and opaque objects. In case of deferred we can still render opaque object in forward.
        void RenderForward(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd, ForwardPass pass)
        {
            // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
            // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
            // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.
            // The RenderForward pass will render the appropriate pass depends on the engine settings. In case of forward only rendering, both "Forward" pass and "ForwardOnly" pass
            // material will be render for both transparent and opaque. In case of deferred, both path are used for transparent but only "ForwardOnly" is use for opaque.
            // (Thus why "Forward" and "ForwardOnly" are exclusive, else they will render two times"

            if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled())
            {
                m_ForwardPassProfileName = k_ForwardPassDebugName[(int)pass];
            }
            else
            {
                m_ForwardPassProfileName = k_ForwardPassName[(int)pass];
            }

            using (new ProfilingSample(cmd, m_ForwardPassProfileName, CustomSamplerId.ForwardPassName.GetSampler()))
            {
                var camera = hdCamera.camera;

                m_LightLoop.RenderForward(camera, cmd, pass == ForwardPass.Opaque);

                if (pass == ForwardPass.Opaque)
                {
                    // In case of forward SSS we will bind all the required target. It is up to the shader to write into it or not.
                    if (hdCamera.frameSettings.enableSubsurfaceScattering)
                    {
                        if(hdCamera.frameSettings.enableMSAA)
                        {
                            m_MRTWithSSS[0] = m_CameraColorMSAABuffer; // Store the specular color
                            m_MRTWithSSS[1] = m_CameraSssDiffuseLightingMSAABuffer;
                            for (int i = 0; i < m_SSSBufferManager.sssBufferCount; ++i)
                            {
                                m_MRTWithSSS[i + 2] = m_SSSBufferManager.GetSSSBufferMSAA(i);
                            }
                        }
                        else
                        {
                            m_MRTWithSSS[0] = m_CameraColorBuffer; // Store the specular color
                            m_MRTWithSSS[1] = m_CameraSssDiffuseLightingBuffer;
                            for (int i = 0; i < m_SSSBufferManager.sssBufferCount; ++i)
                            {
                                m_MRTWithSSS[i + 2] = m_SSSBufferManager.GetSSSBuffer(i);
                            }
                        }
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_MRTWithSSS, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA));
                    }
                    else
                    {
                        HDUtils.SetRenderTarget(cmd, hdCamera, hdCamera.frameSettings.enableMSAA ? m_CameraColorMSAABuffer : m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA));
                    }

                    var passNames = hdCamera.frameSettings.enableForwardRenderingOnly
                        ? m_ForwardAndForwardOnlyPassNames
                        : m_ForwardOnlyPassNames;
                    RenderOpaqueRenderList(cullResults, hdCamera, renderContext, cmd, passNames, m_currentRendererConfigurationBakedLighting);
                }
                else
                {
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer());
                    if ((hdCamera.frameSettings.enableDecals) && (DecalSystem.m_DecalDatasCount > 0)) // enable d-buffer flag value is being interpreted more like enable decals in general now that we have clustered
                                                                                                      // decal datas count is 0 if no decals affect transparency
                    {
                        DecalSystem.instance.SetAtlas(cmd); // for clustered decals
                    }

                    RenderTransparentRenderList(cullResults, hdCamera, renderContext, cmd, m_AllTransparentPassNames, m_currentRendererConfigurationBakedLighting, pass == ForwardPass.PreRefraction ? HDRenderQueue.k_RenderQueue_PreRefraction : HDRenderQueue.k_RenderQueue_Transparent);
                }
            }
        }

        // This is use to Display legacy shader with an error shader
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void RenderForwardError(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Render Forward Error", CustomSamplerId.RenderForwardError.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, hdCamera, m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer());
                RenderOpaqueRenderList(cullResults, hdCamera, renderContext, cmd, m_ForwardErrorPassNames, 0, RenderQueueRange.all, null, m_ErrorMaterial);
            }
        }

        void RenderTransparentDepthPrepass(CullResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.enableTransparentPrepass)
            {
                // Render transparent depth prepass after opaque one
                using (new ProfilingSample(cmd, "Transparent Depth Prepass", CustomSamplerId.TransparentDepthPrepass.GetSampler()))
                {
                    RenderTransparentRenderList(cull, hdCamera, renderContext, cmd, m_TransparentDepthPrepassNames);
                }
            }
        }

        void RenderTransparentDepthPostpass(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.enableTransparentPostpass)
                return;

            using (new ProfilingSample(cmd, "Render Transparent Depth Post ", CustomSamplerId.TransparentDepthPostpass.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetDepthStencilBuffer());
                RenderTransparentRenderList(cullResults, hdCamera, renderContext, cmd, m_TransparentDepthPostpassNames);
            }
        }

        void RenderObjectsVelocity(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.enableObjectMotionVectors)
                return;

            using (new ProfilingSample(cmd, "Objects Velocity", CustomSamplerId.ObjectsVelocity.GetSampler()))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetVelocityPassBuffersRTI(hdCamera.frameSettings), m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA));
                RenderOpaqueRenderList(cullResults, hdCamera, renderContext, cmd, HDShaderPassNames.s_MotionVectorsName, RendererConfiguration.PerObjectMotionVectors);
            }
        }

        void RenderCameraVelocity(CullResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.enableMotionVectors)
                return;

            using (new ProfilingSample(cmd, "Camera Velocity", CustomSamplerId.CameraVelocity.GetSampler()))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                HDUtils.DrawFullScreen(cmd, hdCamera, m_CameraMotionVectorsMaterial, m_SharedRTManager.GetVelocityBuffer(), m_SharedRTManager.GetDepthStencilBuffer(), null, 0);
                PushFullScreenDebugTexture(hdCamera, cmd, m_SharedRTManager.GetVelocityBuffer(), FullScreenDebugMode.MotionVectors);

#if UNITY_EDITOR

                // In scene view there is no motion vector, so we clear the RT to black
                if (hdCamera.camera.cameraType == CameraType.SceneView)
                {
                    HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetVelocityBuffer(), m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                }
#endif
            }
        }

        void RenderSSR(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.enableSSR)
                return;

            var cs = m_ScreenSpaceReflectionsCS;

            using (new ProfilingSample(cmd, "SSR - Tracing", CustomSamplerId.SsrTracing.GetSampler()))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();

                if (!volumeSettings) volumeSettings = ScreenSpaceReflection.@default;

                int kernel = m_SsrTracingKernel;

                int   w = hdCamera.actualWidth;
                int   h = hdCamera.actualHeight;
                float n = hdCamera.camera.nearClipPlane;
                float f = hdCamera.camera.farClipPlane;

                float thickness      = volumeSettings.depthBufferThickness;
                float thicknessScale = 1.0f / (1.0f + thickness);
                float thicknessBias  = -n / (f - n) * (thickness * thicknessScale);


                HDUtils.PackedMipChainInfo info = m_SharedRTManager.GetDepthBufferMipChainInfo();

                float roughnessFadeStart             = 1 - volumeSettings.smoothnessFadeStart;
                float roughnessFadeEnd               = 1 - volumeSettings.minSmoothness;
                float roughnessFadeLength            = roughnessFadeEnd - roughnessFadeStart;
                float roughnessFadeEndTimesRcpLength = (roughnessFadeLength != 0) ? (roughnessFadeEnd * (1.0f / roughnessFadeLength)) : 1;
                float roughnessFadeRcpLength         = (roughnessFadeLength != 0) ? (1.0f / roughnessFadeLength) : 0;
                float edgeFadeRcpLength              = Mathf.Min(1.0f / volumeSettings.screenWeightDistance, float.MaxValue);

                cmd.SetComputeIntParam(  cs, HDShaderIDs._SsrIterLimit,                      volumeSettings.rayMaxIterations);
                cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrThicknessScale,                 thicknessScale);
                cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrThicknessBias,                  thicknessBias);
                cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrRoughnessFadeEnd,               roughnessFadeEnd);
                cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrRoughnessFadeRcpLength,         roughnessFadeRcpLength);
                cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrRoughnessFadeEndTimesRcpLength, roughnessFadeEndTimesRcpLength);
                cmd.SetComputeIntParam(  cs, HDShaderIDs._SsrDepthPyramidMaxMip,             info.mipLevelCount);
                cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrEdgeFadeRcpLength,              edgeFadeRcpLength);

                // cmd.SetComputeTextureParam(cs, kernel, "_SsrDebugTexture",    m_SsrDebugTexture);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrHitPointTexture, m_SsrHitPointTexture);

                cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._SsrDepthPyramidMipOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));

                cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(hdCamera.actualWidth, 8), HDUtils.DivRoundUp(hdCamera.actualHeight, 8), 1);
            }

            using (new ProfilingSample(cmd, "SSR - Reprojection", CustomSamplerId.SsrReprojection.GetSampler()))
            {
                int kernel = m_SsrReprojectionKernel;

                // cmd.SetComputeTextureParam(cs, kernel, "_SsrDebugTexture",    m_SsrDebugTexture);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VelocityTexture,      m_SharedRTManager.GetVelocityBuffer());
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrHitPointTexture,   m_SsrHitPointTexture);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrLightingTextureRW, m_SsrLightingTexture);
                if (hdCamera.colorPyramidIsValid)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ColorPyramidTexture, m_CameraColorBufferMipChain);
                }
                else
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ColorPyramidTexture, RuntimeUtilities.whiteTexture);
                }

                cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(hdCamera.actualWidth, 8), HDUtils.DivRoundUp(hdCamera.actualHeight, 8), 1);
            }
        }

        void RenderColorPyramid(HDCamera hdCamera, CommandBuffer cmd, bool isPreRefraction)
        {
            if (isPreRefraction)
            {
                if (!hdCamera.frameSettings.enableRoughRefraction)
                    return;
            }
            else
            {
                // TODO: This final Gaussian pyramid can be reuse by Bloom and SSR in the future, so disable it only if there is no postprocess AND no distortion
                if (!hdCamera.frameSettings.enableDistortion && !hdCamera.frameSettings.enablePostprocess && !hdCamera.frameSettings.enableSSR)
                    return;
            }

            int lodCount;

            // GC.Alloc
            // String.Format
            using (new ProfilingSample(cmd, "Color Gaussian MIP Chain", CustomSamplerId.ColorPyramid))
            {
                m_PyramidSizeV2I.Set(hdCamera.actualWidth, hdCamera.actualHeight);
                lodCount = m_MipGenerator.RenderColorGaussianPyramid(cmd, m_PyramidSizeV2I, m_CameraColorBuffer, m_CameraColorBufferMipChain);
            }

            float scaleX = hdCamera.actualWidth / (float)m_CameraColorBufferMipChain.rt.width;
            float scaleY = hdCamera.actualHeight / (float)m_CameraColorBufferMipChain.rt.height;
            cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, m_CameraColorBufferMipChain);
            m_PyramidSizeV4F.Set(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);
            m_PyramidScaleLod.Set(scaleX, scaleY, lodCount, 0.0f);
            m_PyramidScale.Set(scaleX, scaleY, 0f, 0f);
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidSize, m_PyramidSizeV4F);
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidScale, m_PyramidScaleLod);
            PushFullScreenDebugTextureMip(hdCamera, cmd, m_CameraColorBufferMipChain, lodCount, m_PyramidScale, isPreRefraction ? FullScreenDebugMode.PreRefractionColorPyramid : FullScreenDebugMode.FinalColorPyramid);
            hdCamera.colorPyramidIsValid = true;
        }

        void GenerateDepthPyramid(HDCamera hdCamera, CommandBuffer cmd, FullScreenDebugMode debugMode)
        {
            CopyDepthBufferIfNeeded(cmd);

            int mipCount = m_SharedRTManager.GetDepthBufferMipChainInfo().mipLevelCount;

            // GC.Alloc
            // String.Format
            using (new ProfilingSample(cmd, "Generate Depth Buffer MIP Chain", CustomSamplerId.DepthPyramid))
            {
                m_MipGenerator.RenderMinDepthPyramid(cmd, m_SharedRTManager.GetDepthTexture(), m_SharedRTManager.GetDepthBufferMipChainInfo());
            }

            float scaleX = hdCamera.actualWidth / (float)m_SharedRTManager.GetDepthTexture().rt.width;
            float scaleY = hdCamera.actualHeight / (float)m_SharedRTManager.GetDepthTexture().rt.height;
            cmd.SetGlobalTexture(HDShaderIDs._DepthPyramidTexture, m_SharedRTManager.GetDepthTexture());
            m_PyramidSizeV4F.Set(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);
            m_PyramidScaleLod.Set(scaleX, scaleY, mipCount, 0.0f);
            m_PyramidScale.Set(scaleX, scaleY, 0f, 0f);
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidSize, m_PyramidSizeV4F);
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidScale, m_PyramidScaleLod);
            PushFullScreenDebugTextureMip(hdCamera, cmd, m_SharedRTManager.GetDepthTexture(), mipCount, m_PyramidScale, debugMode);
        }

        void RenderPostProcess(HDCamera hdcamera, CommandBuffer cmd, PostProcessLayer layer, bool needOutputToColorBuffer)
        {
            using (new ProfilingSample(cmd, "Post-processing", CustomSamplerId.PostProcessing.GetSampler()))
            {
                RenderTargetIdentifier source = m_CameraColorBuffer;

                // For console we are not allowed to resize the windows, so don't use our hack.
                // We also don't do the copy if viewport size and render texture size match.
                bool viewportAndRTSameSize = (hdcamera.actualWidth == m_CameraColorBuffer.rt.width && hdcamera.actualHeight == m_CameraColorBuffer.rt.height);
                bool tempHACK = !m_SharedRTManager.IsConsolePlatform() && !viewportAndRTSameSize;

                if (tempHACK)
                {
                    // TEMPORARY:
                    // Since we don't render to the full render textures, we need to feed the post processing stack with the right scale/bias.
                    // This feature not being implemented yet, we'll just copy the relevant buffers into an appropriately sized RT.
                    cmd.ReleaseTemporaryRT(HDShaderIDs._CameraDepthTexture);
                    cmd.ReleaseTemporaryRT(HDShaderIDs._CameraMotionVectorsTexture);
                    cmd.ReleaseTemporaryRT(HDShaderIDs._CameraColorTexture);

                    cmd.GetTemporaryRT(HDShaderIDs._CameraDepthTexture, hdcamera.actualWidth, hdcamera.actualHeight, m_SharedRTManager.GetDepthStencilBuffer().rt.depth, FilterMode.Point, m_SharedRTManager.GetDepthStencilBuffer().rt.format);
                    m_CopyDepth.SetTexture(HDShaderIDs._InputDepth, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.Blit(null, HDShaderIDs._CameraDepthTexture, m_CopyDepth);
                    if (m_SharedRTManager.GetVelocityBuffer() != null)
                    {
                        cmd.GetTemporaryRT(HDShaderIDs._CameraMotionVectorsTexture, hdcamera.actualWidth, hdcamera.actualHeight, 0, FilterMode.Point, m_SharedRTManager.GetVelocityBuffer().rt.format);
                        HDUtils.BlitCameraTexture(cmd, hdcamera, m_SharedRTManager.GetVelocityBuffer(), HDShaderIDs._CameraMotionVectorsTexture);
                    }
                    cmd.GetTemporaryRT(HDShaderIDs._CameraColorTexture, hdcamera.actualWidth, hdcamera.actualHeight, 0, FilterMode.Point, m_CameraColorBuffer.rt.format);
                    HDUtils.BlitCameraTexture(cmd, hdcamera, m_CameraColorBuffer, HDShaderIDs._CameraColorTexture);
                    source = HDShaderIDs._CameraColorTexture;

                    // When we want to output to color buffer, we have to allocate a temp RT of the right size because post processes don't support output viewport.
                    // We'll then copy the result into the camera color buffer.
                    if (needOutputToColorBuffer)
                    {
                        cmd.ReleaseTemporaryRT(_TempPostProcessOutputTexture);
                        cmd.GetTemporaryRT(_TempPostProcessOutputTexture, hdcamera.actualWidth, hdcamera.actualHeight, 0, FilterMode.Point, m_CameraColorBuffer.rt.format);
                }
                }
                else
                {
                    // Note: Here we don't use GetDepthTexture() to get the depth texture but m_CameraDepthStencilBuffer as the Forward transparent pass can
                    // write extra data to deal with DOF/MB
                    cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, m_SharedRTManager.GetVelocityBuffer());
                }

                RenderTargetIdentifier dest = BuiltinRenderTextureType.CameraTarget;
                if (needOutputToColorBuffer)
                {
                    dest = tempHACK ? _TempPostProcessOutputTextureID : m_CameraColorBuffer;
                }

                var context = hdcamera.postprocessRenderContext;
                context.Reset();
                context.source = source;
                context.destination = dest;
                context.command = cmd;
                context.camera = hdcamera.camera;
                context.sourceFormat = RenderTextureFormat.ARGBHalf;
                context.flip = (hdcamera.camera.targetTexture == null) && (!hdcamera.camera.stereoEnabled) && !needOutputToColorBuffer;

                layer.Render(context);

                if (needOutputToColorBuffer && tempHACK)
                {
                    HDUtils.BlitCameraTexture(cmd, hdcamera, _TempPostProcessOutputTextureID, m_CameraColorBuffer);
                }
            }
        }

        public void ApplyDebugDisplaySettings(HDCamera hdCamera, CommandBuffer cmd)
        {
            // See ShaderPassForward.hlsl: for forward shaders, if DEBUG_DISPLAY is enabled and no DebugLightingMode or DebugMipMapMod
            // modes have been set, lighting is automatically skipped (To avoid some crashed due to lighting RT not set on console).
            // However debug mode like colorPickerModes and false color don't need DEBUG_DISPLAY and must work with the lighting.
            // So we will enabled DEBUG_DISPLAY independently

            // Enable globally the keyword DEBUG_DISPLAY on shader that support it with multi-compile
            CoreUtils.SetKeyword(cmd, "DEBUG_DISPLAY", m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled());

            if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ||
                m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
            {
                // This is for texture streaming
                m_CurrentDebugDisplaySettings.UpdateMaterials();

                var lightingDebugSettings = m_CurrentDebugDisplaySettings.lightingDebugSettings;
                var debugAlbedo = new Vector4(lightingDebugSettings.overrideAlbedo ? 1.0f : 0.0f, lightingDebugSettings.overrideAlbedoValue.r, lightingDebugSettings.overrideAlbedoValue.g, lightingDebugSettings.overrideAlbedoValue.b);
                var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);
                var debugNormal = new Vector4(lightingDebugSettings.overrideNormal ? 1.0f : 0.0f, 0.0f, 0.0f, 0.0f);
                var debugSpecularColor = new Vector4(lightingDebugSettings.overrideSpecularColor ? 1.0f : 0.0f, lightingDebugSettings.overrideSpecularColorValue.r, lightingDebugSettings.overrideSpecularColorValue.g, lightingDebugSettings.overrideSpecularColorValue.b);

                cmd.SetGlobalInt(HDShaderIDs._DebugViewMaterial, (int)m_CurrentDebugDisplaySettings.GetDebugMaterialIndex());
                cmd.SetGlobalInt(HDShaderIDs._DebugLightingMode, (int)m_CurrentDebugDisplaySettings.GetDebugLightingMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugShadowMapMode, (int)m_CurrentDebugDisplaySettings.GetDebugShadowMapMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugMipMapMode, (int)m_CurrentDebugDisplaySettings.GetDebugMipMapMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugMipMapModeTerrainTexture, (int)m_CurrentDebugDisplaySettings.GetDebugMipMapModeTerrainTexture());
                cmd.SetGlobalInt(HDShaderIDs._ColorPickerMode, (int)m_CurrentDebugDisplaySettings.GetDebugColorPickerMode());

                cmd.SetGlobalVector(HDShaderIDs._DebugLightingAlbedo, debugAlbedo);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingSmoothness, debugSmoothness);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingNormal, debugNormal);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingSpecularColor, debugSpecularColor);

                cmd.SetGlobalVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                cmd.SetGlobalVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                cmd.SetGlobalTexture(HDShaderIDs._DebugFont, m_Asset.renderPipelineResources.textures.debugFontTex);

                // The DebugNeedsExposure test allows us to set a neutral value if exposure is not needed. This way we don't need to make various tests inside shaders but only in this function.
                cmd.SetGlobalFloat(HDShaderIDs._DebugExposure, m_CurrentDebugDisplaySettings.DebugNeedsExposure() ? lightingDebugSettings.debugExposure : 0.0f);
            }
        }

        public void PushColorPickerDebugTexture(CommandBuffer cmd, RTHandleSystem.RTHandle textureID, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None || m_DebugDisplaySettings.falseColorDebugSettings.falseColor || m_DebugDisplaySettings.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuminanceMeter)
            {
                using (new ProfilingSample(cmd, "Push To Color Picker"))
                {
                    HDUtils.BlitCameraTexture(cmd, hdCamera, textureID, m_DebugColorPickerBuffer);
                }
            }
        }

        // TODO TEMP: Not sure I want to keep this special case. Gotta see how to get rid of it (not sure it will work correctly for non-full viewports.
        public void PushColorPickerDebugTexture(HDCamera hdCamera, CommandBuffer cmd, RenderTargetIdentifier textureID)
        {
            if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None || m_DebugDisplaySettings.falseColorDebugSettings.falseColor || m_DebugDisplaySettings.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuminanceMeter)
            {
                using (new ProfilingSample(cmd, "Push To Color Picker"))
                {
                    HDUtils.BlitCameraTexture(cmd, hdCamera, textureID, m_DebugColorPickerBuffer);
                }
            }
        }

        bool NeedsFullScreenDebugMode()
        {
            bool fullScreenDebugEnabled = m_CurrentDebugDisplaySettings.fullScreenDebugMode != FullScreenDebugMode.None;
            bool lightingDebugEnabled = m_CurrentDebugDisplaySettings.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow;

            return fullScreenDebugEnabled || lightingDebugEnabled;
        }

        public void PushFullScreenLightingDebugTexture(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle textureID)
        {
            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed == false)
            {
                m_FullScreenDebugPushed = true;
                HDUtils.BlitCameraTexture(cmd, hdCamera, textureID, m_DebugFullScreenTempBuffer);
            }
        }

        public void PushFullScreenDebugTexture(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle textureID, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
                HDUtils.BlitCameraTexture(cmd, hdCamera, textureID, m_DebugFullScreenTempBuffer);
            }
        }

        void PushFullScreenDebugTextureMip(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle texture, int lodCount, Vector4 scaleBias, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.fullscreenDebugMip * (lodCount));

                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
                HDUtils.BlitCameraTexture(cmd, hdCamera, texture, m_DebugFullScreenTempBuffer, scaleBias, mipIndex);
            }
        }

        void RenderDebug(HDCamera hdCamera, CommandBuffer cmd, CullResults cullResults)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return;

            using (new ProfilingSample(cmd, "Render Debug", CustomSamplerId.RenderDebug.GetSampler()))
            {
                // First render full screen debug texture
                if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed)
                {
                    m_FullScreenDebugPushed = false;
                    cmd.SetGlobalTexture(HDShaderIDs._DebugFullScreenTexture, m_DebugFullScreenTempBuffer);
                    // TODO: Replace with command buffer call when available
                    m_DebugFullScreen.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)m_CurrentDebugDisplaySettings.fullScreenDebugMode);
                    // Everything we have capture is flipped (as it happen before FinalPass/postprocess/Blit. So if we are not in SceneView
                    // (i.e. we have perform a flip, we need to flip the input texture)
                    HDUtils.DrawFullScreen(cmd, hdCamera, m_DebugFullScreen, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);

                    PushColorPickerDebugTexture(hdCamera, cmd, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);
                }

                // Then overlays
                float x = 0;
                float overlayRatio = m_CurrentDebugDisplaySettings.debugOverlayRatio;
                float overlaySize = Math.Min(hdCamera.actualHeight, hdCamera.actualWidth) * overlayRatio;
                float y = hdCamera.actualHeight - overlaySize;

                var lightingDebug = m_CurrentDebugDisplaySettings.lightingDebugSettings;

                if (lightingDebug.displaySkyReflection)
                {
                    var skyReflection = m_SkyManager.skyReflection;
                    m_SharedPropertyBlock.SetTexture(HDShaderIDs._InputCubemap, skyReflection);
                    m_SharedPropertyBlock.SetFloat(HDShaderIDs._Mipmap, lightingDebug.skyReflectionMipmap);
                    m_SharedPropertyBlock.SetFloat(HDShaderIDs._DebugExposure, lightingDebug.debugExposure);
                    cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                    cmd.DrawProcedural(Matrix4x4.identity, m_DebugDisplayLatlong, 0, MeshTopology.Triangles, 3, 1, m_SharedPropertyBlock);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                }

                m_LightLoop.RenderDebugOverlay(hdCamera, cmd, m_CurrentDebugDisplaySettings, ref x, ref y, overlaySize, hdCamera.actualWidth, cullResults);

                DecalSystem.instance.RenderDebugOverlay(hdCamera, cmd, m_CurrentDebugDisplaySettings, ref x, ref y, overlaySize, hdCamera.actualWidth);

                if (m_CurrentDebugDisplaySettings.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None || m_CurrentDebugDisplaySettings.falseColorDebugSettings.falseColor || m_CurrentDebugDisplaySettings.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuminanceMeter)
                {
                    ColorPickerDebugSettings colorPickerDebugSettings = m_CurrentDebugDisplaySettings.colorPickerDebugSettings;
                    FalseColorDebugSettings falseColorDebugSettings = m_CurrentDebugDisplaySettings.falseColorDebugSettings;
                    var falseColorThresholds = new Vector4(falseColorDebugSettings.colorThreshold0, falseColorDebugSettings.colorThreshold1, falseColorDebugSettings.colorThreshold2, falseColorDebugSettings.colorThreshold3);

                    // Here we have three cases:
                    // - Material debug is enabled, this is the buffer we display
                    // - Otherwise we display the HDR buffer before postprocess and distortion
                    // - If fullscreen debug is enabled we always use it

                    cmd.SetGlobalTexture(HDShaderIDs._DebugColorPickerTexture, m_DebugColorPickerBuffer); // No SetTexture with RenderTarget identifier... so use SetGlobalTexture
                    // TODO: Replace with command buffer call when available
                    m_DebugColorPicker.SetColor(HDShaderIDs._ColorPickerFontColor, colorPickerDebugSettings.fontColor);
                    m_DebugColorPicker.SetInt(HDShaderIDs._FalseColorEnabled, falseColorDebugSettings.falseColor ? 1 : 0);
                    m_DebugColorPicker.SetVector(HDShaderIDs._FalseColorThresholds, falseColorThresholds);
                    // The material display debug perform sRGBToLinear conversion as the final blit currently hardcode a linearToSrgb conversion. As when we read with color picker this is not done,
                    // we perform it inside the color picker shader. But we shouldn't do it for HDR buffer.
                    m_DebugColorPicker.SetFloat(HDShaderIDs._ApplyLinearToSRGB, m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled() ? 1.0f : 0.0f);
                    // Everything we have capture is flipped (as it happen before FinalPass/postprocess/Blit. So if we are not in SceneView
                    // (i.e. we have perform a flip, we need to flip the input texture) + we need to handle the case were we debug a fullscreen pass that have already perform the flip
                    HDUtils.DrawFullScreen(cmd, hdCamera, m_DebugColorPicker, (RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget);
                }
            }
        }

        void ClearBuffers(HDCamera hdCamera, CommandBuffer cmd)
        {
            FrameSettings settings = hdCamera.frameSettings;

            using (new ProfilingSample(cmd, "ClearBuffers", CustomSamplerId.ClearBuffers.GetSampler()))
            {
                // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.
                // Clear depth/stencil and init buffers
                using (new ProfilingSample(cmd, "Clear Depth/Stencil", CustomSamplerId.ClearDepthStencil.GetSampler()))
                {
                    if (hdCamera.clearDepth)
                    {
                        HDUtils.SetRenderTarget(cmd, hdCamera, hdCamera.frameSettings.enableMSAA? m_CameraColorMSAABuffer : m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA), ClearFlag.Depth);
                        if (hdCamera.frameSettings.enableMSAA)
                        {
                            HDUtils.SetRenderTarget(cmd, hdCamera, m_SharedRTManager.GetDepthTexture(true), m_SharedRTManager.GetDepthStencilBuffer(true), ClearFlag.Color, Color.black);
                        }
                    }
                }

                // Clear the HDR target
                using (new ProfilingSample(cmd, "Clear HDR target", CustomSamplerId.ClearHDRTarget.GetSampler()))
                {
                    if (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.BackgroundColor ||
                        // If the luxmeter is enabled, the sky isn't rendered so we clear the background color
                        m_DebugDisplaySettings.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter ||
                        // If we want the sky but the sky don't exist, still clear with background color
                        (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky && !m_SkyManager.IsVisualSkyValid()) ||
                        // Special handling for Preview we force to clear with background color (i.e black)
                        // Note that the sky use in this case is the last one setup. If there is no scene or game, there is no sky use as reflection in the preview
                        HDUtils.IsRegularPreviewCamera(hdCamera.camera)
                        )
                    {
                        Color clearColor = hdCamera.backgroundColorHDR;

                        // We set the background color to black when the luxmeter is enabled to avoid picking the sky color
                        if (m_DebugDisplaySettings.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter)
                            clearColor = Color.black;

                        HDUtils.SetRenderTarget(cmd, hdCamera, hdCamera.frameSettings.enableMSAA ? m_CameraColorMSAABuffer : m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.enableMSAA), ClearFlag.Color, clearColor);

                    }
                }

                if (settings.enableSubsurfaceScattering)
                {
                    using (new ProfilingSample(cmd, "Clear SSS Lighting Buffer", CustomSamplerId.ClearSssLightingBuffer.GetSampler()))
                    {
                        HDUtils.SetRenderTarget(cmd, hdCamera, hdCamera.frameSettings.enableMSAA ? m_CameraSssDiffuseLightingMSAABuffer : m_CameraSssDiffuseLightingBuffer, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }

                if (settings.enableSSR)
                {
                    using (new ProfilingSample(cmd, "Clear SSR Buffers", CustomSamplerId.ClearSsrBuffers.GetSampler()))
                    {
                        // In practice, these textures are sparse (mostly black). Therefore, clearing them is fast (due to CMASK),
                        // and much faster than fully overwriting them from within SSR shaders.
                        // HDUtils.SetRenderTarget(cmd, hdCamera, m_SsrDebugTexture,    ClearFlag.Color, CoreUtils.clearColorAllBlack);
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_SsrHitPointTexture, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                        HDUtils.SetRenderTarget(cmd, hdCamera, m_SsrLightingTexture, ClearFlag.Color, CoreUtils.clearColorAllBlack);
                    }
                }

                // We don't need to clear the GBuffers as scene is rewrite and we are suppose to only access valid data (invalid data are tagged with stencil as StencilLightingUsage.NoLighting),
                // This is to save some performance
                if (!settings.enableForwardRenderingOnly)
                {
                    // We still clear in case of debug mode
                    if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled())
                    {
                        using (new ProfilingSample(cmd, "Clear GBuffer", CustomSamplerId.ClearGBuffer.GetSampler()))
                        {
                            HDUtils.SetRenderTarget(cmd, hdCamera, m_GbufferManager.GetBuffersRTI(), m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                        }
                    }
                }
            }
        }

        void StartStereoRendering(CommandBuffer cmd, ScriptableRenderContext renderContext, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.enableStereo)
            {
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                renderContext.StartMultiEye(hdCamera.camera);
            }
        }

        void StopStereoRendering(CommandBuffer cmd, ScriptableRenderContext renderContext, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.enableStereo)
            {
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                renderContext.StopMultiEye(hdCamera.camera);
            }

        }

        /// <summary>
        /// Push a RenderTexture handled by a RTHandle in global parameters.
        /// </summary>
        /// <param name="cmd">Command buffer to queue commands</param>
        /// <param name="rth">RTHandle handling the RenderTexture</param>
        /// <param name="textureID">TextureID to use for texture binding.</param>
        /// <param name="sizeID">Property ID to store RTHandle size ((x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size).</param>
        /// <param name="scaleID">PropertyID to store RTHandle scale ((x,y) = Screen Scale, z = lod count, w = unused).</param>
        void PushGlobalRTHandle(CommandBuffer cmd, RTHandleSystem.RTHandle rth, int textureID, int sizeID, int scaleID)
        {
            if (rth != null)
            {
                cmd.SetGlobalTexture(textureID, rth);
                cmd.SetGlobalVector(
                    sizeID,
                    new Vector4(
                    rth.referenceSize.x,
                    rth.referenceSize.y,
                    1f / rth.referenceSize.x,
                    1f / rth.referenceSize.y
                    )
                );
                cmd.SetGlobalVector(
                    scaleID,
                    new Vector4(
                    rth.referenceSize.x / (float)rth.rt.width,
                    rth.referenceSize.y / (float)rth.rt.height,
                    Mathf.Log(Mathf.Min(rth.rt.width, rth.rt.height), 2),
                    0.0f
                    )
                );
            }
            else
            {
                cmd.SetGlobalTexture(textureID, Texture2D.blackTexture);
                cmd.SetGlobalVector(sizeID, Vector4.one);
                cmd.SetGlobalVector(scaleID, Vector4.one);
            }
        }
    }
}
