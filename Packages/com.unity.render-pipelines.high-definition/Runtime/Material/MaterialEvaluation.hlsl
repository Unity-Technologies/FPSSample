// This files include various function uses to evaluate material

//-----------------------------------------------------------------------------
// Lighting structure for light accumulation
//-----------------------------------------------------------------------------

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
struct DirectLighting
{
    float3 diffuse;
    float3 specular;
};

struct IndirectLighting
{
    float3 specularReflected;
    float3 specularTransmitted;
};

struct AggregateLighting
{
    DirectLighting   direct;
    IndirectLighting indirect;
};

void AccumulateDirectLighting(DirectLighting src, inout AggregateLighting dst)
{
    dst.direct.diffuse += src.diffuse;
    dst.direct.specular += src.specular;
}

void AccumulateIndirectLighting(IndirectLighting src, inout AggregateLighting dst)
{
    dst.indirect.specularReflected += src.specularReflected;
    dst.indirect.specularTransmitted += src.specularTransmitted;
}

//-----------------------------------------------------------------------------
// Ambient occlusion helper
//-----------------------------------------------------------------------------

// Ambient occlusion
struct AmbientOcclusionFactor
{
    float3 indirectAmbientOcclusion;
    float3 directAmbientOcclusion;
    float3 indirectSpecularOcclusion;
};

// Get screen space ambient occlusion only:
float GetScreenSpaceDiffuseOcclusion(float2 positionSS)
{
    // Note: When we ImageLoad outside of texture size, the value returned by Load is 0 (Note: On Metal maybe it clamp to value of texture which is also fine)
    // We use this property to have a neutral value for AO that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
    // We store inverse AO so neutral is black. So either we sample inside or outside the texture it return 0 in case of neutral
     // Ambient occlusion use for indirect lighting (reflection probe, baked diffuse lighting)
#ifndef _SURFACE_TYPE_TRANSPARENT
    float indirectAmbientOcclusion = 1.0 - LOAD_TEXTURE2D(_AmbientOcclusionTexture, positionSS).x;
#else
    float indirectAmbientOcclusion = 1.0;
#endif
    return indirectAmbientOcclusion;
}

void GetScreenSpaceAmbientOcclusion(float2 positionSS, float NdotV, float perceptualRoughness, float ambientOcclusionFromData, float specularOcclusionFromData, out AmbientOcclusionFactor aoFactor)
{
    float indirectAmbientOcclusion = GetScreenSpaceDiffuseOcclusion(positionSS);
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);

    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(NdotV), indirectAmbientOcclusion, roughness);

    aoFactor.indirectSpecularOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(specularOcclusionFromData, specularOcclusion));
    aoFactor.indirectAmbientOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(ambientOcclusionFromData, indirectAmbientOcclusion));
    aoFactor.directAmbientOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), directAmbientOcclusion);
}

// Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the diffuseColor)
void GetScreenSpaceAmbientOcclusionMultibounce(float2 positionSS, float NdotV, float perceptualRoughness, float ambientOcclusionFromData, float specularOcclusionFromData, float3 diffuseColor, float3 fresnel0, out AmbientOcclusionFactor aoFactor)
{
    float indirectAmbientOcclusion = GetScreenSpaceDiffuseOcclusion(positionSS);
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);

    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(NdotV), indirectAmbientOcclusion, roughness);

    aoFactor.indirectSpecularOcclusion = GTAOMultiBounce(min(specularOcclusionFromData, specularOcclusion), fresnel0);
    aoFactor.indirectAmbientOcclusion = GTAOMultiBounce(min(ambientOcclusionFromData, indirectAmbientOcclusion), diffuseColor);
    aoFactor.directAmbientOcclusion = GTAOMultiBounce(directAmbientOcclusion, diffuseColor);
}

void ApplyAmbientOcclusionFactor(AmbientOcclusionFactor aoFactor, inout BuiltinData builtinData, inout AggregateLighting lighting)
{
    // Note: In case of deferred Lit, builtinData.bakeDiffuseLighting contains indirect diffuse * surfaceData.ambientOcclusion + emissive,
    // so SSAO is multiplied by emissive which is wrong.
    // Also, we have double occlusion for diffuse lighting since it already had precomputed AO (aka "FromData") applied
    // (the * surfaceData.ambientOcclusion above)
    // This is a tradeoff to avoid storing the precomputed (from data) AO in the GBuffer.
    // (This is also why GetScreenSpaceAmbientOcclusion*() is effectively called with AOFromData = 1.0 in Lit:PostEvaluateBSDF() in the 
    // deferred case since DecodeFromGBuffer will init bsdfData.ambientOcclusion to 1.0 and we will only have SSAO in the aoFactor here)
    builtinData.bakeDiffuseLighting *= aoFactor.indirectAmbientOcclusion;
    lighting.indirect.specularReflected *= aoFactor.indirectSpecularOcclusion;
    lighting.direct.diffuse *= aoFactor.directAmbientOcclusion;
}

#ifdef DEBUG_DISPLAY
// mipmapColor is color use to store texture streaming information in XXXData.hlsl (look for DEBUGMIPMAPMODE_NONE)
void PostEvaluateBSDFDebugDisplay(  AmbientOcclusionFactor aoFactor, BuiltinData builtinData, AggregateLighting lighting, float3 mipmapColor,
                                    inout float3 diffuseLighting, inout float3 specularLighting)
{
    if (_DebugShadowMapMode != 0)
    {
        switch (_DebugShadowMapMode)
        {
        case SHADOWMAPDEBUGMODE_SINGLE_SHADOW:
            diffuseLighting = debugShadowAttenuation.xxx;
            specularLighting = float3(0, 0, 0);
            break ;
        }
    }
    if (_DebugLightingMode != 0)
    {
        // Caution: _DebugLightingMode is used in other part of the code, don't do anything outside of
        // current cases
        switch (_DebugLightingMode)
        {
        case DEBUGLIGHTINGMODE_LUX_METER:
            // Note: We don't include emissive here (and in deferred it is correct as lux calculation of bakeDiffuseLighting don't consider emissive)
            diffuseLighting = lighting.direct.diffuse + builtinData.bakeDiffuseLighting;

            //Compress lighting values for color picker if enabled
            if (_ColorPickerMode != COLORPICKERDEBUGMODE_NONE)
                diffuseLighting = diffuseLighting / LUXMETER_COMPRESSION_RATIO;
            
            specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION:
            diffuseLighting = aoFactor.indirectAmbientOcclusion;
            specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION:
            diffuseLighting = aoFactor.indirectSpecularOcclusion;
            specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

         case DEBUGLIGHTINGMODE_VISUALIZE_SHADOW_MASKS:
            #ifdef SHADOWS_SHADOWMASK
            diffuseLighting = float3(
                builtinData.shadowMask0 / 2 + builtinData.shadowMask1 / 2,
                builtinData.shadowMask1 / 2 + builtinData.shadowMask2 / 2,
                builtinData.shadowMask2 / 2 + builtinData.shadowMask3 / 2
            );
            specularLighting = float3(0, 0, 0);
            #endif
            break ;
        }
    }
    else if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        diffuseLighting = mipmapColor;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
}
#endif
