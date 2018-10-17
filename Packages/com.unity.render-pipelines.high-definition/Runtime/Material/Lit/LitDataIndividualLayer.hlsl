void ADD_IDX(ComputeLayerTexCoord)( // Uv related parameters
                                    float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3, float4 uvMappingMask, float4 uvMappingMaskDetails,
                                    // scale and bias for base and detail + global tiling factor (for layered lit only)
                                    float2 texScale, float2 texBias, float2 texScaleDetails, float2 texBiasDetails, float additionalTiling, float linkDetailsWithBase,
                                    // parameter for planar/triplanar
                                    float3 positionRWS, float worldScale,
                                    // mapping type and output
                                    int mappingType, inout LayerTexCoord layerTexCoord)
{
    // Handle uv0, uv1, uv2, uv3 based on _UVMappingMask weight (exclusif 0..1)
    float2 uvBase = uvMappingMask.x * texCoord0 +
                    uvMappingMask.y * texCoord1 +
                    uvMappingMask.z * texCoord2 +
                    uvMappingMask.w * texCoord3;

    // Only used with layered, allow to have additional tiling
    uvBase *= additionalTiling.xx;


    float2 uvDetails =  uvMappingMaskDetails.x * texCoord0 +
                        uvMappingMaskDetails.y * texCoord1 +
                        uvMappingMaskDetails.z * texCoord2 +
                        uvMappingMaskDetails.w * texCoord3;

    uvDetails *= additionalTiling.xx;

    // If base is planar/triplanar then detail map is forced to be planar/triplanar
    ADD_IDX(layerTexCoord.details).mappingType = ADD_IDX(layerTexCoord.base).mappingType = mappingType;
    ADD_IDX(layerTexCoord.details).normalWS = ADD_IDX(layerTexCoord.base).normalWS = layerTexCoord.vertexNormalWS;
    // Copy data for the uvmapping
    ADD_IDX(layerTexCoord.details).triplanarWeights = ADD_IDX(layerTexCoord.base).triplanarWeights = layerTexCoord.triplanarWeights;

    // TODO: Currently we only handle world planar/triplanar but we may want local planar/triplanar.
    // In this case both position and normal need to be convert to object space.

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;

    GetTriplanarCoordinate(GetAbsolutePositionWS(positionRWS) * worldScale, uvXZ, uvXY, uvZY);

    // Planar is just XZ of triplanar
    if (mappingType == UV_MAPPING_PLANAR)
    {
        uvBase = uvDetails = uvXZ;
    }

    // Apply tiling options
    ADD_IDX(layerTexCoord.base).uv = uvBase * texScale + texBias;
    // Detail map tiling option inherit from the tiling of the base
    ADD_IDX(layerTexCoord.details).uv = uvDetails * texScaleDetails + texBiasDetails;
    if (linkDetailsWithBase > 0.0)
    {
        ADD_IDX(layerTexCoord.details).uv = ADD_IDX(layerTexCoord.details).uv * texScale + texBias;
    }

    ADD_IDX(layerTexCoord.base).uvXZ = uvXZ * texScale + texBias;
    ADD_IDX(layerTexCoord.base).uvXY = uvXY * texScale + texBias;
    ADD_IDX(layerTexCoord.base).uvZY = uvZY * texScale + texBias;

    ADD_IDX(layerTexCoord.details).uvXZ = uvXZ * texScaleDetails + texBiasDetails;
    ADD_IDX(layerTexCoord.details).uvXY = uvXY * texScaleDetails + texBiasDetails;
    ADD_IDX(layerTexCoord.details).uvZY = uvZY * texScaleDetails + texBiasDetails;

    if (linkDetailsWithBase > 0.0)
    {
        ADD_IDX(layerTexCoord.details).uvXZ = ADD_IDX(layerTexCoord.details).uvXZ * texScale + texBias;
        ADD_IDX(layerTexCoord.details).uvXY = ADD_IDX(layerTexCoord.details).uvXY * texScale + texBias;
        ADD_IDX(layerTexCoord.details).uvZY = ADD_IDX(layerTexCoord.details).uvZY * texScale + texBias;
    }


    #ifdef SURFACE_GRADIENT
    // This part is only relevant for normal mapping with UV_MAPPING_UVSET
    // Note: This code work only in pixel shader (as we rely on ddx), it should not be use in other context
    ADD_IDX(layerTexCoord.base).tangentWS = uvMappingMask.x * layerTexCoord.vertexTangentWS0 +
                                            uvMappingMask.y * layerTexCoord.vertexTangentWS1 +
                                            uvMappingMask.z * layerTexCoord.vertexTangentWS2 +
                                            uvMappingMask.w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.base).bitangentWS =   uvMappingMask.x * layerTexCoord.vertexBitangentWS0 +
                                                uvMappingMask.y * layerTexCoord.vertexBitangentWS1 +
                                                uvMappingMask.z * layerTexCoord.vertexBitangentWS2 +
                                                uvMappingMask.w * layerTexCoord.vertexBitangentWS3;

    ADD_IDX(layerTexCoord.details).tangentWS =  uvMappingMaskDetails.x * layerTexCoord.vertexTangentWS0 +
                                                uvMappingMaskDetails.y * layerTexCoord.vertexTangentWS1 +
                                                uvMappingMaskDetails.z * layerTexCoord.vertexTangentWS2 +
                                                uvMappingMaskDetails.w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.details).bitangentWS =    uvMappingMaskDetails.x * layerTexCoord.vertexBitangentWS0 +
                                                    uvMappingMaskDetails.y * layerTexCoord.vertexBitangentWS1 +
                                                    uvMappingMaskDetails.z * layerTexCoord.vertexBitangentWS2 +
                                                    uvMappingMaskDetails.w * layerTexCoord.vertexBitangentWS3;
    #endif
}

// Caution: Duplicate from GetBentNormalTS - keep in sync!
float3 ADD_IDX(GetNormalTS)(FragInputs input, LayerTexCoord layerTexCoord, float3 detailNormalTS, float detailMask)
{
    float3 normalTS;

#ifdef _NORMALMAP_IDX
    #ifdef _NORMALMAP_TANGENT_SPACE_IDX
        normalTS = SAMPLE_UVMAPPING_NORMALMAP(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale));
    #else // Object space
        // We forbid scale in case of object space as it make no sense
        // To be able to combine object space normal with detail map then later we will re-transform it to world space.
        // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
        #ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv).xyz * 2.0 - 1.0;
        // no need to renormalize normalOS for SurfaceGradientFromPerturbedNormal
        normalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], TransformObjectToWorldDir(normalOS));
        #else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv), 1.0);
        normalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        #endif
    #endif

    #ifdef _DETAIL_MAP_IDX
        #ifdef SURFACE_GRADIENT
        normalTS += detailNormalTS * detailMask;
        #else
        normalTS = lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask); // todo: detailMask should lerp the angle of the quaternion rotation, not the normals
        #endif
    #endif
#else
    #ifdef SURFACE_GRADIENT
    normalTS = float3(0.0, 0.0, 0.0); // No gradient
    #else
    normalTS = float3(0.0, 0.0, 1.0);
    #endif
#endif

    return normalTS;
}

// Caution: Duplicate from GetNormalTS - keep in sync!
float3 ADD_IDX(GetBentNormalTS)(FragInputs input, LayerTexCoord layerTexCoord, float3 normalTS, float3 detailNormalTS, float detailMask)
{
    float3 bentNormalTS;

#ifdef _BENTNORMALMAP_IDX
    #ifdef _NORMALMAP_TANGENT_SPACE_IDX
        bentNormalTS = SAMPLE_UVMAPPING_NORMALMAP(ADD_IDX(_BentNormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale));
    #else // Object space
        // We forbid scale in case of object space as it make no sense
        // To be able to combine object space normal with detail map then later we will re-transform it to world space.
        // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
        #ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D(ADD_IDX(_BentNormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv).xyz * 2.0 - 1.0;
        // no need to renormalize normalOS for SurfaceGradientFromPerturbedNormal
        bentNormalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], TransformObjectToWorldDir(normalOS));
        #else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(ADD_IDX(_BentNormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv), 1.0);
        bentNormalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        #endif
    #endif

    #ifdef _DETAIL_MAP_IDX
        #ifdef SURFACE_GRADIENT
        bentNormalTS += detailNormalTS * detailMask;
        #else
        bentNormalTS = lerp(bentNormalTS, BlendNormalRNM(bentNormalTS, detailNormalTS), detailMask);
        #endif
    #endif
#else
    // If there is no bent normal map provided, fallback on regular normal map
    bentNormalTS = normalTS;
#endif

    return bentNormalTS;
}

// Return opacity
float ADD_IDX(GetSurfaceData)(FragInputs input, LayerTexCoord layerTexCoord, out SurfaceData surfaceData, out float3 normalTS, out float3 bentNormalTS)
{
    float alpha = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).a * ADD_IDX(_BaseColor).a;

    // Perform alha test very early to save performance (a killed pixel will not sample textures)
#if defined(_ALPHATEST_ON) && !defined(LAYERED_LIT_SHADER)
    float alphaCutoff = _AlphaCutoff;
    #ifdef CUTOFF_TRANSPARENT_DEPTH_PREPASS
    alphaCutoff = _AlphaCutoffPrepass;
    #elif defined(CUTOFF_TRANSPARENT_DEPTH_POSTPASS)
    alphaCutoff = _AlphaCutoffPostpass;
    #endif
    DoAlphaTest(alpha, alphaCutoff);
#endif

    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;
#ifdef _DETAIL_MAP_IDX
    detailMask = 1.0;
    #ifdef _MASKMAP_IDX
        detailMask = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).b;
    #endif
    float2 detailAlbedoAndSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details)).rb;
    float detailAlbedo = detailAlbedoAndSmoothness.r * 2.0 - 1.0;
    float detailSmoothness = detailAlbedoAndSmoothness.g * 2.0 - 1.0;
    // Resample the detail map but this time for the normal map. This call should be optimize by the compiler
    // We split both call due to trilinear mapping
    detailNormalTS = SAMPLE_UVMAPPING_NORMALMAP_AG(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details), ADD_IDX(_DetailNormalScale));
#endif

    surfaceData.baseColor = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).rgb * ADD_IDX(_BaseColor).rgb;
#ifdef _DETAIL_MAP_IDX
	
    // Goal: we want the detail albedo map to be able to darken down to black and brighten up to white the surface albedo.
    // The scale control the speed of the gradient. We simply remap detailAlbedo from [0..1] to [-1..1] then perform a lerp to black or white
    // with a factor based on speed.
    // For base color we interpolate in sRGB space (approximate here as square) as it get a nicer perceptual gradient
    float albedoDetailSpeed = saturate(abs(detailAlbedo) * ADD_IDX(_DetailAlbedoScale));
    float3 baseColorOverlay = lerp(sqrt(surfaceData.baseColor), (detailAlbedo < 0.0) ? float3(0.0, 0.0, 0.0) : float3(1.0, 1.0, 1.0), albedoDetailSpeed * albedoDetailSpeed);
    baseColorOverlay *= baseColorOverlay;							   
    // Lerp with details mask
    surfaceData.baseColor = lerp(surfaceData.baseColor, saturate(baseColorOverlay), detailMask);
#endif

    surfaceData.specularOcclusion = 1.0; // Will be setup outside of this function

    surfaceData.normalWS = float3(0.0, 0.0, 0.0); // Need to init this to keep quiet the compiler, but this is overriden later (0, 0, 0) so if we forget to override the compiler may comply.

    normalTS = ADD_IDX(GetNormalTS)(input, layerTexCoord, detailNormalTS, detailMask);
    bentNormalTS = ADD_IDX(GetBentNormalTS)(input, layerTexCoord, normalTS, detailNormalTS, detailMask);

#if defined(_MASKMAP_IDX)
    surfaceData.perceptualSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
    surfaceData.perceptualSmoothness = lerp(ADD_IDX(_SmoothnessRemapMin), ADD_IDX(_SmoothnessRemapMax), surfaceData.perceptualSmoothness);
#else
    surfaceData.perceptualSmoothness = ADD_IDX(_Smoothness);
#endif

#ifdef _DETAIL_MAP_IDX
    // See comment for baseColorOverlay
    float smoothnessDetailSpeed = saturate(abs(detailSmoothness) * ADD_IDX(_DetailSmoothnessScale));
    float smoothnessOverlay = lerp(surfaceData.perceptualSmoothness, (detailSmoothness < 0.0) ? 0.0 : 1.0, smoothnessDetailSpeed);
    // Lerp with details mask
    surfaceData.perceptualSmoothness = lerp(surfaceData.perceptualSmoothness, saturate(smoothnessOverlay), detailMask);
#endif

    // MaskMap is RGBA: Metallic, Ambient Occlusion (Optional), detail Mask (Optional), Smoothness
#ifdef _MASKMAP_IDX
    surfaceData.metallic = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).r;
    surfaceData.ambientOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).g;
    surfaceData.ambientOcclusion = lerp(ADD_IDX(_AORemapMin), ADD_IDX(_AORemapMax), surfaceData.ambientOcclusion);
#else
    surfaceData.metallic = 1.0;
    surfaceData.ambientOcclusion = 1.0;
#endif
    surfaceData.metallic *= ADD_IDX(_Metallic);

    surfaceData.diffusionProfile = ADD_IDX(_DiffusionProfile);
    surfaceData.subsurfaceMask = ADD_IDX(_SubsurfaceMask);

#ifdef _SUBSURFACE_MASK_MAP_IDX
    surfaceData.subsurfaceMask *= SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_SubsurfaceMaskMap), SAMPLER_SUBSURFACE_MASK_MAP_IDX, ADD_IDX(layerTexCoord.base)).r;
#endif

#ifdef _THICKNESSMAP_IDX
    surfaceData.thickness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_ThicknessMap), SAMPLER_THICKNESSMAP_IDX, ADD_IDX(layerTexCoord.base)).r;
    surfaceData.thickness = ADD_IDX(_ThicknessRemap).x + ADD_IDX(_ThicknessRemap).y * surfaceData.thickness;
#else
    surfaceData.thickness = ADD_IDX(_Thickness);
#endif

    // This part of the code is not used in case of layered shader but we keep the same macro system for simplicity
#if !defined(LAYERED_LIT_SHADER)

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING;
#endif
#ifdef _MATERIAL_FEATURE_TRANSMISSION
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_TRANSMISSION;
#endif
#ifdef _MATERIAL_FEATURE_ANISOTROPY
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_ANISOTROPY;
#endif
#ifdef _MATERIAL_FEATURE_CLEAR_COAT
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_CLEAR_COAT;
#endif
#ifdef _MATERIAL_FEATURE_IRIDESCENCE
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_IRIDESCENCE;
#endif
#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
#endif

#ifdef _TANGENTMAP
    #ifdef _NORMALMAP_TANGENT_SPACE_IDX // Normal and tangent use same space
    // Tangent space vectors always use only 2 channels.
    float3 tangentTS = UnpackNormalmapRGorAG(SAMPLE_UVMAPPING_TEXTURE2D(_TangentMap, sampler_TangentMap, layerTexCoord.base), 1.0);
    surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.worldToTangent);
    #else // Object space
    // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
    float3 tangentOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(_TangentMapOS, sampler_TangentMapOS,  layerTexCoord.base.uv), 1.0);
    surfaceData.tangentWS = TransformObjectToWorldDir(tangentOS);
    #endif
#else
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
#endif

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_UVMAPPING_TEXTURE2D(_AnisotropyMap, sampler_AnisotropyMap, layerTexCoord.base).r;
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= ADD_IDX(_Anisotropy);

    surfaceData.specularColor = _SpecularColor.rgb;
#ifdef _SPECULARCOLORMAP
    surfaceData.specularColor *= SAMPLE_UVMAPPING_TEXTURE2D(_SpecularColorMap, sampler_SpecularColorMap, layerTexCoord.base).rgb;
#endif
#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
    // Require to have setup baseColor
    // Reproduce the energy conservation done in legacy Unity. Not ideal but better for compatibility and users can unchek it
    surfaceData.baseColor *= _EnergyConservingSpecularColor > 0.0 ? (1.0 - Max3(surfaceData.specularColor.r, surfaceData.specularColor.g, surfaceData.specularColor.b)) : 1.0;
#endif

#if HAS_REFRACTION
    surfaceData.ior = _Ior;
    surfaceData.transmittanceColor = _TransmittanceColor;
    #ifdef _TRANSMITTANCECOLORMAP
    surfaceData.transmittanceColor *= SAMPLE_UVMAPPING_TEXTURE2D(_TransmittanceColorMap, sampler_TransmittanceColorMap, ADD_IDX(layerTexCoord.base)).rgb;
    #endif

    surfaceData.atDistance = _ATDistance;
    // Thickness already defined with SSS (from both thickness and thicknessMap)
    surfaceData.thickness *= _ThicknessMultiplier;
    // Rough refraction don't use opacity. Instead we use opacity as a transmittance mask.
    surfaceData.transmittanceMask = 1.0 - alpha;
    alpha = 1.0;
#else
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1.0;
    surfaceData.transmittanceMask = 0.0;
#endif

#ifdef _MATERIAL_FEATURE_CLEAR_COAT
    surfaceData.coatMask = _CoatMask;
    // To shader feature for keyword to limit the variant
    surfaceData.coatMask *= SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_CoatMaskMap), ADD_ZERO_IDX(sampler_CoatMaskMap), ADD_IDX(layerTexCoord.base)).r;
#else
    surfaceData.coatMask = 0.0;
#endif

#ifdef _MATERIAL_FEATURE_IRIDESCENCE
    #ifdef _IRIDESCENCE_THICKNESSMAP
    surfaceData.iridescenceThickness = SAMPLE_UVMAPPING_TEXTURE2D(_IridescenceThicknessMap, sampler_IridescenceThicknessMap, layerTexCoord.base).r;
    surfaceData.iridescenceThickness = _IridescenceThicknessRemap.x + _IridescenceThicknessRemap.y * surfaceData.iridescenceThickness;
    #else
    surfaceData.iridescenceThickness = _IridescenceThickness;
    #endif
    surfaceData.iridescenceMask = _IridescenceMask;
    surfaceData.iridescenceMask *= SAMPLE_UVMAPPING_TEXTURE2D(_IridescenceMaskMap, sampler_IridescenceMaskMap, layerTexCoord.base).r;
#else
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;
#endif

#else // #if !defined(LAYERED_LIT_SHADER)

    // Mandatory to setup value to keep compiler quiet

    // Layered shader material feature are define outside of this call
    surfaceData.materialFeatures = 0;

    // All these parameters are ignore as they are re-setup outside of the layers function
    // Note: any parameters set here must also be set in GetSurfaceAndBuiltinData() layer version
    surfaceData.tangentWS = float3(0.0, 0.0, 0.0);
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;
    surfaceData.coatMask = 0.0;

    // Transparency
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

#endif // #if !defined(LAYERED_LIT_SHADER)

    return alpha;
}
