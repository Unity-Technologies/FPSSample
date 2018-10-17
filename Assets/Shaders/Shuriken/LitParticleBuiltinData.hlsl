void GetBuiltinData(FragInputs input, SurfaceData surfaceData, float alpha, float3 bentNormalWS, float depthOffset, out BuiltinData builtinData)
{
    // Builtin Data
    builtinData.opacity = alpha;

    // TODO: Sample lightmap/lightprobe/volume proxy
    // This should also handle projective lightmap
    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionWS, bentNormalWS, input.texCoord1, input.texCoord2);

    // It is safe to call this function here as surfaceData have been filled
    // We want to know if we must enable transmission on GI for SSS material, if the material have no SSS, this code will be remove by the compiler.
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // For now simply recall the function with inverted normal, the compiler should be able to optimize the lightmap case to not resample the directional lightmap
        // however it will not optimize the lightprobe case due to the proxy volume relying on dynamic if (we rely must get right of this dynamic if), not a problem for SH9, but a problem for proxy volume.
        // TODO: optimize more this code.
        // Add GI transmission contribution by resampling the GI for inverted vertex normal
        builtinData.bakeDiffuseLighting += SampleBakedGI(input.positionWS, -input.worldToTangent[2], input.texCoord1, input.texCoord2) * bsdfData.transmittance;
    }

#ifdef SHADOWS_SHADOWMASK
    float4 shadowMask = SampleShadowMask(input.positionWS, input.texCoord1);
    builtinData.shadowMask0 = shadowMask.x;
    builtinData.shadowMask1 = shadowMask.y;
    builtinData.shadowMask2 = shadowMask.z;
    builtinData.shadowMask3 = shadowMask.w;
#else
    builtinData.shadowMask0 = 0.0;
    builtinData.shadowMask1 = 0.0;
    builtinData.shadowMask2 = 0.0;
    builtinData.shadowMask3 = 0.0;
#endif

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
    builtinData.emissiveIntensity = _EmissiveIntensity; // We still store intensity here so we can reuse it with debug code

    builtinData.emissiveColor = _EmissiveColor * builtinData.emissiveIntensity * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
#ifdef _EMISSIVE_COLOR_MAP

    // Use layer0 of LayerTexCoord to retrieve emissive color mapping information
    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    layerTexCoord.vertexNormalWS = input.worldToTangent[2].xyz;
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(layerTexCoord.vertexNormalWS);

    int mappingType = UV_MAPPING_UVSET;
    #if defined(_EMISSIVE_MAPPING_PLANAR)
    mappingType = UV_MAPPING_PLANAR;
    #elif defined(_EMISSIVE_MAPPING_TRIPLANAR)
    mappingType = UV_MAPPING_TRIPLANAR;
    #endif

    // Be sure that the compiler is aware that we don't use UV1 to UV3 for main layer so it can optimize code
    #ifndef LAYERED_LIT_SHADER
    ComputeLayerTexCoord(
    #else
    ComputeLayerTexCoord0(
    #endif
                            input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3, _UVMappingMaskEmissive, _UVMappingMaskEmissive,
                            _EmissiveColorMap_ST.xy, _EmissiveColorMap_ST.zw, float2(0.0, 0.0), float2(0.0, 0.0), 1.0, false,
                            input.positionWS, _TexWorldScaleEmissive,
                            mappingType, layerTexCoord);

    #ifndef LAYERED_LIT_SHADER
    UVMapping emissiveMapMapping = layerTexCoord.base;
    #else
    UVMapping emissiveMapMapping = layerTexCoord.base0;
    #endif

    builtinData.emissiveColor *= SAMPLE_UVMAPPING_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, emissiveMapMapping).rgb;
#endif // _EMISSIVE_COLOR_MAP

    builtinData.velocity = float2(0.0, 0.0);

#if (SHADERPASS == SHADERPASS_DISTORTION) || defined(DEBUG_DISPLAY)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0).rgb;
    distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
    builtinData.distortion = distortion.rg * _DistortionScale;
    builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#else
    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
#endif

    builtinData.depthOffset = depthOffset;
}
