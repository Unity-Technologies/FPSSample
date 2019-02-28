// Upgrade NOTE: replaced 'defined at' with 'defined (at)'


#ifdef DEBUG_DISPLAY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#endif
#ifndef SHADERPASS
#error SHADERPASS must be defined (at) this point
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

#if (SHADERPASS == SHADERPASS_FORWARD)
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

    // The light loop (or lighting architecture) is in charge to:
    // - Define light list
    // - Define the light loop
    // - Setup the constant/data
    // - Do the reflection hierarchy
    // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

    #define HAS_LIGHTLOOP

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

    #ifdef HDRP_MATERIAL_TYPE_SIMPLE
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/SimpleLit.hlsl"
        #define _DISABLE_SSR
    #else
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
    #endif
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

#else // (SHADERPASS == SHADERPASS_FORWARD)

    #ifdef HDRP_MATERIAL_TYPE_SIMPLE
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/SimpleLit.hlsl"
    #else
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
    #endif
#endif // (SHADERPASS == SHADERPASS_FORWARD)

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

float3 VFXGetPositionRWS(VFX_VARYING_PS_INPUTS i)
{
    float3 posRWS = (float3)0;
    #ifdef VFX_VARYING_POSWS
    posRWS = i.VFX_VARYING_POSWS;
    #endif
    #if VFX_WORLD_SPACE
    posRWS = GetCameraRelativePositionWS(posRWS);
    #endif
    return posRWS;
}

BuiltinData VFXGetBuiltinData(const VFX_VARYING_PS_INPUTS i,const PositionInputs posInputs, const SurfaceData surfaceData, const BSDFData bsdfData, const PreLightData preLightData, const VFXUVData uvData, float opacity = 1.0f)
{
    BuiltinData builtinData = (BuiltinData)0;

    InitBuiltinData(opacity, surfaceData.normalWS, -surfaceData.normalWS, posInputs.positionWS, (float4)0, (float4)0, builtinData); // We dont care about uvs are we dont sample lightmaps

    #if HDRP_USE_EMISSIVE
    builtinData.emissiveColor = float3(1,1,1);
    #if HDRP_USE_EMISSIVE_MAP
    float emissiveScale = 1.0f;
    #ifdef VFX_VARYING_EMISSIVESCALE
    emissiveScale = i.VFX_VARYING_EMISSIVESCALE;
    #endif
    builtinData.emissiveColor *= SampleTexture(VFX_SAMPLER(emissiveMap),uvData).rgb * emissiveScale;
    #endif
    #if defined(VFX_VARYING_EMISSIVE) && (HDRP_USE_EMISSIVE_COLOR || HDRP_USE_ADDITIONAL_EMISSIVE_COLOR)
    builtinData.emissiveColor *= i.VFX_VARYING_EMISSIVE;
    #endif
    #endif
    builtinData.emissiveColor *= opacity;

    PostInitBuiltinData(GetWorldSpaceNormalizeViewDir(posInputs.positionWS),posInputs,surfaceData, builtinData);

    return builtinData;
}

SurfaceData VFXGetSurfaceData(const VFX_VARYING_PS_INPUTS i, float3 normalWS,const VFXUVData uvData, uint diffusionProfile, out float opacity)
{
    SurfaceData surfaceData = (SurfaceData)0;

    float4 color = float4(1,1,1,1);
    #if HDRP_USE_BASE_COLOR
    color *= VFXGetParticleColor(i);
    #elif HDRP_USE_ADDITIONAL_BASE_COLOR
    #if defined(VFX_VARYING_COLOR)
    color.xyz *= i.VFX_VARYING_COLOR;
    #endif
    #if defined(VFX_VARYING_ALPHA)
    color.a *= i.VFX_VARYING_ALPHA;
    #endif
    #endif
    #if HDRP_USE_BASE_COLOR_MAP
    float4 colorMap = SampleTexture(VFX_SAMPLER(baseColorMap),uvData);
    #if HDRP_USE_BASE_COLOR_MAP_COLOR
    color.xyz *= colorMap.xyz;
    #endif
    #if HDRP_USE_BASE_COLOR_MAP_ALPHA
    color.a *= colorMap.a;
    #endif
    #endif
    color.a *= VFXGetSoftParticleFade(i);
    VFXClipFragmentColor(color.a,i);
    surfaceData.baseColor = saturate(color.rgb);

    #if IS_OPAQUE_PARTICLE
    opacity = 1.0f;
    #else
    opacity = saturate(color.a);
    #endif

    #if HDRP_MATERIAL_TYPE_STANDARD
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
    #ifdef VFX_VARYING_METALLIC
    surfaceData.metallic = i.VFX_VARYING_METALLIC;
    #endif
    #elif HDRP_MATERIAL_TYPE_SPECULAR
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
    #ifdef VFX_VARYING_SPECULAR
    surfaceData.specularColor = saturate(i.VFX_VARYING_SPECULAR);
    #endif
    #elif HDRP_MATERIAL_TYPE_TRANSLUCENT
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_TRANSMISSION;
    #ifdef VFX_VARYING_THICKNESS
    surfaceData.thickness = i.VFX_VARYING_THICKNESS * opacity;
    #endif
    surfaceData.diffusionProfile = diffusionProfile;
    surfaceData.subsurfaceMask = 1.0f;
    #endif

    surfaceData.normalWS = normalWS;
    #ifdef VFX_VARYING_SMOOTHNESS
    surfaceData.perceptualSmoothness = i.VFX_VARYING_SMOOTHNESS;
    #endif
    surfaceData.specularOcclusion = 1.0f;
    surfaceData.ambientOcclusion = 1.0f;

    #if HDRP_USE_MASK_MAP
    float4 mask = SampleTexture(VFX_SAMPLER(maskMap),uvData);
    surfaceData.metallic *= mask.r;
    surfaceData.ambientOcclusion *= mask.g;
    surfaceData.perceptualSmoothness *= mask.a;
    #endif

    return surfaceData;
}
