#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderVariablesDecal.cs.hlsl"
#else

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.cs.hlsl"

StructuredBuffer<DecalData> _DecalDatas;

TEXTURE2D_ARRAY(_DecalAtlas);
SAMPLER(sampler_DecalAtlas);

TEXTURE2D(_DecalAtlas2D);
SAMPLER(_trilinear_clamp_sampler_DecalAtlas2D);

#if defined(PLATFORM_NEEDS_UNORM_UAV_SPECIFIER)
RW_TEXTURE2D(unorm float, _DecalHTile); // DXGI_FORMAT_R8_UINT is not supported by Unity
#else
RW_TEXTURE2D(float, _DecalHTile); // DXGI_FORMAT_R8_UINT is not supported by Unity
#endif
TEXTURE2D(_DecalHTileTexture);

UNITY_INSTANCING_BUFFER_START(Decal)
    UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_INSTANCING_BUFFER_END(matrix)

#endif
