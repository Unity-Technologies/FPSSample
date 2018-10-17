#ifndef UNITY_MATERIAL_INCLUDED
#define UNITY_MATERIAL_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

// Guidelines for Material Keyword.
// There is a set of Material Keyword that a HD shaders must define (or not define). We call them system KeyWord.
// .Shader need to define:
// - _SURFACE_TYPE_TRANSPARENT if they use a transparent material
// - _BLENDMODE_ALPHA, _BLENDMODE_ADD, _BLENDMODE_PRE_MULTIPLY for blend mode
// - _BLENDMODE_PRESERVE_SPECULAR_LIGHTING for correct lighting when blend mode are use with a Lit material
// - _ENABLE_FOG_ON_TRANSPARENT if fog is enable on transparent surface
// - _DISABLE_DECALS if the material don't support decals

#define HAVE_DECALS ( (defined(DECALS_3RT) || defined(DECALS_4RT)) && !defined(_DISABLE_DECALS) )

//-----------------------------------------------------------------------------
// ApplyBlendMode function
//-----------------------------------------------------------------------------

float4 ApplyBlendMode(float3 diffuseLighting, float3 specularLighting, float opacity)
{
    // ref: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
    // Lit transparent object should have reflection and tramission.
    // Transmission when not using "rough refraction mode" (with fetch in preblured background) is handled with blend mode.
    // However reflection should not be affected by blend mode. For example a glass should still display reflection and not lose the highlight when blend
    // This is the purpose of following function, "Cancel" the blend mode effect on the specular lighting but not on the diffuse lighting
#ifdef _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
    // In the case of _BLENDMODE_ALPHA the code should be float4(diffuseLighting + (specularLighting / max(opacity, 0.01)), opacity)
    // However this have precision issue when reaching 0, so we change the blend mode and apply src * src_a inside the shader instead
    #if defined(_BLENDMODE_ADD) || defined(_BLENDMODE_ALPHA)
    return float4(diffuseLighting * opacity + specularLighting, opacity);
    #else // defined(_BLENDMODE_PRE_MULTIPLY)
    return float4(diffuseLighting + specularLighting, opacity);
    #endif
#else
    #if defined(_BLENDMODE_ADD) || defined(_BLENDMODE_ALPHA)
    return float4((diffuseLighting + specularLighting) * opacity, opacity);
    #else // defined(_BLENDMODE_PRE_MULTIPLY)
    return float4(diffuseLighting + specularLighting, opacity);
    #endif
#endif
}

float4 ApplyBlendMode(float3 color, float opacity)
{
    return ApplyBlendMode(color, float3(0.0, 0.0, 0.0), opacity);
}

//-----------------------------------------------------------------------------
// Fog sampling function for materials
//-----------------------------------------------------------------------------

// Used for transparent object. input color is color + alpha of the original transparent pixel.
// This must be call after ApplyBlendMode to work correctly
float4 EvaluateAtmosphericScattering(PositionInputs posInput, float4 inputColor)
{
    float4 result = inputColor;

#ifdef _ENABLE_FOG_ON_TRANSPARENT
    float4 fog = EvaluateAtmosphericScattering(posInput);

    #if defined(_BLENDMODE_ALPHA)
    // Regular alpha blend need to multiply fog color by opacity (as we do src * src_a inside the shader)
    result.rgb = lerp(result.rgb, fog.rgb * result.a, fog.a);
    #elif defined(_BLENDMODE_ADD)
    // For additive, we just need to fade to black with fog density (black + background == background color == fog color)
    result.rgb = result.rgb * (1.0 - fog.a);
    #elif defined(_BLENDMODE_PRE_MULTIPLY)
    // For Pre-Multiplied Alpha Blend, we need to multiply fog color by src alpha to match regular alpha blending formula.
    result.rgb = lerp(result.rgb, fog.rgb * result.a, fog.a);
    #endif
#else
    // Evaluation of fog for opaque objects is currently done in a full screen pass independent from any material parameters.
    // but this funtction is called in generic forward shader code so we need it to be neutral in this case.
#endif

    return result;
}

//-----------------------------------------------------------------------------
// Alpha test replacement
//-----------------------------------------------------------------------------

// This function must be use instead of clip instruction. It allow to manage in which case the clip is perform for optimization purpose
void DoAlphaTest(float alpha, float alphaCutoff)
{
    // For Deferred:
    // If we have a prepass, we  may want to remove the clip from the GBuffer pass (otherwise HiZ does not work on PS4) - SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
    // For Forward Opaque:
    // If we have a prepass, we may want to remove the clip from the forward pass (otherwise HiZ does not work on PS4) - SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
    // For Forward Transparent
    // Also no alpha test for light transport
    // Note: If SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST or SHADERPASS_FORWARD_BYPASS_ALPHA_TEST are used, it mean that we must use ZTest depth equal for the pass (Need to use _ZTestDepthEqualForOpaque property).
#if !defined(SHADERPASS_FORWARD_BYPASS_ALPHA_TEST) && !defined(SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST) && !(SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
    clip(alpha - alphaCutoff);
#endif
}

//-----------------------------------------------------------------------------
// Reflection / Refraction hierarchy handling
//-----------------------------------------------------------------------------

// This function is use with reflection and refraction hierarchy of LightLoop
// It will add weight to hierarchyWeight but ensure that hierarchyWeight is not more than one
// by updating the weight value. Returned weight value must be apply on current lighting
// Example: Total hierarchyWeight is 0.8 and weight is 0.4. Function return hierarchyWeight of 1.0 and weight of 0.2
// hierarchyWeight and weight must be positive and between 0 and 1
void UpdateLightingHierarchyWeights(inout float hierarchyWeight, inout float weight)
{
    float accumulatedWeight = hierarchyWeight + weight;
    hierarchyWeight = saturate(accumulatedWeight);
    weight -= saturate(accumulatedWeight - hierarchyWeight);
}

//-----------------------------------------------------------------------------
// BuiltinData
//-----------------------------------------------------------------------------

#include "Builtin/BuiltinData.hlsl"

//-----------------------------------------------------------------------------
// Material definition
//-----------------------------------------------------------------------------

// Here we include all the different lighting model supported by the renderloop based on define done in .shader
// Only one deferred layout is allowed for a HDRenderPipeline, this will be detect by the redefinition of GBUFFERMATERIAL_COUNT
// If GBUFFERMATERIAL_COUNT is define two time, the shaders will not compile
#ifdef UNITY_MATERIAL_LIT
#include "Lit/Lit.hlsl"
#elif defined(UNITY_MATERIAL_UNLIT)
#include "Unlit/Unlit.hlsl"
#elif defined(UNITY_MATERIAL_STACKLIT)
#include "StackLit/StackLit.hlsl"
#elif defined(UNITY_MATERIAL_FABRIC)
#include "Fabric/Fabric.hlsl"
#elif defined(UNITY_MATERIAL_AXF)
#include "AxF/AxF.hlsl"
#endif

//-----------------------------------------------------------------------------
// Define for GBuffer management
//-----------------------------------------------------------------------------

#ifdef GBUFFERMATERIAL_COUNT

#if GBUFFERMATERIAL_COUNT == 2

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1))

#elif GBUFFERMATERIAL_COUNT == 3

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))

#elif GBUFFERMATERIAL_COUNT == 4

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))

#elif GBUFFERMATERIAL_COUNT == 5

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4))

#elif GBUFFERMATERIAL_COUNT == 6

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4,    \
        out GBufferType5 MERGE_NAME(NAME, 5) : SV_Target5

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), MERGE_NAME(NAME, 5))

#elif GBUFFERMATERIAL_COUNT == 7

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4,    \
        out GBufferType5 MERGE_NAME(NAME, 5) : SV_Target5,    \
        out GBufferType6 MERGE_NAME(NAME, 6) : SV_Target6

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), MERGE_NAME(NAME, 5), MERGE_NAME(NAME, 6))

#elif GBUFFERMATERIAL_COUNT == 8

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4,    \
        out GBufferType5 MERGE_NAME(NAME, 5) : SV_Target5,    \
        out GBufferType6 MERGE_NAME(NAME, 6) : SV_Target6,    \
        out GBufferType7 MERGE_NAME(NAME, 7) : SV_Target7

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), MERGE_NAME(NAME, 5), MERGE_NAME(NAME, 6), MERGE_NAME(NAME, 7))

#endif

#define DECODE_FROM_GBUFFER(UNPOSITIONSS, FEATURE_FLAGS, BSDF_DATA, BUILTIN_DATA) DecodeFromGBuffer(UNPOSITIONSS, FEATURE_FLAGS, BSDF_DATA, BUILTIN_DATA)
#define MATERIAL_FEATURE_FLAGS_FROM_GBUFFER(UNPOSITIONSS) MaterialFeatureFlagsFromGBuffer(UNPOSITIONSS)

#endif // #ifdef GBUFFERMATERIAL_COUNT


#endif // UNITY_MATERIAL_INCLUDED
