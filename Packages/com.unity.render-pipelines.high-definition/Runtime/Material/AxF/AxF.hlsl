//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in AxF.cs which generates AxF.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.cs.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

// Declare the BSDF specific FGD property and its fetching function
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFPreIntegratedFGD.hlsl"

// Add support for LTC Area Lights
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFLTCAreaLight/AxFLTCAreaLight.hlsl"


//-----------------------------------------------------------------------------
#define CLEAR_COAT_ROUGHNESS 0.03
#define CLEAR_COAT_PERCEPTUAL_ROUGHNESS RoughnessToPerceptualRoughness(CLEAR_COAT_ROUGHNESS)

#define FLAKES_ROUGHNESS 0.03
#define FLAKES_PERCEPTUAL_ROUGHNESS RoughnessToPerceptualRoughness(FLAKES_ROUGHNESS)
#define FLAKES_F0 1.0

// Define this to sample the environment maps/LTC samples for each lobe, instead of a single sample with an average lobe
#define USE_COOK_TORRANCE_MULTI_LOBES   1

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

void ClampRoughness(inout BSDFData bsdfData, float minRoughness)
{
    // TODO
}

float ComputeMicroShadowing(BSDFData bsdfData, float NdotL)
{
    return 1; // TODO
}

// No transmission support
// #define MATERIAL_INCLUDE_TRANSMISSION
bool MaterialSupportsTransmission(BSDFData bsdfData)
{
    return false;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------
void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
        break;
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
        break;
    }
}



// This function is used to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 worldToTangent, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // NOTE: THe _Debug* uniforms come from /HDRP/Debug/DebugDisplay.hlsl

    // Override value if requested by user this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;

    if (overrideAlbedo)
    {
        surfaceData.diffuseColor = _DebugLightingAlbedo.yzw;
    }

    if (overrideSmoothness)
    {
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        surfaceData.specularLobe = PerceptualSmoothnessToRoughness(overrideSmoothnessValue);
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = worldToTangent[2];
    }
#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
//
// NOTE:
//  This will be available and used in ShaderPassForward.hlsl since in AxF.shader,
//  just before including the core code of the pass (ShaderPassForward.hlsl) we include
//  Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
//  AxF.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
//
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like specular only
    bool overrideSpecularColor = _DebugLightingSpecularColor.x != 0.0;

    if (overrideSpecularColor)
    {
        float3 overrideSpecularColor = _DebugLightingSpecularColor.yzw;
        bsdfData.specularColor = overrideSpecularColor;
    }
#endif
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    normalData.normalWS = surfaceData.normalWS;
#if defined(_AXF_BRDF_TYPE_SVBRDF)
    normalData.perceptualRoughness = RoughnessToPerceptualRoughness(surfaceData.specularLobe.x);
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    normalData.perceptualRoughness = 0.0;
#else
    // This is only possible if the AxF is a BTF type. However, there is a bunch of ifdefs do not support this third case
    normalData.perceptualRoughness = 0.0;
#endif
    return normalData;
}

//----------------------------------------------------------------------
// From Walter 2007 eq. 40
// Expects incoming pointing AWAY from the surface
// eta = IOR_above / IOR_below
// rayIntensity returns 0 in case of total internal reflection
//
float3  Refract(float3 incoming, float3 normal, float eta, out float rayIntensity)
{
    float   c = dot(incoming, normal);
    float   b = 1.0 + eta * (c*c - 1.0);
    if (b >= 0.0)
    {
        float   k = eta * c - sign(c) * sqrt(b);
        float3  R = k * normal - eta * incoming;
        rayIntensity = 1;
        return normalize(R);
    }
    else
    {
        rayIntensity = 0;
        return -incoming;   // Total internal reflection
    }
}

// Same but without handling total internal reflection because eta < 1
float3  Refract(float3 incoming, float3 normal, float eta)
{
    float   c = dot(incoming, normal);
    float   b = 1.0 + eta * (c*c - 1.0);
    float   k = eta * c - sign(c) * sqrt(b);
    float3  R = k * normal - eta * incoming;
    return normalize(R);
}

//----------------------------------------------------------------------
// Ref: https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
// Fresnel dieletric / dielectric
// Safe version preventing NaNs when IOR = 1
real    F_FresnelDieletricSafe(real IOR, real u)
{
    u = max(1e-3, u); // Prevents NaNs
    real g = sqrt(max(0.0, Sq(IOR) + Sq(u) - 1.0));
    return 0.5 * Sq((g - u) / max(1e-4, g + u)) * (1.0 + Sq(((g + u) * u - 1.0) / ((g - u) * u + 1.0)));
}


//----------------------------------------------------------------------
// Cook-Torrance functions as provided by X-Rite in the "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_BrightnessBRDF" document from the SDK
//
float CT_D(float N_H, float m)
{
    float cosb_sqr = N_H * N_H;
    float m_sqr = m * m;
    float e = (cosb_sqr - 1.0) / (cosb_sqr*m_sqr);  // -tan(a)² / m²
    return exp(e) / (m_sqr*cosb_sqr*cosb_sqr);  // exp(-tan(a)² / m²) / (m² * cos(a)^4)
}

// Classical Schlick approximation for Fresnel
float CT_F(float H_V, float F0)
{
    float f_1_sub_cos = 1.0 - H_V;
    float f_1_sub_cos_sqr = f_1_sub_cos * f_1_sub_cos;
    float f_1_sub_cos_fifth = f_1_sub_cos_sqr * f_1_sub_cos_sqr*f_1_sub_cos;
    return F0 + (1.0 - F0) * f_1_sub_cos_fifth;
}

float3  MultiLobesCookTorrance(float NdotL, float NdotV, float NdotH, float VdotH)
{
    // Ensure numerical stability
    if (NdotV < 0.00174532836589830883577820272085 && NdotL < 0.00174532836589830883577820272085) //sin(0.1°)
        return 0.0;

    float   specularIntensity = 0.0;
    for (uint lobeIndex = 0; lobeIndex < _CarPaint2_LobeCount; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = _CarPaint2_CTSpreads[lobeIndex];

        specularIntensity += coeff * CT_D(NdotH, spread) * CT_F(VdotH, F0);
    }
    specularIntensity *= G_CookTorrance(NdotH, NdotV, NdotL, VdotH)  // Shadowing/Masking term
        / (PI * max(1e-3, NdotV * NdotL));

    return specularIntensity;
}


//----------------------------------------------------------------------
// Simple Oren-Nayar implementation (from http://patapom.com/blog/BRDF/MSBRDFEnergyCompensation/#oren-nayar-diffuse-model)
//  normal, unit surface normal
//  light, unit vector pointing toward the light
//  view, unit vector pointing toward the view
//  roughness, Oren-Nayar roughness parameter in [0,PI/2]
//
float   OrenNayar(in float3 n, in float3 v, in float3 l, in float roughness)
{
    float   LdotN = dot(l, n);
    float   VdotN = dot(v, n);

    float   gamma = dot(v - n * VdotN, l - n * LdotN)
        / (sqrt(saturate(1.0 - VdotN * VdotN)) * sqrt(saturate(1.0 - LdotN * LdotN)));

    float rough_sq = roughness * roughness;
    //    float A = 1.0 - 0.5 * (rough_sq / (rough_sq + 0.33));   // You can replace 0.33 by 0.57 to simulate the missing inter-reflection term, as specified in footnote of page 22 of the 1992 paper
    float A = 1.0 - 0.5 * (rough_sq / (rough_sq + 0.57));   // You can replace 0.33 by 0.57 to simulate the missing inter-reflection term, as specified in footnote of page 22 of the 1992 paper
    float B = 0.45 * (rough_sq / (rough_sq + 0.09));

    // Original formulation
//  float angle_vn = acos(VdotN);
//  float angle_ln = acos(LdotN);
//  float alpha = max(angle_vn, angle_ln);
//  float beta  = min(angle_vn, angle_ln);
//  float C = sin(alpha) * tan(beta);

    // Optimized formulation (without tangents, arccos or sines)
    float2  cos_alpha_beta = VdotN < LdotN ? float2(VdotN, LdotN) : float2(LdotN, VdotN);   // Here we reverse the min/max since cos() is a monotonically decreasing function
    float2  sin_alpha_beta = sqrt(saturate(1.0 - cos_alpha_beta * cos_alpha_beta));           // Saturate to avoid NaN if ever cos_alpha > 1 (it happens with floating-point precision)
    float   C = sin_alpha_beta.x * sin_alpha_beta.y / (1e-6 + cos_alpha_beta.y);

    return A + B * max(0.0, gamma) * C;
}


//----------------------------------------------------------------------
float   G_smith(float NdotV, float roughness)
{
    float   a2 = Sq(roughness);
    return 2 * NdotV / (NdotV + sqrt(a2 + (1 - a2) * Sq(NdotV)));
}


//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData    bsdfData;
    //  ZERO_INITIALIZE(BSDFData, data);

    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.tangentWS = surfaceData.tangentWS;
    bsdfData.biTangentWS = surfaceData.biTangentWS;

    //-----------------------------------------------------------------------------
#ifdef _AXF_BRDF_TYPE_SVBRDF
    bsdfData.diffuseColor = surfaceData.diffuseColor;
    bsdfData.specularColor = surfaceData.specularColor;
    bsdfData.fresnelF0 = surfaceData.fresnelF0;
    bsdfData.roughness = surfaceData.specularLobe;
    bsdfData.height_mm = surfaceData.height_mm;
    bsdfData.anisotropyAngle = surfaceData.anisotropyAngle;
    bsdfData.clearcoatColor = surfaceData.clearcoatColor;
    bsdfData.clearcoatNormalWS = surfaceData.clearcoatNormalWS;
    bsdfData.clearcoatIOR = surfaceData.clearcoatIOR;

    // Useless but pass along anyway
    bsdfData.flakesUV = surfaceData.flakesUV;
    bsdfData.flakesMipLevel = surfaceData.flakesMipLevel;

    //-----------------------------------------------------------------------------
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    bsdfData.diffuseColor = surfaceData.diffuseColor;
    bsdfData.flakesUV = surfaceData.flakesUV;
    bsdfData.flakesMipLevel = surfaceData.flakesMipLevel;
    bsdfData.clearcoatColor = 1.0;  // Not provided, assume white...
    bsdfData.clearcoatIOR = surfaceData.clearcoatIOR;
    bsdfData.clearcoatNormalWS = surfaceData.clearcoatNormalWS;

    // Although not used, needs to be initialized... :'(
    bsdfData.specularColor = 0;
    bsdfData.fresnelF0 = 0;
    bsdfData.roughness = 0;
    bsdfData.height_mm = 0;
    bsdfData.anisotropyAngle = 0;
#endif

    bsdfData.geomNormalWS = surfaceData.geomNormalWS;

    ApplyDebugToBSDFData(bsdfData);
    return bsdfData;
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
    float   NdotV_UnderCoat;    // NdotV after optional clear-coat refraction. Could be negative due to normal mapping, use ClampNdotV()
    float   NdotV_Clearcoat;    // NdotV before optional clear-coat refraction. Could be negative due to normal mapping, use ClampNdotV()
    float3  IOR;
    float3  viewWS_UnderCoat;   // View vector after optional clear-coat refraction.

#ifdef _AXF_BRDF_TYPE_SVBRDF
    // Anisotropy
    float2  anisoX;
    float2  anisoY;
#endif

    // IBL
    float3  iblDominantDirectionWS_UnderCoat;   // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float3  iblDominantDirectionWS_Clearcoat;   // Dominant specular direction, used for IBL in EvaluateBSDF_Env() and also in area lights when clearcoat is enabled
#ifdef _AXF_BRDF_TYPE_SVBRDF
    float   iblPerceptualRoughness;
    float3  specularFGD;
    float   diffuseFGD;
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT) & !defined(USE_COOK_TORRANCE_MULTI_LOBES)
    float   iblPerceptualRoughness;     // Use this to store an average lobe roughness
#endif

// Area lights (18 VGPRs)
// TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3    orthoBasisViewNormal;       // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
#ifdef _AXF_BRDF_TYPE_SVBRDF
    float3x3    ltcTransformDiffuse;    // Inverse transformation                                         (4x VGPRs)
    float       ltcTransformDiffuse_Amplitude;
    float3x3    ltcTransformSpecular;   // Inverse transformation                                         (4x VGPRs)
    float3      ltcTransformSpecular_Amplitude;
#endif
    float3x3    ltcTransformClearcoat;      // Inverse transformation for GGX                                 (4x VGPRs)
    float3      ltcTransformClearcoat_Amplitude;
};

PreLightData    GetPreLightData(float3 viewWS_Clearcoat, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData    preLightData;
    //  ZERO_INITIALIZE(PreLightData, preLightData);

    preLightData.IOR = GetIorN(bsdfData.fresnelF0, 1.0);

    float3  normalWS_Clearcoat = (_Flags & 0x2U) ? bsdfData.clearcoatNormalWS : bsdfData.normalWS;
    preLightData.NdotV_Clearcoat = dot(normalWS_Clearcoat, viewWS_Clearcoat);
    preLightData.viewWS_UnderCoat = viewWS_Clearcoat;   // Save original view before optional refraction by clearcoat


                                                        //-----------------------------------------------------------------------------
    // Handle clearcoat refraction of view ray
    if ((_Flags & 0x6U) == 0x6U)
    {
        preLightData.viewWS_UnderCoat = -Refract(preLightData.viewWS_UnderCoat, bsdfData.clearcoatNormalWS, 1.0 / bsdfData.clearcoatIOR);
    }

    // Compute under-coat view-dependent data after optional refraction
    preLightData.NdotV_UnderCoat = dot(bsdfData.normalWS, preLightData.viewWS_UnderCoat);

    float   NdotV_UnderCoat = ClampNdotV(preLightData.NdotV_UnderCoat);
    float   NdotV_Clearcoat = ClampNdotV(preLightData.NdotV_Clearcoat);


#ifdef _AXF_BRDF_TYPE_SVBRDF
    //-----------------------------------------------------------------------------
    // Handle anisotropy
    float2  anisoDir = float2(1, 0);
    if (_Flags & 1)
    {
        //            sincos(bsdfData.anisotropyAngle, anisoDir.y, anisoDir.x);
        sincos(bsdfData.anisotropyAngle, anisoDir.x, anisoDir.y);    // Eyeballed the fact that an angle of 0 is actually 90° from tangent axis!
    }

    preLightData.anisoX = anisoDir;
    preLightData.anisoY = float2(-anisoDir.y, anisoDir.x);
#endif


    //-----------------------------------------------------------------------------
    // Handle IBL +  multiscattering
    preLightData.iblDominantDirectionWS_UnderCoat = reflect(-preLightData.viewWS_UnderCoat, bsdfData.normalWS);
    preLightData.iblDominantDirectionWS_Clearcoat = reflect(-viewWS_Clearcoat, normalWS_Clearcoat);

#ifdef _AXF_BRDF_TYPE_SVBRDF
    preLightData.iblPerceptualRoughness = RoughnessToPerceptualRoughness(0.5 * (bsdfData.roughness.x + bsdfData.roughness.y));    // @TODO => Anisotropic IBL?
    float specularReflectivity;
    switch ((_SVBRDF_BRDFType >> 1) & 7)
    {
    case 0:
        GetPreIntegratedFGDWardAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, bsdfData.fresnelF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        break;

    // case 1: // @TODO: Support Blinn-Phong FGD?

    case 2:
        GetPreIntegratedFGDCookTorranceAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, bsdfData.fresnelF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        break;
    case 3:
        GetPreIntegratedFGDGGXAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, bsdfData.fresnelF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        break;

     // case 4: // @TODO: Support Blinn-Phong FGD?

    default:    // Use GGX by default
        GetPreIntegratedFGDGGXAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, bsdfData.fresnelF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        break;
    }

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
#if !USE_COOK_TORRANCE_MULTI_LOBES
    //-----------------------------------------------------------------------------
    // Computes weighted average of roughness values
    float2  sumRoughness = 0.0;
    for (uint lobeIndex = 0; lobeIndex < _CarPaint2_LobeCount; lobeIndex++)
    {
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = _CarPaint2_CTSpreads[lobeIndex];
        sumRoughness += coeff * float2(spread, 1);
    }
    preLightData.iblPerceptualRoughness = RoughnessToPerceptualRoughness(sumRoughness.x / sumRoughness.y);    // Not used if sampling the environment for each Cook-Torrance lobe
#endif
#endif


//-----------------------------------------------------------------------------
// Area lights

// Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal[2] = bsdfData.normalWS;
    preLightData.orthoBasisViewNormal[0] = normalize(viewWS_Clearcoat - preLightData.NdotV_Clearcoat * bsdfData.normalWS);    // Do not clamp NdotV here
    preLightData.orthoBasisViewNormal[1] = cross(preLightData.orthoBasisViewNormal[2], preLightData.orthoBasisViewNormal[0]);

#ifdef _AXF_BRDF_TYPE_SVBRDF
    // UVs for sampling the LUTs
    float2  UV = LTCGetSamplingUV(NdotV_UnderCoat, preLightData.iblPerceptualRoughness);

    // Load diffuse LTC & FGD
    if (_SVBRDF_BRDFType & 1)
    {
        preLightData.ltcTransformDiffuse = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_OREN_NAYAR);
        preLightData.ltcTransformDiffuse_Amplitude = 1.0;   // @TODO: Sample Oren-Nayar FGD!
    }
    else
    {
        preLightData.ltcTransformDiffuse = k_identity3x3;   // Lambert
        preLightData.ltcTransformDiffuse_Amplitude = 1.0;
    }

    // Load specular LTC & FGD
    switch ((_SVBRDF_BRDFType >> 1) & 7)
    {
    case 0: preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_WARD); break;
    case 2: preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_COOK_TORRANCE); break;
    case 3: preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_GGX); break;
    case 1: // BLINN-PHONG
    case 4: // PHONG;
    {
        // According to https://computergraphics.stackexchange.com/questions/1515/what-is-the-accepted-method-of-converting-shininess-to-roughness-and-vice-versa
        //  float   exponent = 2/roughness^4 - 2;
        //
        float   exponent = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
        float   roughness = pow(max(0.0, 2.0 / (exponent + 2)), 1.0 / 4.0);
        float2  UV = LTCGetSamplingUV(NdotV_UnderCoat, RoughnessToPerceptualRoughness(roughness));
        preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_COOK_TORRANCE);
        break;
    }

    default:    // @TODO
        preLightData.ltcTransformSpecular = 0;
        break;
    }

    // LTC amplitude is actually BRDF's albedo for a given N.V and roughness, which is conveniently the FGD table term we already computed! <3
    // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
    preLightData.ltcTransformSpecular_Amplitude = preLightData.specularFGD;

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    // NOTHING TO DO!

#endif  // _AXF_BRDF_TYPE_SVBRDF

// Load clear-coat LTC & FGD
    preLightData.ltcTransformClearcoat = 0.0;
    preLightData.ltcTransformClearcoat_Amplitude = 0;
    if (_Flags & 2)
    {
        float   clearcoatF0 = IorToFresnel0(bsdfData.clearcoatIOR);
        float2  UV = LTCGetSamplingUV(NdotV_Clearcoat, CLEAR_COAT_PERCEPTUAL_ROUGHNESS);
        preLightData.ltcTransformClearcoat = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_GGX);
        float   specularReflectivity, dummyDiffuseFGD;
        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV_Clearcoat, CLEAR_COAT_PERCEPTUAL_ROUGHNESS, clearcoatF0, preLightData.ltcTransformClearcoat_Amplitude, dummyDiffuseFGD, specularReflectivity);

        // Cheat a little and make the amplitude go to 0 when F0 is 0 (which the actual dieletric Fresnel should do!)
        preLightData.ltcTransformClearcoat_Amplitude *= smoothstep(0, 0.01, clearcoatF0);
    }

    return preLightData;
}


//----------------------------------------------------------------------
// Computes Fresnel reflection/refraction of view and light vectors due to clearcoating
// Returns the ratios of the incoming reflected and refracted energy
// Also refracts the provided view and light vectors if refraction is enabled
//
//void    ComputeClearcoatReflectionAndExtinction(inout float3 viewWS, inout float3 lightWS, BSDFData bsdfData, out float3 reflectedRatio, out float3 refractedRatio) {
//
//    // Computes perfect mirror reflection
//    float3  H = normalize(viewWS + lightWS);
//    float   LdotH = saturate(dot(lightWS, H));
//
//    reflectedRatio = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotH);    // Full reflection in mirror direction (we use expensive Fresnel here so the clearcoat properly disappears when IOR -> 1)
//
//    // Compute input/output Fresnel reflections
//    float   LdotN = saturate(dot(lightWS, bsdfData.clearcoatNormalWS));
//    float3  Fin = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotN);
//
//    float   VdotN = saturate(dot(viewWS, bsdfData.clearcoatNormalWS));
//    float3  Fout = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, VdotN);
//
//    // Apply optional refraction
//    if (_Flags & 4U) {
//          float eta = 1.0 / bsdfData.clearcoatIOR;
//        lightWS = -Refract(lightWS, bsdfData.clearcoatNormalWS, eta);
//        viewWS = -Refract(viewWS, bsdfData.clearcoatNormalWS, eta);
//    }
//
//    refractedRatio = (1-Fin) * (1-Fout);
//}

void    ComputeClearcoatReflectionAndExtinction_UsePreLightData(inout float3 viewWS, inout float3 lightWS, BSDFData bsdfData, PreLightData preLightData, out float3 reflectedRatio, out float3 refractedRatio)
{

    // Computes perfect mirror reflection
    float3  H = normalize(viewWS + lightWS);
    float   LdotH = saturate(dot(lightWS, H));

    reflectedRatio = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotH);    // Full reflection in mirror direction (we use expensive Fresnel here so the clearcoat properly disappears when IOR -> 1)

    // Compute input/output Fresnel reflections
    float   LdotN = saturate(dot(lightWS, bsdfData.clearcoatNormalWS));
    float3  Fin = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotN);

    float   VdotN = saturate(dot(viewWS, bsdfData.clearcoatNormalWS));
    float3  Fout = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, VdotN);

    // Apply optional refraction
    if (_Flags & 4U)
    {
        lightWS = -Refract(lightWS, bsdfData.clearcoatNormalWS, 1.0 / bsdfData.clearcoatIOR);
        viewWS = preLightData.viewWS_UnderCoat;
    }

    refractedRatio = (1 - Fin) * (1 - Fout);
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

#ifdef _AXF_BRDF_TYPE_SVBRDF
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * bsdfData.diffuseColor;
#else
    builtinData.bakeDiffuseLighting *= bsdfData.diffuseColor;
#endif
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------
LightTransportData  GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    lightTransportData.diffuseColor = bsdfData.diffuseColor;
    lightTransportData.emissiveColor = float3(0.0, 0.0, 0.0);

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF shared between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

#ifdef _AXF_BRDF_TYPE_SVBRDF

float3 ComputeWard(float3 H, float LdotH, float NdotL, float NdotV, float3 positionWS, PreLightData preLightData, BSDFData bsdfData)
{

    // Evaluate Fresnel term
    float3  F = 0.0;
    switch (_SVBRDF_BRDFVariants & 3)
    {
    case 1: F = F_FresnelDieletricSafe(bsdfData.fresnelF0.y, LdotH); break;
    case 2: F = F_Schlick(bsdfData.fresnelF0, LdotH); break;
    }

    // Evaluate normal distribution function
    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.biTangentWS), dot(H, bsdfData.normalWS));
    float2  rotH = (tsH.x * preLightData.anisoX + tsH.y * preLightData.anisoY) / tsH.z;
    float   N = exp(-Sq(rotH.x / bsdfData.roughness.x) - Sq(rotH.y / bsdfData.roughness.y))
        / (PI * bsdfData.roughness.x*bsdfData.roughness.y);

    switch ((_SVBRDF_BRDFVariants >> 2) & 3)
    {
    case 0: N /= 4.0 * Sq(LdotH) * Sq(Sq(tsH.z)); break; // Moroder
    case 1: N /= 4.0 * NdotL; break;                       // Duer
    case 2: N /= 4.0 * sqrt(NdotL); break;               // Ward
    }

    return bsdfData.specularColor * F * N;
}

float3  ComputeBlinnPhong(float3 H, float LdotH, float NdotL, float NdotV, float3 positionWS, PreLightData preLightData, BSDFData bsdfData)
{
    float2  exponents = exp2(bsdfData.roughness);

    // Evaluate normal distribution function
    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.biTangentWS), dot(H, bsdfData.normalWS));
    float2  rotH = tsH.x * preLightData.anisoX + tsH.y * preLightData.anisoY;

    float3  N = 0;
    switch ((_SVBRDF_BRDFVariants >> 4) & 3)
    {
    case 0:
    {   // Ashikmin-Shirley
        N = sqrt((1 + exponents.x) * (1 + exponents.y)) / (8 * PI)
            * pow(saturate(tsH.z), (exponents.x * Sq(rotH.x) + exponents.y * Sq(rotH.y)) / (1 - Sq(tsH.z)))
            / (LdotH * max(NdotL, NdotV));
        break;
    }

    case 1:
    {   // Blinn
        float   exponent = 0.5 * (exponents.x + exponents.y);    // Should be isotropic anyway...
        N = (exponent + 2) / (8 * PI)
            * pow(saturate(tsH.z), exponent);
        break;
    }

    case 2: // VRay
    case 3: // Lewis
        N = 1000 * float3(1, 0, 1);   // Not documented...
        break;
    }

    return bsdfData.specularColor * N;
}

float3  ComputeCookTorrance(float3 H, float LdotH, float NdotL, float NdotV, float3 positionWS, PreLightData preLightData, BSDFData bsdfData)
{
    float   NdotH = dot(H, bsdfData.normalWS);
    float   sqNdotH = Sq(NdotH);

    // Evaluate Fresnel term
    float3  F = F_Schlick(bsdfData.fresnelF0, LdotH);

    // Evaluate (isotropic) normal distribution function (Beckmann)
    float   sqAlpha = bsdfData.roughness.x * bsdfData.roughness.y;
    float   N = exp((sqNdotH - 1) / (sqNdotH * sqAlpha))
        / (PI * Sq(sqNdotH) * sqAlpha);

    // Evaluate shadowing/masking term
    float   G = G_CookTorrance(NdotH, NdotV, NdotL, LdotH);

    return bsdfData.specularColor * F * N * G;
}

float3  ComputeGGX(float3 H, float LdotH, float NdotL, float NdotV, float3 positionWS, PreLightData preLightData, BSDFData bsdfData)
{
    // Evaluate Fresnel term
    float3  F = F_Schlick(bsdfData.fresnelF0, LdotH);

    // Evaluate normal distribution function (Trowbridge-Reitz)
    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.biTangentWS), dot(H, bsdfData.normalWS));
    float3  rotH = float3((tsH.x * preLightData.anisoX + tsH.y * preLightData.anisoY) / bsdfData.roughness, tsH.z);
    float   N = 1.0 / (PI * bsdfData.roughness.x*bsdfData.roughness.y) * 1.0 / Sq(dot(rotH, rotH));

    // Evaluate shadowing/masking term
    float   roughness = 0.5 * (bsdfData.roughness.x + bsdfData.roughness.y);
    float   G = G_smith(NdotL, roughness) * G_smith(NdotV, roughness);
    G /= 4.0 * NdotL * NdotV;

    return bsdfData.specularColor * F * N * G;
}

float3  ComputePhong(float3 H, float LdotH, float NdotL, float NdotV, float3 positionWS, PreLightData preLightData, BSDFData bsdfData)
{
    return 1000 * float3(1, 0, 1);
}


// This function applies the BSDF. Assumes that NdotL is positive.
void BSDF(  float3 viewWS_UnderCoat, float3 lightWS_UnderCoat, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting, out float3 specularLighting)
{

    float3  viewWS_Clearcoat = viewWS_UnderCoat;    // Keep copy before possible refraction

    // Apply clearcoat
    float3  clearcoatExtinction = 1.0;
    float3  clearcoatReflection = 0.0;
    if (_Flags & 2)
    {
        ComputeClearcoatReflectionAndExtinction_UsePreLightData(viewWS_UnderCoat, lightWS_UnderCoat, bsdfData, preLightData, clearcoatReflection, clearcoatExtinction);
        clearcoatReflection *= bsdfData.clearcoatColor / PI;
    }

    float3  H = normalize(viewWS_UnderCoat + lightWS_UnderCoat);
    float   LdotH = saturate(dot(H, lightWS_UnderCoat));
    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    // Compute diffuse term
    float3  diffuseTerm = Lambert();
    if (_SVBRDF_BRDFType & 1)
    {
        float   diffuseRoughness = 0.5 * HALF_PI; // Arbitrary roughness (not specified in the documentation...)
        diffuseTerm = INV_PI * OrenNayar(bsdfData.normalWS, viewWS_UnderCoat, lightWS_UnderCoat, diffuseRoughness);
    }

    // Compute specular term
    float3  specularTerm = float3(1, 0, 0);
    switch ((_SVBRDF_BRDFType >> 1) & 7)
    {
    case 0: specularTerm = ComputeWard(H, LdotH, NdotL, NdotV, positionWS, preLightData, bsdfData); break;
    case 1: specularTerm = ComputeBlinnPhong(H, LdotH, NdotL, NdotV, positionWS, preLightData, bsdfData); break;
    case 2: specularTerm = ComputeCookTorrance(H, LdotH, NdotL, NdotV, positionWS, preLightData, bsdfData); break;
    case 3: specularTerm = ComputeGGX(H, LdotH, NdotL, NdotV, positionWS, preLightData, bsdfData); break;
    case 4: specularTerm = ComputePhong(H, LdotH, NdotL, NdotV, positionWS, preLightData, bsdfData); break;
    default:    // @TODO
        specularTerm = 1000 * float3(1, 0, 1);
        break;
    }

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    diffuseLighting = clearcoatExtinction * diffuseTerm;
    specularLighting = clearcoatExtinction * specularTerm + clearcoatReflection;
}

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

// Samples the "BRDF Color Table" as explained in "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_ColorTable" from the SDK
float3  GetBRDFColor(float thetaH, float thetaD)
{

#if 0   // <== Define this to use the code from the documentation
    // In the documentation they write that we must divide by PI/2 (it would seem)
    float2  UV = float2(2.0 * thetaH / PI, 2.0 * thetaD / PI);
#else
    // But the acos yields values in [0,PI] and the texture seems to be indicating the entire PI range is covered so...
    float2  UV = float2(thetaH / PI, thetaD / PI);
#endif

    // Problem here is that the BRDF color tables are only defined in the upper-left triangular part of the texture
    // It's not indicated anywhere in the SDK documentation but I decided to clamp to the diagonal otherwise we get black values if UV.x+UV.y > 0.5!
    UV *= 2.0;
    UV *= saturate(UV.x + UV.y) / max(1e-3, UV.x + UV.y);
    UV *= 0.5;

    // Rescale UVs to account for 0.5 texel offset
    uint2   textureSize;
    _CarPaint2_BRDFColorMap.GetDimensions(textureSize.x, textureSize.y);
    UV = (0.5 + UV * (textureSize - 1)) / textureSize;

    return _CarPaint2_BRDFColorMapScale * SAMPLE_TEXTURE2D_LOD(_CarPaint2_BRDFColorMap, sampler_CarPaint2_BRDFColorMap, float2(UV.x, 1 - UV.y), 0).xyz;
}

// Samples the "BTF Flakes" texture as explained in "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_FlakeBTF" from the SDK
uint    SampleFlakesLUT(uint index)
{
    return 255.0 * _CarPaint2_FlakeThetaFISliceLUTMap[uint2(index, 0)].x;
    // Hardcoded LUT
    //    uint    pipoLUT[] = { 0, 8, 16, 24, 32, 40, 47, 53, 58, 62, 65, 67 };
    //    return pipoLUT[min(11, _index)];
}

float3  SamplesFlakes(float2 UV, uint sliceIndex, float mipLevel)
{
    return _CarPaint2_BTFFlakeMapScale * SAMPLE_TEXTURE2D_ARRAY_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap, UV, sliceIndex, mipLevel).xyz;
}

#if 0
// Original code from the SDK, cleaned up a bit...
float3  CarPaint_BTF(float thetaH, float thetaD, BSDFData bsdfData)
{
    float2  UV = bsdfData.flakesUV;
    float   mipLevel = bsdfData.flakesMipLevel;

    // thetaH sampling defines the angular sampling, i.e. angular flake lifetime
    float   binIndexH = _CarPaint2_FlakeNumThetaF * (2.0 * thetaH / PI) + 0.5;
    float   binIndexD = _CarPaint2_FlakeNumThetaF * (2.0 * thetaD / PI) + 0.5;

    // Bilinear interpolate indices and weights
    uint    thetaH_low = floor(binIndexH);
    uint    thetaD_low = floor(binIndexD);
    uint    thetaH_high = thetaH_low + 1;
    uint    thetaD_high = thetaD_low + 1;
    float   thetaH_weight = binIndexH - thetaH_low;
    float   thetaD_weight = binIndexD - thetaD_low;

    // To allow lower thetaD samplings while preserving flake lifetime, "virtual" thetaD patches are generated by shifting existing ones
    float2   offset_l = 0;
    float2   offset_h = 0;
    // At the moment I couldn't find any car paint material with the condition below
    //    if (_CarPaint_numThetaI < _CarPaint_numThetaF) {
    //        offset_l = float2(rnd_numbers[2*thetaD_low], rnd_numbers[2*thetaD_low+1]);
    //        offset_h = float2(rnd_numbers[2*thetaD_high], rnd_numbers[2*thetaD_high+1]);
    //        if (thetaD_low & 1)
    //            UV.xy = UV.yx;
    //        if (thetaD_high & 1)
    //            UV.xy = UV.yx;
    //
    //        // Map to the original sampling
    //        thetaD_low = floor(thetaD_low * float(_CarPaint_numThetaI) / _CarPaint_numThetaF);
    //        thetaD_high = floor(thetaD_high * float(_CarPaint_numThetaI) / _CarPaint_numThetaF);
    //    }

    float3  H0_D0 = 0.0;
    float3  H1_D0 = 0.0;
    float3  H0_D1 = 0.0;
    float3  H1_D1 = 0.0;

    // Access flake texture - make sure to stay in the correct slices (no slip over)
    if (thetaD_low < _CarPaint2_FlakeMaxThetaI)
    {
        float2  UVl = UV + offset_l;
        float2  UVh = UV + offset_h;

        uint    LUT0 = SampleFlakesLUT(thetaD_low);
        uint    LUT1 = SampleFlakesLUT(thetaD_high);
        uint    LUT2 = SampleFlakesLUT(thetaD_high + 1);

        if (LUT0 + thetaH_low < LUT1)
        {
            H0_D0 = SamplesFlakes(UVl, LUT0 + thetaH_low, mipLevel);
            if (LUT0 + thetaH_high < LUT1)
            {
                H1_D0 = SamplesFlakes(UVl, LUT0 + thetaH_high, mipLevel);
            }
            else H1_D0 = H0_D0 ? ?
        }

        if (thetaD_high < _CarPaint2_FlakeMaxThetaI)
        {
            if (LUT1 + thetaH_low < LUT2)
            {
                H0_D1 = SamplesFlakes(UVh, LUT1 + thetaH_low, mipLevel);
                if (LUT1 + thetaH_high < LUT2)
                {
                    H1_D1 = SamplesFlakes(UVh, LUT1 + thetaH_high, mipLevel);
                }
            }
        }
    }

    // Bilinear interpolation
    float3  D0 = lerp(H0_D0, H1_D0, thetaH_weight);
    float3  D1 = lerp(H0_D1, H1_D1, thetaH_weight);
    return lerp(D0, D1, thetaD_weight);
}

#else

// Simplified code
float3  CarPaint_BTF(float thetaH, float thetaD, BSDFData bsdfData)
{
    float2  UV = bsdfData.flakesUV;
    float   mipLevel = bsdfData.flakesMipLevel;

    // thetaH sampling defines the angular sampling, i.e. angular flake lifetime
    float   binIndexH = _CarPaint2_FlakeNumThetaF * (2.0 * thetaH / PI) + 0.5;
    float   binIndexD = _CarPaint2_FlakeNumThetaI * (2.0 * thetaD / PI) + 0.5;

    // Bilinear interpolate indices and weights
    uint    thetaH_low = floor(binIndexH);
    uint    thetaD_low = floor(binIndexD);
    uint    thetaH_high = thetaH_low + 1;
    uint    thetaD_high = thetaD_low + 1;
    float   thetaH_weight = binIndexH - thetaH_low;
    float   thetaD_weight = binIndexD - thetaD_low;

    // Access flake texture - make sure to stay in the correct slices (no slip over)
    // @TODO: Store RGB value with all 3 integers? Single tap into LUT...
    uint    LUT0 = SampleFlakesLUT(min(_CarPaint2_FlakeMaxThetaI - 1, thetaD_low));
    uint    LUT1 = SampleFlakesLUT(min(_CarPaint2_FlakeMaxThetaI - 1, thetaD_high));
    uint    LUT2 = SampleFlakesLUT(min(_CarPaint2_FlakeMaxThetaI - 1, thetaD_high + 1));

    float3  H0_D0 = SamplesFlakes(UV, min(LUT0 + thetaH_low, LUT1 - 1), mipLevel);
    float3  H1_D0 = SamplesFlakes(UV, min(LUT0 + thetaH_high, LUT1 - 1), mipLevel);
    float3  H0_D1 = SamplesFlakes(UV, min(LUT1 + thetaH_low, LUT2 - 1), mipLevel);
    float3  H1_D1 = SamplesFlakes(UV, min(LUT1 + thetaH_high, LUT2 - 1), mipLevel);

    // Bilinear interpolation
    float3  D0 = lerp(H0_D0, H1_D0, thetaH_weight);
    float3  D1 = lerp(H0_D1, H1_D1, thetaH_weight);
    return lerp(D0, D1, thetaD_weight);
}

#endif


// This function applies the BSDF. Assumes that NdotL is positive.
void BSDF(  float3 viewWS_UnderCoat, float3 lightWS_UnderCoat, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting, out float3 specularLighting)
{
    float3  viewWS_Clearcoat = viewWS_UnderCoat;    // Keep copy before possible refraction

    // Apply clearcoat
    float3  clearcoatExtinction = 1.0;
    float3  clearcoatReflection = 0.0;
    if (_Flags & 2)
    {
        ComputeClearcoatReflectionAndExtinction_UsePreLightData(viewWS_UnderCoat, lightWS_UnderCoat, bsdfData, preLightData, clearcoatReflection, clearcoatExtinction);
        clearcoatReflection *= bsdfData.clearcoatColor / PI;
    }

    // Compute half vector used by various components of the BSDF
    float3  H = normalize(viewWS_UnderCoat + lightWS_UnderCoat);
    float   NdotH = dot(bsdfData.normalWS, H);
    float   LdotH = dot(H, lightWS_UnderCoat);
    float   VdotH = LdotH;

    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);
    NdotL = dot(bsdfData.normalWS, lightWS_UnderCoat);

    float   thetaH = acos(clamp(NdotH, -1, 1));
    float   thetaD = acos(clamp(LdotH, -1, 1));

    // Simple lambert
    float3  diffuseTerm = Lambert();

    // Apply multi-lobes Cook-Torrance
    float3  specularTerm = MultiLobesCookTorrance(NdotL, NdotV, NdotH, VdotH);

    // Apply BRDF color
    float3  BRDFColor = GetBRDFColor(thetaH, thetaD);
    diffuseTerm *= BRDFColor;
    specularTerm *= BRDFColor;

    // Apply flakes
    specularTerm += CarPaint_BTF(thetaH, thetaD, bsdfData);

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    diffuseLighting = clearcoatExtinction * diffuseTerm;
    specularLighting = clearcoatExtinction * specularTerm + clearcoatReflection;
}

#else

// This function applies the BSDF. Assumes that NdotL is positive.
void    BSDF(float3 viewWS, float3 lightWS, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
    out float3 diffuseLighting, out float3 specularLighting)
{

    float  diffuseTerm = Lambert();

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    diffuseLighting = diffuseTerm;
    specularLighting = 0;
}

#endif

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

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
// AREA LIGHTS
//-----------------------------------------------------------------------------

// ------ HELPERS ------

// Computes the best light direction given an initial light direction
// The direction will be projected onto the area light's plane and clipped by the rectangle's bounds, the resulting normalized vector is returned
//
//  lightPositionRWS, the rectangular area light's position in local space (i.e. relative to the point currently being lit)
//  lightWS, the light direction in world-space
//
float3  ComputeBestLightDirection_Rectangle(float3 lightPositionRWS, float3 lightWS, LightData lightData)
{
    float   halfWidth = lightData.size.x * 0.5;
    float   halfHeight = lightData.size.y * 0.5;

    float   t = dot(lightPositionRWS, lightData.forward) / dot(lightWS, lightData.forward);                  // Distance until we intercept the light plane following light direction
    float3  hitPosLS = t * lightWS;                                                                             // Position of intersection with light plane
    float2  hitPosTS = float2(dot(hitPosLS, lightData.right), dot(hitPosLS, lightData.up));               // Same but in tangent space
    hitPosTS = clamp(hitPosTS, float2(-halfWidth, -halfHeight), float2(halfWidth, halfHeight));   // Clip to rectangle
    hitPosLS = lightWS + hitPosTS.x * lightData.right + hitPosTS.y * lightData.up;                              // Recompose clipped intersection
    return normalize(hitPosLS);                                                                               // Now use that direction as best light vector
}

// Computes the best light direction given an initial light direction
// The direction will be projected onto the area light's line and clipped by the segment's bounds, the resulting normalized vector is returned
//
//  lightPositionRWS, the linear area light's position in local space (i.e. relative to the point currently being lit)
//  lightWS, the light direction in world-space
//
float3  ComputeBestLightDirection_Line(float3 lightPositionRWS, float3 lightWS, LightData lightData)
{

    return lightWS;

    //    float   len = lightData.size.x;
    //    float3  T   = lightData.right;
    //
    //
    //    float   t = dot(lightPositionRWS, lightData.forward) / dot(lightWS, lightData.forward);                  // Distance until we intercept the light plane following light direction
    //    float3  hitPosLS = t * lightWS;                                                                             // Position of intersection with light plane
    //    float2  hitPosTS = float2(dot(hitPosLS, lightData.right), dot(hitPosLS, lightData.up));               // Same but in tangent space
    //            hitPosTS = clamp(hitPosTS, float2(-halfWidth, -halfHeight), float2(halfWidth, halfHeight));   // Clip to rectangle
    //    hitPosLS = lightWS + hitPosTS.x * lightData.right + hitPosTS.y * lightData.up;                              // Recompose clipped intersection
    //    return normalize(hitPosLS);                                                                               // Now use that direction as best light vector
}

// Expects non-normalized vertex positions.
// Same as regular PolygonIrradiance found in AreaLighting.hlsl except I need the form factor F
// (cf. http://blog.selfshadow.com/publications/s2016-advances/s2016_ltc_rnd.pdf pp. 92 for an explanation on the meaning of that sphere approximation)
real PolygonIrradiance(real4x3 L, out float3 F)
{
    UNITY_UNROLL
        for (uint i = 0; i < 4; i++)
        {
            L[i] = normalize(L[i]);
        }

    F = 0.0;

    UNITY_UNROLL
        for (uint edge = 0; edge < 4; edge++)
        {
            real3 V1 = L[edge];
            real3 V2 = L[(edge + 1) % 4];

            F += INV_TWO_PI * ComputeEdgeFactor(V1, V2);
        }

    // Clamp invalid values to avoid visual artifacts.
    real f2 = saturate(dot(F, F));
    real sinSqSigma = min(sqrt(f2), 0.999);
    real cosOmega = clamp(F.z * rsqrt(f2), -1, 1);

    return DiffuseSphereLightIrradiance(sinSqSigma, cosOmega);
}


//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting  EvaluateBSDF_Line(  LightLoopContext lightLoopContext,
                                    float3 viewWS_Clearcoat, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3  positionWS = posInput.positionWS;

    float   len = lightData.size.x;
    float3  T = lightData.right;

    float3  unL = lightData.positionRWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3  axis = lightData.right;

    // We define the ellipsoid s.t. r1 = (r + len / 2), r2 = r3 = r.
    // TODO: This could be precomputed.
    float   radius = rsqrt(lightData.rangeAttenuationScale); //  // rangeAttenuationScale is inverse Square Radius
    float   invAspectRatio = saturate(radius / (radius + (0.5 * len)));

    // Compute the light attenuation.
    float intensity = EllipsoidalDistanceAttenuation(unL, axis, invAspectRatio,
                                                     lightData.rangeAttenuationScale,
                                                     lightData.rangeAttenuationBias);

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0)
        return lighting;

    lightData.diffuseDimmer *= intensity;
    lightData.specularDimmer *= intensity;

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    float3  lightPositionRWS = lightData.positionRWS - positionWS;

    // TODO: some of this could be precomputed.
    float3  P1 = lightPositionRWS - T * (0.5 * len);
    float3  P2 = lightPositionRWS + T * (0.5 * len);

    // Rotate the endpoints into the local coordinate system.
    P1 = mul(P1, transpose(preLightData.orthoBasisViewNormal));
    P2 = mul(P2, transpose(preLightData.orthoBasisViewNormal));

    // Compute the binormal in the local coordinate system.
    float3  B = normalize(cross(P1, P2));

    float   ltcValue;

    //-----------------------------------------------------------------------------
#if defined(_AXF_BRDF_TYPE_SVBRDF)

    // Evaluate the diffuse part
    // Polygon irradiance in the transformed configuration.
    ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformDiffuse);
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = preLightData.ltcTransformDiffuse_Amplitude * ltcValue;


    // Evaluate the specular part
    // Polygon irradiance in the transformed configuration.
    ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformSpecular);
    ltcValue *= lightData.specularDimmer;
    lighting.specular = bsdfData.specularColor * preLightData.ltcTransformSpecular_Amplitude * ltcValue;

    //-----------------------------------------------------------------------------
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    //-----------------------------------------------------------------------------
    // Use Lambert for diffuse
    ltcValue = LTCEvaluate(P1, P2, B, k_identity3x3);    // No transform: Lambert uses identity
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = ltcValue;

    // Evaluate average BRDF response in diffuse direction
    // We project the point onto the area light's plane using the light's forward direction and recompute the light direction from this position
    float3  bestLightWS_Diffuse = ComputeBestLightDirection_Line(lightPositionRWS, -lightData.forward, lightData);

    float3  H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Diffuse);
    float   NdotH = dot(bsdfData.normalWS, H);
    float   VdotH = dot(preLightData.viewWS_UnderCoat, H);

    float   thetaH = acos(clamp(NdotH, -1, 1));
    float   thetaD = acos(clamp(VdotH, -1, 1));

    lighting.diffuse *= GetBRDFColor(thetaH, thetaD);


    //-----------------------------------------------------------------------------
    // Evaluate multi-lobes Cook-Torrance
    // Each CT lobe samples the environment with the appropriate roughness
    for (uint lobeIndex = 0; lobeIndex < _CarPaint2_LobeCount; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = _CarPaint2_CTSpreads[lobeIndex];

        float   perceptualRoughness = RoughnessToPerceptualRoughness(spread);

        float2      UV = LTCGetSamplingUV(NdotV, perceptualRoughness);
        float3x3    ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_COOK_TORRANCE);

        ltcValue = LTCEvaluate(P1, P2, B, ltcTransformSpecular);

        // Apply FGD
        float3  specularFGD = 1;
        float   diffuseFGD, reflectivity;
        GetPreIntegratedFGDCookTorranceAndLambert(NdotV, perceptualRoughness, F0, specularFGD, diffuseFGD, reflectivity);

        lighting.specular += coeff * specularFGD * ltcValue;
    }
    lighting.specular *= lightData.specularDimmer;

    // Evaluate average BRDF response in specular direction
    // We project the point onto the area light's plane using the reflected view direction and recompute the light direction from this position
    float3  bestLightWS_Specular = ComputeBestLightDirection_Line(lightPositionRWS, preLightData.iblDominantDirectionWS_UnderCoat, lightData);

    H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Specular);
    NdotH = dot(bsdfData.normalWS, H);
    VdotH = dot(preLightData.viewWS_UnderCoat, H);

    thetaH = acos(clamp(NdotH, -1, 1));
    thetaD = acos(clamp(VdotH, -1, 1));

    lighting.specular *= GetBRDFColor(thetaH, thetaD);


    //-----------------------------------------------------------------------------
    // Sample flakes as tiny mirrors
    float2      UV = LTCGetSamplingUV(NdotV, FLAKES_PERCEPTUAL_ROUGHNESS);
    float3x3    ltcTransformFlakes = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_GGX);

    ltcValue = LTCEvaluate(P1, P2, B, ltcTransformFlakes);
    ltcValue *= lightData.specularDimmer;

    // Apply FGD
    float3  flakes_FGD;
    float   specularReflectivity, dummyDiffuseFGD;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV, FLAKES_PERCEPTUAL_ROUGHNESS, FLAKES_F0, flakes_FGD, dummyDiffuseFGD, specularReflectivity);

    lighting.specular += flakes_FGD * ltcValue * CarPaint_BTF(thetaH, thetaD, bsdfData);

#endif

    //-----------------------------------------------------------------------------

    // Evaluate the clear-coat
    if (_Flags & 2)
    {

        // Use the complement of FGD value as an approximation of the extinction of the undercoat
        float3  clearcoatExtinction = 1.0 - preLightData.ltcTransformClearcoat_Amplitude;

        // Apply clear-coat extinction to existing lighting
        lighting.diffuse *= clearcoatExtinction;
        lighting.specular *= clearcoatExtinction;

        // Then add clear-coat contribution
        ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformClearcoat);
        ltcValue *= lightData.specularDimmer;
        lighting.specular += preLightData.ltcTransformClearcoat_Amplitude * ltcValue * (bsdfData.clearcoatColor / PI);
    }

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        // Apply area light on lambert then multiply by PI to cancel Lambert
        lighting.diffuse = LTCEvaluate(P1, P2, B, k_identity3x3);
        lighting.diffuse *= PI * lightData.diffuseDimmer;
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting  EvaluateBSDF_Rect(LightLoopContext lightLoopContext,
    float3 viewWS_Clearcoat, PositionInputs posInput,
    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3  positionWS = posInput.positionWS;

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    float3  lightPositionRWS = lightData.positionRWS - positionWS;
    if (dot(lightData.forward, lightPositionRWS) >= 0.0001)
    {
        return lighting;    // The light is back-facing.
    }

    // Rotate the light direction into the light space.
    float3x3    lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
    float3      unL = mul(lightPositionRWS, transpose(lightToWorld));

    // TODO: This could be precomputed.
    float   halfWidth = lightData.size.x * 0.5;
    float   halfHeight = lightData.size.y * 0.5;

    // Define the dimensions of the attenuation volume.
    // TODO: This could be precomputed.
    float   radius = rsqrt(lightData.rangeAttenuationScale); // rangeAttenuationScale is inverse Square Radius
    float3  invHalfDim = rcp(float3(radius + halfWidth,
        radius + halfHeight,
        radius));

    // Compute the light attenuation.
#ifdef ELLIPSOIDAL_ATTENUATION
    // The attenuation volume is an axis-aligned ellipsoid s.t.
    // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
    float intensity = EllipsoidalDistanceAttenuation(unL, invHalfDim,
                                                     lightData.rangeAttenuationScale,
                                                     lightData.rangeAttenuationBias);
#else
    // The attenuation volume is an axis-aligned box s.t.
    // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
    float intensity = BoxDistanceAttenuation(unL, invHalfDim,
                                             lightData.rangeAttenuationScale,
                                             lightData.rangeAttenuationBias);
#endif

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0)
        return lighting;

    lightData.diffuseDimmer *= intensity;
    lightData.specularDimmer *= intensity;

    // TODO: some of this could be precomputed.
    float4x3    lightVerts;
    lightVerts[0] = lightPositionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight;
    lightVerts[1] = lightPositionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight;
    lightVerts[2] = lightPositionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight;
    lightVerts[3] = lightPositionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight;

    // Rotate the endpoints into tangent space
    lightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

    float   ltcValue;

    //-----------------------------------------------------------------------------

#if defined(_AXF_BRDF_TYPE_SVBRDF)

    // Evaluate the diffuse part
    // Polygon irradiance in the transformed configuration.
    ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformDiffuse));
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = preLightData.ltcTransformDiffuse_Amplitude * ltcValue;


    // Evaluate the specular part
    // Polygon irradiance in the transformed configuration.
    ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformSpecular));
    ltcValue *= lightData.specularDimmer;
    lighting.specular = bsdfData.specularColor * preLightData.ltcTransformSpecular_Amplitude * ltcValue;

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    //-----------------------------------------------------------------------------
    // Use Lambert for diffuse
//        float3  bestLightWS_Diffuse;
//        ltcValue  = PolygonIrradiance(lightVerts, bestLightWS_Diffuse);    // No transform: Lambert uses identity
//        bestLightWS_Diffuse = normalize(bestLightWS_Diffuse);
    ltcValue = PolygonIrradiance(lightVerts);    // No transform: Lambert uses identity
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = ltcValue;

    // Evaluate average BRDF response in diffuse direction
    // We project the point onto the area light's plane using the light's forward direction and recompute the light direction from this position
    float3  bestLightWS_Diffuse = ComputeBestLightDirection_Rectangle(lightPositionRWS, -lightData.forward, lightData);

    float3  H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Diffuse);
    float   NdotH = dot(bsdfData.normalWS, H);
    float   VdotH = dot(preLightData.viewWS_UnderCoat, H);

    float   thetaH = acos(clamp(NdotH, -1, 1));
    float   thetaD = acos(clamp(VdotH, -1, 1));

    lighting.diffuse *= GetBRDFColor(thetaH, thetaD);


    //-----------------------------------------------------------------------------
    // Evaluate multi-lobes Cook-Torrance
    // Each CT lobe samples the environment with the appropriate roughness
    for (uint lobeIndex = 0; lobeIndex < _CarPaint2_LobeCount; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = _CarPaint2_CTSpreads[lobeIndex];

        float   perceptualRoughness = RoughnessToPerceptualRoughness(spread);

        float2      UV = LTCGetSamplingUV(NdotV, perceptualRoughness);
        float3x3    ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_COOK_TORRANCE);

        ltcValue = PolygonIrradiance(mul(lightVerts, ltcTransformSpecular));

        // Apply FGD
        float3  specularFGD = 1;
        float   diffuseFGD, reflectivity;
        GetPreIntegratedFGDCookTorranceAndLambert(NdotV, perceptualRoughness, F0, specularFGD, diffuseFGD, reflectivity);

        lighting.specular += coeff * specularFGD * ltcValue;
    }
    lighting.specular *= lightData.specularDimmer;

    // Evaluate average BRDF response in specular direction
    // We project the point onto the area light's plane using the reflected view direction and recompute the light direction from this position
    float3  bestLightWS_Specular = ComputeBestLightDirection_Rectangle(lightPositionRWS, preLightData.iblDominantDirectionWS_UnderCoat, lightData);

    H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Specular);
    NdotH = dot(bsdfData.normalWS, H);
    VdotH = dot(preLightData.viewWS_UnderCoat, H);

    thetaH = acos(clamp(NdotH, -1, 1));
    thetaD = acos(clamp(VdotH, -1, 1));

    lighting.specular *= GetBRDFColor(thetaH, thetaD);


    //-----------------------------------------------------------------------------
    // Sample flakes as tiny mirrors
    float2      UV = LTCGetSamplingUV(NdotV, FLAKES_PERCEPTUAL_ROUGHNESS);
    float3x3    ltcTransformFlakes = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_GGX);

    ltcValue = PolygonIrradiance(mul(lightVerts, ltcTransformFlakes));
    ltcValue *= lightData.specularDimmer;

    // Apply FGD
    float3  flakes_FGD;
    float   specularReflectivity, dummyDiffuseFGD;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV, FLAKES_PERCEPTUAL_ROUGHNESS, FLAKES_F0, flakes_FGD, dummyDiffuseFGD, specularReflectivity);

    lighting.specular += flakes_FGD * ltcValue * CarPaint_BTF(thetaH, thetaD, bsdfData);

#endif


    //-----------------------------------------------------------------------------

    // Evaluate the clear-coat
    if (_Flags & 2)
    {

        // Use the complement of FGD value as an approximation of the extinction of the undercoat
        float3  clearcoatExtinction = 1.0 - preLightData.ltcTransformClearcoat_Amplitude;

        // Apply clear-coat extinction to existing lighting
        lighting.diffuse *= clearcoatExtinction;
        lighting.specular *= clearcoatExtinction;

        // Then add clear-coat contribution
        ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformClearcoat));
        ltcValue *= lightData.specularDimmer;
        lighting.specular += preLightData.ltcTransformClearcoat_Amplitude * ltcValue * (bsdfData.clearcoatColor / PI);
    }

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        // Apply area light on lambert then multiply by PI to cancel Lambert
        lighting.diffuse = PolygonIrradiance(mul(lightVerts, k_identity3x3));
        lighting.diffuse *= PI * lightData.diffuseDimmer;
    }
#endif

    return lighting;
}

DirectLighting  EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 viewWS, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{

    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, viewWS, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, viewWS, posInput, preLightData, lightData, bsdfData, builtinData);
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

    // TODO

    return lighting;
}

IndirectLighting    EvaluateBSDF_ScreenspaceRefraction( LightLoopContext lightLoopContext,
                                                        float3 viewWS_Clearcoat, PositionInputs posInput,
                                                        PreLightData preLightData, BSDFData bsdfData,
                                                        EnvLightData _envLightData,
                                                        inout float hierarchyWeight)
{

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    //NEWLITTODO

// Apply coating
//specularLighting += F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotN) * Irradiance;

    return lighting;
}


//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 viewWS_Clearcoat, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int _influenceShapeType, int _GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    if (_GPUImageBasedLightingType != GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        return lighting;    // We don't support transmission

    float3  positionWS = posInput.positionWS;
    float   weight = 1.0;

    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    float3  environmentSamplingDirectionWS_UnderCoat = preLightData.iblDominantDirectionWS_UnderCoat;

#if defined(_AXF_BRDF_TYPE_SVBRDF)
    if ((lightData.envIndex & 1) == ENVCACHETYPE_CUBEMAP)
    {
        // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
        // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
        // Formula is empirical.
        environmentSamplingDirectionWS_UnderCoat = GetSpecularDominantDir(bsdfData.normalWS, environmentSamplingDirectionWS_UnderCoat, preLightData.iblPerceptualRoughness, NdotV);
        float   IBLRoughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
        environmentSamplingDirectionWS_UnderCoat = lerp(environmentSamplingDirectionWS_UnderCoat, preLightData.iblDominantDirectionWS_UnderCoat, saturate(smoothstep(0, 1, IBLRoughness * IBLRoughness)));
    }

    // Note: using _influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, _influenceShapeType, environmentSamplingDirectionWS_UnderCoat, weight);

    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
    float   IBLMipLevel;
#if !defined(SHADER_API_METAL)
    if (IsEnvIndexTexture2D(lightData.envIndex))
    {
        // Empirical remapping
        IBLMipLevel = PlanarPerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness, _ColorPyramidScale.z);
    }
    else
#endif
    {
        IBLMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);
    }

    //-----------------------------------------------------------------------------
    // Use FGD as factor for the env map
    float3  envBRDF = preLightData.specularFGD;

    // Sample the actual environment lighting
    float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, environmentSamplingDirectionWS_UnderCoat, IBLMipLevel);
    weight *= preLD.w; // Used by planar reflection to discard pixel

    float3  envLighting = envBRDF * preLD.xyz;

    //-----------------------------------------------------------------------------
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    // Evaluate BRDF response in specular direction
    float3  viewWS_UnderCoat = preLightData.viewWS_UnderCoat;
    float3  lightWS_UnderCoat = environmentSamplingDirectionWS_UnderCoat;

    float3  H = normalize(viewWS_UnderCoat + lightWS_UnderCoat);
    float   NdotL = saturate(dot(bsdfData.normalWS, lightWS_UnderCoat));
    float   NdotH = dot(bsdfData.normalWS, H);
    float   VdotH = dot(viewWS_UnderCoat, H);

    float   thetaH = acos(clamp(NdotH, -1, 1));
    float   thetaD = acos(clamp(VdotH, -1, 1));

    //-----------------------------------------------------------------------------
#if USE_COOK_TORRANCE_MULTI_LOBES
    // Multi-lobes approach
    // Each CT lobe samples the environment with the appropriate roughness
    float3  envLighting = 0.0;
    float   sumWeights = 0.0;
    for (uint lobeIndex = 0; lobeIndex < _CarPaint2_LobeCount; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = _CarPaint2_CTSpreads[lobeIndex];

        float   perceptualRoughness = RoughnessToPerceptualRoughness(spread);

        float   lobeIntensity = coeff * CT_D(NdotH, spread) * CT_F(VdotH, F0);
        float   lobeMipLevel = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
        float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, lightWS_UnderCoat, lobeMipLevel);

        // Apply FGD
        float3  specularFGD = 1;
        float   diffuseFGD, reflectivity;
        GetPreIntegratedFGDCookTorranceAndLambert(NdotV, perceptualRoughness, F0, specularFGD, diffuseFGD, reflectivity);

        envLighting += lobeIntensity * specularFGD * preLD.xyz;
        sumWeights += preLD.w;
    }
    envLighting *= G_CookTorrance(NdotH, NdotV, NdotL, VdotH)  // Shadowing/Masking term
        / (PI * max(1e-3, NdotV * NdotL));
    envLighting *= GetBRDFColor(thetaH, thetaD);

    // Sample flakes
    float   flakesMipLevel = 0;   // Flakes are supposed to be perfect mirrors...
    envLighting += CarPaint_BTF(thetaH, thetaD, bsdfData) * SampleEnv(lightLoopContext, lightData.envIndex, lightWS_UnderCoat, flakesMipLevel).xyz;

    envLighting *= NdotL;

    weight *= sumWeights / _CarPaint2_LobeCount;

#else
    // Single lobe approach
    // We computed an average mip level stored in preLightData.iblPerceptualRoughness that we use for all CT lobes
    //
    float3  envBRDF = MultiLobesCookTorrance(NdotL, NdotV, NdotH, VdotH); // Specular multi-lobes CT
    envBRDF *= GetBRDFColor(thetaH, thetaD);
    envBRDF += CarPaint_BTF(thetaH, thetaD, bsdfData);            // Sample flakes

    envBRDF *= NdotL;

    // Sample the actual environment lighting
    float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, lightWS_UnderCoat, IBLMipLevel);
    float3  envLighting = envBRDF * preLD.xyz;

    weight *= preLD.w; // Used by planar reflection to discard pixel

#endif

//-----------------------------------------------------------------------------
#else

    float3  envLighting = 0;

#endif

    //-----------------------------------------------------------------------------
    // Evaluate the clearcoat component if needed
    if (_Flags & 2)
    {

        // Evaluate clearcoat sampling direction
        float   unusedWeight = 0.0;
        float3  lightWS_Clearcoat = preLightData.iblDominantDirectionWS_Clearcoat;
        EvaluateLight_EnvIntersection(positionWS, bsdfData.clearcoatNormalWS, lightData, _influenceShapeType, lightWS_Clearcoat, unusedWeight);

        // Evaluate clearcoat fresnel
#if 1   // Use LdotH ==> Makes more sense! Stick to Cook-Torrance here...
        float3  H = normalize(viewWS_Clearcoat + lightWS_Clearcoat);
        float   LdotH = saturate(dot(lightWS_Clearcoat, H));
        float3  clearcoatF = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotH);
#else   // Use LdotN
        float   LdotN = saturate(dot(lightWS_Clearcoat, bsdfData.clearcoatNormalWS));
        float3  clearcoatF = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotN);
#endif

        // Attenuate environment lighting under the clearcoat by the complement to the Fresnel term
        envLighting *= 1.0 - clearcoatF;

        // Then add the environment lighting reflected by the clearcoat
        // We assume the BRDF here is perfect mirror so there's no masking/shadowing, only the Fresnel term * clearcoatColor/PI
        float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, lightWS_Clearcoat, 0.0);
        envLighting += (bsdfData.clearcoatColor / PI) * clearcoatF * preLD.xyz;

        // Can't attenuate diffuse lighting here, may try to apply something on bakeLighting in PostEvaluateBSDF
    }

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
    // There is no AmbientOcclusion from data with AxF, but let's apply our SSAO
    AmbientOcclusionFactor aoFactor;
    GetScreenSpaceAmbientOcclusionMultibounce(  posInput.positionSS, preLightData.NdotV_UnderCoat, RoughnessToPerceptualRoughness(0.5 * (bsdfData.roughness.x + bsdfData.roughness.y)),
                                                1.0, 1.0, bsdfData.diffuseColor, bsdfData.fresnelF0, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting;
    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#if !defined(_AXF_BRDF_TYPE_SVBRDF) && !defined(_AXF_BRDF_TYPE_CAR_PAINT)
    // Not supported: Display a flashy color instead
    diffuseLighting = 10 * float3(1, 0.3, 0.01);
#endif

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, diffuseLighting, specularLighting);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
