//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Fabric.cs which generates Fabric.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.cs.hlsl"
// Those define allow to include desired SSS/Transmission functions
#define MATERIAL_INCLUDE_SUBSURFACESCATTERING
#define MATERIAL_INCLUDE_TRANSMISSION
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"

// #define FABRIC_DISPLAY_REFERENCE_IBL

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

// Assume bsdfData.normalWS is init
void FillMaterialAnisotropy(float anisotropy, float3 tangentWS, float3 bitangentWS, inout BSDFData bsdfData)
{
    bsdfData.anisotropy = anisotropy;
    bsdfData.tangentWS = tangentWS;
    bsdfData.bitangentWS = bitangentWS;
}

void ClampRoughness(inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.roughnessT = max(minRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB = max(minRoughness, bsdfData.roughnessB);
}

float ComputeMicroShadowing(BSDFData bsdfData, float NdotL)
{
    return ComputeMicroShadowing(bsdfData.ambientOcclusion, NdotL, _MicroShadowOpacity);
}

bool MaterialSupportsTransmission(BSDFData bsdfData)
{
    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION);
}

// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 worldToTangent, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // NOTE: THe _Debug* uniforms come from /HDRP/Debug/DebugDisplay.hlsl

    // Override value if requested by user
    // this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;

    if (overrideAlbedo)
    {
        float3 overrideAlbedoValue = _DebugLightingAlbedo.yzw;
        surfaceData.baseColor = overrideAlbedoValue;
    }

    if (overrideSmoothness)
    {
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        surfaceData.perceptualSmoothness = overrideSmoothnessValue;
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = worldToTangent[2];
    }
#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
// Note: This will be available and used in ShaderPassForward.hlsl since in Fabric.shader,
// just before including the core code of the pass (ShaderPassForward.hlsl) we include
// Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
// Fabric.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like specular only
    bool overrideSpecularColor = _DebugLightingSpecularColor.x != 0.0;

    if (overrideSpecularColor)
    {
        float3 overrideSpecularColor = _DebugLightingSpecularColor.yzw;
        bsdfData.fresnel0 = overrideSpecularColor;
    }
#endif
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    normalData.normalWS = surfaceData.normalWS;
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    return normalData;
}

SSSData ConvertSurfaceDataToSSSData(SurfaceData surfaceData)
{
    SSSData sssData;

    sssData.diffuseColor = surfaceData.baseColor;
    sssData.subsurfaceMask = surfaceData.subsurfaceMask;
    sssData.diffusionProfile = surfaceData.diffusionProfile;

    return sssData;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: All enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures = surfaceData.materialFeatures;

    bsdfData.diffuseColor = surfaceData.baseColor;
    bsdfData.specularOcclusion = surfaceData.specularOcclusion;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.geomNormalWS = surfaceData.geomNormalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

    bsdfData.ambientOcclusion = surfaceData.ambientOcclusion;

    // Note: we have ZERO_INITIALIZE the struct so bsdfData.anisotropy == 0.0
    // Note: DIFFUSION_PROFILE_NEUTRAL_ID is 0

    // In forward everything is statically know and we could theorically cumulate all the material features. So the code reflect it.
    // However in practice we keep parity between deferred and forward, so we should constrain the various features.
    // The UI is in charge of setuping the constrain, not the code. So if users is forward only and want unleash power, it is easy to unleash by some UI change

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialSSS(surfaceData.diffusionProfile, surfaceData.subsurfaceMask, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialTransmission(surfaceData.diffusionProfile, surfaceData.thickness, bsdfData);
    }

    if (!HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
    {
        FillMaterialAnisotropy(surfaceData.anisotropy, surfaceData.tangentWS, cross(surfaceData.normalWS, surfaceData.tangentWS), bsdfData);
    }

    // After the fill material SSS data has operated, in the case of the fabric we force the value of the fresnel0 term
    bsdfData.fresnel0 = surfaceData.specularColor;

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

// This function call the generated debug function and allow to override the debug output if needed
void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);
}

// This function call the generated debug function and allow to override the debug output if needed
void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);
}

//-----------------------------------------------------------------------------
// PreLightData
//
// Make sure we respect naming conventions to reuse ShaderPassForward as is,
// ie struct (even if opaque to the ShaderPassForward) name is PreLightData,
// GetPreLightData prototype.
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;        // Could be negative due to normal mapping, use ClampNdotV()
    float partLambdaV;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;
};

// This function is call to precompute heavy calculation before lightloop
PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    // Don't init to zero to allow to track warning about uninitialized data

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;

    float NdotV = ClampNdotV(preLightData.NdotV);

    float unused;
    float3 iblN;

    // Reminder: This is a static if resolve at compile time
    if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
    {
        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, unused);

        float TdotV = dot(bsdfData.tangentWS, V);
        float BdotV = dot(bsdfData.bitangentWS, V);

        preLightData.partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, bsdfData.roughnessT, bsdfData.roughnessB);

        // perceptualRoughness is use as input and output here
        GetGGXAnisotropicModifiedNormalAndRoughness(bsdfData.bitangentWS, bsdfData.tangentWS, N, V, bsdfData.anisotropy, preLightData.iblPerceptualRoughness, iblN, preLightData.iblPerceptualRoughness);
    }
    else
    {
        preLightData.partLambdaV = 0.0;
        iblN = N;

        GetPreIntegratedFGDCharlieAndFabricLambert(NdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, unused);
    }

    preLightData.iblR = reflect(-V, iblN);

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)
{
    // To get the data we need to do the whole process - compiler should optimize everything
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);
    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    // Add GI transmission contribution to bakeDiffuseLighting, we then drop backBakeDiffuseLighting (i.e it is not used anymore, this save VGPR)
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION))
    {
        builtinData.bakeDiffuseLighting += builtinData.backBakeDiffuseLighting * bsdfData.transmittance;
    }

    // For SSS we need to take into account the state of diffuseColor 
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING))
    {
        bsdfData.diffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);
    }

    // Premultiply (back) bake diffuse lighting information with diffuse pre-integration
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * bsdfData.diffuseColor;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // DiffuseColor for lightmapping
    lightTransportData.diffuseColor = bsdfData.diffuseColor;
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

// Ref: https://www.slideshare.net/jalnaga/custom-fabric-shader-for-unreal-engine-4
// For Fabric we have two type of BRDF
// Non-Metal: Cotton, deim, flax and common fabrics
// Cotton: Roughness of 1.0 (unless wet) - Fuzz rim - specular color is white but is looked like desaturated.
// Metal: Silk, satin, velvet, nylon and polyester
// Silk: Roughness 0.3 - 0.7 - anisotropic - varying specular color

// This function apply BSDF. Assumes that NdotL is positive.
void BSDF(  float3 V, float3 L, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    float LdotV, NdotH, LdotH, NdotV, invLenLV;
    GetBSDFAngle(V, L, NdotL, preLightData.NdotV, LdotV, NdotH, LdotH, NdotV, invLenLV);

    // Fabric are dieletric but we simulate forward scattering effect with colored specular (fuzz tint term)
	float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
    {
        float D = D_Charlie(NdotH, bsdfData.roughnessT);
        // V_Charlie is expensive, use approx with V_Ashikhmin instead
        // float Vis = V_Charlie(NdotL, NdotV, bsdfData.roughness);
        float Vis = V_Ashikhmin(NdotL, NdotV);

        specularLighting = F * Vis * D;

        // Note: diffuseLighting is multiply by color in PostEvaluateBSDF
        diffuseLighting = FabricLambert(bsdfData.roughnessT);
    }
    else // MATERIALFEATUREFLAGS_FABRIC_SILK
    {
        // For silk we just use a tinted anisotropy
        float3 H = (L + V) * invLenLV;

        // For anisotropy we must not saturate these values
        float TdotH = dot(bsdfData.tangentWS, H);
        float TdotL = dot(bsdfData.tangentWS, L);
        float BdotH = dot(bsdfData.bitangentWS, H);
        float BdotL = dot(bsdfData.bitangentWS, L);

        // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
        float DV = DV_SmithJointGGXAniso(   TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL,
                                            bsdfData.roughnessT, bsdfData.roughnessB, preLightData.partLambdaV);

        specularLighting = F * DV;

        // Note: diffuseLighting is multiply by color in PostEvaluateBSDF
        diffuseLighting = DisneyDiffuse(NdotV, NdotL, LdotV, bsdfData.perceptualRoughness);
    }
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

// Here we need to make sure
#define USE_DIFFUSE_LAMBERT_BRDF
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"
#undef USE_DIFFUSE_LAMBERT_BRDF

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    return ShadeSurface_Directional(lightLoopContext, posInput, builtinData, preLightData, lightData,
                                    bsdfData, bsdfData.normalWS, V);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricReference.hlsl"
//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData, preLightData, lightData,
                                 bsdfData, bsdfData.normalWS, V);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    // TODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Rect
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    // TODO

    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    PreLightData   preLightData,
                                                    BSDFData       bsdfData,
                                                    inout float    reflectionHierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO: this texture is sparse (mostly black). Can we avoid reading every texel? How about using Hi-S?
    float4 ssrLighting = LOAD_TEXTURE2D(_SsrLightingTexture, posInput.positionSS);

    // Note: RGB is already premultiplied by A.
    // TODO: we should multiply all indirect lighting by the FGD value only ONCE.
    lighting.specularReflected = ssrLighting.rgb /* * ssrLighting.a */ * preLightData.specularFGD;
    reflectionHierarchyWeight = ssrLighting.a;

    return lighting;
}

IndirectLighting EvaluateBSDF_ScreenspaceRefraction(LightLoopContext lightLoopContext,
                                                    float3 V, PositionInputs posInput,
                                                    PreLightData preLightData, BSDFData bsdfData,
                                                    EnvLightData envLightData,
                                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int influenceShapeType, int GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
        return lighting;

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

#ifdef FABRIC_DISPLAY_REFERENCE_IBL
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
    {
        envLighting = IntegrateSpecularCottonWoolIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
    }
    else
    {
        envLighting = IntegrateSpecularSilkIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
    }
#else
    float3 R = preLightData.iblR;

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float iblMipLevel;
    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
#if !defined(SHADER_API_METAL)
    if (IsEnvIndexTexture2D(lightData.envIndex))
    {
        // Empirical remapping
        iblMipLevel = PositivePow(preLightData.iblPerceptualRoughness, 0.8) * uint(max(_ColorPyramidScale.z - 1, 0));
    }
    else
#endif
    {
        iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);
    }

    // If it is a silk, we need to use the GGX convolution (slice0), otherwise the charlie convolution (slice1)
    int sliceIndex = 0;
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL))
    {
        sliceIndex = 1;
    }

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, iblMipLevel, sliceIndex);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = preLightData.specularFGD * preLD.rgb;

#endif
    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;
    lighting.specularReflected = envLighting;

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    AmbientOcclusionFactor aoFactor;
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Subsurface scattering mode
    float3 modifiedDiffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

    // TODO: Multiscattering for cloth?

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, diffuseLighting, specularLighting);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
