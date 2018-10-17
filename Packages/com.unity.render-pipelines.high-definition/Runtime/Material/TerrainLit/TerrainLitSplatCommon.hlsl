
#ifdef _TERRAIN_8_LAYERS
    #define _LAYER_COUNT 8
#else
    #define _LAYER_COUNT 4
#endif

#define DECLARE_TERRAIN_LAYER_TEXS(n)   \
    TEXTURE2D(_Splat##n);               \
    TEXTURE2D(_Normal##n);              \
    TEXTURE2D(_Mask##n)

DECLARE_TERRAIN_LAYER_TEXS(0);
DECLARE_TERRAIN_LAYER_TEXS(1);
DECLARE_TERRAIN_LAYER_TEXS(2);
DECLARE_TERRAIN_LAYER_TEXS(3);
#ifdef _TERRAIN_8_LAYERS
    DECLARE_TERRAIN_LAYER_TEXS(4);
    DECLARE_TERRAIN_LAYER_TEXS(5);
    DECLARE_TERRAIN_LAYER_TEXS(6);
    DECLARE_TERRAIN_LAYER_TEXS(7);
    TEXTURE2D(_Control1);
#endif

#undef DECLARE_TERRAIN_LAYER_TEXS

TEXTURE2D(_Control0);
SAMPLER(sampler_Splat0);
SAMPLER(sampler_Control0);

#ifdef UNITY_INSTANCING_ENABLED
TEXTURE2D(_TerrainHeightmapTexture);
TEXTURE2D(_TerrainNormalmapTexture);
#endif

#define DECLARE_TERRAIN_LAYER_PROPS(n)  \
    float4 _Splat##n##_ST;              \
    float _Metallic##n;                 \
    float _Smoothness##n;               \
    float _NormalScale##n;              \
    float4 _DiffuseRemapScale##n;       \
    float4 _MaskMapRemapOffset##n;      \
    float4 _MaskMapRemapScale##n

CBUFFER_START(UnityTerrain)

    #ifdef DEBUG_DISPLAY
        float4 _Control0_TexelSize;
        float4 _Control0_MipInfo;
        float4 _Splat0_TexelSize;
        float4 _Splat0_MipInfo;
        float4 _Splat1_TexelSize;
        float4 _Splat1_MipInfo;
        float4 _Splat2_TexelSize;
        float4 _Splat2_MipInfo;
        float4 _Splat3_TexelSize;
        float4 _Splat3_MipInfo;
        #ifdef _TERRAIN_8_LAYERS
            float4 _Splat4_TexelSize;
            float4 _Splat4_MipInfo;
            float4 _Splat5_TexelSize;
            float4 _Splat5_MipInfo;
            float4 _Splat6_TexelSize;
            float4 _Splat6_MipInfo;
            float4 _Splat7_TexelSize;
            float4 _Splat7_MipInfo;
        #endif
    #endif

    DECLARE_TERRAIN_LAYER_PROPS(0);
    DECLARE_TERRAIN_LAYER_PROPS(1);
    DECLARE_TERRAIN_LAYER_PROPS(2);
    DECLARE_TERRAIN_LAYER_PROPS(3);
    #ifdef _TERRAIN_8_LAYERS
        DECLARE_TERRAIN_LAYER_PROPS(4);
        DECLARE_TERRAIN_LAYER_PROPS(5);
        DECLARE_TERRAIN_LAYER_PROPS(6);
        DECLARE_TERRAIN_LAYER_PROPS(7);
    #endif

    float _HeightTransition;
    #ifdef UNITY_INSTANCING_ENABLED
        float4 _TerrainHeightmapRecipSize;   // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
        float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
    #endif

CBUFFER_END

#undef DECLARE_TERRAIN_LAYER_PROPS

#ifdef HAVE_MESH_MODIFICATION
#include "TerrainLitDataMeshModification.hlsl"
#endif

// Declare distortion variables just to make the code compile with the Debug Menu.
// See LitBuiltinData.hlsl:73.
TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

float _DistortionScale;
float _DistortionVectorScale;
float _DistortionVectorBias;
float _DistortionBlurScale;
float _DistortionBlurRemapMin;
float _DistortionBlurRemapMax;

float GetSumHeight(float4 heights0, float4 heights1)
{
    float sumHeight = heights0.x;
    sumHeight += heights0.y;
    sumHeight += heights0.z;
    sumHeight += heights0.w;
    #ifdef _TERRAIN_8_LAYERS
        sumHeight += heights1.x;
        sumHeight += heights1.y;
        sumHeight += heights1.z;
        sumHeight += heights1.w;
    #endif
    return sumHeight;
}

float3 SampleNormalGrad(TEXTURE2D_ARGS(textureName, samplerName), float2 uv, float2 dxuv, float2 dyuv, float scale, float3 tangentWS, float3 bitangentWS)
{
    float4 nrm = SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uv, dxuv, dyuv);
#ifdef SURFACE_GRADIENT
    #ifdef UNITY_NO_DXT5nm
        real2 deriv = UnpackDerivativeNormalRGB(nrm, scale);
    #else
        real2 deriv = UnpackDerivativeNormalRGorAG(nrm, scale);
    #endif
    return SurfaceGradientFromTBN(deriv, tangentWS, bitangentWS);
#else
    #ifdef UNITY_NO_DXT5nm
        return UnpackNormalRGB(nrm, scale);
    #else
        return UnpackNormalmapRGorAG(nrm, scale);
    #endif
#endif
}

float4 RemapMasks(float4 masks, float blendMask, float4 remapOffset, float4 remapScale)
{
    float4 ret = masks;
    ret.b *= blendMask; // height needs to be weighted before remapping
    ret = ret * remapScale + remapOffset;
    return ret;
}

#ifdef OVERRIDE_SAMPLER_NAME
    #define sampler_Splat0 OVERRIDE_SAMPLER_NAME
#endif

void TerrainSplatBlend(float2 uv, float3 tangentWS, float3 bitangentWS,
    out float3 outAlbedo, out float3 outNormalTS, out float outSmoothness, out float outMetallic, out float outAO)
{
    // TODO: triplanar and SURFACE_GRADIENT?
    // TODO: POM

    float4 albedo[_LAYER_COUNT];
    float3 normal[_LAYER_COUNT];
    float4 masks[_LAYER_COUNT];

#ifdef _NORMALMAP
    #define SampleNormal(i) SampleNormalGrad(_Normal##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv, _NormalScale##i, tangentWS, bitangentWS)
#else
    #define SampleNormal(i) float3(0, 0, 1)
#endif

#ifdef _MASKMAP
    #define SampleMasks(i, blendMask) RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv), blendMask, _MaskMapRemapOffset##i, _MaskMapRemapScale##i)
    #define NullMask(i)               float4(0, 1, _MaskMapRemapOffset##i.z, 0) // only height matters when weight is zero.
#else
    #define SampleMasks(i, blendMask) float4(_Metallic##i, 1, 0, albedo[i].a * _Smoothness##i)
    #define NullMask(i)               float4(0, 1, 0, 0)
#endif

#define SampleResults(i, mask)                                                                          \
    UNITY_BRANCH if (mask > 0)                                                                          \
    {                                                                                                   \
        float2 splatuv = uv * _Splat##i##_ST.xy + _Splat##i##_ST.zw;                                    \
        float2 splatdxuv = dxuv * _Splat##i##_ST.x;                                                     \
        float2 splatdyuv = dyuv * _Splat##i##_ST.y;                                                     \
        albedo[i] = SAMPLE_TEXTURE2D_GRAD(_Splat##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv);    \
        albedo[i].rgb *= _DiffuseRemapScale##i.xyz;                                                     \
        normal[i] = SampleNormal(i);                                                                    \
        masks[i] = SampleMasks(i, mask);                                                                \
    }                                                                                                   \
    else                                                                                                \
    {                                                                                                   \
        albedo[i] = float4(0, 0, 0, 0);                                                                 \
        normal[i] = float3(0, 0, 0);                                                                    \
        masks[i] = NullMask(i);                                                                         \
    }

    float2 dxuv = ddx(uv);
    float2 dyuv = ddy(uv);

    float4 blendMasks0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, uv);
    #ifdef _TERRAIN_8_LAYERS
        float4 blendMasks1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, uv);
    #else
        float4 blendMasks1 = float4(0, 0, 0, 0);
    #endif

    SampleResults(0, blendMasks0.x);
    SampleResults(1, blendMasks0.y);
    SampleResults(2, blendMasks0.z);
    SampleResults(3, blendMasks0.w);
    #ifdef _TERRAIN_8_LAYERS
        SampleResults(4, blendMasks1.x);
        SampleResults(5, blendMasks1.y);
        SampleResults(6, blendMasks1.z);
        SampleResults(7, blendMasks1.w);
    #endif

#undef SampleNormal
#undef SampleMasks
#undef SampleResults

    float weights[_LAYER_COUNT];
    ZERO_INITIALIZE_ARRAY(float, weights, _LAYER_COUNT);

    #ifdef _MASKMAP
        #ifdef _TERRAIN_BLEND_HEIGHT
            // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
            float maxHeight = masks[0].z;
            maxHeight = max(maxHeight, masks[1].z);
            maxHeight = max(maxHeight, masks[2].z);
            maxHeight = max(maxHeight, masks[3].z);
            #ifdef _TERRAIN_8_LAYERS
                maxHeight = max(maxHeight, masks[4].z);
                maxHeight = max(maxHeight, masks[5].z);
                maxHeight = max(maxHeight, masks[6].z);
                maxHeight = max(maxHeight, masks[7].z);
            #endif

            // Make sure that transition is not zero otherwise the next computation will be wrong.
            // The epsilon here also has to be bigger than the epsilon in the next computation.
            float transition = max(_HeightTransition, 1e-5);

            // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
            // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
            float4 weightedHeights0 = { masks[0].z, masks[1].z, masks[2].z, masks[3].z };
            weightedHeights0 = weightedHeights0 - maxHeight.xxxx;
            // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
            weightedHeights0 = (max(0, weightedHeights0 + transition) + 1e-6) * blendMasks0;

            #ifdef _TERRAIN_8_LAYERS
                float4 weightedHeights1 = { masks[4].z, masks[5].z, masks[6].z, masks[7].z };
                weightedHeights1 = weightedHeights1 - maxHeight.xxxx;
                weightedHeights1 = (max(0, weightedHeights1 + transition) + 1e-6) * blendMasks1;
            #else
                float4 weightedHeights1 = { 0, 0, 0, 0 };
            #endif

            // Normalize
            float sumHeight = GetSumHeight(weightedHeights0, weightedHeights1);
            blendMasks0 = weightedHeights0 / sumHeight.xxxx;
            #ifdef _TERRAIN_8_LAYERS
                blendMasks1 = weightedHeights1 / sumHeight.xxxx;
            #endif
        #else
            // Denser layers are more visible.
            float4 opacityAsDensity0 = saturate((float4(albedo[0].a, albedo[1].a, albedo[2].a, albedo[3].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
            float4 useOpacityAsDensityParam0 = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
            blendMasks0 = lerp(opacityAsDensity0, blendMasks0, useOpacityAsDensityParam0);
            #ifdef _TERRAIN_8_LAYERS
                float4 opacityAsDensity1 = saturate((float4(albedo[4].a, albedo[5].a, albedo[6].a, albedo[7].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks1)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
                float4 useOpacityAsDensityParam1 = { _DiffuseRemapScale4.w, _DiffuseRemapScale5.w, _DiffuseRemapScale6.w, _DiffuseRemapScale7.w };
                blendMasks1 = lerp(opacityAsDensity1, blendMasks1, useOpacityAsDensityParam1);
            #endif
        #endif // if _TERRAIN_BLEND_HEIGHT
    #endif // if _MASKMAP

    weights[0] = blendMasks0.x;
    weights[1] = blendMasks0.y;
    weights[2] = blendMasks0.z;
    weights[3] = blendMasks0.w;
    #ifdef _TERRAIN_8_LAYERS
        weights[4] = blendMasks1.x;
        weights[5] = blendMasks1.y;
        weights[6] = blendMasks1.z;
        weights[7] = blendMasks1.w;
    #endif

    #if defined(_MASKMAP) && !defined(_TERRAIN_BLEND_HEIGHT)
        bool densityBlendEnabled = any(useOpacityAsDensityParam0 < 1);
        #ifdef _TERRAIN_8_LAYERS
            densityBlendEnabled = densityBlendEnabled || any(useOpacityAsDensityParam1 < 1);
        #endif
        // calculate weight of each layers
        // Algorithm is like this:
        // Top layer have priority on others layers
        // If a top layer doesn't use the full weight, the remaining can be use by the following layer.
        float weightsSum = 0.0;

        if (densityBlendEnabled)
        {
            UNITY_UNROLL for (int i = _LAYER_COUNT - 1; i >= 0; --i)
            {
                weights[i] = min(weights[i], (1.0 - weightsSum));
                weightsSum = saturate(weightsSum + weights[i]);
            }
        }
    #endif

    outAlbedo = 0;
    outNormalTS = 0;
    float3 outMasks = 0;
    UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
    {
        outAlbedo += albedo[i].rgb * weights[i];
        outNormalTS += normal[i].rgb * weights[i]; // no need to normalize
        outMasks += masks[i].xyw * weights[i];
    }
    #ifndef _NORMALMAP
        #ifdef SURFACE_GRADIENT
            outNormalTS = float3(0.0, 0.0, 0.0); // No gradient
        #else
            outNormalTS = float3(0.0, 0.0, 1.0);
        #endif
    #endif
    outSmoothness = outMasks.z;
    outMetallic = outMasks.x;
    outAO = outMasks.y;
}
