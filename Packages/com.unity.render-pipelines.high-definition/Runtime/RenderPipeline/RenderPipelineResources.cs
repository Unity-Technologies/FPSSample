using System;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineResources : ScriptableObject
    {
        const int currentVersion = 4;
        [SerializeField]
        // Silent the warning
        // "The private field `UnityEngine.Experimental.Rendering.HDPipeline.RenderPipelineResources.m_Version' is assigned but its value is never used"
        // As it is used only in editor currently and when building a player we get this warning.
#pragma warning disable 414
        [FormerlySerializedAs("version")]
        int m_Version = 1;
#pragma warning restore 414

        [Serializable]
        public sealed class ShaderResources
        {
            // Defaults
            public Shader defaultPS;

            // Debug
            public Shader debugDisplayLatlongPS;
            public Shader debugViewMaterialGBufferPS;
            public Shader debugViewTilesPS;
            public Shader debugFullScreenPS;
            public Shader debugColorPickerPS;
            public Shader debugLightVolumePS;

            // Lighting
            public Shader deferredPS;
            public ComputeShader colorPyramidCS;
            public ComputeShader depthPyramidCS;
            public ComputeShader copyChannelCS;
            public ComputeShader applyDistortionCS;
            public ComputeShader screenSpaceReflectionsCS;

            // Lighting tile pass
            public ComputeShader clearDispatchIndirectCS;
            public ComputeShader buildDispatchIndirectCS;
            public ComputeShader buildScreenAABBCS;
            public ComputeShader buildPerTileLightListCS;               // FPTL
            public ComputeShader buildPerBigTileLightListCS;
            public ComputeShader buildPerVoxelLightListCS;              // clustered
            public ComputeShader buildMaterialFlagsCS;
            public ComputeShader deferredCS;
            public ComputeShader screenSpaceShadowCS;
            public ComputeShader volumeVoxelizationCS;
            public ComputeShader volumetricLightingCS;

            public ComputeShader subsurfaceScatteringCS;                // Disney SSS
            public Shader combineLightingPS;

            // General
            public Shader cameraMotionVectorsPS;
            public Shader copyStencilBufferPS;
            public Shader copyDepthBufferPS;
            public Shader blitPS;

            // Sky
            public Shader blitCubemapPS;
            public ComputeShader buildProbabilityTablesCS;
            public ComputeShader computeGgxIblSampleDataCS;
            public Shader GGXConvolvePS;
            public Shader opaqueAtmosphericScatteringPS;
            public Shader hdriSkyPS;
            public Shader integrateHdriSkyPS;
            public Shader proceduralSkyPS;
            public Shader skyboxCubemapPS;
            public Shader gradientSkyPS;

            // Material
            public Shader preIntegratedFGD_GGXDisneyDiffusePS;
            public Shader preIntegratedFGD_CharlieFabricLambertPS;
            public Shader preIntegratedFGD_WardPS;
            public Shader preIntegratedFGD_CookTorrancePS;

            // Utilities / Core
            public ComputeShader encodeBC6HCS;
            public Shader cubeToPanoPS;
            public Shader blitCubeTextureFacePS;

            // Shadow
            public Shader shadowClearPS;
            public ComputeShader shadowBlurMomentsCS;
            public Shader debugShadowMapPS;
            public Shader debugHDShadowMapPS;

            // Decal
            public Shader decalNormalBufferPS;

            // MSAA Shaders
            public Shader depthValuesPS;
            public Shader aoResolvePS;
            public Shader colorResolvePS;
        }

        [Serializable]
        public sealed class MaterialResources
        {
            // Defaults
            public Material defaultDiffuseMat;
            public Material defaultMirrorMat;
            public Material defaultDecalMat;
            public Material defaultTerrainMat;
        }

        [Serializable]
        public sealed class TextureResources
        {
            // Debug
            public Texture2D debugFontTex;
        }

        [Serializable]
        public sealed class ShaderGraphResources
        {
        }

        public ShaderResources shaders;
        public MaterialResources materials;
        public TextureResources textures;
        public ShaderGraphResources shaderGraphs;

#if UNITY_EDITOR
        public void UpgradeIfNeeded()
        {
            if (m_Version != currentVersion)
            {
                Init();

                m_Version = currentVersion;
            }
        }

        // Note: move this to a static using once we can target C#6+
        T Load<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public void Init()
        {
            // Load default renderPipelineResources / Material / Shader
            string HDRenderPipelinePath = HDUtils.GetHDRenderPipelinePath() + "Runtime/";
            string CorePath = HDUtils.GetHDRenderPipelinePath() + "Runtime/Core/"; // HDUtils.GetCorePath(); // All CoreRP have been move to HDRP currently for out of preview of SRP and LW

            // Shaders
            shaders = new ShaderResources
            {
                // Defaults
                defaultPS = Load<Shader>(HDRenderPipelinePath + "Material/Lit/Lit.shader"),

                // Debug
                debugDisplayLatlongPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugDisplayLatlong.Shader"),
                debugViewMaterialGBufferPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewMaterialGBuffer.Shader"),
                debugViewTilesPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewTiles.Shader"),
                debugFullScreenPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugFullScreen.Shader"),
                debugColorPickerPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugColorPicker.Shader"),
                debugLightVolumePS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugLightVolume.Shader"),

                // Lighting
                deferredPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Deferred.Shader"),
                colorPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/ColorPyramid.compute"),
                depthPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/DepthPyramid.compute"),
                copyChannelCS = Load<ComputeShader>(CorePath + "CoreResources/GPUCopy.compute"),
                applyDistortionCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/Distortion/ApplyDistorsion.compute"),
                screenSpaceReflectionsCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceLighting/ScreenSpaceReflections.compute"),

                // Lighting tile pass
                clearDispatchIndirectCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/cleardispatchindirect.compute"),
                buildDispatchIndirectCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/builddispatchindirect.compute"),
                buildScreenAABBCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/scrbound.compute"),
                buildPerTileLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild.compute"),
                buildPerBigTileLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-bigtile.compute"),
                buildPerVoxelLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-clustered.compute"),
                buildMaterialFlagsCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/materialflags.compute"),
                deferredCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/Deferred.compute"),

                screenSpaceShadowCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Shadow/ScreenSpaceShadow.compute"),
                volumeVoxelizationCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/VolumetricLighting/VolumeVoxelization.compute"),
                volumetricLightingCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/VolumetricLighting/VolumetricLighting.compute"),

                subsurfaceScatteringCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.compute"),
                combineLightingPS = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/CombineLighting.shader"),

                // General
                cameraMotionVectorsPS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MotionVectors/CameraMotionVectors.shader"),
                copyStencilBufferPS = Load<Shader>(HDRenderPipelinePath + "ShaderLibrary/CopyStencilBuffer.shader"),
                copyDepthBufferPS = Load<Shader>(HDRenderPipelinePath + "ShaderLibrary/CopyDepthBuffer.shader"),
                blitPS = Load<Shader>(HDRenderPipelinePath + "ShaderLibrary/Blit.shader"),

                // Sky
                blitCubemapPS = Load<Shader>(HDRenderPipelinePath + "Sky/BlitCubemap.shader"),
                buildProbabilityTablesCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/BuildProbabilityTables.compute"),
                computeGgxIblSampleDataCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/ComputeGgxIblSampleData.compute"),
                GGXConvolvePS = Load<Shader>(HDRenderPipelinePath + "Material/GGXConvolution/GGXConvolve.shader"),
                opaqueAtmosphericScatteringPS = Load<Shader>(HDRenderPipelinePath + "Lighting/AtmosphericScattering/OpaqueAtmosphericScattering.shader"),
                hdriSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/HDRISky.shader"),
                integrateHdriSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/IntegrateHDRISky.shader"),
                proceduralSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/ProceduralSky/ProceduralSky.shader"),
                gradientSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/GradientSky/GradientSky.shader"),

                // Skybox/Cubemap is a builtin shader, must use Shader.Find to access it. It is fine because we are in the editor
                skyboxCubemapPS = Shader.Find("Skybox/Cubemap"),

                // Material
                preIntegratedFGD_GGXDisneyDiffusePS = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_GGXDisneyDiffuse.shader"),
                preIntegratedFGD_CharlieFabricLambertPS = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_CharlieFabricLambert.shader"),
                preIntegratedFGD_CookTorrancePS = Load<Shader>(HDRenderPipelinePath + "Material/AxF/PreIntegratedFGD_CookTorrance.shader"),
                preIntegratedFGD_WardPS = Load<Shader>(HDRenderPipelinePath + "Material/AxF/PreIntegratedFGD_Ward.shader"),

                // Utilities / Core
                encodeBC6HCS = Load<ComputeShader>(CorePath + "CoreResources/EncodeBC6H.compute"),
                cubeToPanoPS = Load<Shader>(CorePath + "CoreResources/CubeToPano.shader"),
                blitCubeTextureFacePS = Load<Shader>(CorePath + "CoreResources/BlitCubeTextureFace.shader"),

                // Shadow
                shadowClearPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Shadow/ShadowClear.shader"),
                shadowBlurMomentsCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Shadow/ShadowBlurMoments.compute"),
                debugShadowMapPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Shadow/DebugDisplayShadowMap.shader"),
                debugHDShadowMapPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Shadow/DebugDisplayHDShadowMap.shader"),

                // Decal
                decalNormalBufferPS = Load<Shader>(HDRenderPipelinePath + "Material/Decal/DecalNormalBuffer.shader"),
                
                // MSAA
                depthValuesPS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MSAA/DepthValues.shader"),
                aoResolvePS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MSAA/AOResolve.shader"),
                colorResolvePS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MSAA/ColorResolve.shader"),
            };

            // Materials
            materials = new MaterialResources
            {
                // Defaults
                defaultDiffuseMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDMaterial.mat"),
                defaultMirrorMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDMirrorMaterial.mat"),
                defaultDecalMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDDecalMaterial.mat"),
                defaultTerrainMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat"),
            };

            // Textures
            textures = new TextureResources
            {
                // Debug
                debugFontTex = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/DebugFont.tga"),
            };

            // ShaderGraphs
            shaderGraphs = new ShaderGraphResources
            {
            };
        }
#endif
    }
}
