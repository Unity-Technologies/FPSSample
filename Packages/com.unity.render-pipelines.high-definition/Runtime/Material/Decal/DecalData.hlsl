//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
void GetSurfaceData(float2 texCoordDS, float4x4 normalToWorld, out DecalSurfaceData surfaceData)
#elif (SHADERPASS == SHADERPASS_DBUFFER_MESH)
void GetSurfaceData(FragInputs input, out DecalSurfaceData surfaceData)
#endif
{
    surfaceData.baseColor = _BaseColor;
    surfaceData.normalWS = float4(0,0,0,0);
    surfaceData.mask = float4(0,0,0,0);
	surfaceData.MAOSBlend = float2(0, 0);
    surfaceData.HTileMask = 0;
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
    float albedoMapBlend = clamp(normalToWorld[0][3], 0.0f, 1.0f);
    float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
    float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
	float2 texCoords = texCoordDS * scale + offset;
#elif (SHADERPASS == SHADERPASS_DBUFFER_MESH)
	float albedoMapBlend = _DecalBlend;
	float2 texCoords = input.texCoord0.xy;
#endif

#if _COLORMAP
    surfaceData.baseColor *= SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, texCoords);    
#endif
	surfaceData.baseColor.w *= albedoMapBlend;
	albedoMapBlend = surfaceData.baseColor.w;   
// outside _COLORMAP because we still have base color
#if _ALBEDOCONTRIBUTION
	surfaceData.HTileMask |= DBUFFERHTILEBIT_DIFFUSE;
#else
	surfaceData.baseColor.w = 0;	// dont blend any albedo
#endif

    // Default to _DecalBlend, if we use _NormalBlendSrc as maskmap and there is no maskmap, it mean we have 1
	float maskMapBlend = _DecalBlend;

#if _MASKMAP
    surfaceData.mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, texCoords);
	maskMapBlend *= surfaceData.mask.z;	// store before overwriting with smoothness
    surfaceData.mask.z = surfaceData.mask.w;
	surfaceData.HTileMask |= DBUFFERHTILEBIT_MASK;
	surfaceData.mask.w = _MaskBlendSrc ? maskMapBlend : albedoMapBlend;
#endif

	// needs to be after mask, because blend source could be in the mask map blue
#if _NORMALMAP
	float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoords));
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
	float3 normalWS = mul((float3x3)normalToWorld, normalTS);
#elif (SHADERPASS == SHADERPASS_DBUFFER_MESH)	
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    float3 normalWS = normalize(TransformTangentToWorld(normalTS, input.worldToTangent));
#endif
	surfaceData.normalWS.xyz = normalWS * 0.5f + 0.5f;
	surfaceData.HTileMask |= DBUFFERHTILEBIT_NORMAL;
	surfaceData.normalWS.w = _NormalBlendSrc ? maskMapBlend : albedoMapBlend;
#endif
	surfaceData.MAOSBlend.xy = float2(surfaceData.mask.w, surfaceData.mask.w);
}
