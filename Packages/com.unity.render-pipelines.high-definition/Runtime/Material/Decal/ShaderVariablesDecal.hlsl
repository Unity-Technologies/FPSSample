#ifdef SHADER_VARIABLES_INCLUDE_CB
uint    _EnableDecals;
float2  _DecalAtlasResolution;
uint    _DecalCount;
#else

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal//Decal.cs.hlsl"

StructuredBuffer<DecalData> _DecalDatas;

TEXTURE2D_ARRAY(_DecalAtlas);
SAMPLER(sampler_DecalAtlas);

TEXTURE2D(_DecalAtlas2D);
SAMPLER(_trilinear_clamp_sampler_DecalAtlas2D);

RW_TEXTURE2D(float, _DecalHTile); // DXGI_FORMAT_R8_UINT is not supported by Unity
TEXTURE2D(_DecalHTileTexture);

UNITY_INSTANCING_BUFFER_START(Decal)
    UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_INSTANCING_BUFFER_END(matrix)

#endif
