//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE)
    {
        surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_NORMAL)
    {
        surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_MASK)
    {
#ifdef DECALS_4RT // only smoothness in 3RT mode
        // Don't apply any metallic modification
        surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
#endif

        surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
    }
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal
    
    // Initial value of the material features
    surfaceData.materialFeatures = 0;
    
// Transform the preprocess macro into a material feature (note that silk flag is deduced from the abscence of this one)
#ifdef _MATERIAL_FEATURE_COTTON_WOOL
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL;
#endif

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING;
#endif

#ifdef _MATERIAL_FEATURE_TRANSMISSION
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION;
#endif
    
    // Generate the primary uv coordinates
    float2 uvBase = _UVMappingMask.x * input.texCoord0.xy +
                    _UVMappingMask.y * input.texCoord1.xy +
                    _UVMappingMask.z * input.texCoord2.xy +
                    _UVMappingMask.w * input.texCoord3.xy;

    // Apply tiling and offset
    uvBase = uvBase * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;


    // Generate the detail uv coordinates
    float2 uvThread =  _UVMappingMaskThread.x * input.texCoord0.xy +
                        _UVMappingMaskThread.y * input.texCoord1.xy +
                        _UVMappingMaskThread.z * input.texCoord2.xy +
                        _UVMappingMaskThread.w * input.texCoord3.xy;

    // Apply offset and tiling
    uvThread = uvThread * _ThreadMap_ST.xy + _ThreadMap_ST.zw;

    if (_LinkDetailsWithBase > 0.0)
    {
        uvThread = uvThread * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
    }

// The Mask map also contains the detail mask flag, se we need to read it first
#ifdef _MASKMAP
    float4 maskValue = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvBase);
#else
    #ifdef _THREAD_MAP
        // If we have no mask map, but we have a detail map; we use the detail map and the smoothness is the value version
        float4 maskValue = float4(1, 1, 1, _Smoothness);
    #else
        // If we have no mask map, no detail map AO is 1, smoothness is the value and mask
        float4 maskValue = float4(1, 1, 0, _Smoothness);
    #endif
#endif

// We need to start by reading the detail (if any available to override the initial values)
#ifdef _THREAD_MAP
    float4 threadSample = SAMPLE_TEXTURE2D(_ThreadMap, sampler_ThreadMap, uvThread);
    float threadAO = threadSample.x;
    float threadSmoothness = threadSample.z * 2.0 - 1.0;

    // Handle the normal detail
    float2 threadDerivative = UnpackDerivativeNormalRGorAG(float4(threadSample.w, threadSample.y, 1, 1), _ThreadNormalScale);
    float3 threadGradient =  SurfaceGradientFromTBN(threadDerivative, input.worldToTangent[0], input.worldToTangent[1]);
#else
    float4 threadSample = float4(1.0, 0.0, 0.0, 1.0);
    float3 threadGradient = float3(0.0, 0.0, 0.0);
#endif
    
    // The base color of the object mixed with the base color texture
    surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, uvBase).rgb * _BaseColor.rgb;

    // Extract the alpha value (will be useful if we need to trigger the alpha test)
    float alpha = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, uvBase).a * _BaseColor.a * threadSample.r;

    // Propagate the geometry normal
    surfaceData.geomNormalWS = input.worldToTangent[2];
    
#ifdef _NORMALMAP
    float2 derivative = UnpackDerivativeNormalRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvBase), _NormalScale);
    #ifdef _THREAD_MAP
        float3 gradient =  SurfaceGradientFromTBN(derivative, input.worldToTangent[0], input.worldToTangent[1]) + threadGradient * maskValue.z;
    #else
        float3 gradient =  SurfaceGradientFromTBN(derivative, input.worldToTangent[0], input.worldToTangent[1]);
    #endif
    surfaceData.normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], gradient);
#else
    #ifdef _THREAD_MAP
        surfaceData.normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], threadGradient);
    #else
        surfaceData.normalWS = input.worldToTangent[2];
    #endif
#endif

#ifdef _TANGENTMAP
    float3 tangentTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_TangentMap, sampler_TangentMap, uvBase, 1.0));
    surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.worldToTangent);
#else
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
#endif

    // Make the tagent match the normal
    surfaceData.tangentWS = Orthonormalize(input.worldToTangent[0], surfaceData.normalWS);


#ifdef _MASKMAP
    surfaceData.ambientOcclusion = lerp(_AORemapMin, _AORemapMax, maskValue.y);
    surfaceData.perceptualSmoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, maskValue.w);
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#else
    surfaceData.ambientOcclusion = maskValue.y;
    surfaceData.perceptualSmoothness = maskValue.w;
    surfaceData.specularOcclusion = 1.0;
#endif

// If a thread map was provided, modify the matching smoothness
#ifdef _THREAD_MAP
    float smoothnessDetailSpeed = saturate(abs(threadSmoothness) * _ThreadSmoothnessScale);
    float smoothnessOverlay = lerp(surfaceData.perceptualSmoothness, (threadSmoothness < 0.0) ? 0.0 : 1.0, smoothnessDetailSpeed);
    surfaceData.perceptualSmoothness = lerp(surfaceData.perceptualSmoothness, saturate(smoothnessOverlay), maskValue.z);
#endif
    
// If a thread map was provided, modify the matching ao
#ifdef _THREAD_MAP
    float aoOverlay = lerp(surfaceData.ambientOcclusion, threadAO * surfaceData.ambientOcclusion, _ThreadAOScale);
    surfaceData.ambientOcclusion = lerp(surfaceData.ambientOcclusion, saturate(aoOverlay), maskValue.z);
#endif

    // Propagate the fuzz tint
    surfaceData.specularColor = _SpecularColor.xyz;

#ifdef _FUZZDETAIL_MAP
    surfaceData.baseColor.rgb = saturate(surfaceData.baseColor.rgb + SAMPLE_TEXTURE2D(_FuzzDetailMap, sampler_FuzzDetailMap, uvThread * _FuzzDetailUVScale).rgb * _FuzzDetailScale);
#endif

    surfaceData.baseColor *= 1.0 - Max3(surfaceData.specularColor.r, surfaceData.specularColor.g, surfaceData.specularColor.b);
#if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) || defined(_MATERIAL_FEATURE_TRANSMISSION)
    surfaceData.diffusionProfile = _DiffusionProfile;
    #ifdef _SUBSURFACEMASK
        float4 subSurfaceMaskSample = SAMPLE_TEXTURE2D(_SubsurfaceMaskMap, sampler_SubsurfaceMaskMap, uvBase);
        surfaceData.subsurfaceMask = subSurfaceMaskSample.x;
    #else
        surfaceData.subsurfaceMask = _SubsurfaceMask;
    #endif
#else
    surfaceData.subsurfaceMask = 0.0;
    surfaceData.diffusionProfile = 0;
#endif

#ifdef _THICKNESSMAP
    float4 subSurfaceMaskSample = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, uvBase);
    surfaceData.thickness = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_ThicknessMap), _ThicknessMapChannelMask);
    surfaceData.thickness = lerp(_ThicknessMapRange.x, _ThicknessMapRange.y, surfaceData.thickness);
    surfaceData.thickness = lerp(_Thickness, surfaceData.thickness, _ThicknessUseMap);
    surfaceData.thickness = _ThicknessRemap.x +  surfaceData.thickness * _ThicknessRemap.y;
#else
    surfaceData.thickness = _Thickness;
#endif

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_TEXTURE2D(_AnisotropyMap, sample_AnisotropyMap, uvBase).x;
#else
    surfaceData.anisotropy = _Anisotropy;
#endif

#ifdef _ALPHATEST_ON
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

#if HAVE_DECALS
    if (_EnableDecals)
    {
        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
        ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
    }
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, uvBase, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.baseColor);
    }

    // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
    // as it can modify attribute use for static lighting
    ApplyDebugToSurfaceData(input.worldToTangent, surfaceData);
#endif

    // -------------------------------------------------------------
    // Builtin Data
    // -------------------------------------------------------------

    // For back lighting we use the oposite vertex normal 
    InitBuiltinData(alpha, surfaceData.normalWS, -input.worldToTangent[2], input.positionRWS, input.texCoord1, input.texCoord2, builtinData);
    
    // Support the emissive color and map
    builtinData.emissiveColor = _EmissiveColor.rgb * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
#ifdef _EMISSIVE_COLOR_MAP
    // Generate the primart uv coordinates
    float2 uvEmissive = _UVMappingMaskEmissive.x * input.texCoord0.xy +
                    _UVMappingMaskEmissive.y * input.texCoord1.xy +
                    _UVMappingMaskEmissive.z * input.texCoord2.xy +
                    _UVMappingMaskEmissive.w * input.texCoord3.xy;
    
    uvEmissive = uvEmissive * _EmissiveColorMap_ST.xy + _EmissiveColorMap_ST.zw;

    builtinData.emissiveColor *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, uvEmissive).rgb;
#endif

    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
