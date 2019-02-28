//-----------------------------------------------------------------------------
// Includes
//-----------------------------------------------------------------------------
// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.cs.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"

// Those define allow to include desired SSS/Transmission functions
#define MATERIAL_INCLUDE_SUBSURFACESCATTERING
#define MATERIAL_INCLUDE_TRANSMISSION
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

#define USE_DIFFUSE_LAMBERT_BRDF

void ClampRoughness(inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.roughnessT    = max(minRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB    = max(minRoughness, bsdfData.roughnessB);
    bsdfData.coatRoughness = max(minRoughness, bsdfData.coatRoughness);
}

float ComputeMicroShadowing(BSDFData bsdfData, float NdotL)
{
#ifdef LIGHT_LAYERS
    return ComputeMicroShadowing(bsdfData.ambientOcclusion, NdotL, _MicroShadowOpacity);
#else
    // No extra G-Buffer for AO, so 'bsdfData.ambientOcclusion' does not hold a meaningful value.
    return ComputeMicroShadowing(bsdfData.specularOcclusion, NdotL, _MicroShadowOpacity);
#endif
}

bool MaterialSupportsTransmission(BSDFData bsdfData)
{
    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.normalWS;
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
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

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In case of foward or gbuffer pass all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures    = surfaceData.materialFeatures;

    // Standard material
    bsdfData.ambientOcclusion    = surfaceData.ambientOcclusion;
    bsdfData.specularOcclusion   = surfaceData.specularOcclusion;
    bsdfData.normalWS            = surfaceData.normalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

    // Check if we read value of normal and roughness from buffer. This is a tradeoff
#ifdef FORWARD_MATERIAL_READ_FROM_WRITTEN_NORMAL_BUFFER
#if (SHADERPASS == SHADERPASS_FORWARD) && !defined(_SURFACE_TYPE_TRANSPARENT)
    UpdateSurfaceDataFromNormalData(positionSS, bsdfData);
#endif
#endif

    // There is no metallic with SSS and specular color mode
    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0     = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR) ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    // Note: we have ZERO_INITIALIZE the struct so bsdfData.anisotropy == 0.0
    // Note: DIFFUSION_PROFILE_NEUTRAL_ID is 0

    // In forward everything is statically know and we could theorically cumulate all the material features. So the code reflect it.
    // However in practice we keep parity between deferred and forward, so we should constrain the various features.
    // The UI is in charge of setuping the constrain, not the code. So if users is forward only and want unleash power, it is easy to unleash by some UI change

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialSSS(surfaceData.diffusionProfile, surfaceData.subsurfaceMask, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialTransmission(surfaceData.diffusionProfile, surfaceData.thickness, bsdfData);
    }

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

#if HAS_REFRACTION
    // Note: Reuse thickness of transmission's property set
    FillMaterialTransparencyData(surfaceData.baseColor, surfaceData.metallic, surfaceData.ior, surfaceData.transmittanceColor, surfaceData.atDistance,
        surfaceData.thickness, surfaceData.transmittanceMask, bsdfData);
#endif

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;                     // Could be negative due to normal mapping, use ClampNdotV()

    // GGX
    float partLambdaV;
    float energyCompensation;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)

    // Clear coat
    float    coatPartLambdaV;
    float3   coatIblR;
    float    coatIblF;               // Fresnel term for view vector
    float3x3 ltcTransformCoat;       // Inverse transformation for GGX                                 (4x VGPRs)

#if HAS_REFRACTION
    // Refraction
    float3 transparentRefractV;      // refracted view vector after exiting the shape
    float3 transparentPositionWS;    // start of the refracted ray after exiting the shape
    float3 transparentTransmittance; // transmittance due to absorption
    float transparentSSMipLevel;     // mip level of the screen space gaussian pyramid for rough refraction
#endif
};

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    // Don't init to zero to allow to track warning about uninitialized data

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;

    float NdotV = ClampNdotV(preLightData.NdotV);

    preLightData.coatPartLambdaV = 0;
    preLightData.coatIblR = 0;
    preLightData.coatIblF = 0;

    // Handle IBL + area light + multiscattering.
    // Note: use the not modified by anisotropy iblPerceptualRoughness here.
    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
    preLightData.diffuseFGD = 1.0;

    preLightData.energyCompensation = 0.0;

    preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, bsdfData.roughnessT);

    preLightData.iblR = reflect(-V, N);

    // Area light
    // UVs for sampling the LUTs
    float theta = FastACosPos(NdotV); // For Area light - UVs for sampling the LUTs
    float2 uv = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI);

    preLightData.ltcTransformDiffuse = k_identity3x3;

    preLightData.ltcTransformSpecular      = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);

    preLightData.ltcTransformCoat = 0.0;

#if HAS_REFRACTION
    RefractionModelResult refraction = REFRACTION_MODEL(V, posInput, bsdfData);
    preLightData.transparentRefractV = refraction.rayWS;
    preLightData.transparentPositionWS = refraction.positionWS;
    preLightData.transparentTransmittance = exp(-bsdfData.absorptionCoefficient * refraction.dist);
    // Empirical remap to try to match a bit the refraction probe blurring for the fallback
    // Use IblPerceptualRoughness so we can handle approx of clear coat.
    preLightData.transparentSSMipLevel = PositivePow(preLightData.iblPerceptualRoughness, 1.3) * uint(max(_ColorPyramidScale.z - 1, 0));
#endif

    return preLightData;
}

#ifdef HAS_LIGHTLOOP

// This function apply BSDF. Assumes that NdotL is positive.
void BSDF(  float3 V, float3 L, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    diffuseLighting = Lambert();
    specularLighting = 0;

#if HDRP_ENABLE_SPECULAR
    float LdotV, NdotH, LdotH, NdotV, invLenLV;
    GetBSDFAngle(V, L, NdotL, preLightData.NdotV, LdotV, NdotH, LdotH, NdotV, invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    float DV = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughnessT, preLightData.partLambdaV);
    specularLighting = F * DV;
#endif // HDRP_ENABLE_SPECULAR
}

float3 EvaluateCookie_Directional(LightLoopContext lightLoopContext, DirectionalLightData lightData,
                                  float3 lightToSample)
{

    // Translate and rotate 'positionWS' into the light space.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3x3 lightToWorld  = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS    = mul(lightToSample, transpose(lightToWorld));

    // Perform orthographic projection.
    float2 positionCS    = positionLS.xy;

    // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
    float2 positionNDC = positionCS * 0.5 + 0.5;

    // We let the sampler handle clamping to border.
    return SampleCookie2D(lightLoopContext, positionNDC, lightData.cookieIndex, lightData.tileCookie);
}

float4 EvaluateCookie_Punctual(LightLoopContext lightLoopContext, LightData lightData,
                               float3 lightToSample)
{
    int lightType = lightData.lightType;

    // Translate and rotate 'positionWS' into the light space.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS   = mul(lightToSample, transpose(lightToWorld));

    float4 cookie;

    UNITY_BRANCH if (lightType == GPULIGHTTYPE_POINT)
    {
        cookie.rgb = SampleCookieCube(lightLoopContext, positionLS, lightData.cookieIndex);
        cookie.a   = 1;
    }
    else
    {
        // Perform orthographic or perspective projection.
        float  perspectiveZ = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? positionLS.z : 1.0;
        float2 positionCS   = positionLS.xy / perspectiveZ;
        bool   isInBounds   = Max3(abs(positionCS.x), abs(positionCS.y), 1.0 - positionLS.z) <= 1.0;

        // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
        float2 positionNDC = positionCS * 0.5 + 0.5;

        // Manually clamp to border (black).
        cookie.rgb = SampleCookie2D(lightLoopContext, positionNDC, lightData.cookieIndex, false);
        cookie.a   = isInBounds ? 1 : 0;
    }

    return cookie;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"

void EvaluateLight_EnvIntersection(float3 positionWS, float3 normalWS, EnvLightData lightData, int influenceShapeType, inout float3 R, inout float weight)
{
    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3x3 worldToIS = WorldToInfluenceSpace(lightData); // IS: Influence space
    float3 positionIS = WorldToInfluencePosition(lightData, worldToIS, positionWS);
    float3 dirIS = mul(R, worldToIS);

    // Process the projection
    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.
    if (influenceShapeType == ENVSHAPETYPE_SPHERE)
    {
        weight = InfluenceSphereWeight(lightData, normalWS, positionWS, positionIS, dirIS);
    }
    else if (influenceShapeType == ENVSHAPETYPE_BOX)
    {
        weight = InfluenceBoxWeight(lightData, normalWS, positionWS, positionIS, dirIS);
    }

    // Smooth weighting
    weight = Smoothstep01(weight);
    weight *= lightData.weight;
}

#define OVERRIDE_EVALUATE_COOKIE_DIRECTIONAL
#define OVERRIDE_EVALUATE_COOKIE_PUNCTUAL
#define OVERRIDE_EVALUATE_ENV_INTERSECTION

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------
// None of the outputs are premul   tiplied.
// Note: When doing transmission we always have only one shadow sample to do: Either front or back. We use NdotL to know on which side we are
void EvaluateLight_Directional(LightLoopContext lightLoopContext, PositionInputs posInput,
                               DirectionalLightData lightData, BuiltinData builtinData,
                               float3 N, float3 L,
                               out float3 color, out float attenuation)
{
    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;

    color       = lightData.color;
    attenuation = 1.0; // Note: no volumetric attenuation along shadow rays for directional lights

#if HDRP_ENABLE_COOKIE
    UNITY_BRANCH if (lightData.cookieIndex >= 0)
    {
        float3 lightToSample = positionWS - lightData.positionRWS;
        float3 cookie = EvaluateCookie_Directional(lightLoopContext, lightData, lightToSample);

        color *= cookie;
    }
#endif

#if HDRP_ENABLE_SHADOWS
    // We test NdotL >= 0.0 to not sample the shadow map if it is not required.
    float NdotL = dot(N, L);
    UNITY_BRANCH if (lightData.shadowIndex >= 0 && (NdotL >= 0.0))
    {
        shadow = lightLoopContext.shadowValue;
    }
#endif // HDRP_ENABLE_SHADOWS

    attenuation *= shadow;
}

float3 EvaluateTransmission(BSDFData bsdfData, float3 transmittance, float NdotL, float NdotV, float LdotV, float attenuation)
{
    // Apply wrapped lighting to better handle thin objects at grazing angles.
    float wrappedNdotL = ComputeWrappedDiffuseLighting(-NdotL, TRANSMISSION_WRAP_LIGHT);

    // Apply BSDF-specific diffuse transmission to attenuation. See also: [SSS-NOTE-TRSM]
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    attenuation *= Lambert();
#else
    attenuation *= DisneyDiffuse(NdotV, max(0, -NdotL), LdotV, bsdfData.perceptualRoughness);
#endif

    float intensity = attenuation * wrappedNdotL;
    return intensity * transmittance;
}


DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L = -lightData.forward;
    float3 N = bsdfData.normalWS;
    float NdotL = dot(N, L);

    float3 transmittance = float3(0.0, 0.0, 0.0);
#if HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
    {
        float3 a = 0; float b = 0;
        // Caution: This function modify N and contactShadowIndex
        transmittance = PreEvaluateDirectionalLightTransmission(bsdfData, lightData, a, b); // contactShadowIndex is only modify for the code of this function
    }
#endif

    float3 color;
    float attenuation;
    EvaluateLight_Directional(lightLoopContext, posInput, lightData, builtinData, N, L, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseDimmer;
        lighting.specular *= intensity * lightData.specularDimmer;
    }

#if HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT
    // The mixed thickness mode is not supported by directional lights due to poor quality and high performance impact.
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
    {
        float  NdotV = ClampNdotV(preLightData.NdotV);
        float  LdotV = dot(L, V);
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, transmittance, NdotL, NdotV, LdotV, attenuation * lightData.diffuseDimmer);
    }
#endif

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseDimmer;
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------
void EvaluateLight_Punctual(LightLoopContext lightLoopContext, PositionInputs posInput,
                            LightData lightData, BuiltinData builtinData,
                            float3 N, float3 L, float3 lightToSample, float4 distances,
                            out float3 color, out float attenuation)
{
    float3 positionWS    = posInput.positionWS;
    float  shadow        = 1.0;

    color       = lightData.color;
    attenuation = PunctualLightAttenuation(distances, lightData.rangeAttenuationScale, lightData.rangeAttenuationBias,
                                                 lightData.angleScale, lightData.angleOffset);

    // Projector lights always have cookies, so we can perform clipping inside the if().
    UNITY_BRANCH if (lightData.cookieIndex >= 0)
    {
        float4 cookie = EvaluateCookie_Punctual(lightLoopContext, lightData, lightToSample);

        color       *= cookie.rgb;
        attenuation *= cookie.a;
    }

#if HDRP_ENABLE_SHADOWS
    // We test NdotL >= 0.0 to not sample the shadow map if it is not required.
    UNITY_BRANCH if (lightData.shadowIndex >= 0 && (dot(N, L) >= 0.0))
    {
        // TODO: make projector lights cast shadows.
        // Note:the case of NdotL < 0 can appear with isThinModeTransmission, in this case we need to flip the shadow bias
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, posInput.positionSS, positionWS, N, lightData.shadowIndex, L, distances.x, lightData.lightType == GPULIGHTTYPE_POINT, lightData.lightType != GPULIGHTTYPE_PROJECTOR_BOX);
        shadow = lerp(1.0, shadow, lightData.shadowDimmer);
    }

    // Transparent have no contact shadow information
#ifndef _SURFACE_TYPE_TRANSPARENT
    shadow = min(shadow, GetContactShadow(lightLoopContext, lightData.contactShadowIndex));
#endif

#endif // HDRP_ENABLE_SHADOWS

    attenuation *= shadow;
}

// This function return transmittance to provide to EvaluateTransmission
float3 PreEvaluatePunctualLightTransmission(LightLoopContext lightLoopContext, PositionInputs posInput, float distFrontFaceToLight,
                                            float NdotL, float3 L, BSDFData bsdfData,
                                            inout float3 normalWS, inout LightData lightData)
{
    float3 transmittance = bsdfData.transmittance;

    // if NdotL is positive, we do one fetch on front face done by EvaluateLight_XXX. Just regular lighting
    // If NdotL is negative, we have two cases:
    // - Thin mode: Reuse the front face fetch as shadow for back face - flip the normal for the bias (and the NdotL test) and disable contact shadow
    // - Mixed mode: Do a fetch on back face to retrieve the thickness. The thickness will provide a shadow attenuation (with distance travelled there is less transmission).
    // (Note: EvaluateLight_Punctual discard the fetch if NdotL < 0)
    if (NdotL < 0 && lightData.shadowIndex >= 0)
    {
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
        {
            normalWS = -normalWS; // Flip normal for shadow bias
            lightData.contactShadowIndex = -1;  //  Disable shadow contact
        }
        transmittance = lerp( bsdfData.transmittance, transmittance, lightData.shadowDimmer);
    }

    return transmittance;
}

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L;
    float3 lightToSample;
    float4 distances; // {d, d^2, 1/d, d_proj}
    GetPunctualLightVectors(posInput.positionWS, lightData, L, lightToSample, distances);

    float3 N     = bsdfData.normalWS;
    float  NdotL = dot(N, L);

    float3 transmittance = float3(0.0, 0.0, 0.0);
#if HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Caution: This function modify N and lightData.contactShadowIndex
        transmittance = PreEvaluatePunctualLightTransmission(lightLoopContext, posInput, distances.x, NdotL, L, bsdfData, N, lightData);
    }
#endif

    float3 color;
    float attenuation;
    EvaluateLight_Punctual(lightLoopContext, posInput, lightData, builtinData, N, L,
                           lightToSample, distances, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        // Simulate a sphere light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway
        bsdfData.coatRoughness = max(bsdfData.coatRoughness, lightData.minRoughness);
        bsdfData.roughnessT = max(bsdfData.roughnessT, lightData.minRoughness);
        bsdfData.roughnessB = max(bsdfData.roughnessB, lightData.minRoughness);

        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseDimmer;
        lighting.specular *= intensity * lightData.specularDimmer;
    }

#if HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        float  NdotV = ClampNdotV(preLightData.NdotV);
        float  LdotV = dot(L, V);
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, transmittance, NdotL, NdotV, LdotV, attenuation * lightData.diffuseDimmer);
    }
#endif

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseDimmer;
    }
#endif

    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    // We not support area lights
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);
    return lighting;
}

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    PreLightData   preLightData,
                                                    BSDFData       bsdfData,
                                                    inout float    reflectionHierarchyWeight)
{
    // We not support screen space reflections
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
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

    float3 envLighting;
    float weight = 1.0;

    float3 R = preLightData.iblR;

    EvaluateLight_EnvIntersection(posInput.positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float iblMipLevel;
    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
#if !defined(SHADER_API_METAL)
    if (IsEnvIndexTexture2D(lightData.envIndex))
    {
        // Empirical remapping
        iblMipLevel = PlanarPerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness, _ColorPyramidScale.z);
    }
    else
#endif
    {
        iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);
    }

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, iblMipLevel);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = F_Schlick(bsdfData.fresnel0, dot(bsdfData.normalWS, V)) * preLD.rgb;

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    lighting.specularReflected = envLighting;

    return lighting;
}

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    AmbientOcclusionFactor aoFactor;
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
    GetScreenSpaceAmbientOcclusion(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Subsurface scattering mode
    float3 modifiedDiffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    // Note: In deferred bakeDiffuseLighting also contain emissive and in this case emissiveColor is 0
    diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + builtinData.emissiveColor;
    #ifdef HDRP_ENABLE_ENV_LIGHT
    diffuseLighting += builtinData.bakeDiffuseLighting;
    #endif
    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, diffuseLighting, specularLighting);
#endif
}

#endif
