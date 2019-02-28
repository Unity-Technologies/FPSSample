//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"

#define LAYERS_HEIGHTMAP_ENABLE (defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || (_LAYER_COUNT > 2 && defined(_HEIGHTMAP2)) || (_LAYER_COUNT > 3 && defined(_HEIGHTMAP3)))

// Number of sampler are limited, we need to share sampler as much as possible with lit material
// for this we put the constraint that the sampler are the same in a layered material for all textures of the same type
// then we take the sampler matching the first textures use of this type
#if defined(_NORMALMAP0)
    #if defined(_NORMALMAP_TANGENT_SPACE0)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap0
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS0
    #endif
#elif defined(_NORMALMAP1)
    #if defined(_NORMALMAP_TANGENT_SPACE1)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap1
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS1
    #endif
#elif defined(_NORMALMAP2)
    #if defined(_NORMALMAP_TANGENT_SPACE2)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap2
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS2
    #endif
#elif defined(_NORMALMAP3)
    #if defined(_NORMALMAP_TANGENT_SPACE3)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap3
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS3
    #endif
#elif defined(_BENTNORMALMAP0)
    #if defined(_NORMALMAP_TANGENT_SPACE0)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap0
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS0
    #endif
#elif defined(_BENTNORMALMAP1)
    #if defined(_NORMALMAP_TANGENT_SPACE1)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap1
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS1
    #endif
#elif defined(_BENTNORMALMAP2)
    #if defined(_NORMALMAP_TANGENT_SPACE2)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap2
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS2
    #endif
#else
    #if defined(_NORMALMAP_TANGENT_SPACE3)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap3
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS3
    #endif
#endif

#if defined(_DETAIL_MAP0)
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap0
#elif defined(_DETAIL_MAP1)
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap1
#elif defined(_DETAIL_MAP2)
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap2
#else
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap3
#endif

#if defined(_MASKMAP0)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap0
#elif defined(_MASKMAP1)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap1
#elif defined(_MASKMAP2)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap2
#else
#define SAMPLER_MASKMAP_IDX sampler_MaskMap3
#endif

#if defined(_HEIGHTMAP0)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap0
#elif defined(_HEIGHTMAP1)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap1
#elif defined(_HEIGHTMAP2)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap2
#elif defined(_HEIGHTMAP3)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap3
#endif

#if defined(_SUBSURFACE_MASK_MAP0)
#define SAMPLER_SUBSURFACE_MASK_MAP_IDX sampler_SubsurfaceMaskMap0
#elif defined(_SUBSURFACE_MASK_MAP1)
#define SAMPLER_SUBSURFACE_MASK_MAP_IDX sampler_SubsurfaceMaskMap1
#elif defined(_SUBSURFACE_MASK_MAP2)
#define SAMPLER_SUBSURFACE_MASK_MAP_IDX sampler_SubsurfaceMaskMap2
#elif defined(_SUBSURFACE_MASK_MAP3)
#define SAMPLER_SUBSURFACE_MASK_MAP_IDX sampler_SubsurfaceMaskMap3
#endif

#if defined(_THICKNESSMAP0)
#define SAMPLER_THICKNESSMAP_IDX sampler_ThicknessMap0
#elif defined(_THICKNESSMAP1)
#define SAMPLER_THICKNESSMAP_IDX sampler_ThicknessMap1
#elif defined(_THICKNESSMAP2)
#define SAMPLER_THICKNESSMAP_IDX sampler_ThicknessMap2
#elif defined(_THICKNESSMAP3)
#define SAMPLER_THICKNESSMAP_IDX sampler_ThicknessMap3
#endif

// Define a helper macro

#define ADD_ZERO_IDX(Name) Name##0

// include LitDataInternal multiple time to define the variation of GetSurfaceData for each layer
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name##0
#ifdef _NORMALMAP0
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE0
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP0
#define _DETAIL_MAP_IDX
#endif
#ifdef _SUBSURFACE_MASK_MAP0
#define _SUBSURFACE_MASK_MAP_IDX
#endif
#ifdef _THICKNESSMAP0
#define _THICKNESSMAP_IDX
#endif
#ifdef _MASKMAP0
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP0
#define _BENTNORMALMAP_IDX
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _SUBSURFACE_MASK_MAP_IDX
#undef _THICKNESSMAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

#define LAYER_INDEX 1
#define ADD_IDX(Name) Name##1
#ifdef _NORMALMAP1
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE1
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP1
#define _DETAIL_MAP_IDX
#endif
#ifdef _SUBSURFACE_MASK_MAP1
#define _SUBSURFACE_MASK_MAP_IDX
#endif
#ifdef _THICKNESSMAP1
#define _THICKNESSMAP_IDX
#endif
#ifdef _MASKMAP1
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP1
#define _BENTNORMALMAP_IDX
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _SUBSURFACE_MASK_MAP_IDX
#undef _THICKNESSMAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

#define LAYER_INDEX 2
#define ADD_IDX(Name) Name##2
#ifdef _NORMALMAP2
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE2
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP2
#define _DETAIL_MAP_IDX
#endif
#ifdef _SUBSURFACE_MASK_MAP2
#define _SUBSURFACE_MASK_MAP_IDX
#endif
#ifdef _THICKNESSMAP2
#define _THICKNESSMAP_IDX
#endif
#ifdef _MASKMAP2
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP2
#define _BENTNORMALMAP_IDX
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _SUBSURFACE_MASK_MAP_IDX
#undef _THICKNESSMAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

#define LAYER_INDEX 3
#define ADD_IDX(Name) Name##3
#ifdef _NORMALMAP3
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE3
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP3
#define _DETAIL_MAP_IDX
#endif
#ifdef _SUBSURFACE_MASK_MAP3
#define _SUBSURFACE_MASK_MAP_IDX
#endif
#ifdef _THICKNESSMAP3
#define _THICKNESSMAP_IDX
#endif
#ifdef _MASKMAP3
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP3
#define _BENTNORMALMAP_IDX
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _SUBSURFACE_MASK_MAP_IDX
#undef _THICKNESSMAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

float3 BlendLayeredVector3(float3 x0, float3 x1, float3 x2, float3 x3, float weight[4])
{
    float3 result = float3(0.0, 0.0, 0.0);

    result = x0 * weight[0] + x1 * weight[1];
#if _LAYER_COUNT >= 3
    result += (x2 * weight[2]);
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

float BlendLayeredScalar(float x0, float x1, float x2, float x3, float weight[4])
{
    float result = 0.0;

    result = x0 * weight[0] + x1 * weight[1];
#if _LAYER_COUNT >= 3
    result += x2 * weight[2];
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

// In the case of subsurface profile index, the goal is to take the index with the hights weights.
// Or the last found in case of equality.
float BlendLayeredDiffusionProfile(float x0, float x1, float x2, float x3, float weight[4])
{
    int diffusionProfileId = x0;
    float currentMax = weight[0];

    diffusionProfileId = currentMax < weight[1] ? x1 : diffusionProfileId;
    currentMax = max(currentMax, weight[1]);

#if _LAYER_COUNT >= 3
    diffusionProfileId = currentMax < weight[2] ? x2 : diffusionProfileId;
    currentMax = max(currentMax, weight[2]);
#endif
#if _LAYER_COUNT >= 4
    diffusionProfileId = currentMax < weight[3] ? x3 : diffusionProfileId;
#endif

    return diffusionProfileId;
}

#define SURFACEDATA_BLEND_VECTOR3(surfaceData, name, mask) BlendLayeredVector3(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define SURFACEDATA_BLEND_SCALAR(surfaceData, name, mask) BlendLayeredScalar(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define SURFACEDATA_BLEND_DIFFUSION_PROFILE(surfaceData, name, mask) BlendLayeredDiffusionProfile(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define PROP_BLEND_SCALAR(name, mask) BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 vertexNormalWS, inout LayerTexCoord layerTexCoord)
{
    layerTexCoord.vertexNormalWS = vertexNormalWS;
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(vertexNormalWS);

    int mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR_BLENDMASK)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR_BLENDMASK)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif

    // Note: Blend mask have its dedicated mapping and tiling.
    // To share code, we simply call the regular code from the main layer for it then save the result, then do regular call for all layers.
    ComputeLayerTexCoord0(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMaskBlendMask, _UVMappingMaskBlendMask,
                            _LayerMaskMap_ST.xy, _LayerMaskMap_ST.zw, float2(0.0, 0.0), float2(0.0, 0.0), 1.0, false,
                            positionWS, _TexWorldScaleBlendMask,
                            mappingType, layerTexCoord);

    layerTexCoord.blendMask = layerTexCoord.base0;

    // On all layers (but not on blend mask) we can scale the tiling with object scale (only uniform supported)
    // Note: the object scale doesn't affect planar/triplanar mapping as they already handle the object scale.
    float tileObjectScale = 1.0;
#ifdef _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    // Extract scaling from world transform
    float4x4 worldTransform = GetObjectToWorldMatrix();
    // assuming uniform scaling, take only the first column
    tileObjectScale = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
#endif

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR0)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR0)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif

    ComputeLayerTexCoord0(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask0, _UVDetailsMappingMask0,
                            _BaseColorMap0_ST.xy, _BaseColorMap0_ST.zw, _DetailMap0_ST.xy, _DetailMap0_ST.zw, 1.0
                            #if !defined(_MAIN_LAYER_INFLUENCE_MODE)
                            * tileObjectScale  // We only affect layer0 in case we are not in influence mode (i.e we should not change the base object)
                            #endif
                            , _LinkDetailsWithBase0
                            , positionWS, _TexWorldScale0,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR1)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR1)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord1(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask1, _UVDetailsMappingMask1,
                            _BaseColorMap1_ST.xy, _BaseColorMap1_ST.zw, _DetailMap1_ST.xy, _DetailMap1_ST.zw, tileObjectScale, _LinkDetailsWithBase1,
                            positionWS, _TexWorldScale1,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR2)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR2)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord2(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask2, _UVDetailsMappingMask2,
                            _BaseColorMap2_ST.xy, _BaseColorMap2_ST.zw, _DetailMap2_ST.xy, _DetailMap2_ST.zw, tileObjectScale, _LinkDetailsWithBase2,
                            positionWS, _TexWorldScale2,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR3)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR3)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord3(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask3, _UVDetailsMappingMask3,
                            _BaseColorMap3_ST.xy, _BaseColorMap3_ST.zw, _DetailMap3_ST.xy, _DetailMap3_ST.zw, tileObjectScale, _LinkDetailsWithBase3,
                            positionWS, _TexWorldScale3,
                            mappingType, layerTexCoord);
}

// This is call only in this file
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(FragInputs input, inout LayerTexCoord layerTexCoord)
{
#ifdef SURFACE_GRADIENT
    GenerateLayerTexCoordBasisTB(input, layerTexCoord);
#endif

    GetLayerTexCoord(   input.texCoord0.xy, input.texCoord1.xy, input.texCoord2.xy, input.texCoord3.xy,
                        input.positionRWS, input.worldToTangent[2].xyz, layerTexCoord);
}

void ApplyDisplacementTileScale(inout float height0, inout float height1, inout float height2, inout float height3)
{
    // When we change the tiling, we have want to conserve the ratio with the displacement (and this is consistent with per pixel displacement)
#ifdef _DISPLACEMENT_LOCK_TILING_SCALE
    float tileObjectScale = 1.0;
    #ifdef _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    // Extract scaling from world transform
    float4x4 worldTransform = GetObjectToWorldMatrix();
    // assuming uniform scaling, take only the first column
    tileObjectScale = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    #endif

    // TODO: precompute all these scaling factors!
    height0 *= _InvTilingScale0;
    #if !defined(_MAIN_LAYER_INFLUENCE_MODE)
    height0 /= tileObjectScale;  // We only affect layer0 in case we are not in influence mode (i.e we should not change the base object)
    #endif
    height1 = (height1 / tileObjectScale) * _InvTilingScale1;
    height2 = (height2 / tileObjectScale) * _InvTilingScale2;
    height3 = (height3 / tileObjectScale) * _InvTilingScale3;
#endif
}

// This function is just syntaxic sugar to nullify height not used based on heightmap avaibility and layer
void SetEnabledHeightByLayer(inout float height0, inout float height1, inout float height2, inout float height3)
{
#ifndef _HEIGHTMAP0
    height0 = 0.0;
#endif
#ifndef _HEIGHTMAP1
    height1 = 0.0;
#endif
#ifndef _HEIGHTMAP2
    height2 = 0.0;
#endif
#ifndef _HEIGHTMAP3
    height3 = 0.0;
#endif

#if _LAYER_COUNT < 4
    height3 = 0.0;
#endif
#if _LAYER_COUNT < 3
    height2 = 0.0;
#endif
}

void ComputeMaskWeights(float4 inputMasks, out float outWeights[_MAX_LAYER])
{
    ZERO_INITIALIZE_ARRAY(float, outWeights, _MAX_LAYER);

    float masks[_MAX_LAYER];
    masks[0] = inputMasks.a;

    masks[1] = inputMasks.r;
#if _LAYER_COUNT > 2
    masks[2] = inputMasks.g;
#else
    masks[2] = 0.0;
#endif
#if _LAYER_COUNT > 3
    masks[3] = inputMasks.b;
#else
    masks[3] = 0.0;
#endif

    // calculate weight of each layers
    // Algorithm is like this:
    // Top layer have priority on others layers
    // If a top layer doesn't use the full weight, the remaining can be use by the following layer.
    float weightsSum = 0.0;

    UNITY_UNROLL
    for (int i = _LAYER_COUNT - 1; i >= 0; --i)
    {
        outWeights[i] = min(masks[i], (1.0 - weightsSum));
        weightsSum = saturate(weightsSum + masks[i]);
    }
}

// Caution: Blend mask are Layer 1 R - Layer 2 G - Layer 3 B - Main Layer A
float4 GetBlendMask(LayerTexCoord layerTexCoord, float4 vertexColor, bool useLodSampling = false, float lod = 0)
{
    // Caution:
    // Blend mask are Main Layer A - Layer 1 R - Layer 2 G - Layer 3 B
    // Value for main layer is not use for blending itself but for alternate weighting like density.
    // Settings this specific Main layer blend mask in alpha allow to be transparent in case we don't use it and 1 is provide by default.
    float4 blendMasks = useLodSampling ? SAMPLE_UVMAPPING_TEXTURE2D_LOD(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask, lod) : SAMPLE_UVMAPPING_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask);

    // Wind uses vertex alpha as an intensity parameter.
    // So in case Layered shader uses wind, we need to hardcode the alpha here so that the main layer can be visible without affecting wind intensity.
    // It also means that when using wind, users can't use vertex color to modulate the effect of influence from the main layer.
    float4 maskVertexColor = vertexColor;
#if defined(_LAYER_MASK_VERTEX_COLOR_MUL)
    #if defined(_VERTEX_WIND)
    // For multiplicative vertex color blend mask. 1.0f is the neutral value
    maskVertexColor.a = 1.0f;
    #endif
    blendMasks *= maskVertexColor;
#elif defined(_LAYER_MASK_VERTEX_COLOR_ADD)
    #if defined(_VERTEX_WIND)
    // For additive vertex color blend mask. 0.5f is the neutral value (0.5 * 2.0 - 1.0 = 0.0)
    maskVertexColor.a = 0.5f;
    #endif
    blendMasks = saturate(blendMasks + maskVertexColor * 2.0 - 1.0);
#endif

    return blendMasks;
}

float GetInfluenceMask(LayerTexCoord layerTexCoord, bool useLodSampling = false, float lod = 0)
{
    // Sample influence mask with same mapping as Main layer
    return useLodSampling ? SAMPLE_UVMAPPING_TEXTURE2D_LOD(_LayerInfluenceMaskMap, sampler_LayerInfluenceMaskMap, layerTexCoord.base0, lod).r : SAMPLE_UVMAPPING_TEXTURE2D(_LayerInfluenceMaskMap, sampler_LayerInfluenceMaskMap, layerTexCoord.base0).r;
}

float GetMaxHeight(float4 heights)
{
    float maxHeight = max(heights.r, heights.g);
#ifdef _LAYEREDLIT_4_LAYERS
    maxHeight = max(Max3(heights.r, heights.g, heights.b), heights.a);
#endif
#ifdef _LAYEREDLIT_3_LAYERS
    maxHeight = Max3(heights.r, heights.g, heights.b);
#endif

    return maxHeight;
}

// Returns layering blend mask after application of height based blend.
float4 ApplyHeightBlend(float4 heights, float4 blendMask)
{
    // We need to mask out inactive layers so that their height does not impact the result.
    float4 maskedHeights = heights * blendMask.argb;

    float maxHeight = GetMaxHeight(maskedHeights);
    // Make sure that transition is not zero otherwise the next computation will be wrong.
    // The epsilon here also has to be bigger than the epsilon in the next computation.
    float transition = max(_HeightTransition, 1e-5);

    // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
    // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
    maskedHeights = maskedHeights - maxHeight.xxxx;
    // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
    maskedHeights = (max(0, maskedHeights + transition) + 1e-6) * blendMask.argb;

    // Normalize
    maxHeight = GetMaxHeight(maskedHeights);
    maskedHeights = maskedHeights / max(maxHeight.xxxx, 1e-6);

    return maskedHeights.yzwx;
}

// Calculate weights to apply to each layer
// Caution: This function must not be use for per vertex/pixel displacement, there is a dedicated function for them.
// This function handle triplanar
void ComputeLayerWeights(FragInputs input, LayerTexCoord layerTexCoord, float4 inputAlphaMask, float4 blendMasks, out float outWeights[_MAX_LAYER])
{
#if defined(_DENSITY_MODE)
    // Note: blendMasks.argb because a is main layer
    float4 opacityAsDensity = saturate((inputAlphaMask - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks.argb)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
    float4 useOpacityAsDensityParam = float4(_OpacityAsDensity0, _OpacityAsDensity1, _OpacityAsDensity2, _OpacityAsDensity3);
    blendMasks.argb = lerp(blendMasks.argb, opacityAsDensity, useOpacityAsDensityParam);
#endif

#if LAYERS_HEIGHTMAP_ENABLE
    float height0 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base0).r - _HeightCenter0) * _HeightAmplitude0;
    float height1 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base1).r - _HeightCenter1) * _HeightAmplitude1;
    float height2 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base2).r - _HeightCenter2) * _HeightAmplitude2;
    float height3 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base3).r - _HeightCenter3) * _HeightAmplitude3;
    // Height is affected by tiling property and by object scale (depends on option).
    // Apply scaling from tiling properties (TexWorldScale and tiling from BaseColor)
    ApplyDisplacementTileScale(height0, height1, height2, height3);
    // Nullify height that are not used, so compiler can remove unused case
    SetEnabledHeightByLayer(height0, height1, height2, height3);

    // Reminder: _MAIN_LAYER_INFLUENCE_MODE is a purely visual mode, it is not take into account for the blendMasks
    // As it is purely visual, it is not apply in ComputeLayerWeights

    #if defined(_HEIGHT_BASED_BLEND)
    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    blendMasks = ApplyHeightBlend(float4(height0, height1, height2, height3), blendMasks);
    #endif
#endif

    ComputeMaskWeights(blendMasks, outWeights);
}

float3 ComputeMainNormalInfluence(float influenceMask, FragInputs input, float3 normalTS0, float3 normalTS1, float3 normalTS2, float3 normalTS3, LayerTexCoord layerTexCoord, float inputMainLayerMask, float weights[_MAX_LAYER])
{
    // Get our regular normal from regular layering
    float3 normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);

    // THen get Main Layer Normal influence factor. Main layer is 0 because it can't be influence. In this case the final lerp return normalTS.
    float influenceFactor = BlendLayeredScalar(0.0, _InheritBaseNormal1, _InheritBaseNormal2, _InheritBaseNormal3, weights) * influenceMask;
    // We will add smoothly the contribution of the normal map by lerping between vertex normal ( (0,0,1) in tangent space) and the actual normal from the main layer depending on the influence factor.
    // Note: that we don't take details map into account here.
    #ifdef SURFACE_GRADIENT
    float3 neutralNormalTS = float3(0.0, 0.0, 0.0);
    #else
    float3 neutralNormalTS = float3(0.0, 0.0, 1.0);
    #endif
    float3 mainNormalTS = lerp(neutralNormalTS, normalTS0, influenceFactor);

    // Add on our regular normal a bit of Main Layer normal base on influence factor. Note that this affect only the "visible" normal.
    #ifdef SURFACE_GRADIENT
    return normalTS + influenceFactor * mainNormalTS * inputMainLayerMask;
    #else
    return lerp(normalTS, BlendNormalRNM(normalTS, mainNormalTS), influenceFactor * inputMainLayerMask); // Multiply by inputMainLayerMask in order to avoid influence where main layer should never be present
    #endif
}

float3 ComputeMainBaseColorInfluence(float influenceMask, float3 baseColor0, float3 baseColor1, float3 baseColor2, float3 baseColor3, LayerTexCoord layerTexCoord, float inputMainLayerMask, float weights[_MAX_LAYER])
{
    float3 baseColor = BlendLayeredVector3(baseColor0, baseColor1, baseColor2, baseColor3, weights);

    float influenceFactor = BlendLayeredScalar(0.0, _InheritBaseColor1, _InheritBaseColor2, _InheritBaseColor3, weights) * influenceMask * inputMainLayerMask; // Multiply by inputMainLayerMask in order to avoid influence where main layer should never be present

    // We want to calculate the mean color of the texture. For this we will sample a low mipmap
    float textureBias = 15.0; // Use maximum bias
    float3 baseMeanColor0 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap0, sampler_BaseColorMap0, layerTexCoord.base0, textureBias).rgb *_BaseColor0.rgb;
    float3 baseMeanColor1 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap1, sampler_BaseColorMap0, layerTexCoord.base1, textureBias).rgb *_BaseColor1.rgb;
    float3 baseMeanColor2 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap2, sampler_BaseColorMap0, layerTexCoord.base2, textureBias).rgb *_BaseColor2.rgb;
    float3 baseMeanColor3 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap3, sampler_BaseColorMap0, layerTexCoord.base3, textureBias).rgb *_BaseColor3.rgb;

    float3 meanColor = BlendLayeredVector3(baseMeanColor0, baseMeanColor1, baseMeanColor2, baseMeanColor3, weights);

    // If we inherit from base layer, we will add a bit of it
    // We add variance of current visible level and the base color 0 or mean (to retrieve initial color) depends on influence
    // (baseColor - meanColor) + lerp(meanColor, baseColor0, inheritBaseColor) simplify to
    // saturate(influenceFactor * (baseColor0 - meanColor) + baseColor);
    // There is a special case when baseColor < meanColor to avoid getting negative values.
    float3 factor = baseColor > meanColor ? (baseColor0 - meanColor) : (baseColor0 * baseColor / max(meanColor, 0.001) - baseColor); // max(to avoid divide by 0)
    return influenceFactor * factor + baseColor;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitDataDisplacement.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitBuiltinData.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    uint3 fadeMaskSeed = asuint((int3)(V * _ScreenSize.xyx)); // Quantize V to _ScreenSize values
    LODDitheringTransition(fadeMaskSeed, unity_LODFade.x);
#endif

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    float4 blendMasks = GetBlendMask(layerTexCoord, input.color);
    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord, blendMasks);

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, depthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
#endif

    SurfaceData surfaceData0, surfaceData1, surfaceData2, surfaceData3;
    float3 normalTS0, normalTS1, normalTS2, normalTS3;
    float3 bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3;
    float alpha0 = GetSurfaceData0(input, layerTexCoord, surfaceData0, normalTS0, bentNormalTS0);
    float alpha1 = GetSurfaceData1(input, layerTexCoord, surfaceData1, normalTS1, bentNormalTS1);
    float alpha2 = GetSurfaceData2(input, layerTexCoord, surfaceData2, normalTS2, bentNormalTS2);
    float alpha3 = GetSurfaceData3(input, layerTexCoord, surfaceData3, normalTS3, bentNormalTS3);

    // Note: If per pixel displacement is enabled it mean we will fetch again the various heightmaps at the intersection location. Not sure the compiler can optimize.
    float weights[_MAX_LAYER];
    ComputeLayerWeights(input, layerTexCoord, float4(alpha0, alpha1, alpha2, alpha3), blendMasks, weights);

    // For layered shader, alpha of base color is used as either an opacity mask, a composition mask for inheritance parameters or a density mask.
    float alpha = PROP_BLEND_SCALAR(alpha, weights);

#ifdef _ALPHATEST_ON
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

    float3 normalTS;
    float3 bentNormalTS;
    float3 bentNormalWS;
#if defined(_MAIN_LAYER_INFLUENCE_MODE)

    #ifdef _INFLUENCEMASK_MAP
    float influenceMask = GetInfluenceMask(layerTexCoord);
    #else
    float influenceMask = 1.0;
    #endif

    if (influenceMask > 0.0f)
    {
        surfaceData.baseColor = ComputeMainBaseColorInfluence(influenceMask, surfaceData0.baseColor, surfaceData1.baseColor, surfaceData2.baseColor, surfaceData3.baseColor, layerTexCoord, blendMasks.a, weights);
        normalTS = ComputeMainNormalInfluence(influenceMask, input, normalTS0, normalTS1, normalTS2, normalTS3, layerTexCoord, blendMasks.a, weights);
        bentNormalTS = ComputeMainNormalInfluence(influenceMask, input, bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3, layerTexCoord, blendMasks.a, weights);
    }
    else
#endif
    {
        surfaceData.baseColor = SURFACEDATA_BLEND_VECTOR3(surfaceData, baseColor, weights);
        normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
        bentNormalTS = BlendLayeredVector3(bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3, weights);
    }

    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.subsurfaceMask = SURFACEDATA_BLEND_SCALAR(surfaceData, subsurfaceMask, weights);
    surfaceData.thickness = SURFACEDATA_BLEND_SCALAR(surfaceData, thickness, weights);
    surfaceData.diffusionProfile = SURFACEDATA_BLEND_DIFFUSION_PROFILE(surfaceData, diffusionProfile, weights);

    // Layered shader support SSS and Transmission features
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING;
#endif
#ifdef _MATERIAL_FEATURE_TRANSMISSION
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_TRANSMISSION;
#endif

    // Init other parameters
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

    GetNormalWS(input, normalTS, surfaceData.normalWS);
    // Use bent normal to sample GI if available
    // If any layer use a bent normal map, then bentNormalTS contain the interpolated result of bentnormal and normalmap (in case no bent normal are available)
    // Note: the code in LitDataInternal ensure that we fallback on normal map for layer that have no bentnormal
#if defined(_BENTNORMALMAP0) || defined(_BENTNORMALMAP1) || defined(_BENTNORMALMAP2) || defined(_BENTNORMALMAP3)
    GetNormalWS(input, bentNormalTS, bentNormalWS);
#else // if no bent normal are available at all just keep the calculation fully
    bentNormalWS = surfaceData.normalWS;
#endif

    // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
    // If user provide bent normal then we process a better term
#if (defined(_BENTNORMALMAP0) || defined(_BENTNORMALMAP1) || defined(_BENTNORMALMAP2) || defined(_BENTNORMALMAP3)) && defined(_ENABLESPECULAROCCLUSION)
    // If we have bent normal and ambient occlusion, process a specular occlusion
    #ifdef SPECULAR_OCCLUSION_USE_SPTD
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAOPivot(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness));
    #else
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    #endif
#elif defined(_MASKMAP0) || defined(_MASKMAP1) || defined(_MASKMAP2) || defined(_MASKMAP3)
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(dot(surfaceData.normalWS, V), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#else
    surfaceData.specularOcclusion = 1.0;
#endif

#if HAVE_DECALS
    if (_EnableDecals)
    {
        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
        ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
    }
#endif

#ifdef _ENABLE_GEOMETRIC_SPECULAR_AA
    // Specular AA
    surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, input.worldToTangent[2], _SpecularAAScreenSpaceVariance, _SpecularAAThreshold);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, layerTexCoord.base0.uv, _BaseColorMap0, _BaseColorMap0_TexelSize, _BaseColorMap0_MipInfo, surfaceData.baseColor);
        surfaceData.metallic = 0;
    }

    // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
    // as it can modify attribute use for static lighting
    ApplyDebugToSurfaceData(input.worldToTangent, surfaceData);
#endif

    GetBuiltinData(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, builtinData);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataMeshModification.hlsl"
