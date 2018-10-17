#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataDisplacement.hlsl"

// Return the maximum amplitude use by all enabled heightmap
// use for tessellation culling and per pixel displacement
// TODO: For vertex displacement this should take into account the modification in ApplyTessellationTileScale but it should be conservative here (as long as tiling is not negative)
float GetMaxDisplacement()
{
    float maxDisplacement = 0.0;

    // _HeightAmplitudeX can be negative if min and max are inverted, but the max displacement must be positive, take abs()
#if defined(_HEIGHTMAP0)
    maxDisplacement = abs(_HeightAmplitude0);
#endif

#if defined(_HEIGHTMAP1)
    maxDisplacement = max(  abs(_HeightAmplitude1)
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            + abs(_HeightAmplitude0) * _InheritBaseHeight1
                            #endif
                            , maxDisplacement);
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    maxDisplacement = max(  abs(_HeightAmplitude2)
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            + abs(_HeightAmplitude0) * _InheritBaseHeight2
                            #endif
                            , maxDisplacement);
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    maxDisplacement = max(  abs(_HeightAmplitude3)
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            + abs(_HeightAmplitude0) * _InheritBaseHeight3
                            #endif
                            , maxDisplacement);
#endif
#endif

    return maxDisplacement;
}

// Return the minimun uv size for all layers including triplanar
float2 GetMinUvSize(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

#if defined(_HEIGHTMAP0)
    if (layerTexCoord.base0.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base0.uvZY * _HeightMap0_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base0.uvXZ * _HeightMap0_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base0.uvXY * _HeightMap0_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base0.uv * _HeightMap0_TexelSize.zw, minUvSize);
    }
#endif

#if defined(_HEIGHTMAP1)
    if (layerTexCoord.base1.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base1.uvZY * _HeightMap1_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base1.uvXZ * _HeightMap1_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base1.uvXY * _HeightMap1_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base1.uv * _HeightMap1_TexelSize.zw, minUvSize);
    }
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    if (layerTexCoord.base2.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base2.uvZY * _HeightMap2_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base2.uvXZ * _HeightMap2_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base2.uvXY * _HeightMap2_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base2.uv * _HeightMap2_TexelSize.zw, minUvSize);
    }
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    if (layerTexCoord.base3.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base3.uvZY * _HeightMap3_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base3.uvXZ * _HeightMap3_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base3.uvXY * _HeightMap3_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base3.uv * _HeightMap3_TexelSize.zw, minUvSize);
    }
#endif
#endif

    return minUvSize;
}

#if defined(_PIXEL_DISPLACEMENT) && LAYERS_HEIGHTMAP_ENABLE
struct PerPixelHeightDisplacementParam
{
    float4 blendMasks;
    float2 uv[_MAX_LAYER];
    float2 uvSpaceScale[_MAX_LAYER];
#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    float heightInfluence[_MAX_LAYER];
#endif
};

// Calculate displacement for per vertex displacement mapping
float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
{
    // See function ComputePerVertexDisplacement() for comment about the weights/influenceMask/BlendMask

    // Note: Amplitude is handled in uvSpaceScale, no need to multiply by it here.
    float height0 = SAMPLE_TEXTURE2D_LOD(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, param.uv[0] + texOffsetCurrent * param.uvSpaceScale[0], lod).r;
    float height1 = SAMPLE_TEXTURE2D_LOD(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, param.uv[1] + texOffsetCurrent * param.uvSpaceScale[1], lod).r;
    float height2 = SAMPLE_TEXTURE2D_LOD(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, param.uv[2] + texOffsetCurrent * param.uvSpaceScale[2], lod).r;
    float height3 = SAMPLE_TEXTURE2D_LOD(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, param.uv[3] + texOffsetCurrent * param.uvSpaceScale[3], lod).r;

    SetEnabledHeightByLayer(height0, height1, height2, height3);

    float4 blendMasks = param.blendMasks;
#if defined(_HEIGHT_BASED_BLEND)
    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    blendMasks = ApplyHeightBlend(float4(height0, height1, height2, height3), param.blendMasks);
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(blendMasks, weights);

#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    float influenceMask = blendMasks.a;
    #ifdef _INFLUENCEMASK_MAP
    influenceMask *= SAMPLE_TEXTURE2D_LOD(_LayerInfluenceMaskMap, sampler_BaseColorMap0, param.uv[0], lod).r;
    #endif
    height1 += height0 * _InheritBaseHeight1 * influenceMask;
    height2 += height0 * _InheritBaseHeight2 * influenceMask;
    height3 += height0 * _InheritBaseHeight3 * influenceMask;
#endif

    return BlendLayeredScalar(height0, height1, height2, height3, weights);
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"

#endif // defined(_PIXEL_DISPLACEMENT) && LAYERS_HEIGHTMAP_ENABLE

// PPD is affecting only one mapping at the same time, mean we need to execute it for each mapping (UV0, UV1, 3 times for triplanar etc..)
// We chose to not support all this case that are extremely hard to manage (for example mixing different mapping, mean it also require different tangent space that is not supported in Unity)
// For these reasons we put the following rules
// Rules:
// - Mapping is the same for all layers that use an Heightmap (i.e all are UV, planar or triplanar)
// - Mapping UV is UV0 only because we need to convert view vector in texture space and this is only available for UV0
// - Heightmap can be enabled per layer
// - Blend Mask use same mapping as main layer (UVO, Planar, Triplanar)
// From these rules it mean that PPD is enable only if the user 1) ask for it, 2) if there is one heightmap enabled on active layer, 3) if mapping is the same for all layer respecting 2), 4) if mapping is UV0, planar or triplanar mapping
// Most contraint are handled by the inspector (i.e the UI) like the mapping constraint and is assumed in the shader.
float ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord, float4 blendMasks)
{
#if defined(_PIXEL_DISPLACEMENT) && LAYERS_HEIGHTMAP_ENABLE
    bool isPlanar = false;
    bool isTriplanar = false;

    // To know if we are planar or triplanar just need to check if any of the active heightmap layer is true as they are enforce to be the same mapping
#if defined(_HEIGHTMAP0)
    isPlanar = layerTexCoord.base0.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base0.mappingType == UV_MAPPING_TRIPLANAR;
#endif

#if defined(_HEIGHTMAP1)
    isPlanar = layerTexCoord.base1.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base1.mappingType == UV_MAPPING_TRIPLANAR;
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    isPlanar = layerTexCoord.base2.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base2.mappingType == UV_MAPPING_TRIPLANAR;
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    isPlanar = layerTexCoord.base3.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base3.mappingType == UV_MAPPING_TRIPLANAR;
#endif
#endif

    // Compute lod as we will sample inside a loop(so can't use regular sampling)
    // Note: It appear that CALCULATE_TEXTURE2D_LOD only return interger lod. We want to use float lod to have smoother transition and fading, so do our own calculation.
    // Approximation of lod to used. Be conservative here, we will take the highest mip of all layers.
    // Remember, we assume that we used the same mapping for all layer, so only size matter.
    float2 minUvSize = GetMinUvSize(layerTexCoord);
    float lod = ComputeTextureLOD(minUvSize);

    // TODO: Here we calculate the scale transform from world to UV space , which is what we have done in GetLayerTexCoord but without the texBias.
    // Mean we must also apply the same "additionalTiling", currently not apply Also precompute all this!
    float  maxHeight0 = abs(_HeightAmplitude0);
    float  maxHeight1 = abs(_HeightAmplitude1);
    float  maxHeight2 = abs(_HeightAmplitude2);
    float  maxHeight3 = abs(_HeightAmplitude3);

    ApplyDisplacementTileScale(maxHeight0, maxHeight1, maxHeight2, maxHeight3);
#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    maxHeight1 += abs(_HeightAmplitude0) * _InheritBaseHeight1;
    maxHeight2 += abs(_HeightAmplitude0) * _InheritBaseHeight2;
    maxHeight3 += abs(_HeightAmplitude0) * _InheritBaseHeight3;
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(blendMasks, weights);
    float maxHeight = BlendLayeredScalar(maxHeight0, maxHeight1, maxHeight2, maxHeight3, weights);

    float2 worldScale0 = (isPlanar || isTriplanar) ? _TexWorldScale0.xx : _InvPrimScale.xy;
    float2 worldScale1 = (isPlanar || isTriplanar) ? _TexWorldScale1.xx : _InvPrimScale.xy;
    float2 worldScale2 = (isPlanar || isTriplanar) ? _TexWorldScale2.xx : _InvPrimScale.xy;
    float2 worldScale3 = (isPlanar || isTriplanar) ? _TexWorldScale3.xx : _InvPrimScale.xy;

    PerPixelHeightDisplacementParam ppdParam;
    ppdParam.blendMasks = blendMasks;
    ppdParam.uvSpaceScale[0] = _BaseColorMap0_ST.xy * worldScale0;// *maxHeight0;
    ppdParam.uvSpaceScale[1] = _BaseColorMap1_ST.xy * worldScale1;// *maxHeight1;
    ppdParam.uvSpaceScale[2] = _BaseColorMap2_ST.xy * worldScale2;// *maxHeight2;
    ppdParam.uvSpaceScale[3] = _BaseColorMap3_ST.xy * worldScale3;// *maxHeight3;

    float uvSpaceScale = BlendLayeredScalar(ppdParam.uvSpaceScale[0], ppdParam.uvSpaceScale[1], ppdParam.uvSpaceScale[2], ppdParam.uvSpaceScale[3], weights);

    float2 scaleOffsetDetails0 =_DetailMap0_ST.xy;
    float2 scaleOffsetDetails1 =_DetailMap1_ST.xy;
    float2 scaleOffsetDetails2 =_DetailMap2_ST.xy;
    float2 scaleOffsetDetails3 =_DetailMap3_ST.xy;

    float height; // final height processed
    float NdotV;

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(V, uvXZ, uvXY, uvZY);

    // We need to calculate the texture space direction. It depends on the mapping.
    if (isTriplanar)
    {
        // This is not supported currently
        height = 1.0;
        NdotV  = 1.0;
    }
    else
    {
        // For planar it is uv too, not uvXZ
        ppdParam.uv[0] = layerTexCoord.base0.uv;
        ppdParam.uv[1] = layerTexCoord.base1.uv;
        ppdParam.uv[2] = layerTexCoord.base2.uv;
        ppdParam.uv[3] = layerTexCoord.base3.uv;

        // Note: The TBN is not normalize as it is based on mikkt. We should normalize it, but POM is always use on simple enough surface that mean it is not required (save 2 normalize). Tag: SURFACE_GRADIENT
        // Note: worldToTangent is only define for UVSet0, so we expect that layer that use POM have UVSet0
        float3 viewDirTS = isPlanar ? float3(uvXZ, V.y) : TransformWorldToTangent(V, input.worldToTangent) * GetDisplacementObjectScale(false).xzy; // Switch from Y-up to Z-up (as we move to tangent space)
        NdotV = viewDirTS.z;

        // Transform the view vector into the UV space.
        float3 viewDirUV = normalize(float3(viewDirTS.xy * maxHeight, viewDirTS.z));
        float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
        int    numSteps = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
        float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, height);
        offset *= uvSpaceScale;

        layerTexCoord.base0.uv += offset;
        layerTexCoord.base1.uv += offset;
        layerTexCoord.base2.uv += offset;
        layerTexCoord.base3.uv += offset;

        layerTexCoord.details0.uv += offset * scaleOffsetDetails0;
        layerTexCoord.details1.uv += offset * scaleOffsetDetails1;
        layerTexCoord.details2.uv += offset * scaleOffsetDetails2;
        layerTexCoord.details3.uv += offset * scaleOffsetDetails3;
    }

    // Since POM "pushes" geometry inwards (rather than extrude it), { height = height - 1 }.
    // Since the result is used as a 'depthOffsetVS', it needs to be positive, so we flip the sign. { height = -height + 1 }.

    float verticalDisplacement = maxHeight - height * maxHeight;
    return verticalDisplacement / ClampNdotV(NdotV);
#else
    return 0.0;
#endif
}

// Calculate displacement for per vertex displacement mapping
float3 ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
#if LAYERS_HEIGHTMAP_ENABLE
    float height0 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base0, lod).r - _HeightCenter0) * _HeightAmplitude0;
    float height1 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base1, lod).r - _HeightCenter1) * _HeightAmplitude1;
    float height2 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base2, lod).r - _HeightCenter2) * _HeightAmplitude2;
    float height3 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base3, lod).r - _HeightCenter3) * _HeightAmplitude3;
    // Height is affected by tiling property and by object scale (depends on option).
    // Apply scaling from tiling properties (TexWorldScale and tiling from BaseColor)
    ApplyDisplacementTileScale(height0, height1, height2, height3);
    // Nullify height that are not used, so compiler can remove unused case
    SetEnabledHeightByLayer(height0, height1, height2, height3);

    float4 blendMasks = GetBlendMask(layerTexCoord, vertexColor, true, lod);

    #if defined(_HEIGHT_BASED_BLEND)
    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    blendMasks = ApplyHeightBlend(float4(height0, height1, height2, height3), blendMasks);
    #endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(blendMasks, weights);

    // _MAIN_LAYER_INFLUENCE_MODE is a pure visual mode that doesn't contribute to the weights of a layer
    // The motivation is like this: if a layer is visible, then we will apply influence on top of it (so it is only visual).
    // This is what is done for normal and baseColor and we do the same for height.
    // Note that if we apply influence before ApplyHeightBlend, then have a different behavior.
#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    // Add main layer influence if any (simply add main layer add on other layer)
    // We multiply by the input mask for the first layer (blendMask.a) because if the mask here is black it means that the layer
    // is not actually underneath any visible layer so we don't want to inherit its height.
    float influenceMask = blendMasks.a;
    #ifdef _INFLUENCEMASK_MAP
    influenceMask *= GetInfluenceMask(layerTexCoord, true, lod);
    #endif
    height1 += height0 * _InheritBaseHeight1 * influenceMask;
    height2 += height0 * _InheritBaseHeight2 * influenceMask;
    height3 += height0 * _InheritBaseHeight3 * influenceMask;
#endif

    float heightResult = BlendLayeredScalar(height0, height1, height2, height3, weights);

   // Applying scaling of the object if requested
    #ifdef _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    float3 objectScale = GetDisplacementObjectScale(true);
    // Reminder: mappingType is know statically, so code below is optimize by the compiler
    // Planar and Triplanar are in world space thus it is independent of object scale
    return heightResult.xxx * BlendLayeredVector3( ((layerTexCoord.base0.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)),
                                                   ((layerTexCoord.base1.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)),
                                                   ((layerTexCoord.base2.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)),
                                                   ((layerTexCoord.base3.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)), weights);
    #else
    return heightResult.xxx;
    #endif
#else
    return float3(0.0, 0.0, 0.0);
#endif
}
