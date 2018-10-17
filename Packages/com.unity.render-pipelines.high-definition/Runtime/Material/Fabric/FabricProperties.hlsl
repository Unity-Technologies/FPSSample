// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
//TEXTURE2D(_BentNormalMap); // Reuse sampler from normal map
//SAMPLER(sampler_BentNormalMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_FuzzDetailMap);
SAMPLER(sampler_FuzzDetailMap);
TEXTURE2D(_ThreadMap);
SAMPLER(sampler_ThreadMap);

TEXTURE2D(_TangentMap);
SAMPLER(sampler_TangentMap);

TEXTURE2D(_AnisotropyMap);
SAMPLER(sampler_AnisotropyMap);

TEXTURE2D(_SubsurfaceMaskMap);
SAMPLER(sampler_SubsurfaceMaskMap);
TEXTURE2D(_ThicknessMap);
SAMPLER(sampler_ThicknessMap);

TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);

CBUFFER_START(UnityPerMaterial)

float4 _UVMappingMask;
float4 _UVMappingMaskThread;
float4 _UVMappingMaskEmissive;

float4 _DoubleSidedConstants;

float _LinkDetailsWithBase;

float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;

float4 _SpecularColor;

float _AlphaCutoff;

float _EnableSpecularOcclusion;

float _Smoothness;
float _SmoothnessRemapMin;
float _SmoothnessRemapMax;
float _AORemapMin;
float _AORemapMax;

float _NormalScale;

float4 _ThreadMap_ST;
float _ThreadAOScale;
float _ThreadNormalScale;
float _ThreadSmoothnessScale;

float _FuzzDetailScale;
float _FuzzDetailUVScale;

float _Anisotropy;

int   _DiffusionProfile;
float _SubsurfaceMask;
float _Thickness;
float4 _ThicknessRemap;

float4 _EmissiveColorMap_ST;
float4 _EmissiveColor;
float _AlbedoAffectEmissive;

CBUFFER_END
