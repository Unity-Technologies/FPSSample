// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================
TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);

TEXTURE2D(_AmbientOcclusionMap);
SAMPLER(sampler_AmbientOcclusionMap);

TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);

TEXTURE2D(_SmoothnessAMap);
SAMPLER(sampler_SmoothnessAMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_CoatNormalMap);
SAMPLER(sampler_CoatNormalMap);

TEXTURE2D(_SmoothnessBMap);
SAMPLER(sampler_SmoothnessBMap);

TEXTURE2D(_AnisotropyMap);
SAMPLER(sampler_AnisotropyMap);

TEXTURE2D(_CoatSmoothnessMap);
SAMPLER(sampler_CoatSmoothnessMap);

TEXTURE2D(_IridescenceThicknessMap);
SAMPLER(sampler_IridescenceThicknessMap);

TEXTURE2D(_IridescenceMaskMap);
SAMPLER(sampler_IridescenceMaskMap);

TEXTURE2D(_SubsurfaceMaskMap);
SAMPLER(sampler_SubsurfaceMaskMap);

TEXTURE2D(_ThicknessMap);
SAMPLER(sampler_ThicknessMap);

// Details
TEXTURE2D(_DetailMaskMap);
SAMPLER(sampler_DetailMaskMap);

TEXTURE2D(_DetailSmoothnessMap);
SAMPLER(sampler_DetailSmoothnessMap);

TEXTURE2D(_DetailNormalMap);
SAMPLER(sampler_DetailNormalMap);

TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);

CBUFFER_START(UnityPerMaterial)

float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;
float _BaseColorMapUV;
float _BaseColorMapUVLocal;

float _Metallic;
float _MetallicUseMap;
float _MetallicMapUV;
float _MetallicMapUVLocal;
float4 _MetallicMap_ST;
float4 _MetallicMap_TexelSize;
float4 _MetallicMap_MipInfo;
float4 _MetallicMapChannelMask;
float4 _MetallicMapRange;

float _DielectricIor;

float _SmoothnessA;
float _SmoothnessAUseMap;
float _SmoothnessAMapUV;
float _SmoothnessAMapUVLocal;
float4 _SmoothnessAMap_ST;
float4 _SmoothnessAMap_TexelSize;
float4 _SmoothnessAMap_MipInfo;
float4 _SmoothnessAMapChannelMask;
float4 _SmoothnessAMapRange;

float4 _DebugEnvLobeMask;
float4 _DebugLobeMask;
float4 _DebugAniso;

float _NormalScale;
float _NormalMapUV;
float _NormalMapUVLocal;
float _NormalMapObjSpace;
float4 _NormalMap_ST;
float4 _NormalMap_TexelSize;
float4 _NormalMap_MipInfo;

float _AmbientOcclusion;
float _AmbientOcclusionUseMap;
float _AmbientOcclusionMapUV;
float _AmbientOcclusionMapUVLocal;
float4 _AmbientOcclusionMap_ST;
float4 _AmbientOcclusionMap_TexelSize;
float4 _AmbientOcclusionMap_MipInfo;
float4 _AmbientOcclusionMapChannelMask;
float4 _AmbientOcclusionMapRange;

float _SmoothnessB;
float _SmoothnessBUseMap;
float _SmoothnessBMapUV;
float _SmoothnessBMapUVLocal;
float4 _SmoothnessBMap_ST;
float4 _SmoothnessBMap_TexelSize;
float4 _SmoothnessBMap_MipInfo;
float4 _SmoothnessBMapChannelMask;
float4 _SmoothnessBMapRange;
float _LobeMix;

float _Anisotropy;
float _AnisotropyUseMap;
float _AnisotropyMapUV;
float _AnisotropyMapUVLocal;
float4 _AnisotropyMap_ST;
float4 _AnisotropyMap_TexelSize;
float4 _AnisotropyMap_MipInfo;
float4 _AnisotropyMapChannelMask;
float4 _AnisotropyMapRange;

float _CoatSmoothness;
float _CoatSmoothnessUseMap;
float _CoatSmoothnessMapUV;
float _CoatSmoothnessMapUVLocal;
float4 _CoatSmoothnessMap_ST;
float4 _CoatSmoothnessMap_TexelSize;
float4 _CoatSmoothnessMap_MipInfo;
float4 _CoatSmoothnessMapChannelMask;
float4 _CoatSmoothnessMapRange;
float _CoatIor;
float _CoatThickness;
float3 _CoatExtinction;

float _CoatNormalScale;
float _CoatNormalMapUV;
float _CoatNormalMapUVLocal;
float _CoatNormalMapObjSpace;
float4 _CoatNormalMap_ST;
float4 _CoatNormalMap_TexelSize;
float4 _CoatNormalMap_MipInfo;

float _IridescenceThickness;
float _IridescenceThicknessUseMap;
float _IridescenceThicknessMapUV;
float _IridescenceThicknessMapUVLocal;
float4 _IridescenceThicknessMap_ST;
float4 _IridescenceThicknessMap_TexelSize;
float4 _IridescenceThicknessMap_MipInfo;
float4 _IridescenceThicknessMapChannelMask;
float4 _IridescenceThicknessMapRange;
float _IridescenceIor;

float _IridescenceMask;
float _IridescenceMaskUseMap;
float _IridescenceMaskMapUV;
float _IridescenceMaskMapUVLocal;
float4 _IridescenceMaskMap_ST;
float4 _IridescenceMaskMap_TexelSize;
float4 _IridescenceMaskMap_MipInfo;
float4 _IridescenceMaskMapChannelMask;
float4 _IridescenceMaskMapRange;

int _DiffusionProfile;
float _SubsurfaceMask;
float _SubsurfaceMaskUseMap;
float _SubsurfaceMaskMapUV;
float _SubsurfaceMaskMapUVLocal;
float4 _SubsurfaceMaskMap_ST;
float4 _SubsurfaceMaskMap_TexelSize;
float4 _SubsurfaceMaskMap_MipInfo;
float4 _SubsurfaceMaskMapChannelMask;
float4 _SubsurfaceMaskMapRange;

float _Thickness;
float _ThicknessUseMap;
float _ThicknessMapUV;
float _ThicknessMapUVLocal;
float4 _ThicknessMap_ST;
float4 _ThicknessMap_TexelSize;
float4 _ThicknessMap_MipInfo;
float4 _ThicknessMapChannelMask;
float4 _ThicknessMapRange;

// Details
float _DetailMaskMapUV;
float _DetailMaskMapUVLocal;
float4 _DetailMaskMap_ST;
float4 _DetailMaskMap_TexelSize;
float4 _DetailMaskMap_MipInfo;
float4 _DetailMaskMapChannelMask;

float _DetailSmoothnessMapUV;
float _DetailSmoothnessMapUVLocal;
float4 _DetailSmoothnessMap_ST;
float4 _DetailSmoothnessMap_TexelSize;
float4 _DetailSmoothnessMap_MipInfo;
float4 _DetailSmoothnessMapChannelMask;
float4 _DetailSmoothnessMapRange;
float _DetailSmoothnessScale;

float _DetailNormalScale;
float _DetailNormalMapUV;
float _DetailNormalMapUVLocal;
float4 _DetailNormalMap_ST;
float4 _DetailNormalMap_TexelSize;
float4 _DetailNormalMap_MipInfo;


float3 _EmissiveColor;
float4 _EmissiveColorMap_ST;
float4 _EmissiveColorMap_TexelSize;
float4 _EmissiveColorMap_MipInfo;
float _EmissiveColorMapUV;
float _EmissiveColorMapUVLocal;
float _AlbedoAffectEmissive;

float _GeometricNormalFilteringEnabled;
float _TextureNormalFilteringEnabled;
float _SpecularAntiAliasingScreenSpaceVariance;
float _SpecularAntiAliasingThreshold;

float _AlphaCutoff;
float4 _DoubleSidedConstants;

float _DistortionScale;
float _DistortionVectorScale;
float _DistortionVectorBias;
float _DistortionBlurScale;
float _DistortionBlurRemapMin;
float _DistortionBlurRemapMax;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the behavior for GI
float3 _EmissionColor;

// Following two variables are feeded by the C++ Editor for Scene selection
int _ObjectId;
int _PassValue;

CBUFFER_END
