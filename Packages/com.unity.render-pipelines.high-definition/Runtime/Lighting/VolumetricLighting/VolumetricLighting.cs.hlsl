//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef VOLUMETRICLIGHTING_CS_HLSL
#define VOLUMETRICLIGHTING_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DensityVolumeEngineData
// PackingRules = Exact
struct DensityVolumeEngineData
{
    float3 scattering;
    float extinction;
    float3 textureTiling;
    int textureIndex;
    float3 textureScroll;
    int invertFade;
    float3 rcpPosFade;
    float pad1;
    float3 rcpNegFade;
    float pad2;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.DensityVolumeEngineData
//
float3 GetScattering(DensityVolumeEngineData value)
{
    return value.scattering;
}
float GetExtinction(DensityVolumeEngineData value)
{
    return value.extinction;
}
float3 GetTextureTiling(DensityVolumeEngineData value)
{
    return value.textureTiling;
}
int GetTextureIndex(DensityVolumeEngineData value)
{
    return value.textureIndex;
}
float3 GetTextureScroll(DensityVolumeEngineData value)
{
    return value.textureScroll;
}
int GetInvertFade(DensityVolumeEngineData value)
{
    return value.invertFade;
}
float3 GetRcpPosFade(DensityVolumeEngineData value)
{
    return value.rcpPosFade;
}
float GetPad1(DensityVolumeEngineData value)
{
    return value.pad1;
}
float3 GetRcpNegFade(DensityVolumeEngineData value)
{
    return value.rcpNegFade;
}
float GetPad2(DensityVolumeEngineData value)
{
    return value.pad2;
}


#endif
